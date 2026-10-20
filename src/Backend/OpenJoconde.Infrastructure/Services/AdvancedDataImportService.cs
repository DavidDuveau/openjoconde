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
            _jsonParser = jsonParser ?? throw new ArgumentNullException(nameof(jsonParser));
            _csvParser = csvParser ?? throw new ArgumentNullException(nameof(csvParser));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Importe les données du résultat de parsing dans la base de données.
        /// Les statistiques comptent ce qui a été écrit ; ce qui a été perdu est dans <see cref="ImportStatistics.Errors"/>.
        /// </summary>
        public async Task<ImportStatistics> ImportDataAsync(
            ParsingResult parsingResult,
            Action<string, int, int>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            var stats = new ImportStatistics();
            await ImportIntoAsync(parsingResult, stats, progressCallback, cancellationToken);
            return stats;
        }

        /// <summary>
        /// Importe dans les statistiques fournies, pour que les comptes déjà écrits restent disponibles si l'import est annulé
        /// </summary>
        private async Task ImportIntoAsync(
            ParsingResult parsingResult,
            ImportStatistics stats,
            Action<string, int, int>? progressCallback,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var errors = new ImportErrors(stats);

            try
            {
                _logger.LogInformation("Début de l'importation des données");

                if (parsingResult.RejectedRecords > 0)
                {
                    errors.Add($"{parsingResult.RejectedRecords} notice(s) rejetée(s) au parsing", parsingResult.RejectedRecords);
                }

                // Les liens œuvre-artiste du parseur ne portent que l'Id d'origine de l'artiste :
                // on le retient avant l'upsert, qui remplace l'Id par celui déjà en base.
                var artistsByParsedId = parsingResult.Artists.ToDictionary(a => a.Id);

                // 1. Importation des entités de référence
                await ImportReferenceEntitiesAsync(parsingResult, stats, errors, progressCallback, cancellationToken);

                // 2. Importation des œuvres
                await ImportArtworksAsync(parsingResult.Artworks, artistsByParsedId, stats, errors, progressCallback, cancellationToken);
            }
            finally
            {
                stats.Duration = stopwatch.Elapsed;
            }

            _logger.LogInformation("Importation des données terminée en {Duration}. " +
                                  "Œuvres: {ArtworksCount}, Artistes: {ArtistsCount}, " +
                                  "Musées: {MuseumsCount}, Domaines: {DomainsCount}, " +
                                  "Techniques: {TechniquesCount}, Périodes: {PeriodsCount}, Erreurs: {ErrorsCount}",
                                  stats.Duration,
                                  stats.ArtworksImported,
                                  stats.ArtistsImported,
                                  stats.MuseumsImported,
                                  stats.DomainsImported,
                                  stats.TechniquesImported,
                                  stats.PeriodsImported,
                                  stats.Errors);
        }

        /// <summary>
        /// Importe les entités de référence (domaines, techniques, périodes, artistes, musées)
        /// </summary>
        private async Task ImportReferenceEntitiesAsync(
            ParsingResult parsingResult,
            ImportStatistics stats,
            ImportErrors errors,
            Action<string, int, int>? progressCallback,
            CancellationToken cancellationToken)
        {
            stats.DomainsImported = await UpsertAsync("Domaines", parsingResult.Domains, _domainRepository.BulkUpsertAsync, errors, progressCallback, cancellationToken);
            stats.TechniquesImported = await UpsertAsync("Techniques", parsingResult.Techniques, _techniqueRepository.BulkUpsertAsync, errors, progressCallback, cancellationToken);
            stats.PeriodsImported = await UpsertAsync("Périodes", parsingResult.Periods, _periodRepository.BulkUpsertAsync, errors, progressCallback, cancellationToken);
            stats.ArtistsImported = await UpsertAsync("Artistes", parsingResult.Artists, _artistRepository.BulkUpsertAsync, errors, progressCallback, cancellationToken);
            stats.MuseumsImported = await UpsertAsync("Musées", parsingResult.Museums, _museumRepository.BulkUpsertAsync, errors, progressCallback, cancellationToken);
        }

        /// <summary>
        /// Enregistre une liste d'entités de référence ; renvoie le nombre réellement enregistré
        /// </summary>
        private async Task<int> UpsertAsync<T>(
            string label,
            List<T> items,
            Func<IEnumerable<T>, Task<int>> upsert,
            ImportErrors errors,
            Action<string, int, int>? progressCallback,
            CancellationToken cancellationToken)
        {
            if (items.Count == 0)
                return 0;

            cancellationToken.ThrowIfCancellationRequested();
            progressCallback?.Invoke(label, 0, items.Count);

            int written;
            try
            {
                written = await upsert(items);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Échec de l'enregistrement de {Count} élément(s) : {Label}", items.Count, label);
                errors.Add(label, ex, items.Count);
                return 0;
            }

            if (written < items.Count)
            {
                errors.Add($"{label} : {items.Count - written} sur {items.Count} non enregistré(s)", items.Count - written);
            }

            progressCallback?.Invoke(label, items.Count, items.Count);
            _logger.LogInformation("Importation de {Written}/{Count} élément(s) terminée : {Label}", written, items.Count, label);
            return written;
        }

        /// <summary>
        /// Importe les œuvres et leurs relations
        /// </summary>
        private async Task ImportArtworksAsync(
            List<Artwork> artworks,
            IReadOnlyDictionary<Guid, Artist> artistsByParsedId,
            ImportStatistics stats,
            ImportErrors errors,
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
                cancellationToken.ThrowIfCancellationRequested();

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

                try
                {
                    var written = await _artworkRepository.BulkUpsertAsync(artworksToImport);
                    stats.ArtworksImported += written;
                    if (written < batch.Count)
                    {
                        errors.Add($"Œuvres : {batch.Count - written} sur {batch.Count} non enregistrée(s)", batch.Count - written);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Le lot entier est perdu : sans œuvre en base, ses relations ne peuvent pas être écrites
                    _logger.LogError(ex, "Échec de l'enregistrement d'un lot de {Count} œuvres", batch.Count);
                    errors.Add("Œuvres", ex, batch.Count);
                    totalProcessed += batch.Count;
                    progressCallback?.Invoke("Œuvres", totalProcessed, artworks.Count);
                    continue;
                }

                // 2. Ensuite, écrire les relations de tout le lot en une fois.
                // L'upsert a fixé l'Id réel (en base) de chaque copie, dans le même ordre que le lot.
                var relationSets = new List<ArtworkRelationSet>(batch.Count);
                for (int index = 0; index < batch.Count; index++)
                {
                    var artwork = batch[index];
                    var set = new ArtworkRelationSet
                    {
                        ArtworkId = artworksToImport[index].Id,
                        Artists = artwork.Artists
                            .Where(r => artistsByParsedId.ContainsKey(r.ArtistId))
                            .Select(r => new ArtworkArtist
                            {
                                ArtworkId = artworksToImport[index].Id,
                                ArtistId = artistsByParsedId[r.ArtistId].Id,
                                Role = r.Role
                            })
                            .ToList(),
                        DomainIds = artwork.Domains.Select(d => d.Id).Distinct().ToList(),
                        TechniqueIds = artwork.Techniques.Select(t => t.Id).Distinct().ToList(),
                        PeriodIds = artwork.Periods.Select(p => p.Id).Distinct().ToList()
                    };

                    if (set.RelationCount > 0)
                        relationSets.Add(set);
                }

                if (relationSets.Count > 0)
                {
                    try
                    {
                        await _artworkRelationsRepository.SaveBatchAsync(relationSets);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Les relations de tout le lot sont perdues (écriture en une transaction)
                        _logger.LogError(ex, "Échec de l'enregistrement des relations de {Count} œuvres", relationSets.Count);
                        errors.Add("Relations des œuvres", ex, relationSets.Sum(r => r.RelationCount));
                    }
                }

                totalProcessed += batch.Count;
                progressCallback?.Invoke("Œuvres", totalProcessed, artworks.Count);
            }

            _logger.LogInformation("Importation de {Written}/{Count} œuvres et de leurs relations terminée", stats.ArtworksImported, artworks.Count);
        }

        /// <summary>
        /// Cumule les éléments perdus pendant un import et garde quelques messages distincts
        /// </summary>
        private sealed class ImportErrors
        {
            private const int MaxMessages = 5;

            private readonly ImportStatistics _stats;

            public ImportErrors(ImportStatistics stats) => _stats = stats;

            public void Add(string message, int count = 1)
            {
                _stats.Errors += count;
                if (_stats.ErrorMessages.Count < MaxMessages && !_stats.ErrorMessages.Contains(message))
                {
                    _stats.ErrorMessages.Add(message);
                }
            }

            public void Add(string context, Exception exception, int count = 1) => Add($"{context} : {exception.Message}", count);
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
        /// Importe un fichier en choisissant le parseur selon l'extension (.csv ou .json)
        /// </summary>
        public Task<ImportReport> ImportFromFileAsync(string filePath, Action<string, int, int>? progressCallback = null, CancellationToken cancellationToken = default) =>
            Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".csv" => ImportFromCsvFileAsync(filePath, progressCallback, cancellationToken),
                ".json" => ImportFromJsonFileAsync(filePath, progressCallback, cancellationToken),
                var extension => throw new NotSupportedException($"Format de fichier non pris en charge : '{extension}' (attendu : .csv ou .json)")
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
            var stats = new ImportStatistics();

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

                // Éléments trouvés dans le fichier
                report.TotalArtworks = parsingResult.Artworks.Count;
                report.TotalArtists = parsingResult.Artists.Count;
                report.TotalMuseums = parsingResult.Museums.Count;
                report.TotalDomains = parsingResult.Domains.Count;
                report.TotalTechniques = parsingResult.Techniques.Count;
                report.TotalPeriods = parsingResult.Periods.Count;

                await ImportIntoAsync(parsingResult, stats, progressCallback, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Importation {Format} annulée", format);
                report.Success = false;
                report.ErrorMessage = "Importation annulée";
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

            // Ce qui a réellement été écrit, y compris pour un import annulé ou interrompu
            report.ImportedArtworks = stats.ArtworksImported;
            report.ImportedArtists = stats.ArtistsImported;
            report.ImportedMuseums = stats.MuseumsImported;
            report.ImportedDomains = stats.DomainsImported;
            report.ImportedTechniques = stats.TechniquesImported;
            report.ImportedPeriods = stats.PeriodsImported;

            if (stats.Errors > 0)
            {
                report.Errors += stats.Errors;
                report.Success = false;
                report.ErrorMessage = string.Join(" | ", stats.ErrorMessages);
            }

            return report;
        }
    }
}
