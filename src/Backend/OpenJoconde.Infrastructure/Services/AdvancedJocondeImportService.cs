using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using OpenJoconde.Core.Parsers;
using OpenJoconde.Infrastructure.Data;
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
    /// Service avancé pour l'importation des données Joconde
    /// Utilise le parseur XML spécialisé pour une extraction complète des entités
    /// </summary>
    public class AdvancedJocondeImportService : IDataImportService
    {
        private readonly OpenJocondeDbContext _dbContext;
        private readonly IJocondeXmlParser _xmlParser;
        private readonly ILogger<AdvancedJocondeImportService> _logger;
        private readonly HttpClient _httpClient;

        public AdvancedJocondeImportService(
            OpenJocondeDbContext dbContext,
            IJocondeXmlParser xmlParser,
            ILogger<AdvancedJocondeImportService> logger,
            HttpClient httpClient)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _xmlParser = xmlParser ?? throw new ArgumentNullException(nameof(xmlParser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Télécharge le fichier XML Joconde depuis l'URL spécifiée
        /// </summary>
        /// <param name="url">URL du fichier XML</param>
        /// <param name="destinationPath">Chemin de destination</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Chemin du fichier téléchargé</returns>
        public async Task<string> DownloadJocondeDataAsync(string url, string destinationPath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Téléchargement des données Joconde depuis {Url}", url);

            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("L'URL ne peut pas être vide", nameof(url));

            if (string.IsNullOrEmpty(destinationPath))
                throw new ArgumentException("Le chemin de destination ne peut pas être vide", nameof(destinationPath));

            // Créer le répertoire de destination s'il n'existe pas
            var directory = Path.GetDirectoryName(destinationPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            try
            {
                // Télécharger le fichier
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                // Écrire le contenu dans le fichier de destination de manière optimisée
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                using var downloadStream = await response.Content.ReadAsStreamAsync();
                
                await downloadStream.CopyToAsync(fileStream, 81920, cancellationToken);

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
        /// Importe les données du résultat de parsing dans la base de données
        /// </summary>
        /// <param name="parsingResult">Résultat du parsing contenant les entités à importer</param>
        /// <param name="progressCallback">Callback pour suivre la progression</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Statistiques sur l'importation</returns>
        public async Task<ImportStatistics> ImportDataAsync(
        ParsingResult parsingResult, 
        Action<string, int, int>? progressCallback = null, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var stats = new ImportStatistics();

        try
        {
            // Désactiver le suivi des changements pour améliorer les performances
            _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            // 1. Importer les domaines
            _logger.LogInformation("Importation des domaines");
            await ImportDomainsAsync(parsingResult.Domains, progressCallback, cancellationToken);
            stats.DomainsImported = parsingResult.Domains.Count;

            // 2. Importer les techniques
            _logger.LogInformation("Importation des techniques");
            await ImportTechniquesAsync(parsingResult.Techniques, progressCallback, cancellationToken);
            stats.TechniquesImported = parsingResult.Techniques.Count;

            // 3. Importer les périodes
            _logger.LogInformation("Importation des périodes");
            await ImportPeriodsAsync(parsingResult.Periods, progressCallback, cancellationToken);
            stats.PeriodsImported = parsingResult.Periods.Count;

            // 4. Importer les musées
            _logger.LogInformation("Importation des musées");
            await ImportMuseumsAsync(parsingResult.Museums, progressCallback, cancellationToken);
            stats.MuseumsImported = parsingResult.Museums.Count;

            // 5. Importer les artistes
            _logger.LogInformation("Importation des artistes");
            await ImportArtistsAsync(parsingResult.Artists, progressCallback, cancellationToken);
            stats.ArtistsImported = parsingResult.Artists.Count;

            // 6. Importer les œuvres et leurs relations
            _logger.LogInformation("Importation des œuvres");
            await ImportArtworksAsync(parsingResult.Artworks, progressCallback, cancellationToken);
            stats.ArtworksImported = parsingResult.Artworks.Count;

            // Réactiver le suivi des changements
            _dbContext.ChangeTracker.AutoDetectChangesEnabled = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'importation des données: {Message}", ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            stats.Duration = stopwatch.Elapsed;
            _logger.LogInformation("Importation terminée en {Duration}", stats.Duration);
        }

        return stats;
    }
    
    /// <summary>
    /// Importe les données Joconde à partir d'un fichier XML
    /// </summary>
    /// <param name="xmlFilePath">Chemin du fichier XML</param>
    /// <param name="progressCallback">Callback de progression</param>
    /// <param name="cancellationToken">Token d'annulation</param>
    /// <returns>Rapport d'importation</returns>
    public async Task<ImportReport> ImportFromXmlFileAsync(
        string xmlFilePath, 
        Action<string, int, int>? progressCallback = null, 
        CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var report = new ImportReport
            {
                ImportDate = DateTime.UtcNow,
                FileName = Path.GetFileName(xmlFilePath)
            };

            try
            {
                _logger.LogInformation("Début de l'analyse du fichier XML: {FilePath}", xmlFilePath);
                
                // Utilisation du parseur XML pour extraire toutes les entités
                var parsingResult = await _xmlParser.ParseAsync(
                    xmlFilePath,
                    (current, total) => progressCallback?.Invoke("Analyse XML", current, total),
                    cancellationToken);
                
                _logger.LogInformation("Analyse XML terminée. {ArtworkCount} œuvres, {ArtistCount} artistes, " +
                                     "{DomainCount} domaines, {TechniqueCount} techniques, {PeriodCount} périodes extraits",
                    parsingResult.Artworks.Count, 
                    parsingResult.Artists.Count,
                    parsingResult.Domains.Count,
                    parsingResult.Techniques.Count,
                    parsingResult.Periods.Count);

                // Mise à jour du rapport avec les compteurs
                report.TotalArtworks = parsingResult.Artworks.Count;
                report.TotalArtists = parsingResult.Artists.Count;
                report.TotalDomains = parsingResult.Domains.Count;
                report.TotalTechniques = parsingResult.Techniques.Count;
                report.TotalPeriods = parsingResult.Periods.Count;
                report.TotalMuseums = parsingResult.Museums.Count;

                // Importer les données dans la base de données
                await ImportEntitiesAsync(
                    parsingResult, 
                    (stage, current, total) => progressCallback?.Invoke(stage, current, total),
                    cancellationToken);

                report.ImportedArtworks = parsingResult.Artworks.Count;
                report.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'importation des données Joconde: {Message}", ex.Message);
                report.Success = false;
                report.ErrorMessage = ex.Message;
                report.Errors++;
            }
            finally
            {
                stopwatch.Stop();
                report.Duration = stopwatch.Elapsed;
                _logger.LogInformation("Importation terminée en {Duration}", report.Duration);
            }

            return report;
        }

        /// <summary>
        /// Importe toutes les entités dans la base de données
        /// </summary>
        private async Task ImportEntitiesAsync(
            ParsingResult parsingResult,
            Action<string, int, int>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            // Désactiver le suivi des changements pour améliorer les performances
            _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            // 1. Importer les domaines
            _logger.LogInformation("Importation des domaines");
            await ImportDomainsAsync(parsingResult.Domains, progressCallback, cancellationToken);

            // 2. Importer les techniques
            _logger.LogInformation("Importation des techniques");
            await ImportTechniquesAsync(parsingResult.Techniques, progressCallback, cancellationToken);

            // 3. Importer les périodes
            _logger.LogInformation("Importation des périodes");
            await ImportPeriodsAsync(parsingResult.Periods, progressCallback, cancellationToken);

            // 4. Importer les musées
            _logger.LogInformation("Importation des musées");
            await ImportMuseumsAsync(parsingResult.Museums, progressCallback, cancellationToken);

            // 5. Importer les artistes
            _logger.LogInformation("Importation des artistes");
            await ImportArtistsAsync(parsingResult.Artists, progressCallback, cancellationToken);

            // 6. Importer les œuvres et leurs relations
            _logger.LogInformation("Importation des œuvres");
            await ImportArtworksAsync(parsingResult.Artworks, progressCallback, cancellationToken);

            // Réactiver le suivi des changements
            _dbContext.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        /// <summary>
        /// Importe les domaines dans la base de données
        /// </summary>
        private async Task ImportDomainsAsync(
            List<Domain> domains,
            Action<string, int, int>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (domains.Count == 0)
                return;

            int count = 0;
            int total = domains.Count;

            // Récupérer les domaines existants par nom
            var existingDomains = await _dbContext.Domains
                .ToDictionaryAsync(d => d.Name.ToLower(), d => d, cancellationToken);

            foreach (var domain in domains)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Vérifier si le domaine existe déjà
                if (existingDomains.TryGetValue(domain.Name.ToLower(), out var existingDomain))
                {
                    // Mise à jour si nécessaire
                    domain.Id = existingDomain.Id;
                }
                else
                {
                    // Ajout du nouveau domaine
                    _dbContext.Domains.Add(domain);
                }

                count++;
                progressCallback?.Invoke("Importation des domaines", count, total);

                // Sauvegarder par lots
                if (count % 100 == 0 || count == total)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _dbContext.ChangeTracker.Clear();
                }
            }

            // Sauvegarder les changements restants
            if (count % 100 != 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                _dbContext.ChangeTracker.Clear();
            }
        }

        /// <summary>
        /// Importe les techniques dans la base de données
        /// </summary>
        private async Task ImportTechniquesAsync(
            List<Technique> techniques,
            Action<string, int, int>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (techniques.Count == 0)
                return;

            int count = 0;
            int total = techniques.Count;

            // Récupérer les techniques existantes par nom
            var existingTechniques = await _dbContext.Techniques
                .ToDictionaryAsync(t => t.Name.ToLower(), t => t, cancellationToken);

            foreach (var technique in techniques)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Vérifier si la technique existe déjà
                if (existingTechniques.TryGetValue(technique.Name.ToLower(), out var existingTechnique))
                {
                    // Mise à jour si nécessaire
                    technique.Id = existingTechnique.Id;
                }
                else
                {
                    // Ajout de la nouvelle technique
                    _dbContext.Techniques.Add(technique);
                }

                count++;
                progressCallback?.Invoke("Importation des techniques", count, total);

                // Sauvegarder par lots
                if (count % 100 == 0 || count == total)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _dbContext.ChangeTracker.Clear();
                }
            }

            // Sauvegarder les changements restants
            if (count % 100 != 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                _dbContext.ChangeTracker.Clear();
            }
        }

        /// <summary>
        /// Importe les périodes dans la base de données
        /// </summary>
        private async Task ImportPeriodsAsync(
            List<Period> periods,
            Action<string, int, int>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (periods.Count == 0)
                return;

            int count = 0;
            int total = periods.Count;

            // Récupérer les périodes existantes par nom
            var existingPeriods = await _dbContext.Periods
                .ToDictionaryAsync(p => p.Name.ToLower(), p => p, cancellationToken);

            foreach (var period in periods)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Vérifier si la période existe déjà
                if (existingPeriods.TryGetValue(period.Name.ToLower(), out var existingPeriod))
                {
                    // Mise à jour si nécessaire
                    period.Id = existingPeriod.Id;
                }
                else
                {
                    // Ajout de la nouvelle période
                    _dbContext.Periods.Add(period);
                }

                count++;
                progressCallback?.Invoke("Importation des périodes", count, total);

                // Sauvegarder par lots
                if (count % 100 == 0 || count == total)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _dbContext.ChangeTracker.Clear();
                }
            }

            // Sauvegarder les changements restants
            if (count % 100 != 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                _dbContext.ChangeTracker.Clear();
            }
        }

        /// <summary>
        /// Importe les musées dans la base de données
        /// </summary>
        private async Task ImportMuseumsAsync(
            List<Museum> museums,
            Action<string, int, int>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (museums.Count == 0)
                return;

            int count = 0;
            int total = museums.Count;

            // Récupérer les musées existants par nom
            var existingMuseums = await _dbContext.Museums
                .ToDictionaryAsync(m => m.Name.ToLower(), m => m, cancellationToken);

            foreach (var museum in museums)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Vérifier si le musée existe déjà
                if (existingMuseums.TryGetValue(museum.Name.ToLower(), out var existingMuseum))
                {
                    // Mise à jour si nécessaire
                    museum.Id = existingMuseum.Id;
                    existingMuseum.City = museum.City ?? existingMuseum.City;
                    existingMuseum.Department = museum.Department ?? existingMuseum.Department;
                    existingMuseum.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Ajout du nouveau musée
                    _dbContext.Museums.Add(museum);
                }

                count++;
                progressCallback?.Invoke("Importation des musées", count, total);

                // Sauvegarder par lots
                if (count % 50 == 0 || count == total)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _dbContext.ChangeTracker.Clear();
                }
            }

            // Sauvegarder les changements restants
            if (count % 50 != 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                _dbContext.ChangeTracker.Clear();
            }
        }

        /// <summary>
        /// Importe les artistes dans la base de données
        /// </summary>
        private async Task ImportArtistsAsync(
            List<Artist> artists,
            Action<string, int, int>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (artists.Count == 0)
                return;

            int count = 0;
            int total = artists.Count;

            // Récupérer les artistes existants
            // Utilisation d'une clé combinée nom+prénom pour l'identification
            var existingArtists = await _dbContext.Artists
                .ToDictionaryAsync(
                    a => $"{a.LastName?.ToLower() ?? ""}|{a.FirstName?.ToLower() ?? ""}",
                    a => a,
                    cancellationToken);

            foreach (var artist in artists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var artistKey = $"{artist.LastName?.ToLower() ?? ""}|{artist.FirstName?.ToLower() ?? ""}";

                // Vérifier si l'artiste existe déjà
                if (existingArtists.TryGetValue(artistKey, out var existingArtist))
                {
                    // Mise à jour si nécessaire
                    artist.Id = existingArtist.Id;
                    existingArtist.BirthDate = artist.BirthDate ?? existingArtist.BirthDate;
                    existingArtist.DeathDate = artist.DeathDate ?? existingArtist.DeathDate;
                    existingArtist.Nationality = artist.Nationality ?? existingArtist.Nationality;
                    existingArtist.Biography = artist.Biography ?? existingArtist.Biography;
                    existingArtist.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Ajout du nouvel artiste
                    _dbContext.Artists.Add(artist);
                }

                count++;
                progressCallback?.Invoke("Importation des artistes", count, total);

                // Sauvegarder par lots
                if (count % 100 == 0 || count == total)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _dbContext.ChangeTracker.Clear();
                }
            }

            // Sauvegarder les changements restants
            if (count % 100 != 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                _dbContext.ChangeTracker.Clear();
            }
        }

        /// <summary>
        /// Importe les œuvres et leurs relations dans la base de données
        /// </summary>
        private async Task ImportArtworksAsync(
            List<Artwork> artworks,
            Action<string, int, int>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (artworks.Count == 0)
                return;

            int count = 0;
            int total = artworks.Count;

            // Récupérer les œuvres existantes par référence
            var existingArtworks = await _dbContext.Artworks
                .Include(a => a.Artists)
                .Include(a => a.Domains)
                .Include(a => a.Techniques)
                .Include(a => a.Periods)
                .Where(a => !a.IsDeleted)
                .ToDictionaryAsync(a => a.Reference, a => a, cancellationToken);

            foreach (var artwork in artworks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Vérifier si l'œuvre existe déjà
                if (existingArtworks.TryGetValue(artwork.Reference, out var existingArtwork))
                {
                    // Mise à jour de l'œuvre existante
                    existingArtwork.Title = artwork.Title ?? existingArtwork.Title;
                    existingArtwork.Description = artwork.Description ?? existingArtwork.Description;
                    existingArtwork.Dimensions = artwork.Dimensions ?? existingArtwork.Dimensions;
                    existingArtwork.CreationDate = artwork.CreationDate ?? existingArtwork.CreationDate;
                    existingArtwork.CreationPlace = artwork.CreationPlace ?? existingArtwork.CreationPlace;
                    existingArtwork.ConservationPlace = artwork.ConservationPlace ?? existingArtwork.ConservationPlace;
                    existingArtwork.Copyright = artwork.Copyright ?? existingArtwork.Copyright;
                    existingArtwork.ImageUrl = artwork.ImageUrl ?? existingArtwork.ImageUrl;
                    existingArtwork.UpdatedAt = DateTime.UtcNow;

                    // Mettre à jour les relations
                    UpdateArtworkRelations(existingArtwork, artwork);
                }
                else
                {
                    // Ajout de la nouvelle œuvre
                    _dbContext.Artworks.Add(artwork);
                }

                count++;
                progressCallback?.Invoke("Importation des œuvres", count, total);

                // Sauvegarder par lots
                if (count % 50 == 0 || count == total)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _dbContext.ChangeTracker.Clear();
                }
            }

            // Sauvegarder les changements restants
            if (count % 50 != 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                _dbContext.ChangeTracker.Clear();
            }
        }

        /// <summary>
        /// Met à jour les relations d'une œuvre existante
        /// </summary>
        private void UpdateArtworkRelations(Artwork existingArtwork, Artwork newArtwork)
        {
            // Mise à jour des artistes
            foreach (var artist in newArtwork.Artists)
            {
                if (!existingArtwork.Artists.Any(a => a.Artist.Id == artist.Artist.Id))
                {
                    existingArtwork.Artists.Add(new ArtworkArtist
                    {
                        ArtistId = artist.Artist.Id,
                        Role = artist.Role
                    });
                }
            }

            // Mise à jour des domaines
            foreach (var domain in newArtwork.Domains)
            {
                if (!existingArtwork.Domains.Any(d => d.Id == domain.Id))
                {
                    existingArtwork.Domains.Add(domain);
                }
            }

            // Mise à jour des techniques
            foreach (var technique in newArtwork.Techniques)
            {
                if (!existingArtwork.Techniques.Any(t => t.Id == technique.Id))
                {
                    existingArtwork.Techniques.Add(technique);
                }
            }

            // Mise à jour des périodes
            foreach (var period in newArtwork.Periods)
            {
                if (!existingArtwork.Periods.Any(p => p.Id == period.Id))
                {
                    existingArtwork.Periods.Add(period);
                }
            }
        }
    }

    /// <summary>
    /// Rapport d'importation des données Joconde
    /// </summary>
    public class ImportReport
    {
        /// <summary>
        /// Date de l'importation
        /// </summary>
        public DateTime ImportDate { get; set; }

        /// <summary>
        /// Nom du fichier importé
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// Nombre total d'œuvres dans le fichier
        /// </summary>
        public int TotalArtworks { get; set; }

        /// <summary>
        /// Nombre d'œuvres importées avec succès
        /// </summary>
        public int ImportedArtworks { get; set; }

        /// <summary>
        /// Nombre total d'artistes extraits
        /// </summary>
        public int TotalArtists { get; set; }

        /// <summary>
        /// Nombre total de domaines extraits
        /// </summary>
        public int TotalDomains { get; set; }

        /// <summary>
        /// Nombre total de techniques extraites
        /// </summary>
        public int TotalTechniques { get; set; }

        /// <summary>
        /// Nombre total de périodes extraites
        /// </summary>
        public int TotalPeriods { get; set; }

        /// <summary>
        /// Nombre total de musées extraits
        /// </summary>
        public int TotalMuseums { get; set; }

        /// <summary>
        /// Nombre d'erreurs rencontrées
        /// </summary>
        public int Errors { get; set; }

        /// <summary>
        /// Message d'erreur en cas d'échec
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Durée de l'importation
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Indique si l'importation a réussi
        /// </summary>
        public bool Success { get; set; }
    }
}
