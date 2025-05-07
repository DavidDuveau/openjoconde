using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using OpenJoconde.Core.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Service pour le traitement par lots des données Joconde
    /// Permet le téléchargement et l'importation de données volumineuses
    /// en les divisant en lots de taille gérable
    /// </summary>
    public class BatchProcessingService
    {
        private readonly ILogger<BatchProcessingService> _logger;
        private readonly IJocondeDataService _jocondeDataService;
        private readonly IDataImportService _dataImportService;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly int _batchSize;

        public BatchProcessingService(
            ILogger<BatchProcessingService> logger,
            IJocondeDataService jocondeDataService,
            IDataImportService dataImportService,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jocondeDataService = jocondeDataService ?? throw new ArgumentNullException(nameof(jocondeDataService));
            _dataImportService = dataImportService ?? throw new ArgumentNullException(nameof(dataImportService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            
            // Récupérer la taille des lots depuis la configuration
            _batchSize = _configuration.GetValue<int>("JocondeSync:BatchSize", 5000);
        }

        /// <summary>
        /// Récupère le nombre total d'enregistrements disponibles dans le jeu de données
        /// </summary>
        private async Task<int> GetTotalRecordsCountAsync(string baseUrl, CancellationToken cancellationToken)
        {
            try
            {
                // URL de la requête d'information (metadata) sur le jeu de données avec format v2.1 mis à jour
                string metadataUrl = "https://data.culture.gouv.fr/api/explore/v2.1/catalog/datasets/base-joconde-extrait?lang=fr";
                
                var response = await _httpClient.GetAsync(metadataUrl, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                using var document = JsonDocument.Parse(content);
                
                // Essayer de récupérer le nombre total d'enregistrements
                if (document.RootElement.TryGetProperty("dataset", out var datasetElement) &&
                    datasetElement.TryGetProperty("dataset_size", out var sizeElement))
                {
                    return sizeElement.GetInt32();
                }
                
                // Si on ne peut pas obtenir le nombre exact, utiliser une valeur par défaut
                return 50000; // Estimation par défaut
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible de déterminer le nombre total d'enregistrements. Utilisation d'une estimation par défaut.");
                return 50000; // Estimation par défaut en cas d'erreur
            }
        }

        /// <summary>
        /// Télécharge et importe les données Joconde par lots
        /// </summary>
        public async Task<ImportReport> ProcessDataInBatchesAsync(
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Démarrage du traitement par lots des données Joconde (taille des lots: {BatchSize})", _batchSize);
            
            var report = new ImportReport
            {
                ImportDate = DateTime.UtcNow,
                Success = true
            };
            
            var tempDirectory = _configuration.GetValue<string>("JocondeSync:TempDirectory", Path.Combine(Path.GetTempPath(), "OpenJoconde"));
            var baseUrl = _configuration.GetValue<string>("JocondeSync:DataSourceUrl", "https://data.culture.gouv.fr/api/explore/v2.1/catalog/datasets/base-joconde-extrait/exports/json");
            
            try
            {
                // Au lieu d'utiliser des lots par offset, utiliser une seule requête pour tout télécharger
                // L'API semble avoir changé et ne supporte plus bien les requêtes avec offset
                string fullDataUrl = $"{baseUrl}?lang=fr&timezone=Europe%2FBerlin";
                string dataFilePath = Path.Combine(tempDirectory, $"joconde_full_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                
                _logger.LogInformation("Téléchargement complet des données Joconde depuis {Url}", fullDataUrl);
                
                try
                {
                    // Télécharger le jeu de données complet
                    await _jocondeDataService.DownloadJocondeDataAsync(fullDataUrl, dataFilePath, cancellationToken);
                    
                    // Analyser et importer le lot
                    var importReport = await _jocondeDataService.ImportFromJsonFileAsync(dataFilePath, cancellationToken);
                    
                    // Mise à jour du rapport global
                    report.ImportedArtworks = importReport.ImportedArtworks;
                    report.ImportedArtists = importReport.ImportedArtists;
                    report.ImportedMuseums = importReport.ImportedMuseums;
                    report.ImportedDomains = importReport.ImportedDomains;
                    report.ImportedTechniques = importReport.ImportedTechniques;
                    report.ImportedPeriods = importReport.ImportedPeriods;
                    
                    _logger.LogInformation("Importation réussie. {Artworks} œuvres importées.", importReport.ImportedArtworks);
                    
                    // Nettoyage du fichier temporaire
                    if (File.Exists(dataFilePath))
                    {
                        File.Delete(dataFilePath);
                        _logger.LogDebug("Fichier temporaire supprimé: {FilePath}", dataFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors du traitement des données: {Message}", ex.Message);
                    report.Errors++;
                }
                
                // Estimer le nombre d'enregistrements (puisque nous ne faisons plus d'appel pour le compter)
                int estimatedRecords = 50000; // Valeur par défaut estimée
                
                _logger.LogInformation("Traitement des données terminé. Total traité: {Total} œuvres importées.",
                    report.ImportedArtworks);
                
                report.TotalArtworks = report.ImportedArtworks; // Utiliser le nombre réel importé
                report.Success = report.Errors == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement par lots: {Message}", ex.Message);
                report.Success = false;
                report.ErrorMessage = ex.Message;
                report.Errors++;
            }
            
            report.Duration = DateTime.UtcNow - report.ImportDate;
            return report;
        }
    }
}
