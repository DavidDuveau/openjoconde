using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenJoconde.API.Security;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly ISyncCoordinator _coordinator;
        private readonly OpenJocondeDbContext _dbContext;
        private readonly ILogger<SyncController> _logger;

        public SyncController(
            ISyncCoordinator coordinator,
            OpenJocondeDbContext dbContext,
            ILogger<SyncController> logger)
        {
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtient le statut de la dernière synchronisation (sans message d'erreur : voir /history)
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetSyncStatus(CancellationToken cancellationToken)
        {
            var lastSync = await _dbContext.DataSyncLogs
                .AsNoTracking()
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);

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
                SyncId = lastSync.Id,
                lastSync.Status,
                lastSync.TriggeredBy,
                lastSync.StartedAt,
                LastSync = lastSync.CompletedAt ?? lastSync.StartedAt,
                lastSync.ItemsProcessed,
                lastSync.ErrorCount
            });
        }

        /// <summary>
        /// Historique des dernières synchronisations, messages d'erreur compris
        /// </summary>
        [RequireApiKey]
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int count = 20, CancellationToken cancellationToken = default)
        {
            count = Math.Clamp(count, 1, 100);
            var logs = await _dbContext.DataSyncLogs
                .AsNoTracking()
                .OrderByDescending(s => s.StartedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            return Ok(logs.Select(s => new
            {
                s.Id,
                s.Status,
                s.SyncType,
                s.TriggeredBy,
                s.StartedAt,
                s.CompletedAt,
                s.ArtworksProcessed,
                s.ArtistsProcessed,
                s.ErrorCount,
                s.ErrorMessage
            }));
        }

        /// <summary>
        /// Déclenche une synchronisation manuelle
        /// </summary>
        [RequireApiKey]
        [EnableRateLimiting("ecriture")]
        [HttpPost("start")]
        public async Task<IActionResult> StartSync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Demande de synchronisation manuelle");
                var result = await _coordinator.TryStartAsync("Manual", cancellationToken);

                if (!result.Started)
                {
                    return Conflict(new
                    {
                        Success = false,
                        Message = "Une synchronisation est déjà en cours",
                        StartedAt = result.RunningSince
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Synchronisation démarrée avec succès",
                    SyncId = result.SyncId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du démarrage de la synchronisation");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Erreur lors du démarrage de la synchronisation"
                });
            }
        }

        /// <summary>
        /// Annule une synchronisation en cours
        /// </summary>
        [RequireApiKey]
        [EnableRateLimiting("ecriture")]
        [HttpPost("cancel/{id}")]
        public async Task<IActionResult> CancelSync(Guid id, CancellationToken cancellationToken)
        {
            if (_coordinator.RequestCancel(id))
            {
                return Accepted(new
                {
                    Success = true,
                    Message = "Annulation demandée : la synchronisation s'arrête à la fin de l'étape en cours"
                });
            }

            var status = await _dbContext.DataSyncLogs
                .Where(s => s.Id == id)
                .Select(s => s.Status)
                .FirstOrDefaultAsync(cancellationToken);

            if (status == null)
            {
                return NotFound(new { Success = false, Message = "Synchronisation non trouvée" });
            }

            return Conflict(new
            {
                Success = false,
                Message = status == "Running"
                    ? "Cette synchronisation ne tourne pas dans ce serveur : elle sera marquée interrompue au prochain démarrage"
                    : "Impossible d'annuler une synchronisation qui n'est pas en cours"
            });
        }
    }
}
