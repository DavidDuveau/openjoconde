using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using OpenJoconde.Core.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Service pour parser les fichiers JSON Joconde
    /// </summary>
    public class JocondeJsonParserService : IJocondeJsonParser
    {
        private readonly ILogger<JocondeJsonParserService> _logger;

        public JocondeJsonParserService(ILogger<JocondeJsonParserService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Parse un fichier JSON Joconde et extrait toutes les entités
        /// </summary>
        public async Task<ParsingResult> ParseAsync(
            string jsonFilePath,
            Action<int, int> progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Début du parsing du fichier JSON Joconde: {FilePath}", jsonFilePath);

            if (string.IsNullOrEmpty(jsonFilePath))
                throw new ArgumentException("Le chemin du fichier JSON ne peut pas être vide", nameof(jsonFilePath));

            if (!File.Exists(jsonFilePath))
                throw new FileNotFoundException("Le fichier JSON n'existe pas", jsonFilePath);

            // Utiliser des dictionnaires pour éviter les doublons
            var artists = new Dictionary<string, Artist>();
            var domains = new Dictionary<string, Domain>();
            var techniques = new Dictionary<string, Technique>();
            var periods = new Dictionary<string, Period>();
            var museums = new Dictionary<string, Museum>();
            var artworks = new List<Artwork>();

            try
            {
                _logger.LogInformation("Lecture du fichier JSON par flux: {FilePath}", jsonFilePath);
                
                // Créer les options du JsonDocumentOptions pour une meilleure performance
                var options = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                    MaxDepth = 64 // Augmenter si nécessaire
                };
                
                // Ouvrir un flux de fichier pour éviter de charger tout le fichier en mémoire
                using var fileStream = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                
                // Créer un JsonDocument à partir du flux
                using JsonDocument jsonDocument = await JsonDocument.ParseAsync(fileStream, options, cancellationToken);
                
                // Récupérer l'élément racine
                JsonElement root = jsonDocument.RootElement;
                
                // Longueur du tableau, si disponible (ou estimation)
                int totalItems = 0;
                try {
                    totalItems = root.GetArrayLength();
                    _logger.LogInformation("Nombre total d'éléments trouvés: {Count}", totalItems);
                } catch {
                    _logger.LogWarning("Impossible de déterminer le nombre total d'éléments. Estimation utilisée.");
                    totalItems = 100000; // Estimation par défaut
                }
                
                int processedItems = 0;
                
                // Traiter les éléments un par un
                foreach (JsonElement artworkElement in root.EnumerateArray())
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Parsing annulé par l'utilisateur");
                        break;
                    }

                    try
                    {
                        // Extraction de l'œuvre avec vérification de la dénomination
                        var artwork = ExtractArtwork(artworkElement);
                        
                        // Vérifier que la dénomination n'est pas nulle
                        if (string.IsNullOrEmpty(artwork.Denomination))
                        {
                            artwork.Denomination = "Œuvre sans dénomination";
                        }
                        
                        // Extraction des entités liées
                        ExtractRelatedEntities(artworkElement, artwork, artists, domains, techniques, periods, museums);
                        
                        // Ajout de l'œuvre au résultat
                        artworks.Add(artwork);
                        
                        // Mise à jour de la progression
                        processedItems++;
                        if (processedItems % 1000 == 0 || processedItems == totalItems)
                        {
                            _logger.LogInformation("Progression: {Current}/{Total} œuvres traitées", processedItems, totalItems);
                            progressCallback?.Invoke(processedItems, totalItems);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erreur lors de l'extraction d'une œuvre: {Message}", ex.Message);
                        // Continuer avec la suivante
                    }
                }
                
                // Créer le résultat final avec des List<T> pour toutes les collections
                var result = new ParsingResult
                {
                    Artworks = artworks,
                    Artists = artists.Values.ToList(),
                    Domains = domains.Values.ToList(),
                    Techniques = techniques.Values.ToList(),
                    Periods = periods.Values.ToList(),
                    Museums = museums.Values.ToList()
                };
                
                _logger.LogInformation("Parsing JSON terminé: {ArtworksCount} œuvres, {ArtistsCount} artistes, {DomainsCount} domaines, {TechniquesCount} techniques, {PeriodsCount} périodes, {MuseumsCount} musées", 
                    result.Artworks.Count,
                    result.Artists.Count,
                    result.Domains.Count,
                    result.Techniques.Count,
                    result.Periods.Count,
                    result.Museums.Count);
                    
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du parsing du fichier JSON: {Message}", ex.Message);
                throw new Exception($"Erreur lors du parsing du fichier JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Extraction des informations de base d'une œuvre
        /// </summary>
        private Artwork ExtractArtwork(JsonElement artworkElement)
        {
            // Extraire explicitement la dénomination
            string denomination = GetStringValue(artworkElement, "DENO");
            
            // Si la dénomination est vide, utiliser une valeur par défaut
            if (string.IsNullOrEmpty(denomination))
            {
                denomination = "Œuvre sans dénomination";
            }
            
            return new Artwork
            {
                Id = Guid.NewGuid(),
                Reference = GetStringValue(artworkElement, "REF"),
                InventoryNumber = GetStringValue(artworkElement, "INV"),
                Denomination = denomination, // Utiliser la valeur extraite ou par défaut
                Title = GetStringValue(artworkElement, "TITR"),
                Description = GetStringValue(artworkElement, "DESC"),
                Dimensions = GetStringValue(artworkElement, "DIMS"),
                CreationDate = GetStringValue(artworkElement, "DAPT"),
                CreationPlace = GetStringValue(artworkElement, "LOCA"),
                ConservationPlace = GetStringValue(artworkElement, "LOCA2"),
                Copyright = GetStringValue(artworkElement, "COPY"),
                ImageUrl = GetStringValue(artworkElement, "IMG"),
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        /// <summary>
        /// Extraction des entités liées (artistes, domaines, techniques, etc.)
        /// </summary>
        private void ExtractRelatedEntities(
            JsonElement artworkElement, 
            Artwork artwork,
            Dictionary<string, Artist> artists,
            Dictionary<string, Domain> domains,
            Dictionary<string, Technique> techniques,
            Dictionary<string, Period> periods,
            Dictionary<string, Museum> museums)
        {
            // Extraction des artistes
            var artistName = GetStringValue(artworkElement, "AUTR");
            if (!string.IsNullOrEmpty(artistName))
            {
                // Utiliser le nom comme clé
                string artistKey = artistName.ToLower();
                
                if (!artists.TryGetValue(artistKey, out var artist))
                {
                    artist = new Artist
                    {
                        Id = Guid.NewGuid(),
                        LastName = artistName,
                        // D'autres champs peuvent être extraits selon la structure JSON
                    };
                    
                    artists[artistKey] = artist;
                }
                
                // Lier l'artiste à l'œuvre
                artwork.Artists.Add(new ArtworkArtist
                {
                    ArtistId = artist.Id,
                    ArtworkId = artwork.Id,
                    Role = "Créateur" // À adapter selon les données
                });
            }
            
            // Extraction des domaines
            var domainName = GetStringValue(artworkElement, "DOMN");
            if (!string.IsNullOrEmpty(domainName))
            {
                // Utiliser le nom comme clé
                string domainKey = domainName.ToLower();
                
                if (!domains.TryGetValue(domainKey, out var domain))
                {
                    domain = new Domain
                    {
                        Id = Guid.NewGuid(),
                        Name = domainName
                    };
                    
                    domains[domainKey] = domain;
                }
                
                artwork.Domains.Add(domain);
            }
            
            // Extraction des techniques
            var techniqueName = GetStringValue(artworkElement, "TECH");
            if (!string.IsNullOrEmpty(techniqueName))
            {
                // Utiliser le nom comme clé
                string techniqueKey = techniqueName.ToLower();
                
                if (!techniques.TryGetValue(techniqueKey, out var technique))
                {
                    technique = new Technique
                    {
                        Id = Guid.NewGuid(),
                        Name = techniqueName
                    };
                    
                    techniques[techniqueKey] = technique;
                }
                
                artwork.Techniques.Add(technique);
            }
            
            // Extraction des périodes
            var periodName = GetStringValue(artworkElement, "PERI");
            if (!string.IsNullOrEmpty(periodName))
            {
                // Utiliser le nom comme clé
                string periodKey = periodName.ToLower();
                
                if (!periods.TryGetValue(periodKey, out var period))
                {
                    period = new Period
                    {
                        Id = Guid.NewGuid(),
                        Name = periodName
                    };
                    
                    periods[periodKey] = period;
                }
                
                artwork.Periods.Add(period);
            }
            
            // Extraction du musée
            var museumName = GetStringValue(artworkElement, "LOCA2");
            if (!string.IsNullOrEmpty(museumName))
            {
                // Utiliser le nom comme clé
                string museumKey = museumName.ToLower();
                
                if (!museums.TryGetValue(museumKey, out var museum))
                {
                    museum = new Museum
                    {
                        Id = Guid.NewGuid(),
                        Name = museumName
                    };
                    
                    museums[museumKey] = museum;
                }
                
                // La relation entre œuvre et musée peut être gérée ici
            }
        }

        /// <summary>
        /// Récupère une valeur string à partir d'un élément JSON
        /// </summary>
        private string GetStringValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out JsonElement property))
            {
                return property.ValueKind == JsonValueKind.String ? property.GetString() : string.Empty;
            }
            
            return string.Empty;
        }
    }
}
