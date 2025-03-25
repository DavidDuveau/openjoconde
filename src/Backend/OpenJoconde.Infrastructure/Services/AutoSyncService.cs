using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder; // Ajouté pour résoudre le problème avec GetValue
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly IJocondeDataService _jocondeDataService;
        private readonly IDataImportService _dataImportService;
        private readonly IJocondeXmlParser _xmlParser;
        private readonly TimeSpan _syncInterval;
        private readonly string _dataSourceUrl;
        private readonly string _tempDirectory;

        public AutoSyncService(
            ILogger<AutoSyncService> logger,
            IConfiguration configuration,
            IJocondeDataService jocondeDataService,
            IDataImportService dataImportService,
            IJocondeXmlParser xmlParser)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _jocondeDataService = jocondeDataService ?? throw new ArgumentNullException(nameof(jocondeDataService));
            _dataImportService = dataImportService ?? throw new ArgumentNullException(nameof(dataImportService));
            _xmlParser = xmlParser ?? throw new ArgumentNullException(nameof(xmlParser));

            // Configuration
            _syncInterval = TimeSpan.FromHours(
                configuration.GetValue<double>("JocondeSync:IntervalHours", 24));
            _dataSourceUrl = configuration.GetValue<string>(
                "JocondeSync:DataSourceUrl", 
                "https://data.culture.gouv.fr/api/datasets/1.0/joconde-catalogue-collectif-des-collections-des-musees-de-france/attachments/base_joconde_extrait_xml_zip");
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

                    // Générer un timestamp pour les fichiers temporaires
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var tempFilePath = Path.Combine(_tempDirectory, $"joconde_{timestamp}.xml");

                    // Télécharger les données
                    await _jocondeDataService.DownloadJocondeDataAsync(_dataSourceUrl, tempFilePath, stoppingToken);
                    _logger.LogInformation("Fichier téléchargé avec succès: {FilePath}", tempFilePath);

                    // Analyser les données
                    var xmlParseResult = await _xmlParser.ParseAsync(tempFilePath, progressCallback: null, stoppingToken);
                    _logger.LogInformation("Fichier XML analysé: {ArtworksCount} œuvres, {ArtistsCount} artistes extraits",
                        xmlParseResult.Artworks.Count, xmlParseResult.Artists.Count);

                    // Importer les données
                    var importResult = await _dataImportService.ImportDataAsync(xmlParseResult, progressCallback: null, stoppingToken);
                    _logger.LogInformation("Importation terminée: {ArtworksCount} œuvres, {ArtistsCount} artistes importés",
                        importResult.ArtworksImported, importResult.ArtistsImported);

                    // Nettoyer les fichiers temporaires
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                        _logger.LogInformation("Fichier temporaire supprimé: {FilePath}", tempFilePath);
                    }

                    // Enregistrer le log de synchronisation
                    var syncLog = new DataSyncLog
                    {
                        Id = Guid.NewGuid(),
                        SyncType = "Full", // Définir le type de synchronisation
                        StartTime = DateTime.UtcNow.Subtract(importResult.Duration),
                        EndTime = DateTime.UtcNow,
                        ArtworksProcessed = importResult.ArtworksImported,
                        ArtistsProcessed = importResult.ArtistsImported,
                        Success = true,
                        Status = "Completed" // Définir le statut
                    };

                    // TODO: Sauvegarder le log dans la base de données

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
