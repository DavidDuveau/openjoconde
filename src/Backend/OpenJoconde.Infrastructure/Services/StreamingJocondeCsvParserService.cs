using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Parse l'export CSV de la base Joconde (data.gouv.fr) : séparateur « | », valeurs entre guillemets
    /// (guillemet doublé pour échapper), une ligne d'en-tête dont les noms sont ceux de l'export.
    /// Le fichier est lu en flux ; les entités produites restent accumulées en mémoire dans le <see cref="ParsingResult"/>.
    /// </summary>
    public class StreamingJocondeCsvParserService : IJocondeCsvParser
    {
        private const char Delimiter = '|';
        private const int BufferSize = 65536;
        private const int ProgressInterval = 10000;
        private const int EstimatedBytesPerArtwork = 1700;

        private readonly ILogger<StreamingJocondeCsvParserService> _logger;

        public StreamingJocondeCsvParserService(ILogger<StreamingJocondeCsvParserService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ParsingResult> ParseAsync(
            string csvFilePath,
            Action<int, int>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Début du parsing CSV: {FilePath}", csvFilePath);

            if (string.IsNullOrEmpty(csvFilePath))
                throw new ArgumentException("Le chemin du fichier CSV ne peut pas être vide", nameof(csvFilePath));

            if (!File.Exists(csvFilePath))
                throw new FileNotFoundException("Le fichier CSV n'existe pas", csvFilePath);

            int estimatedItems = (int)(new FileInfo(csvFilePath).Length / EstimatedBytesPerArtwork);
            var mapper = new JocondeRecordMapper();
            int processedItems = 0;
            int rejectedItems = 0;

            try
            {
                using var reader = new StreamReader(csvFilePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, BufferSize);
                var csv = new CsvRowReader(reader, Delimiter);

                var header = await csv.ReadRowAsync(cancellationToken);
                if (header == null)
                    throw new FormatException("Le fichier CSV est vide");

                var columns = new Dictionary<string, int>(header.Count);
                for (int i = 0; i < header.Count; i++)
                    columns[header[i].Trim().ToLowerInvariant()] = i;

                if (!columns.ContainsKey("reference"))
                    throw new FormatException("La colonne 'Reference' est absente de l'en-tête du fichier CSV");

                List<string>? row;
                while ((row = await csv.ReadRowAsync(cancellationToken)) != null)
                {
                    processedItems++;

                    try
                    {
                        if (row.Count != header.Count)
                            throw new FormatException($"{row.Count} valeurs au lieu de {header.Count}");

                        mapper.Add(new CsvJocondeRecord(columns, row));
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        rejectedItems++;
                        _logger.LogWarning("Ligne {Index} rejetée: {Message}", processedItems, ex.Message);
                    }

                    if (processedItems % ProgressInterval == 0)
                    {
                        progressCallback?.Invoke(processedItems, Math.Max(estimatedItems, processedItems));
                    }
                }

                progressCallback?.Invoke(processedItems, processedItems);

                var result = mapper.ToResult();
                _logger.LogInformation(
                    "Parsing CSV terminé: {ArtworksCount} œuvres ({RejectedCount} lignes rejetées), {ArtistsCount} artistes, {DomainsCount} domaines, {TechniquesCount} techniques, {PeriodsCount} périodes, {MuseumsCount} musées",
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
                _logger.LogWarning("Parsing annulé après {ProcessedCount} lignes", processedItems);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du parsing du fichier CSV: {Message}", ex.Message);
                throw new Exception($"Erreur lors du parsing du fichier CSV: {ex.Message}", ex);
            }
        }

        /// <summary>Notice Joconde lue depuis une ligne CSV.</summary>
        private sealed class CsvJocondeRecord : IJocondeRecord
        {
            private readonly Dictionary<string, int> _columns;
            private readonly List<string> _values;

            public CsvJocondeRecord(Dictionary<string, int> columns, List<string> values)
            {
                _columns = columns;
                _values = values;
            }

            public string? GetString(string field)
            {
                if (!_columns.TryGetValue(field, out var index))
                    return null;

                var value = _values[index];
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }

            public List<string> GetValues(string field) => JocondeRecordMapper.SplitValues(GetString(field));

            public bool TryGetCoordinates(out double latitude, out double longitude) =>
                JocondeRecordMapper.TryParseLatLon(GetString("coordonnees"), out latitude, out longitude);
        }

        /// <summary>
        /// Lit les lignes d'un CSV : valeurs entre guillemets (guillemet doublé = guillemet littéral),
        /// retours à la ligne autorisés à l'intérieur des guillemets.
        /// </summary>
        private sealed class CsvRowReader
        {
            private readonly TextReader _reader;
            private readonly char _delimiter;
            private readonly char[] _buffer = new char[BufferSize];
            private int _length;
            private int _position;

            public CsvRowReader(TextReader reader, char delimiter)
            {
                _reader = reader;
                _delimiter = delimiter;
            }

            /// <summary>Ligne suivante, ou null en fin de fichier. Les lignes vides sont ignorées.</summary>
            public async Task<List<string>?> ReadRowAsync(CancellationToken cancellationToken)
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var row = new List<string>();
                    var field = new StringBuilder();
                    bool inQuotes = false;
                    bool wasQuoted = false;
                    bool anyChar = false;

                    while (true)
                    {
                        int c = await ReadCharAsync(cancellationToken);

                        if (c < 0)
                        {
                            if (!anyChar)
                                return null;

                            row.Add(field.ToString());
                            return row;
                        }

                        anyChar = true;

                        if (inQuotes)
                        {
                            if (c == '"')
                            {
                                if (await PeekCharAsync(cancellationToken) == '"')
                                {
                                    _position++;
                                    field.Append('"');
                                }
                                else
                                {
                                    inQuotes = false;
                                }
                            }
                            else
                            {
                                field.Append((char)c);
                            }
                        }
                        else if (c == '"' && field.Length == 0 && !wasQuoted)
                        {
                            inQuotes = true;
                            wasQuoted = true;
                        }
                        else if (c == _delimiter)
                        {
                            row.Add(field.ToString());
                            field.Clear();
                            wasQuoted = false;
                        }
                        else if (c == '\n' || c == '\r')
                        {
                            if (c == '\r' && await PeekCharAsync(cancellationToken) == '\n')
                                _position++;

                            // Ligne vide : on passe à la suivante
                            if (row.Count == 0 && field.Length == 0 && !wasQuoted)
                                break;

                            row.Add(field.ToString());
                            return row;
                        }
                        else
                        {
                            field.Append((char)c);
                        }
                    }
                }
            }

            private async ValueTask<int> ReadCharAsync(CancellationToken cancellationToken)
            {
                if (!await FillBufferAsync(cancellationToken))
                    return -1;

                return _buffer[_position++];
            }

            private async ValueTask<int> PeekCharAsync(CancellationToken cancellationToken)
            {
                if (!await FillBufferAsync(cancellationToken))
                    return -1;

                return _buffer[_position];
            }

            private async ValueTask<bool> FillBufferAsync(CancellationToken cancellationToken)
            {
                if (_position < _length)
                    return true;

                _length = await _reader.ReadAsync(_buffer.AsMemory(), cancellationToken);
                _position = 0;
                return _length > 0;
            }
        }
    }
}
