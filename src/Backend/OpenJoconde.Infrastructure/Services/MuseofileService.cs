using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;
using System.Text;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Enrichit les musées avec le fichier Muséofile. Muséofile fait foi pour ce que Joconde ne donne pas (adresse, code
    /// postal, téléphone, site, histoire) ; département, région et coordonnées de Joconde sont gardés, Muséofile ne les
    /// complète que s'ils manquent. La ville n'est jamais touchée : avec le nom, elle sert à l'import Joconde à retrouver
    /// un musée, et la changer en ferait recréer un doublon à la synchronisation suivante. Une valeur absente du fichier
    /// n'efface jamais celle de la base.
    /// </summary>
    public sealed class MuseofileService : IMuseofileService
    {
        private const char Delimiter = '|';
        private const int PhoneMax = 50;
        private const int ZipMax = 20;
        private const int WebsiteMax = 500;
        private const int NameMax = 200;

        private readonly OpenJocondeDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<MuseofileService> _logger;

        public MuseofileService(OpenJocondeDbContext context, HttpClient httpClient, ILogger<MuseofileService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<MuseofileReport> EnrichFromUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            var path = Path.Combine(Path.GetTempPath(), $"museofile_{Guid.NewGuid():N}.csv");
            try
            {
                using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
                    await using var destination = File.Create(path);
                    await source.CopyToAsync(destination, cancellationToken);
                }

                return await EnrichFromFileAsync(path, cancellationToken);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        public async Task<MuseofileReport> EnrichFromFileAsync(string csvFilePath, CancellationToken cancellationToken = default)
        {
            var entries = await ReadEntriesAsync(csvFilePath, cancellationToken);

            var museums = await _context.Museums.Where(m => m.Code != null).ToListAsync(cancellationToken);
            int matched = 0, updated = 0;
            foreach (var museum in museums)
            {
                if (!entries.TryGetValue(CodeKey(museum.Code!), out var entry))
                    continue;

                matched++;
                if (Apply(museum, entry))
                {
                    museum.UpdatedAt = DateTime.UtcNow;
                    updated++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            var report = new MuseofileReport(entries.Count, matched, updated, await _context.Museums.CountAsync(cancellationToken) - matched);
            _logger.LogInformation(
                "Muséofile : {Entries} musées lus, {Matched} retrouvés, {Updated} mis à jour, {Unmatched} sans correspondance",
                report.Entries, report.Matched, report.Updated, report.Unmatched);
            return report;
        }

        private static string CodeKey(string code) => code.Trim().ToUpperInvariant();

        private static async Task<Dictionary<string, MuseofileEntry>> ReadEntriesAsync(string path, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var csv = new CsvRowReader(reader, Delimiter);

            var header = await csv.ReadRowAsync(cancellationToken)
                ?? throw new InvalidDataException("Le fichier Muséofile est vide");
            var columns = header
                .Select((name, index) => (name: name.Trim().ToLowerInvariant(), index))
                .GroupBy(c => c.name)
                .ToDictionary(g => g.Key, g => g.First().index);
            if (!columns.ContainsKey("identifiant"))
                throw new InvalidDataException("Le fichier Muséofile n'a pas de colonne « Identifiant »");

            var entries = new Dictionary<string, MuseofileEntry>();
            List<string>? row;
            while ((row = await csv.ReadRowAsync(cancellationToken)) != null)
            {
                string? Get(string column) =>
                    columns.TryGetValue(column, out var index) && index < row.Count && !string.IsNullOrWhiteSpace(row[index])
                        ? row[index].Trim()
                        : null;

                var code = Get("identifiant");
                if (code == null)
                    continue;

                double? latitude = null, longitude = null;
                if (JocondeRecordMapper.TryParseLatLon(Get("coordonnees"), out var lat, out var lon))
                {
                    latitude = lat;
                    longitude = lon;
                }

                // En cas de doublon de code, la première ligne l'emporte
                entries.TryAdd(CodeKey(code), new MuseofileEntry(
                    Address: Get("adresse"),
                    ZipCode: Cut(Get("code_postal"), ZipMax),
                    Department: Cut(Get("departement"), NameMax),
                    Region: Get("region"),
                    Phone: Cut(Get("telephone"), PhoneMax),
                    Website: WebsiteUrl(Get("url")),
                    History: Get("histoire"),
                    Latitude: latitude,
                    Longitude: longitude));
            }

            return entries;
        }

        /// <summary>
        /// Adresse de site affichable : Muséofile la donne le plus souvent sans schéma (« www.musee.fr/ »), parfois avec
        /// une espace en fin. Seules les adresses http et https sortent.
        /// </summary>
        internal static string? WebsiteUrl(string? raw)
        {
            var url = raw?.Trim();
            if (string.IsNullOrEmpty(url) || url.Any(char.IsWhiteSpace))
                return null;
            if (!url.Contains("://", StringComparison.Ordinal))
                url = "https://" + url;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return null;
            return url.Length <= WebsiteMax ? url : null;
        }

        /// <summary>
        /// Coupe sur une séparation entre deux valeurs (« 03 88 00 38 39 / 03 88 00 38 40 ») plutôt qu'au milieu d'une
        /// valeur : un numéro de téléphone tronqué est faux.
        /// </summary>
        internal static string? Cut(string? value, int max)
        {
            if (value == null || value.Length <= max)
                return value;
            var cut = value[..max];
            var separator = cut.LastIndexOfAny(new[] { ';', '/', ',', ' ' });
            return separator > 0 ? cut[..separator].TrimEnd(' ', ';', '/', ',') : cut;
        }

        /// <summary>
        /// Reporte l'entrée sur le musée ; vrai si un champ a changé
        /// </summary>
        private static bool Apply(Museum museum, MuseofileEntry entry)
        {
            var changed = false;

            void Set(string? current, string? value, Action<string> assign)
            {
                if (value != null && value != current)
                {
                    assign(value);
                    changed = true;
                }
            }

            Set(museum.Address, entry.Address, v => museum.Address = v);
            Set(museum.ZipCode, entry.ZipCode, v => museum.ZipCode = v);
            Set(museum.Phone, entry.Phone, v => museum.Phone = v);
            Set(museum.Website, entry.Website, v => museum.Website = v);
            Set(museum.Description, entry.History, v => museum.Description = v);

            // Joconde fait foi pour la localisation : Muséofile ne comble que les manques
            if (string.IsNullOrWhiteSpace(museum.Department)) Set(museum.Department, entry.Department, v => museum.Department = v);
            if (string.IsNullOrWhiteSpace(museum.Region)) Set(museum.Region, entry.Region, v => museum.Region = v);
            if (museum.Latitude == null && museum.Longitude == null && entry.Latitude != null && entry.Longitude != null)
            {
                museum.Latitude = entry.Latitude;
                museum.Longitude = entry.Longitude;
                changed = true;
            }

            return changed;
        }

        private sealed record MuseofileEntry(
            string? Address, string? ZipCode, string? Department, string? Region,
            string? Phone, string? Website, string? History, double? Latitude, double? Longitude);
    }
}
