using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
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

            var mapper = new JocondeRecordMapper();
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

                        mapper.Add(new JsonJocondeRecord(element));
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

                var result = mapper.ToResult();
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

        /// <summary>Notice Joconde lue depuis un objet JSON.</summary>
        private sealed class JsonJocondeRecord : IJocondeRecord
        {
            private readonly JsonElement _element;

            public JsonJocondeRecord(JsonElement element) => _element = element;

            public string? GetString(string field)
            {
                if (!_element.TryGetProperty(field, out var property))
                    return null;

                var value = property.ValueKind switch
                {
                    JsonValueKind.String => property.GetString(),
                    JsonValueKind.Number => property.GetRawText(),
                    _ => null
                };

                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }

            public List<string> GetValues(string field)
            {
                if (!_element.TryGetProperty(field, out var property))
                    return new List<string>();

                if (property.ValueKind == JsonValueKind.Array)
                {
                    return property.EnumerateArray()
                        .Where(item => item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                        .Select(item => item.GetString()!.Trim())
                        .Distinct()
                        .ToList();
                }

                return JocondeRecordMapper.SplitValues(GetString(field));
            }

            public bool TryGetCoordinates(out double latitude, out double longitude)
            {
                latitude = longitude = 0;
                if (!_element.TryGetProperty("coordonnees", out var coordinates))
                    return false;

                if (coordinates.ValueKind == JsonValueKind.Object)
                {
                    return coordinates.TryGetProperty("lat", out var lat) && lat.TryGetDouble(out latitude)
                        && coordinates.TryGetProperty("lon", out var lon) && lon.TryGetDouble(out longitude);
                }

                return coordinates.ValueKind == JsonValueKind.String
                    && JocondeRecordMapper.TryParseLatLon(coordinates.GetString(), out latitude, out longitude);
            }
        }
    }
}
