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
                    int firstChar = await streamReader.ReadAsync();
                    if (firstChar != '[')
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
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow // Ajouter la date de création
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
                        FirstName = "",
                        BirthDate = "",
                        DeathDate = "",
                        Nationality = "",
                        Biography = "",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
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
                        Name = domainName,
                        Description = "",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
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
                        Name = techniqueName,
                        Description = "",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
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
                        Name = periodName,
                        Description = "",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
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
                        Name = museumName,
                        City = "",
                        Department = "",
                        Address = "",
                        ZipCode = "",
                        Phone = "",
                        Email = "",
                        Website = "",
                        Description = "",
                        Region = "",
                        Code = "",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
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
