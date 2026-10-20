using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Service de synchronisation automatique des données Joconde
    /// Exécute des synchronisations périodiques en arrière-plan
    /// </summary>
    public class AutoSyncService : BackgroundService
    {
        private readonly ILogger<AutoSyncService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly TimeSpan _syncInterval;
        private readonly string _dataSourceUrl;
        private readonly string _tempDirectory;

        public AutoSyncService(
            ILogger<AutoSyncService> logger,
            IConfiguration configuration,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

            // Configuration
            _syncInterval = TimeSpan.FromHours(
                configuration.GetValue<double>("JocondeSync:IntervalHours", 24));
            _dataSourceUrl = configuration.GetValue<string>(
                "JocondeSync:DataSourceUrl", 
                "https://ministere-culture.s3.sbg.io.cloud.ovh.net/POP/joconde.csv");
            _tempDirectory = configuration.GetValue<string>(
                "JocondeSync:TempDirectory", 
                Path.Combine(Path.GetTempPath(), "OpenJoconde"));

            // Créer le répertoire temporaire s'il n'existe pas
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service de synchronisation automatique démarré. Intervalle: {SyncInterval}", _syncInterval);

            // Première synchronisation après un délai pour laisser l'application démarrer
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Démarrage de la synchronisation automatique des données Joconde");

                    // Utiliser un scope pour accéder aux services scoped
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        ImportReport importResult;

                        // Générer un timestamp pour les fichiers temporaires
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var tempFilePath = Path.Combine(_tempDirectory, $"joconde_{timestamp}{JocondeSourceFile.ExtensionFor(_dataSourceUrl)}");

                        var jocondeDataService = scope.ServiceProvider.GetRequiredService<IJocondeDataService>();
                        var dataImportService = scope.ServiceProvider.GetRequiredService<IDataImportService>();

                        try
                        {
                            await jocondeDataService.DownloadJocondeDataAsync(_dataSourceUrl, tempFilePath, stoppingToken);
                            _logger.LogInformation("Fichier téléchargé avec succès: {FilePath}", tempFilePath);

                            // Importer les données (le format est déduit de l'extension du fichier)
                            importResult = await dataImportService.ImportFromFileAsync(tempFilePath, null, stoppingToken);
                            _logger.LogInformation("Importation terminée: {ArtworksCount} œuvres importées en {Duration}",
                                importResult.ImportedArtworks, importResult.Duration);
                        }
                        finally
                        {
                            if (File.Exists(tempFilePath))
                            {
                                File.Delete(tempFilePath);
                                _logger.LogInformation("Fichier temporaire supprimé: {FilePath}", tempFilePath);
                            }
                        }

                        // Enregistrer le log de synchronisation
                        var syncLog = new DataSyncLog
                        {
                            Id = Guid.NewGuid(),
                            SyncType = "FullSync",
                            StartTime = DateTime.UtcNow.Subtract(importResult.Duration),
                            EndTime = DateTime.UtcNow,
                            ArtworksProcessed = importResult.ImportedArtworks,
                            ArtistsProcessed = importResult.ImportedArtists,
                            Success = importResult.Success,
                            Status = importResult.Success ? "Completed" : "CompletedWithErrors",
                            ErrorMessage = importResult.ErrorMessage
                        };

                        // TODO: Sauvegarder le log dans la base de données
                    }

                    _logger.LogInformation("Synchronisation automatique terminée avec succès");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la synchronisation automatique: {Message}", ex.Message);
                    
                    // Enregistrer le log d'erreur
                    var errorLog = new DataSyncLog
                    {
                        Id = Guid.NewGuid(),
                        SyncType = "Full", // Définir le type de synchronisation
                        StartTime = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5)), // Estimation
                        EndTime = DateTime.UtcNow,
                        Success = false,
                        ErrorMessage = ex.Message,
                        Status = "Failed" // Définir le statut
                    };

                    // TODO: Sauvegarder le log d'erreur dans la base de données
                }

                // Attendre jusqu'à la prochaine synchronisation
                _logger.LogInformation("Prochaine synchronisation prévue dans {SyncInterval}", _syncInterval);
                await Task.Delay(_syncInterval, stoppingToken);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service de synchronisation automatique arrêté");
            return base.StopAsync(cancellationToken);
        }
    }
}
