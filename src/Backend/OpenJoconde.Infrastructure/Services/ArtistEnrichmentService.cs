using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Configuration;
using OpenJoconde.Infrastructure.Data;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Complète les artistes avec Wikidata et Wikipédia. Candidats : artistes d'année de naissance connue, sans
    /// biographie, jamais examinés, ayant assez d'œuvres ; les plus fournis d'abord. Un rapprochement n'est retenu que
    /// s'il est unique et sûr (voir <see cref="ArtistMatcher"/>). Un artiste sans correspondance est marqué examiné et
    /// n'est pas redemandé ; une panne de Wikimedia le laisse pour plus tard.
    /// </summary>
    public sealed class ArtistEnrichmentService : IArtistEnrichmentService
    {
        /// <summary>Pannes consécutives après lesquelles le lot est interrompu : Wikimedia est probablement hors d'atteinte</summary>
        private const int MaxConsecutiveFailures = 5;

        private readonly OpenJocondeDbContext _context;
        private readonly IWikidataClient _client;
        private readonly EnrichmentOptions _options;
        private readonly ILogger<ArtistEnrichmentService> _logger;

        public ArtistEnrichmentService(
            OpenJocondeDbContext context,
            IWikidataClient client,
            IOptions<EnrichmentOptions> options,
            ILogger<ArtistEnrichmentService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ArtistEnrichmentReport> EnrichBatchAsync(CancellationToken cancellationToken = default)
        {
            var artists = await _context.Artists
                .Where(a => a.Biography == null
                    && a.EnrichmentAttemptedAt == null
                    && a.BirthDate != null && Regex.IsMatch(a.BirthDate, "^[0-9]{4}$")
                    && a.Artworks.Count >= _options.MinimumArtworks)
                .OrderByDescending(a => a.Artworks.Count)
                .ThenBy(a => a.Id)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);

            int examined = 0, enriched = 0, failed = 0, consecutiveFailures = 0;
            var aborted = false;

            foreach (var artist in artists)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (await TryEnrichAsync(artist, cancellationToken))
                        enriched++;
                    examined++;
                    consecutiveFailures = 0;
                    artist.EnrichmentAttemptedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException
                                           && !cancellationToken.IsCancellationRequested)
                {
                    failed++;
                    _logger.LogWarning(ex, "Enrichissement de l'artiste {ArtistId} reporté : Wikimedia n'a pas répondu", artist.Id);
                    // Ce que le rapprochement a pu écrire avant la panne n'est pas gardé : l'artiste sera repris entier
                    _context.Entry(artist).State = EntityState.Unchanged;
                    await _context.Entry(artist).ReloadAsync(cancellationToken);
                    if (++consecutiveFailures >= MaxConsecutiveFailures)
                    {
                        aborted = true;
                        _logger.LogWarning("Enrichissement des artistes interrompu après {Count} pannes consécutives", consecutiveFailures);
                        break;
                    }
                }

                if (_options.PauseBetweenArtistsMilliseconds > 0)
                    await Task.Delay(_options.PauseBetweenArtistsMilliseconds, cancellationToken);
            }

            _logger.LogInformation("Enrichissement des artistes : {Examined} examinés, {Enriched} enrichis, {Failed} reportés", examined, enriched, failed);
            return new ArtistEnrichmentReport(examined, enriched, failed, aborted);
        }

        /// <summary>Vrai si l'artiste a été rapproché de façon sûre et complété</summary>
        private async Task<bool> TryEnrichAsync(Artist artist, CancellationToken cancellationToken)
        {
            var name = ArtistMatcher.FullName(artist.FirstName, artist.LastName);
            if (name == null || !int.TryParse(artist.BirthDate, out var birthYear))
                return false;
            var candidate = new ArtistCandidate(name, birthYear, int.TryParse(artist.DeathDate, out var death) && artist.DeathDate!.Length == 4 ? death : null);

            var ids = await _client.SearchAsync(name, cancellationToken);
            if (ids.Count == 0)
                return false;

            var entities = await _client.GetEntitiesAsync(ids, cancellationToken);
            var matches = entities.Values.Where(e => ArtistMatcher.Matches(e, candidate)).ToList();
            if (matches.Count != 1)
                return false;

            var entity = matches[0];
            var summary = await _client.GetSummaryAsync(entity.FrenchWikipediaTitle!, cancellationToken);
            if (summary == null || !ArtistMatcher.IsPersonSummary(summary))
                return false;

            var linked = await _client.GetEntitiesAsync(entity.CitizenshipIds.Concat(entity.MovementIds).ToList(), cancellationToken);
            string? LabelOf(IEnumerable<string> linkedIds) =>
                linkedIds.Select(id => linked.TryGetValue(id, out var e) ? e.Label : null).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));

            artist.WikidataId = entity.Id;
            artist.WikipediaUrl = "https://fr.wikipedia.org/wiki/" + Uri.EscapeDataString(entity.FrenchWikipediaTitle!.Replace(' ', '_'));
            artist.Biography = summary;
            artist.Nationality ??= LabelOf(entity.CitizenshipIds);
            artist.Movement = LabelOf(entity.MovementIds);
            return true;
        }
    }
}
