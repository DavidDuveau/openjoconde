using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Parsers
{
    /// <summary>
    /// Parser pour les fichiers JSON Joconde
    /// </summary>
    public class JocondeJsonParser : IJocondeJsonParser
    {
        /// <summary>
        /// Parse un fichier JSON Joconde et extrait toutes les entités (œuvres, artistes, domaines, etc.)
        /// </summary>
        public async Task<ParsingResult> ParseAsync(
            string jsonFilePath,
            Action<int, int> progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(jsonFilePath))
                throw new ArgumentException("Le chemin du fichier JSON ne peut pas être vide", nameof(jsonFilePath));

            if (!File.Exists(jsonFilePath))
                throw new FileNotFoundException("Le fichier JSON n'existe pas", jsonFilePath);

            var result = new ParsingResult
            {
                Artworks = new List<Artwork>(),
                Artists = new HashSet<Artist>(),
                Domains = new HashSet<Domain>(),
                Techniques = new HashSet<Technique>(),
                Periods = new HashSet<Period>(),
                Museums = new HashSet<Museum>()
            };

            try
            {
                // Lecture du fichier JSON
                using FileStream fileStream = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read);
                
                // Désérialisation du JSON
                using JsonDocument jsonDocument = await JsonDocument.ParseAsync(fileStream, 
                    new JsonDocumentOptions { AllowTrailingCommas = true }, 
                    cancellationToken);

                // Récupérer l'élément racine
                JsonElement root = jsonDocument.RootElement;

                // Récupérer les œuvres (tableau à la racine)
                int totalItems = root.GetArrayLength();
                int processedItems = 0;

                foreach (JsonElement artworkElement in root.EnumerateArray())
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        // Extraction de l'œuvre
                        var artwork = ExtractArtwork(artworkElement);
                        
                        // Extraction des entités liées (artistes, domaines, techniques, etc.)
                        ExtractRelatedEntities(artworkElement, artwork, result);
                        
                        // Ajout de l'œuvre au résultat
                        result.Artworks.Add(artwork);
                        
                        // Mise à jour de la progression
                        processedItems++;
                        progressCallback?.Invoke(processedItems, totalItems);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors de l'extraction d'une œuvre: {ex.Message}");
                        // Continuer avec la suivante
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors du parsing du fichier JSON: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Extraction des informations de base d'une œuvre
        /// </summary>
        private Artwork ExtractArtwork(JsonElement artworkElement)
        {
            return new Artwork
            {
                Id = Guid.NewGuid(),
                // Adapter les champs selon la structure JSON réelle
                Reference = GetStringValue(artworkElement, "REF"),
                InventoryNumber = GetStringValue(artworkElement, "INV"),
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
        private void ExtractRelatedEntities(JsonElement artworkElement, Artwork artwork, ParsingResult result)
        {
            // Extraction des artistes
            var artistName = GetStringValue(artworkElement, "AUTR");
            if (!string.IsNullOrEmpty(artistName))
            {
                var artist = new Artist
                {
                    Id = Guid.NewGuid(),
                    LastName = artistName,
                    // D'autres champs peuvent être extraits selon la structure JSON
                };
                
                result.Artists.Add(artist);
                
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
                var domain = new Domain
                {
                    Id = Guid.NewGuid(),
                    Name = domainName
                };
                
                result.Domains.Add(domain);
                artwork.Domains.Add(domain);
            }
            
            // Extraction des techniques
            var techniqueName = GetStringValue(artworkElement, "TECH");
            if (!string.IsNullOrEmpty(techniqueName))
            {
                var technique = new Technique
                {
                    Id = Guid.NewGuid(),
                    Name = techniqueName
                };
                
                result.Techniques.Add(technique);
                artwork.Techniques.Add(technique);
            }
            
            // Extraction des périodes
            var periodName = GetStringValue(artworkElement, "PERI");
            if (!string.IsNullOrEmpty(periodName))
            {
                var period = new Period
                {
                    Id = Guid.NewGuid(),
                    Name = periodName
                };
                
                result.Periods.Add(period);
                artwork.Periods.Add(period);
            }
            
            // Extraction du musée
            var museumName = GetStringValue(artworkElement, "LOCA2");
            if (!string.IsNullOrEmpty(museumName))
            {
                var museum = new Museum
                {
                    Id = Guid.NewGuid(),
                    Name = museumName
                };
                
                result.Museums.Add(museum);
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
