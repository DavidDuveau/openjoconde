using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using OpenJoconde.Core.Parsers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Parse un export JSON Joconde dont la racine est un tableau de notices.
    /// Les notices sont lues une à une depuis le flux : le découpage est fait par le lecteur JSON,
    /// donc des accolades dans un texte ne perturbent plus les notices voisines.
    /// Les entités produites restent accumulées en mémoire dans le <see cref="ParsingResult"/>.
    /// </summary>
    public class StreamingJocondeJsonParserService : IJocondeJsonParser
    {
        private const int BufferSize = 16384;
        private const int ProgressInterval = 1000;
        private const int EstimatedBytesPerArtwork = 5000;

        private static readonly Regex ArtistDatesInParentheses = new(@"\(([^)]+)\)", RegexOptions.Compiled);

        private readonly ILogger<StreamingJocondeJsonParserService> _logger;

        public StreamingJocondeJsonParserService(ILogger<StreamingJocondeJsonParserService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Parse un fichier JSON Joconde et extrait toutes les entités
        /// </summary>
        public async Task<ParsingResult> ParseAsync(
            string jsonFilePath,
            Action<int, int>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Début du parsing JSON par streaming: {FilePath}", jsonFilePath);

            if (string.IsNullOrEmpty(jsonFilePath))
                throw new ArgumentException("Le chemin du fichier JSON ne peut pas être vide", nameof(jsonFilePath));

            if (!File.Exists(jsonFilePath))
                throw new FileNotFoundException("Le fichier JSON n'existe pas", jsonFilePath);

            long fileSize = new FileInfo(jsonFilePath).Length;
            int estimatedItems = (int)(fileSize / EstimatedBytesPerArtwork);
            _logger.LogInformation("Taille du fichier: {Size} octets, estimation du nombre d'œuvres: ~{Count}", fileSize, estimatedItems);

            // Dictionnaires pour dédoublonner les entités liées
            var artists = new Dictionary<string, Artist>();
            var domains = new Dictionary<string, Domain>();
            var techniques = new Dictionary<string, Technique>();
            var periods = new Dictionary<string, Period>();
            var museums = new Dictionary<string, Museum>();
            var artworks = new List<Artwork>();

            int processedItems = 0;
            int rejectedItems = 0;

            try
            {
                await using var fileStream = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
                var options = new JsonSerializerOptions { AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip };

                await foreach (var element in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(fileStream, options, cancellationToken))
                {
                    processedItems++;

                    try
                    {
                        if (element.ValueKind != JsonValueKind.Object)
                            throw new FormatException($"Notice {processedItems} : objet JSON attendu, {element.ValueKind} trouvé");

                        var artwork = ExtractArtwork(element);
                        ExtractRelatedEntities(element, artwork, artists, domains, techniques, periods, museums);
                        artworks.Add(artwork);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        rejectedItems++;
                        _logger.LogWarning(ex, "Notice {Index} rejetée: {Message}", processedItems, ex.Message);
                    }

                    if (processedItems % ProgressInterval == 0)
                    {
                        progressCallback?.Invoke(processedItems, Math.Max(estimatedItems, processedItems));
                    }
                }

                progressCallback?.Invoke(processedItems, processedItems);

                var result = new ParsingResult
                {
                    Artworks = artworks,
                    Artists = artists.Values.ToList(),
                    Domains = domains.Values.ToList(),
                    Techniques = techniques.Values.ToList(),
                    Periods = periods.Values.ToList(),
                    Museums = museums.Values.ToList()
                };

                _logger.LogInformation(
                    "Parsing JSON terminé: {ArtworksCount} œuvres ({RejectedCount} notices rejetées), {ArtistsCount} artistes, {DomainsCount} domaines, {TechniquesCount} techniques, {PeriodsCount} périodes, {MuseumsCount} musées",
                    result.Artworks.Count,
                    rejectedItems,
                    result.Artists.Count,
                    result.Domains.Count,
                    result.Techniques.Count,
                    result.Periods.Count,
                    result.Museums.Count);

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Parsing annulé après {ProcessedCount} notices", processedItems);
                throw;
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
        private static Artwork ExtractArtwork(JsonElement element)
        {
            var periods = GetValues(element, "periode_de_creation");
            var creationDate = periods.Count > 0
                ? string.Join(" ; ", periods)
                : GetString(element, "millesime_de_creation") ?? GetString(element, "epoque");

            return new Artwork
            {
                Id = Guid.NewGuid(),
                Reference = GetString(element, "reference") ?? string.Empty,
                InventoryNumber = GetString(element, "numero_inventaire"),
                Denomination = GetString(element, "denomination") ?? "Œuvre sans dénomination",
                Title = GetString(element, "titre"),
                Description = GetString(element, "description"),
                Dimensions = GetString(element, "mesures"),
                CreationDate = creationDate,
                CreationPlace = GetString(element, "lieu_de_creation_utilisation") ?? GetString(element, "ecole_pays"),
                ConservationPlace = GetString(element, "localisation") ?? GetString(element, "lieu_de_depot"),
                // L'export ne fournit ni URL d'image (seulement presence_image) ni texte de droits.
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Extraction des entités liées (artiste, domaines, techniques, périodes, musée)
        /// </summary>
        private static void ExtractRelatedEntities(
            JsonElement element,
            Artwork artwork,
            Dictionary<string, Artist> artists,
            Dictionary<string, Domain> domains,
            Dictionary<string, Technique> techniques,
            Dictionary<string, Period> periods,
            Dictionary<string, Museum> museums)
        {
            var artistName = GetString(element, "auteur");
            if (artistName != null)
            {
                var artistKey = artistName.ToLowerInvariant();
                if (!artists.TryGetValue(artistKey, out var artist))
                {
                    artist = ParseArtistName(artistName);
                    artists[artistKey] = artist;
                }

                artwork.Artists.Add(new ArtworkArtist
                {
                    ArtistId = artist.Id,
                    ArtworkId = artwork.Id,
                    Role = "Créateur"
                });
            }

            foreach (var name in GetValues(element, "domaine"))
            {
                if (!domains.TryGetValue(name.ToLowerInvariant(), out var domain))
                {
                    domain = new Domain { Id = Guid.NewGuid(), Name = name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    domains[name.ToLowerInvariant()] = domain;
                }
                artwork.Domains.Add(domain);
            }

            foreach (var name in GetValues(element, "materiaux_techniques"))
            {
                if (!techniques.TryGetValue(name.ToLowerInvariant(), out var technique))
                {
                    technique = new Technique { Id = Guid.NewGuid(), Name = name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    techniques[name.ToLowerInvariant()] = technique;
                }
                artwork.Techniques.Add(technique);
            }

            var periodNames = GetValues(element, "periode_de_creation");
            if (periodNames.Count == 0)
                periodNames = GetValues(element, "epoque");

            foreach (var name in periodNames)
            {
                if (!periods.TryGetValue(name.ToLowerInvariant(), out var period))
                {
                    period = new Period { Id = Guid.NewGuid(), Name = name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    periods[name.ToLowerInvariant()] = period;
                }
                artwork.Periods.Add(period);
            }

            var museumName = GetString(element, "nom_officiel_musee") ?? GetString(element, "localisation");
            if (museumName != null && !museums.ContainsKey(museumName.ToLowerInvariant()))
            {
                var museum = new Museum
                {
                    Id = Guid.NewGuid(),
                    Name = museumName,
                    Code = GetString(element, "code_museofile"),
                    City = GetString(element, "ville"),
                    Department = GetString(element, "departement"),
                    Region = GetString(element, "region"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (TryGetCoordinates(element, out var latitude, out var longitude))
                {
                    museum.Latitude = latitude;
                    museum.Longitude = longitude;
                }

                museums[museumName.ToLowerInvariant()] = museum;
            }
        }

        /// <summary>
        /// Valeur texte d'une propriété, ou null si elle est absente, vide ou non textuelle.
        /// Les nombres sont convertis en texte.
        /// </summary>
        private static string? GetString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
                return null;

            var value = property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetRawText(),
                _ => null
            };

            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Valeurs d'un champ multivalué : un tableau JSON, ou un texte dont les valeurs sont séparées par « ; ».
        /// </summary>
        private static List<string> GetValues(JsonElement element, string propertyName)
        {
            var values = new List<string>();
            if (!element.TryGetProperty(propertyName, out var property))
                return values;

            if (property.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in property.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                        values.Add(item.GetString()!.Trim());
                }
            }
            else if (property.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(property.GetString()))
            {
                values.AddRange(property.GetString()!.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            return values.Distinct().ToList();
        }

        /// <summary>
        /// Coordonnées du musée : objet { lon, lat } (export JSON) ou texte « lat, lon » (export CSV).
        /// </summary>
        private static bool TryGetCoordinates(JsonElement element, out double latitude, out double longitude)
        {
            latitude = longitude = 0;
            if (!element.TryGetProperty("coordonnees", out var coordinates))
                return false;

            if (coordinates.ValueKind == JsonValueKind.Object)
            {
                return coordinates.TryGetProperty("lat", out var lat) && lat.TryGetDouble(out latitude)
                    && coordinates.TryGetProperty("lon", out var lon) && lon.TryGetDouble(out longitude);
            }

            if (coordinates.ValueKind == JsonValueKind.String)
            {
                var parts = coordinates.GetString()!.Split(',', StringSplitOptions.TrimEntries);
                return parts.Length == 2
                    && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out latitude)
                    && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out longitude);
            }

            return false;
        }

        /// <summary>
        /// Découpe un nom d'auteur Joconde (ex. « DUBREUIL Toussaint », « Charnay Armand (1844-1915) »)
        /// </summary>
        private static Artist ParseArtistName(string fullName)
        {
            var artist = new Artist
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var dateMatch = ArtistDatesInParentheses.Match(fullName);
            if (dateMatch.Success)
            {
                var dateParts = dateMatch.Groups[1].Value.Split('-');
                if (dateParts.Length == 2)
                {
                    artist.BirthDate = dateParts[0].Trim();
                    artist.DeathDate = dateParts[1].Trim();
                }

                fullName = fullName.Replace(dateMatch.Value, "").Trim();
            }

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
    }
}
