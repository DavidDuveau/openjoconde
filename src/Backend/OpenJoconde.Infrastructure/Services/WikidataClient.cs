using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Infrastructure.Configuration;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Client de l'API Wikidata (recherche et lecture d'entités) et de l'API REST de Wikipédia en français (résumés). Se
    /// présente avec un contact, comme la politique d'usage de Wikimedia le demande.
    /// </summary>
    public sealed class WikidataClient : IWikidataClient
    {
        private const string WikidataApi = "https://www.wikidata.org/w/api.php";
        private const string WikipediaSummary = "https://fr.wikipedia.org/api/rest_v1/page/summary/";
        private const int MaxIdsPerRequest = 50;

        private readonly HttpClient _httpClient;

        public WikidataClient(HttpClient httpClient, IOptions<EnrichmentOptions> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("openjoconde", "1.0"));
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue($"({options.Value.ContactUrl})"));
        }

        public async Task<IReadOnlyList<string>> SearchAsync(string name, CancellationToken cancellationToken = default)
        {
            var url = $"{WikidataApi}?action=wbsearchentities&search={Uri.EscapeDataString(name)}&language=fr&uselang=fr&limit=5&type=item&format=json";
            using var document = await GetJsonAsync(url, cancellationToken);
            if (!document.RootElement.TryGetProperty("search", out var results))
                return Array.Empty<string>();
            return results.EnumerateArray()
                .Select(r => r.TryGetProperty("id", out var id) ? id.GetString() : null)
                .OfType<string>()
                .ToList();
        }

        public async Task<IReadOnlyDictionary<string, WikidataEntity>> GetEntitiesAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default)
        {
            var entities = new Dictionary<string, WikidataEntity>();
            foreach (var batch in ids.Distinct().Chunk(MaxIdsPerRequest))
            {
                var url = $"{WikidataApi}?action=wbgetentities&ids={string.Join("%7C", batch)}&props=claims%7Csitelinks%7Clabels%7Caliases&languages=fr&format=json";
                using var document = await GetJsonAsync(url, cancellationToken);
                if (!document.RootElement.TryGetProperty("entities", out var found))
                    continue;
                foreach (var property in found.EnumerateObject())
                    if (!property.Value.TryGetProperty("missing", out _))
                        entities[property.Name] = Parse(property.Name, property.Value);
            }
            return entities;
        }

        public async Task<string?> GetSummaryAsync(string title, CancellationToken cancellationToken = default)
        {
            var url = WikipediaSummary + Uri.EscapeDataString(title.Replace(' ', '_'));
            using var document = await GetJsonAsync(url, cancellationToken);
            var extract = document.RootElement.TryGetProperty("extract", out var value) ? value.GetString() : null;
            return string.IsNullOrWhiteSpace(extract) ? null : extract.Trim();
        }

        private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        }

        private static WikidataEntity Parse(string id, JsonElement entity)
        {
            string? label = entity.TryGetProperty("labels", out var labels) && labels.TryGetProperty("fr", out var fr)
                ? fr.GetProperty("value").GetString() : null;

            var aliases = entity.TryGetProperty("aliases", out var all) && all.TryGetProperty("fr", out var frAliases)
                ? frAliases.EnumerateArray().Select(a => a.GetProperty("value").GetString()).OfType<string>().ToList()
                : new List<string>();

            string? title = entity.TryGetProperty("sitelinks", out var links) && links.TryGetProperty("frwiki", out var frwiki)
                ? frwiki.GetProperty("title").GetString() : null;

            return new WikidataEntity(
                id, label, aliases,
                IsHuman: Claims(entity, "P31", "id").Contains("Q5"),
                BirthYears: Claims(entity, "P569", "time").Select(ArtistMatcher.YearOf).OfType<int>().ToList(),
                DeathYears: Claims(entity, "P570", "time").Select(ArtistMatcher.YearOf).OfType<int>().ToList(),
                FrenchWikipediaTitle: title,
                CitizenshipIds: Claims(entity, "P27", "id"),
                MovementIds: Claims(entity, "P135", "id"));
        }

        /// <summary>Valeurs d'une propriété (« id » pour une entité liée, « time » pour une date) ; les déclarations sans valeur sont ignorées</summary>
        private static List<string> Claims(JsonElement entity, string property, string field)
        {
            var values = new List<string>();
            if (!entity.TryGetProperty("claims", out var claims) || !claims.TryGetProperty(property, out var statements))
                return values;
            foreach (var statement in statements.EnumerateArray())
            {
                if (statement.TryGetProperty("mainsnak", out var snak)
                    && snak.TryGetProperty("datavalue", out var data)
                    && data.TryGetProperty("value", out var value)
                    && value.ValueKind == JsonValueKind.Object
                    && value.TryGetProperty(field, out var text)
                    && text.GetString() is { } s)
                    values.Add(s);
            }
            return values;
        }
    }
}
