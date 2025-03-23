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
using System.Xml.Linq;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Service pour télécharger, analyser et importer les données Joconde
    /// </summary>
    public class JocondeDataService : IJocondeDataService
    {
        private readonly OpenJocondeDbContext _dbContext;
        private readonly ILogger<JocondeDataService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IJocondeXmlParser _xmlParser;

        public JocondeDataService(
            OpenJocondeDbContext dbContext,
            ILogger<JocondeDataService> logger,
            HttpClient httpClient,
            IJocondeXmlParser xmlParser)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _xmlParser = xmlParser ?? throw new ArgumentNullException(nameof(xmlParser));
        }

        /// <summary>
        /// Télécharge les données Joconde depuis l'URL spécifiée
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
            if (!Directory.Exists(directory))
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
        /// Analyse un fichier XML Joconde et extrait les oeuvres d'art
        /// </summary>
        public async Task<IEnumerable<Artwork>> ParseJocondeXmlAsync(string xmlFilePath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Analyse du fichier XML Joconde {FilePath}", xmlFilePath);

            if (!File.Exists(xmlFilePath))
                throw new FileNotFoundException("Le fichier XML Joconde n'existe pas", xmlFilePath);

            try
            {
                // Chargement du fichier XML
                var doc = await Task.Run(() => XDocument.Load(xmlFilePath), cancellationToken);
                
                // Extraction des oeuvres (notice)
                var artworks = new List<Artwork>();
                
                // Espace de noms XML
                var ns = doc.Root.GetDefaultNamespace();
                
                // Trouver toutes les notices (oeuvres)
                var records = doc.Root.Elements(ns + "record");
                
                foreach (var record in records)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    
                    // Extraction des données de base de l'oeuvre
                    var artwork = new Artwork
                    {
                        Reference = GetElementValue(record, "REF", ns),
                        InventoryNumber = GetElementValue(record, "INV", ns),
                        Title = GetElementValue(record, "TITR", ns),
                        Description = GetElementValue(record, "DESC", ns),
                        Dimensions = GetElementValue(record, "DIMS", ns),
                        CreationDate = GetElementValue(record, "DAPT", ns),
                        CreationPlace = GetElementValue(record, "LOCA", ns),
                        ConservationPlace = GetElementValue(record, "LOCA2", ns),
                        Copyright = GetElementValue(record, "COPY", ns),
                        ImageUrl = GetElementValue(record, "IMG", ns),
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    
                    // Ajouter l'oeuvre à la liste
                    artworks.Add(artwork);
                    
                    // Limiter la taille de la liste pour éviter les problèmes de mémoire
                    if (artworks.Count >= 1000)
                    {
                        _logger.LogInformation("Traitement par lot: {Count} oeuvres analysées", artworks.Count);
                    }
                }
                
                _logger.LogInformation("Analyse XML terminée: {Count} oeuvres extraites", artworks.Count);
                return artworks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'analyse du fichier XML Joconde: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Extrait la valeur d'un élément XML
        /// </summary>
        private string GetElementValue(XElement element, string name, XNamespace ns)
        {
            return element.Element(ns + name)?.Value?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Importe les oeuvres d'art dans la base de données
        /// </summary>
        public async Task<int> ImportArtworksAsync(IEnumerable<Artwork> artworks, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Importation de {Count} oeuvres dans la base de données", artworks.Count());
            
            int importedCount = 0;
            
            foreach (var artwork in artworks)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                try
                {
                    // Vérifier si l'oeuvre existe déjà (par référence)
                    var existingArtwork = _dbContext.Artworks.SingleOrDefault(a => a.Reference == artwork.Reference);
                    
                    if (existingArtwork == null)
                    {
                        // Nouvelle oeuvre
                        _dbContext.Artworks.Add(artwork);
                    }
                    else
                    {
                        // Mise à jour d'une oeuvre existante
                        existingArtwork.InventoryNumber = artwork.InventoryNumber;
                        existingArtwork.Title = artwork.Title;
                        existingArtwork.Description = artwork.Description;
                        existingArtwork.Dimensions = artwork.Dimensions;
                        existingArtwork.CreationDate = artwork.CreationDate;
                        existingArtwork.CreationPlace = artwork.CreationPlace;
                        existingArtwork.ConservationPlace = artwork.ConservationPlace;
                        existingArtwork.Copyright = artwork.Copyright;
                        existingArtwork.ImageUrl = artwork.ImageUrl;
                        existingArtwork.UpdatedAt = DateTime.UtcNow;
                        existingArtwork.IsDeleted = false;
                    }
                    
                    importedCount++;
                    
                    // Sauvegarder régulièrement pour éviter des transactions trop volumineuses
                    if (importedCount % 100 == 0)
                    {
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Progression: {Count} oeuvres importées", importedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erreur lors de l'importation de l'oeuvre {Reference}: {Message}", 
                        artwork.Reference, ex.Message);
                }
            }
            
            // Sauvegarder les dernières modifications
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Importation terminée: {Count} oeuvres importées avec succès", importedCount);
            return importedCount;
        }

        /// <summary>
        /// Exécute le processus complet de mise à jour des données Joconde
        /// </summary>
        public async Task<ImportReport> UpdateJocondeDataAsync(string xmlUrl, string tempDirectory, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Démarrage de la mise à jour des données Joconde");
            
            var report = new ImportReport
            {
                ImportDate = DateTime.UtcNow,
                Success = true
            };
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Créer le répertoire temporaire s'il n'existe pas
                if (!Directory.Exists(tempDirectory))
                    Directory.CreateDirectory(tempDirectory);
                
                // Générer un nom de fichier temporaire
                var tempFilePath = Path.Combine(tempDirectory, $"joconde_{DateTime.Now:yyyyMMdd_HHmmss}.xml");
                
                // Télécharger les données
                await DownloadJocondeDataAsync(xmlUrl, tempFilePath, cancellationToken);
                
                // Analyser le fichier XML
                var artworks = await ParseJocondeXmlAsync(tempFilePath, cancellationToken);
                report.TotalArtworks = artworks.Count();
                
                // Importer les oeuvres
                report.ImportedArtworks = await ImportArtworksAsync(artworks, cancellationToken);
                
                // Nettoyer les fichiers temporaires
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour des données Joconde: {Message}", ex.Message);
                report.Errors++;
                report.Success = false;
                report.ErrorMessage = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                report.Duration = stopwatch.Elapsed;
                _logger.LogInformation("Mise à jour des données Joconde terminée en {Duration}", report.Duration);
            }
            
            return report;
        }

        /// <summary>
        /// Télécharge le dernier fichier Joconde disponible
        /// </summary>
        public async Task<string> DownloadLatestFileAsync(string destinationDirectory, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Téléchargement du dernier fichier Joconde disponible");
                
                // URL par défaut pour les données Joconde (à personnaliser selon les besoins)
                var url = "https://data.culture.gouv.fr/api/datasets/1.0/joconde-catalogue-collectif-des-collections-des-musees-de-france/attachments/base_joconde_extrait_xml_zip";
                
                // Créer le répertoire de destination s'il n'existe pas
                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }
                
                // Générer un nom de fichier avec timestamp
                var fileName = $"joconde_{DateTime.Now:yyyyMMdd_HHmmss}.xml";
                var filePath = Path.Combine(destinationDirectory, fileName);
                
                // Télécharger le fichier
                return await DownloadJocondeDataAsync(url, filePath, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement du dernier fichier Joconde: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Importe les données depuis un fichier XML
        /// </summary>
        public async Task<ImportReport> ImportFromXmlFileAsync(string xmlFilePath, CancellationToken cancellationToken = default)
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
                var parsingResult = await _xmlParser.ParseAsync(xmlFilePath, null, cancellationToken);
                
                // Mettre à jour le rapport avec le nombre d'éléments trouvés
                report.TotalArtworks = parsingResult.Artworks.Count;
                report.TotalArtists = parsingResult.Artists.Count;
                report.TotalMuseums = parsingResult.Museums.Count;
                report.TotalDomains = parsingResult.Domains.Count;
                report.TotalTechniques = parsingResult.Techniques.Count;
                report.TotalPeriods = parsingResult.Periods.Count;
                
                // Importer les œuvres
                report.ImportedArtworks = await ImportArtworksAsync(parsingResult.Artworks, cancellationToken);
                
                // Pour un rapport complet, il faudrait aussi suivre l'importation des autres entités
                // Ce code simplifié se concentre uniquement sur les œuvres
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