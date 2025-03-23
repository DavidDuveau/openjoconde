using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using OpenJoconde.Core.Parsers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Service avancé pour l'importation des données depuis le résultat du parsing vers la base de données
    /// Optimisé pour les performances et la gestion des relations
    /// </summary>
    public class AdvancedDataImportService : IDataImportService
    {
        private readonly ILogger<AdvancedDataImportService> _logger;
        private readonly IArtworkRepository _artworkRepository;
        private readonly IArtistRepository _artistRepository;
        private readonly IMuseumRepository _museumRepository;
        private readonly IDomainRepository _domainRepository;
        private readonly ITechniqueRepository _techniqueRepository;
        private readonly IPeriodRepository _periodRepository;
        private readonly IArtworkRelationsRepository _artworkRelationsRepository;
        private readonly IJocondeXmlParser _xmlParser;
        private readonly HttpClient _httpClient;

        public AdvancedDataImportService(
            ILogger<AdvancedDataImportService> logger,
            IArtworkRepository artworkRepository,
            IArtistRepository artistRepository,
            IMuseumRepository museumRepository,
            IDomainRepository domainRepository,
            ITechniqueRepository techniqueRepository,
            IPeriodRepository periodRepository,
            IArtworkRelationsRepository artworkRelationsRepository,
            IJocondeXmlParser xmlParser,
            HttpClient httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _artworkRepository = artworkRepository ?? throw new ArgumentNullException(nameof(artworkRepository));
            _artistRepository = artistRepository ?? throw new ArgumentNullException(nameof(artistRepository));
            _museumRepository = museumRepository ?? throw new ArgumentNullException(nameof(museumRepository));
            _domainRepository = domainRepository ?? throw new ArgumentNullException(nameof(domainRepository));
            _techniqueRepository = techniqueRepository ?? throw new ArgumentNullException(nameof(techniqueRepository));
            _periodRepository = periodRepository ?? throw new ArgumentNullException(nameof(periodRepository));
            _artworkRelationsRepository = artworkRelationsRepository ?? throw new ArgumentNullException(nameof(artworkRelationsRepository));
            _xmlParser = xmlParser ?? throw new ArgumentNullException(nameof(xmlParser));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Importe les données du résultat de parsing dans la base de données
        /// </summary>
        public async Task<ImportStatistics> ImportDataAsync(
            ParsingResult parsingResult, 
            Action<string, int, int>? progressCallback = null, 
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var stats = new ImportStatistics();

            try
            {
                _logger.LogInformation("Début de l'importation des données");

                // 1. Importation des entités de référence
                await ImportReferenceEntitiesAsync(parsingResult, progressCallback, cancellationToken);

                // 2. Importation des œuvres
                await ImportArtworksAsync(parsingResult.Artworks, progressCallback, cancellationToken);

                // 3. Mise à jour des statistiques
                stats.ArtworksImported = parsingResult.Artworks.Count;
                stats.ArtistsImported = parsingResult.Artists.Count;
                stats.MuseumsImported = parsingResult.Museums.Count;
                stats.DomainsImported = parsingResult.Domains.Count;
                stats.TechniquesImported = parsingResult.Techniques.Count;
                stats.PeriodsImported = parsingResult.Periods.Count;
                stats.Duration = stopwatch.Elapsed;

                _logger.LogInformation("Importation des données terminée en {Duration}. " +
                                      "Œuvres: {ArtworksCount}, Artistes: {ArtistsCount}, " +
                                      "Musées: {MuseumsCount}, Domaines: {DomainsCount}, " +
                                      "Techniques: {TechniquesCount}, Périodes: {PeriodsCount}",
                                      stopwatch.Elapsed,
                                      stats.ArtworksImported,
                                      stats.ArtistsImported,
                                      stats.MuseumsImported,
                                      stats.DomainsImported,
                                      stats.TechniquesImported,
                                      stats.PeriodsImported);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'importation des données");
                throw;
            }
        }

        /// <summary>
        /// Importe les entités de référence (domaines, techniques, périodes, artistes, musées)
        /// </summary>
        private async Task ImportReferenceEntitiesAsync(
            ParsingResult parsingResult,
            Action<string, int, int>? progressCallback,
            CancellationToken cancellationToken)
        {
            // Importer les domaines
            if (parsingResult.Domains.Any())
            {
                progressCallback?.Invoke("Domaines", 0, parsingResult.Domains.Count);
                await _domainRepository.BulkUpsertAsync(parsingResult.Domains);
                progressCallback?.Invoke("Domaines", parsingResult.Domains.Count, parsingResult.Domains.Count);
                _logger.LogInformation("Importation de {Count} domaines terminée", parsingResult.Domains.Count);
            }

            // Importer les techniques
            if (parsingResult.Techniques.Any())
            {
                progressCallback?.Invoke("Techniques", 0, parsingResult.Techniques.Count);
                await _techniqueRepository.BulkUpsertAsync(parsingResult.Techniques);
                progressCallback?.Invoke("Techniques", parsingResult.Techniques.Count, parsingResult.Techniques.Count);
                _logger.LogInformation("Importation de {Count} techniques terminée", parsingResult.Techniques.Count);
            }

            // Importer les périodes
            if (parsingResult.Periods.Any())
            {
                progressCallback?.Invoke("Périodes", 0, parsingResult.Periods.Count);
                await _periodRepository.BulkUpsertAsync(parsingResult.Periods);
                progressCallback?.Invoke("Périodes", parsingResult.Periods.Count, parsingResult.Periods.Count);
                _logger.LogInformation("Importation de {Count} périodes terminée", parsingResult.Periods.Count);
            }

            // Importer les artistes
            if (parsingResult.Artists.Any())
            {
                progressCallback?.Invoke("Artistes", 0, parsingResult.Artists.Count);
                await _artistRepository.BulkUpsertAsync(parsingResult.Artists);
                progressCallback?.Invoke("Artistes", parsingResult.Artists.Count, parsingResult.Artists.Count);
                _logger.LogInformation("Importation de {Count} artistes terminée", parsingResult.Artists.Count);
            }

            // Importer les musées
            if (parsingResult.Museums.Any())
            {
                progressCallback?.Invoke("Musées", 0, parsingResult.Museums.Count);
                await _museumRepository.BulkUpsertAsync(parsingResult.Museums);
                progressCallback?.Invoke("Musées", parsingResult.Museums.Count, parsingResult.Museums.Count);
                _logger.LogInformation("Importation de {Count} musées terminée", parsingResult.Museums.Count);
            }
        }

        /// <summary>
        /// Importe les œuvres et leurs relations
        /// </summary>
        private async Task ImportArtworksAsync(
            List<Artwork> artworks,
            Action<string, int, int>? progressCallback,
            CancellationToken cancellationToken)
        {
            if (!artworks.Any())
                return;

            progressCallback?.Invoke("Œuvres", 0, artworks.Count);

            // Pour les grandes quantités de données, on traite par lots
            const int batchSize = 500;
            var totalProcessed = 0;

            for (int i = 0; i < artworks.Count; i += batchSize)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var batch = artworks.Skip(i).Take(batchSize).ToList();
                
                // 1. D'abord importer les œuvres sans les relations
                var artworksToImport = batch.Select(artwork => new Artwork
                {
                    Id = artwork.Id,
                    Reference = artwork.Reference,
                    InventoryNumber = artwork.InventoryNumber,
                    Denomination = artwork.Denomination,
                    Title = artwork.Title,
                    Description = artwork.Description,
                    Dimensions = artwork.Dimensions,
                    CreationDate = artwork.CreationDate,
                    CreationPlace = artwork.CreationPlace,
                    ConservationPlace = artwork.ConservationPlace,
                    Copyright = artwork.Copyright,
                    ImageUrl = artwork.ImageUrl,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }).ToList();

                await _artworkRepository.BulkUpsertAsync(artworksToImport);

                // 2. Ensuite, traiter les relations pour chaque œuvre
                foreach (var artwork in batch)
                {
                    // Relations avec les artistes
                    if (artwork.Artists.Any())
                    {
                        await _artworkRelationsRepository.SaveArtworkArtistRelationsAsync(
                            artwork.Id, artwork.Artists);
                    }

                    // Relations avec les domaines
                    if (artwork.Domains.Any())
                    {
                        await _artworkRelationsRepository.SaveArtworkDomainRelationsAsync(
                            artwork.Id, artwork.Domains.Select(d => d.Id));
                    }

                    // Relations avec les techniques
                    if (artwork.Techniques.Any())
                    {
                        await _artworkRelationsRepository.SaveArtworkTechniqueRelationsAsync(
                            artwork.Id, artwork.Techniques.Select(t => t.Id));
                    }

                    // Relations avec les périodes
                    if (artwork.Periods.Any())
                    {
                        await _artworkRelationsRepository.SaveArtworkPeriodRelationsAsync(
                            artwork.Id, artwork.Periods.Select(p => p.Id));
                    }
                }

                totalProcessed += batch.Count;
                progressCallback?.Invoke("Œuvres", totalProcessed, artworks.Count);

                // Libérer la mémoire
                if (i % (batchSize * 5) == 0)
                {
                    GC.Collect();
                }
            }

            _logger.LogInformation("Importation de {Count} œuvres et leurs relations terminée", totalProcessed);
        }

        /// <summary>
        /// Télécharge le fichier Joconde depuis l'URL spécifiée
        /// </summary>
        public async Task<string> DownloadJocondeDataAsync(string url, string destinationPath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Téléchargement des données Joconde depuis {Url}", url);

            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("L'URL ne peut pas être vide", nameof(url));

            if (string.IsNullOrEmpty(destinationPath))
                throw new ArgumentException("Le chemin de destination ne peut pas être vide", nameof(destinationPath));

            // Créer le répertoire de destination s'il n'existe pas
            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            try
            {
                // Télécharger le fichier
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                // Écrire le contenu dans le fichier de destination
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fileStream, cancellationToken);

                _logger.LogInformation("Données Joconde téléchargées avec succès dans {FilePath}", destinationPath);
                return destinationPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement des données Joconde: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Importe les données depuis un fichier XML
        /// </summary>
        public async Task<ImportReport> ImportFromXmlFileAsync(string xmlFilePath, Action<string, int, int> progressCallback = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Démarrage de l'importation depuis le fichier XML {FilePath}", xmlFilePath);
            
            var report = new ImportReport
            {
                ImportDate = DateTime.UtcNow,
                Success = true
            };
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Vérifier l'existence du fichier
                if (!File.Exists(xmlFilePath))
                {
                    throw new FileNotFoundException("Le fichier XML Joconde n'existe pas", xmlFilePath);
                }
                
                // Analyser le fichier XML avec le parser dédié
                var parsingResult = await _xmlParser.ParseAsync(xmlFilePath, (current, total) => 
                {
                    progressCallback?.Invoke("Analyse XML", current, total);
                }, cancellationToken);
                
                // Mettre à jour le rapport avec le nombre d'éléments trouvés
                report.TotalArtworks = parsingResult.Artworks.Count;
                report.TotalArtists = parsingResult.Artists.Count;
                report.TotalMuseums = parsingResult.Museums.Count;
                report.TotalDomains = parsingResult.Domains.Count;
                report.TotalTechniques = parsingResult.Techniques.Count;
                report.TotalPeriods = parsingResult.Periods.Count;
                
                // Importer les données
                var importStats = await ImportDataAsync(parsingResult, progressCallback, cancellationToken);
                
                // Mettre à jour le rapport avec les statistiques d'importation
                report.ImportedArtworks = importStats.ArtworksImported;
                report.ImportedArtists = importStats.ArtistsImported;
                report.ImportedMuseums = importStats.MuseumsImported;
                report.ImportedDomains = importStats.DomainsImported;
                report.ImportedTechniques = importStats.TechniquesImported;
                report.ImportedPeriods = importStats.PeriodsImported;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'importation depuis le fichier XML: {Message}", ex.Message);
                report.Errors++;
                report.Success = false;
                report.ErrorMessage = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                report.Duration = stopwatch.Elapsed;
                _logger.LogInformation("Importation depuis le fichier XML terminée en {Duration}", report.Duration);
            }
            
            return report;
        }
    }
}
