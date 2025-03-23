using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Infrastructure.Data;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Service de synchronisation automatique des données Joconde
    /// Vérifie si le fichier XML source a changé et déclenche l'importation si nécessaire
    /// </summary>
    public class AutoSyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutoSyncService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        
        private string _dataSourceUrl;
        private string _tempDirectory;
        private string _lastEtagFilePath;
        private bool _initialSyncRequired;

        public AutoSyncService(
            IServiceProvider serviceProvider,
            ILogger<AutoSyncService> logger,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            
            // Charger la configuration
            _dataSourceUrl = _configuration["JocondeData:SourceUrl"];
            _tempDirectory = _configuration["JocondeData:TempDirectory"] ?? Path.Combine(Path.GetTempPath(), "openjoconde");
            _lastEtagFilePath = Path.Combine(_tempDirectory, "last_etag.txt");
            _initialSyncRequired = Convert.ToBoolean(_configuration["JocondeData:InitialSyncRequired"] ?? "false");
            
            // Créer le répertoire temporaire s'il n'existe pas
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service de synchronisation automatique démarré");

            try
            {
                // Synchronisation initiale si requise ou si jamais synchronisé
                if (_initialSyncRequired || !File.Exists(_lastEtagFilePath))
                {
                    _logger.LogInformation("Synchronisation initiale requise");
                    await SynchronizeDataAsync(stoppingToken);
                }
                else
                {
                    // Vérifier si une mise à jour est disponible
                    var isUpdateAvailable = await CheckForUpdateAsync(stoppingToken);
                    if (isUpdateAvailable)
                    {
                        _logger.LogInformation("Mise à jour des données Joconde disponible");
                        await SynchronizeDataAsync(stoppingToken);
                    }
                    else
                    {
                        _logger.LogInformation("Aucune mise à jour des données Joconde disponible");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification initiale des données Joconde: {Message}", ex.Message);
            }
        }
        
        /// <summary>
        /// Vérifie si une mise à jour est disponible en comparant les ETags HTTP
        /// </summary>
        private async Task<bool> CheckForUpdateAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Lire l'ETag précédent
                string lastEtag = null;
                if (File.Exists(_lastEtagFilePath))
                {
                    lastEtag = await File.ReadAllTextAsync(_lastEtagFilePath, cancellationToken);
                }
                
                // Envoyer une requête HEAD pour récupérer les en-têtes sans télécharger le fichier
                var request = new HttpRequestMessage(HttpMethod.Head, _dataSourceUrl);
                if (!string.IsNullOrEmpty(lastEtag))
                {
                    request.Headers.TryAddWithoutValidation("If-None-Match", lastEtag);
                }
                
                var response = await _httpClient.SendAsync(request, cancellationToken);
                
                // Si le statut est NotModified (304), le fichier n'a pas changé
                if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
                {
                    return false;
                }
                
                // Si nous avons une réponse 200 OK, vérifier l'ETag
                if (response.IsSuccessStatusCode)
                {
                    var currentEtag = response.Headers.ETag?.Tag;
                    
                    // Si nous avons un ETag et qu'il est différent du précédent, il y a une mise à jour
                    if (!string.IsNullOrEmpty(currentEtag) && currentEtag != lastEtag)
                    {
                        await File.WriteAllTextAsync(_lastEtagFilePath, currentEtag, cancellationToken);
                        return true;
                    }
                    
                    // Si nous n'avons pas d'ETag, vérifier la date de dernière modification
                    if (string.IsNullOrEmpty(currentEtag) && response.Content.Headers.LastModified.HasValue)
                    {
                        var lastModified = response.Content.Headers.LastModified.Value.UtcDateTime.ToString("o");
                        
                        if (lastEtag != lastModified)
                        {
                            await File.WriteAllTextAsync(_lastEtagFilePath, lastModified, cancellationToken);
                            return true;
                        }
                    }
                }
                
                // Par défaut, si nous ne pouvons pas déterminer clairement, on considère qu'il y a une mise à jour
                // pour s'assurer que les données sont toujours à jour
                return !File.Exists(_lastEtagFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification des mises à jour: {Message}", ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Télécharge et importe les données Joconde
        /// </summary>
        private async Task SynchronizeDataAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Démarrage de la synchronisation des données Joconde");
            
            try
            {
                // Créer un scope pour les services scoped
                using var scope = _serviceProvider.CreateScope();
                var importService = scope.ServiceProvider.GetRequiredService<IDataImportService>();
                
                // Générer un nom de fichier temporaire unique
                var tempFilePath = Path.Combine(_tempDirectory, $"joconde_{DateTime.Now:yyyyMMdd_HHmmss}.xml");
                
                // Télécharger le fichier
                _logger.LogInformation("Téléchargement du fichier XML depuis {Url}", _dataSourceUrl);
                var downloadTask = await importService.DownloadJocondeDataAsync(_dataSourceUrl, tempFilePath, cancellationToken);
                
                // Vérifier le fichier téléchargé
                if (!File.Exists(tempFilePath))
                {
                    throw new FileNotFoundException("Le fichier téléchargé est introuvable", tempFilePath);
                }
                
                // Calculer et stocker le hash du fichier pour les vérifications futures
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(tempFilePath))
                    {
                        var hashBytes = await md5.ComputeHashAsync(stream, cancellationToken);
                        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                        await File.WriteAllTextAsync(_lastEtagFilePath, hashString, cancellationToken);
                    }
                }
                
                // Créer un enregistrement dans le journal de synchronisation
                var dbContext = scope.ServiceProvider.GetRequiredService<OpenJocondeDbContext>();
                var syncLog = new JocondeMetadata
                {
                    SourceVersion = $"Synchronisation automatique {DateTime.Now:yyyy-MM-dd}",
                    LastUpdateDate = DateTime.UtcNow,
                    SchemaVersion = "1.1",
                    Notes = $"Fichier source: {_dataSourceUrl}"
                };
                dbContext.Set<JocondeMetadata>().Add(syncLog);
                await dbContext.SaveChangesAsync(cancellationToken);
                
                // Importer les données
                _logger.LogInformation("Importation des données depuis {FilePath}", tempFilePath);
                var importReport = await importService.ImportFromXmlFileAsync(
                    tempFilePath,
                    (stage, current, total) => {
                        if (current % 1000 == 0 || current == total)
                        {
                            _logger.LogInformation("{Stage}: {Current}/{Total} ({Percent}%)", 
                                stage, current, total, Math.Round((double)current / total * 100));
                        }
                    },
                    cancellationToken);
                
                // Enregistrer le résultat de l'importation
                var dataSyncLog = new DataSyncLog
                {
                    SyncType = "Automatic",
                    StartedAt = DateTime.UtcNow.AddMinutes(-importReport.Duration.TotalMinutes),
                    CompletedAt = DateTime.UtcNow,
                    Status = importReport.Success ? "Completed" : "Failed",
                    ItemsProcessed = importReport.ImportedArtworks,
                    ErrorMessage = importReport.ErrorMessage,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Set<DataSyncLog>().Add(dataSyncLog);
                await dbContext.SaveChangesAsync(cancellationToken);
                
                // Nettoyer les fichiers temporaires
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
                
                _logger.LogInformation("Synchronisation des données Joconde terminée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la synchronisation des données Joconde: {Message}", ex.Message);
                throw;
            }
        }
    }
}
