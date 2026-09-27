using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using OpenJoconde.Core.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Service optimisé pour parser les fichiers JSON Joconde par streaming, évitant les OutOfMemoryException
    /// </summary>
    public class StreamingJocondeJsonParserService : IJocondeJsonParser
    {
        private readonly ILogger<StreamingJocondeJsonParserService> _logger;
        private const int BUFFER_SIZE = 16384; // 16Ko pour le buffer de lecture

        public StreamingJocondeJsonParserService(ILogger<StreamingJocondeJsonParserService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Parse un fichier JSON Joconde et extrait toutes les entités en utilisant un traitement par flux
        /// </summary>
        public async Task<ParsingResult> ParseAsync(
            string jsonFilePath,
            Action<int, int> progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Début du parsing JSON par streaming: {FilePath}", jsonFilePath);

            if (string.IsNullOrEmpty(jsonFilePath))
                throw new ArgumentException("Le chemin du fichier JSON ne peut pas être vide", nameof(jsonFilePath));

            if (!File.Exists(jsonFilePath))
                throw new FileNotFoundException("Le fichier JSON n'existe pas", jsonFilePath);

            // Estimation du nombre d'œuvres (basée sur la taille du fichier)
            long fileSize = new FileInfo(jsonFilePath).Length;
            int estimatedItems = (int)(fileSize / 5000); // Estimation grossière basée sur une taille moyenne d'objet JSON
            _logger.LogInformation("Taille du fichier: {Size} octets, estimation du nombre d'œuvres: ~{Count}", fileSize, estimatedItems);

            // Utiliser des dictionnaires pour éviter les doublons
            var artists = new Dictionary<string, Artist>();
            var domains = new Dictionary<string, Domain>();
            var techniques = new Dictionary<string, Technique>();
            var periods = new Dictionary<string, Period>();
            var museums = new Dictionary<string, Museum>();
            var artworks = new List<Artwork>();

            // Compteurs pour le suivi
            int processedItems = 0;
            int totalItems = estimatedItems;
            int totalBatches = 0;
            int successfulBatches = 0;

            try
            {
                using (var fileStream = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: BUFFER_SIZE, useAsync: true))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    // Vérifier que le fichier commence par un tableau '['
                    char[] firstCharBuffer = new char[1];
                    await streamReader.ReadAsync(firstCharBuffer, 0, 1);
                    if (firstCharBuffer[0] != '[')
                    {
                        throw new FormatException("Le fichier JSON ne commence pas par un tableau '['");
                    }

                    // Utiliser un JsonReaderOptions pour optimiser la lecture
                    var readerOptions = new JsonReaderOptions
                    {
                        AllowTrailingCommas = true,
                        CommentHandling = JsonCommentHandling.Skip,
                        MaxDepth = 64
                    };

                    StringBuilder jsonObjectBuilder = new StringBuilder();
                    bool inObject = false;
                    int objectDepth = 0;
                    int batchSize = 1000; // Traitement par lots de 1000 objets
                    List<string> batchObjects = new List<string>(batchSize);

                    char[] buffer = new char[BUFFER_SIZE];
                    int bytesRead;

                    // Lecture caractère par caractère pour extraire les objets JSON individuellement
                    while ((bytesRead = await streamReader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogWarning("Parsing annulé par l'utilisateur");
                            break;
                        }

                        for (int i = 0; i < bytesRead; i++)
                        {
                            char c = buffer[i];

                            // Gestion de la profondeur des objets
                            if (c == '{')
                            {
                                if (!inObject)
                                {
                                    inObject = true;
                                    objectDepth = 1;
                                    jsonObjectBuilder.Clear();
                                }
                                else
                                {
                                    objectDepth++;
                                }
                                jsonObjectBuilder.Append(c);
                            }
                            else if (c == '}')
                            {
                                objectDepth--;
                                jsonObjectBuilder.Append(c);

                                if (inObject && objectDepth == 0)
                                {
                                    // Objet JSON complet
                                    inObject = false;
                                    batchObjects.Add(jsonObjectBuilder.ToString());

                                    // Si nous avons un lot complet, traiter le lot
                                    if (batchObjects.Count >= batchSize)
                                    {
                                        await ProcessBatchAsync(batchObjects, artworks, artists, domains, techniques, periods, museums);
                                        totalBatches++;
                                        successfulBatches++;

                                        // Mise à jour des compteurs
                                        processedItems += batchObjects.Count;
                                        _logger.LogInformation("Lot {BatchNumber} traité: {ItemCount} œuvres, total traité: {TotalProcessed}/{EstimatedTotal}",
                                            totalBatches, batchObjects.Count, processedItems, totalItems);

                                        // Invoquer le callback de progression
                                        progressCallback?.Invoke(processedItems, totalItems);

                                        // Réinitialiser le lot
                                        batchObjects.Clear();
                                    }
                                }
                            }
                            else if (inObject)
                            {
                                jsonObjectBuilder.Append(c);
                            }
                            // Ignorer les caractères en dehors des objets (virgules, espaces, etc.)
                        }
                    }

                    // Traiter le dernier lot s'il n'est pas vide
                    if (batchObjects.Count > 0)
                    {
                        await ProcessBatchAsync(batchObjects, artworks, artists, domains, techniques, periods, museums);
                        totalBatches++;
                        successfulBatches++;
                        processedItems += batchObjects.Count;
                        _logger.LogInformation("Dernier lot {BatchNumber} traité: {ItemCount} œuvres, total traité: {TotalProcessed}",
                            totalBatches, batchObjects.Count, processedItems);
                        progressCallback?.Invoke(processedItems, processedItems);
                    }
                }

                // Mettre à jour le compteur total avec le nombre réel
                totalItems = processedItems;

                var result = new ParsingResult
                {
                    Artworks = artworks,
                    Artists = artists.Values.ToList(),
                    Domains = domains.Values.ToList(),
                    Techniques = techniques.Values.ToList(),
                    Periods = periods.Values.ToList(),
                    Museums = museums.Values.ToList()
                };

                _logger.LogInformation("Parsing JSON terminé: {ArtworksCount} œuvres, {ArtistsCount} artistes, {DomainsCount} domaines, {TechniquesCount} techniques, {PeriodsCount} périodes, {MuseumsCount} musées, {SuccessfulBatches}/{TotalBatches} lots réussis",
                    result.Artworks.Count,
                    result.Artists.Count,
                    result.Domains.Count,
                    result.Techniques.Count,
                    result.Periods.Count,
                    result.Museums.Count,
                    successfulBatches,
                    totalBatches);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du parsing du fichier JSON: {Message}", ex.Message);
                throw new Exception($"Erreur lors du parsing du fichier JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Traite un lot d'objets JSON et extrait les entités
        /// </summary>
        private async Task ProcessBatchAsync(
            List<string> jsonObjects,
            List<Artwork> artworks,
            Dictionary<string, Artist> artists,
            Dictionary<string, Domain> domains,
            Dictionary<string, Technique> techniques,
            Dictionary<string, Period> periods,
            Dictionary<string, Museum> museums)
        {
            int batchSuccessCount = 0;

            foreach (var jsonObject in jsonObjects)
            {
                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(jsonObject))
                    {
                        JsonElement root = doc.RootElement;
                        var artwork = ExtractArtwork(root);

                        // Vérifier que la dénomination n'est pas nulle
                        if (string.IsNullOrEmpty(artwork.Denomination))
                        {
                            artwork.Denomination = "Œuvre sans dénomination";
                        }

                        // Extraction des entités liées
                        ExtractRelatedEntities(root, artwork, artists, domains, techniques, periods, museums);

                        // Ajout de l'œuvre au résultat
                        artworks.Add(artwork);
                        batchSuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erreur lors de l'extraction d'une œuvre: {Message}", ex.Message);
                    // Continuer avec la suivante
                }
            }

            _logger.LogDebug("Lot traité: {Success}/{Total} objets extraits avec succès", batchSuccessCount, jsonObjects.Count);
            await Task.CompletedTask; // Pour le support de l'async
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
    }
}
