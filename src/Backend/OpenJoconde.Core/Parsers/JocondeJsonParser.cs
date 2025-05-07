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

            // Utiliser des dictionnaires pour éviter les doublons
            var artists = new Dictionary<string, Artist>();
            var domains = new Dictionary<string, Domain>();
            var techniques = new Dictionary<string, Technique>();
            var periods = new Dictionary<string, Period>();
            var museums = new Dictionary<string, Museum>();
            var artworks = new List<Artwork>();

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

                // Vérifier la structure du JSON pour déterminer s'il contient un objet avec 'results' ou directement un tableau
                bool hasResultsProperty = root.TryGetProperty("results", out JsonElement resultsElement);
                JsonElement artworksArray = hasResultsProperty ? resultsElement : root;
                
                // Récupérer le nombre total d'œuvres
                int totalItems = artworksArray.GetArrayLength();
                int processedItems = 0;
                
                // Journalisation pour le débogage
                Console.WriteLine($"Démarrage du parsing de {totalItems} œuvres");

                foreach (JsonElement artworkElement in artworksArray.EnumerateArray())
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        // Extraction de l'œuvre
                        var artwork = ExtractArtwork(artworkElement);
                        
                        // Extraction des entités liées (artistes, domaines, techniques, etc.)
                        ExtractRelatedEntities(artworkElement, artwork, artists, domains, techniques, periods, museums);
                        
                        // Ajout de l'œuvre au résultat
                        artworks.Add(artwork);
                        
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

            return result;
        }

        /// <summary>
        /// Extraction des informations de base d'une œuvre
        /// </summary>
        private Artwork ExtractArtwork(JsonElement artworkElement)
        {
            // Mappage entre les noms de champs JSON et les propriétés du modèle
            // Format : { "nom_champ_json", "nom_champ_alternatif_xml" }
            var fieldMappings = new Dictionary<string, string>
            {
                {"reference", "REF"},
                {"numero_inventaire", "INV"},
                {"denomination", "DENO"},
                {"titre", "TITR"},
                {"description", "DESC"},
                {"mesures", "DIMS"},
                {"periode_de_creation", "DAPT"},
                {"lieu_de_creation_utilisation", "LOCA"},
                {"localisation", "LOCA2"},
                {"copyright", "COPY"},
                {"image", "IMG"}
            };
            
            // Journalisation pour débogage
            Console.WriteLine("Extraction d'une œuvre");
            Console.WriteLine($"Propriétés disponibles: {string.Join(", ", GetAllPropertyNames(artworkElement))}");
            
            // Extraire la dénomination avec le mappage flexible
            string denomination = GetMappedStringValue(artworkElement, fieldMappings["denomination"], "denomination");
            
            // Si la dénomination est vide, utiliser une valeur par défaut
            if (string.IsNullOrEmpty(denomination))
            {
                denomination = "Œuvre sans dénomination";
            }
            
            var artwork = new Artwork
            {
                Id = Guid.NewGuid(),
                Reference = GetMappedStringValue(artworkElement, fieldMappings["reference"], "reference"),
                InventoryNumber = GetMappedStringValue(artworkElement, fieldMappings["numero_inventaire"], "numero_inventaire"),
                Denomination = denomination,
                Title = GetMappedStringValue(artworkElement, fieldMappings["titre"], "titre"),
                Description = GetMappedStringValue(artworkElement, fieldMappings["description"], "description"),
                Dimensions = GetMappedStringValue(artworkElement, fieldMappings["mesures"], "mesures"),
                CreationDate = GetMappedStringValue(artworkElement, fieldMappings["periode_de_creation"], "periode_de_creation"),
                CreationPlace = GetMappedStringValue(artworkElement, fieldMappings["lieu_de_creation_utilisation"], "lieu_de_creation_utilisation"),
                ConservationPlace = GetMappedStringValue(artworkElement, fieldMappings["localisation"], "localisation"),
                Copyright = GetMappedStringValue(artworkElement, fieldMappings["copyright"], "copyright"),
                ImageUrl = GetMappedStringValue(artworkElement, fieldMappings["image"], "image"),
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            
            // Journalisation pour vérifier l'extraction
            Console.WriteLine($"Œuvre extraite - Reference: {artwork.Reference}, Titre: {artwork.Title}");
            
            return artwork;
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
            // Mappage entre les noms de champs JSON et les propriétés du modèle
            var fieldMappings = new Dictionary<string, string[]>
            {
                {"auteur", new[] {"AUTR", "auteur"}},
                {"domaine", new[] {"DOMN", "domaine"}},
                {"materiaux_techniques", new[] {"TECH", "materiaux_techniques"}},
                {"periode_de_creation", new[] {"PERI", "periode_de_creation"}},
                {"localisation", new[] {"LOCA2", "localisation"}},
                {"nom_officiel_musee", new[] {"nom_officiel_musee"}}
            };
            
            // Journalisation pour débogage
            Console.WriteLine("Extraction des entités liées");
            
            // Extraction des artistes
            var artistName = GetMappedStringValue(artworkElement, fieldMappings["auteur"][0], fieldMappings["auteur"][1]);
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
                        // Autres champs artiste
                        FirstName = "", // À extraire si disponible
                        Nationality = GetMappedStringValue(artworkElement, "ecole_pays", "ecole_pays"),
                        BirthDate = "", // À extraire à partir de precisions_sur_l_auteur si disponible
                        DeathDate = "", // À extraire à partir de precisions_sur_l_auteur si disponible
                        Biography = GetMappedStringValue(artworkElement, "precisions_sur_l_auteur", "precisions_sur_l_auteur")
                    };
                    
                    // Traitement des dates de naissance et décès à partir de la biographie
                    // Format typique: "Paris, 1600 ; Rome, 1682"
                    string biography = artist.Biography;
                    if (!string.IsNullOrEmpty(biography) && biography.Contains(";"))
                    {
                        try
                        {
                            var parts = biography.Split(';');
                            if (parts.Length >= 2)
                            {
                                // Extraire les dates à partir des parties de la biographie
                                var birthPart = parts[0].Trim();
                                var deathPart = parts[1].Trim();
                                
                                // Extraire les années (4 chiffres consécutifs)
                                var birthMatch = System.Text.RegularExpressions.Regex.Match(birthPart, "\\d{4}");
                                var deathMatch = System.Text.RegularExpressions.Regex.Match(deathPart, "\\d{4}");
                                
                                if (birthMatch.Success)
                                {
                                    artist.BirthDate = birthMatch.Value;
                                }
                                
                                if (deathMatch.Success)
                                {
                                    artist.DeathDate = deathMatch.Value;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Erreur lors de l'extraction des dates de l'artiste: {ex.Message}");
                        }
                    }
                    
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
            
            // Extraction des domaines (peut être un tableau dans le JSON)
            var domainValues = GetArrayOrSingleValue(artworkElement, fieldMappings["domaine"]);
            foreach (var domainName in domainValues)
            {
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
            }
            
            // Extraction des techniques (peut être un tableau dans le JSON)
            var techniqueValues = GetArrayOrSingleValue(artworkElement, fieldMappings["materiaux_techniques"]);
            foreach (var techniqueName in techniqueValues)
            {
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
            }
            
            // Extraction des périodes
            var periodName = GetMappedStringValue(artworkElement, fieldMappings["periode_de_creation"][0], fieldMappings["periode_de_creation"][1]);
            if (!string.IsNullOrEmpty(periodName))
            {
                // Utiliser le nom comme clé
                string periodKey = periodName.ToLower();
                
                if (!periods.TryGetValue(periodKey, out var period))
                {
                    period = new Period
                    {
                        Id = Guid.NewGuid(),
                        Name = periodName,
                        StartYear = null, // À calculer si possible
                        EndYear = null    // À calculer si possible
                    };
                    
                    // Tenter d'extraire les années de début et de fin à partir du nom de la période
                    try
                    {
                        var matches = System.Text.RegularExpressions.Regex.Matches(periodName, "\\d+");
                        if (matches.Count >= 2)
                        {
                            if (int.TryParse(matches[0].Value, out int startYear))
                            {
                                period.StartYear = startYear;
                            }
                            
                            if (int.TryParse(matches[1].Value, out int endYear))
                            {
                                period.EndYear = endYear;
                            }
                        }
                        else if (matches.Count == 1)
                        {
                            // Si une seule date est présente, l'utiliser comme année de début
                            if (int.TryParse(matches[0].Value, out int year))
                            {
                                period.StartYear = year;
                                
                                // Si la période contient "moitié", estimer l'année de fin
                                if (periodName.Contains("moitié"))
                                {
                                    period.EndYear = year + 50;
                                }
                                // Si la période contient "quart", estimer l'année de fin
                                else if (periodName.Contains("quart"))
                                {
                                    period.EndYear = year + 25;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors de l'extraction des années de la période: {ex.Message}");
                    }
                    
                    periods[periodKey] = period;
                }
                
                artwork.Periods.Add(period);
            }
            
            // Extraction du musée
            var museumName = GetMappedStringValue(artworkElement, fieldMappings["nom_officiel_musee"][0]);
            if (string.IsNullOrEmpty(museumName))
            {
                museumName = GetMappedStringValue(artworkElement, fieldMappings["localisation"][0], fieldMappings["localisation"][1]);
            }
            
            if (!string.IsNullOrEmpty(museumName))
            {
                // Utiliser le nom comme clé
                string museumKey = museumName.ToLower();
                
                if (!museums.TryGetValue(museumKey, out var museum))
                {
                    museum = new Museum
                    {
                        Id = Guid.NewGuid(),
                        Name = museumName,
                        City = GetMappedStringValue(artworkElement, "ville", "ville"),
                        Department = GetMappedStringValue(artworkElement, "departement", "departement"),
                        Region = GetMappedStringValue(artworkElement, "region", "region"),
                        Code = GetMappedStringValue(artworkElement, "code_museofile", "code_museofile")
                    };
                    
                    // Extraire les coordonnées géographiques si disponibles
                    try
                    {
                        if (artworkElement.TryGetProperty("coordonnees", out JsonElement coordElement))
                        {
                            if (coordElement.TryGetProperty("lon", out JsonElement lonElement) && 
                                coordElement.TryGetProperty("lat", out JsonElement latElement))
                            {
                                if (lonElement.TryGetDouble(out double lon) && latElement.TryGetDouble(out double lat))
                                {
                                    museum.Longitude = lon;
                                    museum.Latitude = lat;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors de l'extraction des coordonnées du musée: {ex.Message}");
                    }
                    
                    museums[museumKey] = museum;
                }
                
                // Stocker la référence au musée dans l'œuvre si nécessaire
                // artwork.MuseumId = museum.Id;
            }
        }

        /// <summary>
        /// Récupère une valeur string à partir d'un élément JSON avec un mappage flexible des noms de propriétés
        /// </summary>
        private string GetMappedStringValue(JsonElement element, string primaryPropertyName, string alternatePropertyName = null)
        {
            // Essayer avec le nom de propriété principal
            if (element.TryGetProperty(primaryPropertyName, out JsonElement property))
            {
                if (property.ValueKind == JsonValueKind.String)
                {
                    return property.GetString();
                }
                else if (property.ValueKind == JsonValueKind.Null)
                {
                    return string.Empty;
                }
                else if (property.ValueKind == JsonValueKind.Number)
                {
                    // Convertir les nombres en chaînes
                    return property.GetRawText();
                }
                else
                {
                    return string.Empty;
                }
            }
            
            // Si le nom de propriété alternatif est spécifié et différent du principal, essayer avec celui-ci
            if (!string.IsNullOrEmpty(alternatePropertyName) && alternatePropertyName != primaryPropertyName && 
                element.TryGetProperty(alternatePropertyName, out property))
            {
                if (property.ValueKind == JsonValueKind.String)
                {
                    return property.GetString();
                }
                else if (property.ValueKind == JsonValueKind.Null)
                {
                    return string.Empty;
                }
                else if (property.ValueKind == JsonValueKind.Number)
                {
                    // Convertir les nombres en chaînes
                    return property.GetRawText();
                }
                else
                {
                    return string.Empty;
                }
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// Récupère toutes les propriétés d'un élément JSON pour faciliter le débogage
        /// </summary>
        private List<string> GetAllPropertyNames(JsonElement element)
        {
            var propertyNames = new List<string>();
            
            try
            {
                foreach (var property in element.EnumerateObject())
                {
                    propertyNames.Add(property.Name);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'énumération des propriétés: {ex.Message}");
            }
            
            return propertyNames;
        }
        
        /// <summary>
        /// Récupère un tableau de valeurs ou une valeur unique à partir d'un élément JSON
        /// </summary>
        private List<string> GetArrayOrSingleValue(JsonElement element, string[] propertyNames)
        {
            var values = new List<string>();
            
            // Essayer chaque nom de propriété dans l'ordre
            foreach (var propertyName in propertyNames)
            {
                if (element.TryGetProperty(propertyName, out JsonElement property))
                {
                    if (property.ValueKind == JsonValueKind.Array)
                    {
                        // C'est un tableau, récupérer toutes les valeurs
                        foreach (var item in property.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                values.Add(item.GetString());
                            }
                        }
                        
                        // Si on a trouvé des valeurs, on arrête là
                        if (values.Count > 0)
                        {
                            break;
                        }
                    }
                    else if (property.ValueKind == JsonValueKind.String)
                    {
                        // C'est une chaîne simple
                        values.Add(property.GetString());
                        break;
                    }
                }
            }
            
            return values;
        }
    }
}
