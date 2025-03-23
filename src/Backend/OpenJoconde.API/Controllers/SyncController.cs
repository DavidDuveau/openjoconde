using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly IDataImportService _importService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SyncController> _logger;
        private readonly OpenJocondeDbContext _dbContext;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public SyncController(
            IDataImportService importService,
            IConfiguration configuration,
            ILogger<SyncController> logger,
            OpenJocondeDbContext dbContext,
            IServiceScopeFactory serviceScopeFactory)
        {
            _importService = importService ?? throw new ArgumentNullException(nameof(importService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        /// <summary>
        /// Obtient le statut de la dernière synchronisation
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetSyncStatus()
        {
            var lastSync = await _dbContext.DataSyncLogs
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();

            if (lastSync == null)
            {
                return Ok(new
                {
                    Status = "Never Synced",
                    Message = "Aucune synchronisation n'a encore été effectuée",
                    LastSync = (DateTime?)null
                });
            }

            return Ok(new
            {
                Status = lastSync.Status,
                Message = lastSync.Status == "Failed" ? lastSync.ErrorMessage : "Dernière synchronisation réussie",
                LastSync = lastSync.CompletedAt ?? lastSync.StartedAt,
                ItemsProcessed = lastSync.ItemsProcessed
            });
        }

        /// <summary>
        /// Déclenche une synchronisation manuelle
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartSync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Démarrage d'une synchronisation manuelle");

                // Vérifier si une synchronisation est déjà en cours
                var runningSync = await _dbContext.DataSyncLogs
                    .Where(s => s.Status == "Running")
                    .FirstOrDefaultAsync(cancellationToken);

                if (runningSync != null)
                {
                    return Conflict(new
                    {
                        Success = false,
                        Message = "Une synchronisation est déjà en cours",
                        StartedAt = runningSync.StartedAt
                    });
                }

                // Créer un enregistrement pour la nouvelle synchronisation
                var syncLog = new DataSyncLog
                {
                    SyncType = "Manual",
                    StartedAt = DateTime.UtcNow,
                    Status = "Running",
                    ItemsProcessed = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.DataSyncLogs.Add(syncLog);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Lancer la synchronisation en arrière-plan
                _ = Task.Run(async () =>
                {
                    // Créer un nouveau scope pour avoir accès aux services scoped
                    using var scope = _serviceScopeFactory.CreateScope();
                    var importService = scope.ServiceProvider.GetRequiredService<IDataImportService>();
                    var dbContext = scope.ServiceProvider.GetRequiredService<OpenJocondeDbContext>();
                    
                    try
                    {
                        // Configuration
                        var dataSourceUrl = _configuration["JocondeData:SourceUrl"];
                        var tempDirectory = _configuration["JocondeData:TempDirectory"] ?? Path.Combine(Path.GetTempPath(), "openjoconde");

                        // Créer le répertoire temporaire s'il n'existe pas
                        if (!Directory.Exists(tempDirectory))
                        {
                            Directory.CreateDirectory(tempDirectory);
                        }

                        // Générer un nom de fichier temporaire unique
                        var tempFilePath = Path.Combine(tempDirectory, $"joconde_manual_{DateTime.Now:yyyyMMdd_HHmmss}.xml");

                        // Télécharger le fichier
                        await importService.DownloadJocondeDataAsync(dataSourceUrl, tempFilePath);

                        // Importer les données
                        var importReport = await importService.ImportFromXmlFileAsync(
                            tempFilePath,
                            (stage, current, total) =>
                            {
                                if (current % 1000 == 0 || current == total)
                                {
                                    _logger.LogInformation("{Stage}: {Current}/{Total} ({Percent}%)",
                                        stage, current, total, Math.Round((double)current / total * 100));
                                }
                            });

                        // Mettre à jour l'enregistrement de synchronisation
                        var syncToUpdate = await dbContext.DataSyncLogs.FindAsync(syncLog.Id);
                        if (syncToUpdate != null)
                        {
                            syncToUpdate.CompletedAt = DateTime.UtcNow;
                            syncToUpdate.Status = importReport.Success ? "Completed" : "Failed";
                            syncToUpdate.ItemsProcessed = importReport.ImportedArtworks;
                            syncToUpdate.ErrorMessage = importReport.ErrorMessage;
                            await dbContext.SaveChangesAsync();
                        }

                        // Nettoyer les fichiers temporaires
                        if (File.Exists(tempFilePath))
                        {
                            File.Delete(tempFilePath);
                        }

                        _logger.LogInformation("Synchronisation manuelle terminée avec succès");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors de la synchronisation manuelle: {Message}", ex.Message);
                        
                        // Mettre à jour l'enregistrement de synchronisation avec l'erreur
                        var syncToUpdate = await dbContext.DataSyncLogs.FindAsync(syncLog.Id);
                        if (syncToUpdate != null)
                        {
                            syncToUpdate.CompletedAt = DateTime.UtcNow;
                            syncToUpdate.Status = "Failed";
                            syncToUpdate.ErrorMessage = ex.Message;
                            await dbContext.SaveChangesAsync();
                        }
                    }
                });

                return Ok(new
                {
                    Success = true,
                    Message = "Synchronisation démarrée avec succès",
                    SyncId = syncLog.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du démarrage de la synchronisation: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Erreur lors du démarrage de la synchronisation: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Annule une synchronisation en cours
        /// </summary>
        [HttpPost("cancel/{id}")]
        public async Task<IActionResult> CancelSync(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var syncLog = await _dbContext.DataSyncLogs.FindAsync(new object[] { id }, cancellationToken);
                
                if (syncLog == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Synchronisation non trouvée"
                    });
                }

                if (syncLog.Status != "Running")
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Impossible d'annuler une synchronisation qui n'est pas en cours"
                    });
                }

                // Mettre à jour le statut
                syncLog.Status = "Canceled";
                syncLog.CompletedAt = DateTime.UtcNow;
                syncLog.ErrorMessage = "Synchronisation annulée par l'utilisateur";
                
                await _dbContext.SaveChangesAsync(cancellationToken);
                
                return Ok(new
                {
                    Success = true,
                    Message = "Synchronisation annulée avec succès"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'annulation de la synchronisation: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Erreur lors de l'annulation de la synchronisation: {ex.Message}"
                });
            }
        }
    }
}
