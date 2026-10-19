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
        private readonly IJocondeJsonParser _jsonParser;
        private readonly IJocondeCsvParser _csvParser;
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
            IJocondeJsonParser jsonParser,
            IJocondeCsvParser csvParser,
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
            _jsonParser = jsonParser ?? throw new ArgumentNullException(nameof(jsonParser));
            _csvParser = csvParser ?? throw new ArgumentNullException(nameof(csvParser));
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

                // Les liens œuvre-artiste du parseur ne portent que l'Id d'origine de l'artiste :
                // on le retient avant l'upsert, qui remplace l'Id par celui déjà en base.
                var artistsByParsedId = parsingResult.Artists.ToDictionary(a => a.Id);

                // 1. Importation des entités de référence
                await ImportReferenceEntitiesAsync(parsingResult, progressCallback, cancellationToken);

                // 2. Importation des œuvres
                await ImportArtworksAsync(parsingResult.Artworks, artistsByParsedId, progressCallback, cancellationToken);

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
            IReadOnlyDictionary<Guid, Artist> artistsByParsedId,
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
                    UpdatedAt = DateTime.UtcNow
                }).ToList();

                await _artworkRepository.BulkUpsertAsync(artworksToImport);

                // 2. Ensuite, traiter les relations pour chaque œuvre.
                // L'upsert a fixé l'Id réel (en base) de chaque copie, dans le même ordre que le lot.
                for (int index = 0; index < batch.Count; index++)
                {
                    var artwork = batch[index];
                    var artworkId = artworksToImport[index].Id;
                    try
                    {
                        // Relations avec les artistes
                        if (artwork.Artists.Any())
                        {
                            try
                            {
                                var artistRelations = artwork.Artists
                                    .Where(r => artistsByParsedId.ContainsKey(r.ArtistId))
                                    .Select(r =>
                                    {
                                        var artist = artistsByParsedId[r.ArtistId];
                                        return new ArtworkArtist
                                        {
                                            ArtworkId = artworkId,
                                            ArtistId = artist.Id,
                                            Artist = artist,
                                            Role = r.Role
                                        };
                                    })
                                    .ToList();
                                await _artworkRelationsRepository.SaveArtworkArtistRelationsAsync(
                                    artworkId, artistRelations);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to save artist relations for artwork {ArtworkId}", artworkId);
                            }
                        }

                        // Relations avec les domaines
                        if (artwork.Domains.Any())
                        {
                            try
                            {
                                await _artworkRelationsRepository.SaveArtworkDomainRelationsAsync(
                                    artworkId, artwork.Domains.Select(d => d.Id));
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to save domain relations for artwork {ArtworkId}", artworkId);
                            }
                        }

                        // Relations avec les techniques
                        if (artwork.Techniques.Any())
                        {
                            try
                            {
                                await _artworkRelationsRepository.SaveArtworkTechniqueRelationsAsync(
                                    artworkId, artwork.Techniques.Select(t => t.Id));
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to save technique relations for artwork {ArtworkId}", artworkId);
                            }
                        }

                        // Relations avec les périodes
                        if (artwork.Periods.Any())
                        {
                            try
                            {
                                await _artworkRelationsRepository.SaveArtworkPeriodRelationsAsync(
                                    artworkId, artwork.Periods.Select(p => p.Id));
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to save period relations for artwork {ArtworkId}", artworkId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process relations for artwork {ArtworkId}", artworkId);
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
        public async Task<ImportReport> ImportFromXmlFileAsync(string xmlFilePath, Action<string, int, int>? progressCallback = null, CancellationToken cancellationToken = default)
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

        /// <summary>
        /// Importe les données depuis un fichier JSON
        /// </summary>
        public Task<ImportReport> ImportFromJsonFileAsync(string jsonFilePath, Action<string, int, int>? progressCallback = null, CancellationToken cancellationToken = default) =>
            ImportParsedFileAsync("JSON", jsonFilePath, _jsonParser.ParseAsync, progressCallback, cancellationToken);

        /// <summary>
        /// Importe les données depuis un fichier CSV (export data.gouv.fr de la base Joconde)
        /// </summary>
        public Task<ImportReport> ImportFromCsvFileAsync(string csvFilePath, Action<string, int, int>? progressCallback = null, CancellationToken cancellationToken = default) =>
            ImportParsedFileAsync("CSV", csvFilePath, _csvParser.ParseAsync, progressCallback, cancellationToken);

        /// <summary>
        /// Importe un fichier en choisissant le parseur selon l'extension (.csv, .json ou .xml)
        /// </summary>
        public Task<ImportReport> ImportFromFileAsync(string filePath, Action<string, int, int>? progressCallback = null, CancellationToken cancellationToken = default) =>
            Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".csv" => ImportFromCsvFileAsync(filePath, progressCallback, cancellationToken),
                ".json" => ImportFromJsonFileAsync(filePath, progressCallback, cancellationToken),
                ".xml" => ImportFromXmlFileAsync(filePath, progressCallback, cancellationToken),
                var extension => throw new NotSupportedException($"Format de fichier non pris en charge : '{extension}' (attendu : .csv, .json ou .xml)")
            };

        private async Task<ImportReport> ImportParsedFileAsync(
            string format,
            string filePath,
            Func<string, Action<int, int>?, CancellationToken, Task<ParsingResult>> parse,
            Action<string, int, int>? progressCallback,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Démarrage de l'importation depuis le fichier {Format} {FilePath}", format, filePath);

            var report = new ImportReport
            {
                ImportDate = DateTime.UtcNow,
                Success = true
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Le fichier {format} Joconde n'existe pas", filePath);
                }

                progressCallback?.Invoke("Parsing", 0, 1);
                var parsingResult = await parse(filePath, (current, total) =>
                {
                    if (current % 100 == 0 || current == total)
                    {
                        _logger.LogInformation("Progression du parsing {Format}: {Current}/{Total}", format, current, total);
                        progressCallback?.Invoke("Parsing", current, total);
                    }
                }, cancellationToken);

                progressCallback?.Invoke("Parsing", 1, 1);

                // Mettre à jour le rapport avec le nombre d'éléments trouvés
                report.TotalArtworks = parsingResult.Artworks.Count;
                report.TotalArtists = parsingResult.Artists.Count;
                report.TotalMuseums = parsingResult.Museums.Count;
                report.TotalDomains = parsingResult.Domains.Count;
                report.TotalTechniques = parsingResult.Techniques.Count;
                report.TotalPeriods = parsingResult.Periods.Count;

                var importStats = await ImportDataAsync(parsingResult, progressCallback, cancellationToken);

                report.ImportedArtworks = importStats.ArtworksImported;
                report.ImportedArtists = importStats.ArtistsImported;
                report.ImportedMuseums = importStats.MuseumsImported;
                report.ImportedDomains = importStats.DomainsImported;
                report.ImportedTechniques = importStats.TechniquesImported;
                report.ImportedPeriods = importStats.PeriodsImported;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'importation depuis le fichier {Format}: {Message}", format, ex.Message);
                report.Errors++;
                report.Success = false;
                report.ErrorMessage = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                report.Duration = stopwatch.Elapsed;
                _logger.LogInformation("Importation depuis le fichier {Format} terminée en {Duration}", format, report.Duration);
            }

            return report;
        }
    }
}
