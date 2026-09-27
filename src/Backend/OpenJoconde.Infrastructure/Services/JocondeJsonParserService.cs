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
            string denomination = GetStringValue(artworkElement, "denomination");
            
            // Si la dénomination est vide, utiliser une valeur par défaut
            if (string.IsNullOrEmpty(denomination))
            {
                denomination = "Œuvre sans dénomination";
            }
            
            return new Artwork
            {
                Id = Guid.NewGuid(),
                Reference = GetStringValue(artworkElement, "reference"),
                InventoryNumber = GetStringValue(artworkElement, "numero_inventaire"),
                Denomination = denomination,
                Title = GetStringValue(artworkElement, "titre"), // Use 'titre' if available, otherwise fall back to denomination
                Description = GetStringValue(artworkElement, "description"),
                Dimensions = GetStringValue(artworkElement, "mesures"),
                CreationDate = GetStringValue(artworkElement, "date_sujet_represente") ?? GetStringValue(artworkElement, "epoque"),
                CreationPlace = GetStringValue(artworkElement, "lieu_creation") ?? GetStringValue(artworkElement, "ecole_pays"),
                ConservationPlace = GetStringValue(artworkElement, "lieu_conservation") ?? GetStringValue(artworkElement, "lieu_de_depot"),
                Copyright = GetStringValue(artworkElement, "droits"), 
                ImageUrl = GetStringValue(artworkElement, "image_url") ?? GetStringValue(artworkElement, "lien_image"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
            var artistName = GetStringValue(artworkElement, "auteur");
            if (!string.IsNullOrEmpty(artistName))
            {
                // Parse artist name (e.g., "Charnay Armand (1844-1915)")
                var artist = ParseArtistName(artistName);
                string artistKey = artistName.ToLower();
                
                if (!artists.TryGetValue(artistKey, out var existingArtist))
                {
                    existingArtist = artist;
                    artists[artistKey] = existingArtist;
                }
                
                // Lier l'artiste à l'œuvre
                artwork.Artists.Add(new ArtworkArtist
                {
                    ArtistId = existingArtist.Id,
                    ArtworkId = artwork.Id,
                    Role = "Créateur"
                });
            }
            
            // Extraction des domaines (array field)
            if (artworkElement.TryGetProperty("domaine", out JsonElement domaineArray) && domaineArray.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement domainElement in domaineArray.EnumerateArray())
                {
                    var domainName = domainElement.GetString();
                    if (!string.IsNullOrEmpty(domainName))
                    {
                        string domainKey = domainName.ToLower();
                        
                        if (!domains.TryGetValue(domainKey, out var domain))
                        {
                            domain = new Domain
                            {
                                Id = Guid.NewGuid(),
                                Name = domainName,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            
                            domains[domainKey] = domain;
                        }
                        
                        artwork.Domains.Add(domain);
                    }
                }
            }
            
            // Extraction des techniques depuis description
            var description = GetStringValue(artworkElement, "description");
            if (!string.IsNullOrEmpty(description))
            {
                var technique = ExtractTechniqueFromDescription(description);
                if (!string.IsNullOrEmpty(technique))
                {
                    string techniqueKey = technique.ToLower();
                    
                    if (!techniques.TryGetValue(techniqueKey, out var techniqueObj))
                    {
                        techniqueObj = new Technique
                        {
                            Id = Guid.NewGuid(),
                            Name = technique,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        
                        techniques[techniqueKey] = techniqueObj;
                    }
                    
                    artwork.Techniques.Add(techniqueObj);
                }
            }
            
            // Extraction des périodes
            var epochName = GetStringValue(artworkElement, "epoque");
            if (!string.IsNullOrEmpty(epochName))
            {
                string periodKey = epochName.ToLower();
                
                if (!periods.TryGetValue(periodKey, out var period))
                {
                    period = new Period
                    {
                        Id = Guid.NewGuid(),
                        Name = epochName,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    periods[periodKey] = period;
                }
                
                artwork.Periods.Add(period);
            }
            
            // Extraction du musée
            var museumName = GetStringValue(artworkElement, "musee") ?? GetStringValue(artworkElement, "lieu_conservation");
            if (!string.IsNullOrEmpty(museumName))
            {
                string museumKey = museumName.ToLower();
                
                if (!museums.TryGetValue(museumKey, out var museum))
                {
                    museum = new Museum
                    {
                        Id = Guid.NewGuid(),
                        Name = museumName,
                        City = GetStringValue(artworkElement, "ville") ?? GetStringValue(artworkElement, "region"),
                        Department = GetStringValue(artworkElement, "departement"),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    museums[museumKey] = museum;
                }
            }
        }
        
        /// <summary>
        /// Parse artist name from Joconde format (e.g., "Charnay Armand (1844-1915)")
        /// </summary>
        private Artist ParseArtistName(string fullName)
        {
            var artist = new Artist
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            // Extract birth/death dates from parentheses
            var dateMatch = System.Text.RegularExpressions.Regex.Match(fullName, @"\(([^)]+)\)");
            if (dateMatch.Success)
            {
                var dates = dateMatch.Groups[1].Value;
                if (dates.Contains("-"))
                {
                    var dateParts = dates.Split('-');
                    if (dateParts.Length == 2)
                    {
                        artist.BirthDate = dateParts[0].Trim();
                        artist.DeathDate = dateParts[1].Trim();
                    }
                }
                
                // Remove dates from name
                fullName = fullName.Replace(dateMatch.Value, "").Trim();
            }
            
            // Split name into parts
            var nameParts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length >= 2)
            {
                artist.LastName = nameParts[0];
                artist.FirstName = string.Join(" ", nameParts.Skip(1));
            }
            else
            {
                artist.LastName = fullName.Trim();
            }
            
            return artist;
        }
        
        /// <summary>
        /// Extract technique from description field
        /// </summary>
        private string ExtractTechniqueFromDescription(string description)
        {
            if (string.IsNullOrEmpty(description)) return null;
            
            // Common technique keywords
            var techniques = new[] { "huile", "aquarelle", "crayon", "encre", "pastel", "gouache", "acrylique", "tempera", "fusain" };
            
            var lowerDesc = description.ToLower();
            foreach (var technique in techniques)
            {
                if (lowerDesc.Contains(technique))
                {
                    return technique;
                }
            }
            
            return description; // Return full description as technique if no specific technique found
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
