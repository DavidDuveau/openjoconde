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
                // URL de la requête d'information (metadata) sur le jeu de données
                string metadataUrl = "https://data.culture.gouv.fr/api/explore/v2.1/catalog/datasets/base-joconde-extrait";
                
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
                // Obtenir le nombre total d'enregistrements disponibles
                int totalRecords = await GetTotalRecordsCountAsync(baseUrl, cancellationToken);
                _logger.LogInformation("Nombre total d'enregistrements à traiter: {Count}", totalRecords);
                
                // Calcul du nombre de lots
                int totalBatches = (int)Math.Ceiling((double)totalRecords / _batchSize);
                _logger.LogInformation("Nombre total de lots à traiter: {BatchCount}", totalBatches);
                
                int totalProcessed = 0;
                
                // Traiter chaque lot
                for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Traitement par lots annulé par l'utilisateur");
                        break;
                    }
                    
                    int offset = batchIndex * _batchSize;
                    int currentBatchSize = Math.Min(_batchSize, totalRecords - offset);
                    
                    _logger.LogInformation("Traitement du lot {Current}/{Total} (offset: {Offset}, taille: {Size})",
                        batchIndex + 1, totalBatches, offset, currentBatchSize);
                    
                    // Construire l'URL spécifique pour ce lot
                    string batchUrl = $"{baseUrl}?limit={currentBatchSize}&offset={offset}&timezone=UTC&use_labels=false&epsg=4326";
                    string batchFilePath = Path.Combine(tempDirectory, $"joconde_batch_{batchIndex + 1}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                    
                    try
                    {
                        // Télécharger le lot
                        await _jocondeDataService.DownloadJocondeDataAsync(batchUrl, batchFilePath, cancellationToken);
                        
                        // Analyser et importer le lot
                        var batchReport = await _jocondeDataService.ImportFromJsonFileAsync(batchFilePath, cancellationToken);
                        
                        // Mise à jour du rapport global
                        totalProcessed += batchReport.ImportedArtworks;
                        report.ImportedArtworks += batchReport.ImportedArtworks;
                        report.ImportedArtists += batchReport.ImportedArtists;
                        report.ImportedMuseums += batchReport.ImportedMuseums;
                        report.ImportedDomains += batchReport.ImportedDomains;
                        report.ImportedTechniques += batchReport.ImportedTechniques;
                        report.ImportedPeriods += batchReport.ImportedPeriods;
                        
                        _logger.LogInformation("Lot {Current}/{Total} traité avec succès. {Artworks} œuvres importées.",
                            batchIndex + 1, totalBatches, batchReport.ImportedArtworks);
                        
                        // Nettoyage du fichier temporaire
                        if (File.Exists(batchFilePath))
                        {
                            File.Delete(batchFilePath);
                            _logger.LogDebug("Fichier temporaire supprimé: {FilePath}", batchFilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors du traitement du lot {Current}/{Total}: {Message}",
                            batchIndex + 1, totalBatches, ex.Message);
                        
                        report.Errors++;
                        // Continuer avec le lot suivant malgré l'erreur
                    }
                    
                    // Petite pause entre les lots pour éviter de surcharger l'API
                    if (batchIndex < totalBatches - 1)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    }
                }
                
                _logger.LogInformation("Traitement par lots terminé. Total traité: {Total} œuvres importées sur {Expected}.",
                    report.ImportedArtworks, totalRecords);
                
                report.TotalArtworks = totalRecords;
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
