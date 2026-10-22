using OpenJoconde.Core.Models;
using OpenJoconde.Core.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Compare, lot par lot, les notices du fichier aux empreintes de la base pour ne garder que ce qui doit être écrit.
    /// Une instance par fichier lu.
    /// </summary>
    public sealed class IncrementalFilter
    {
        private readonly IReadOnlyDictionary<string, Guid?> _existing;
        private readonly HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);

        // Entités rencontrées mais pas encore rendues : une entité n'est écrite que si une œuvre à écrire l'utilise
        private readonly Dictionary<Guid, Artist> _pendingArtists = new();
        private readonly HashSet<Domain> _pendingDomains = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<Technique> _pendingTechniques = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<Period> _pendingPeriods = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<Museum> _pendingMuseums = new(ReferenceEqualityComparer.Instance);

        /// <param name="existing">Référence et empreinte de chaque œuvre en base</param>
        public IncrementalFilter(IReadOnlyDictionary<string, Guid?> existing)
        {
            _existing = existing;
        }

        /// <summary>
        /// Nombre d'œuvres de la source déjà à jour en base
        /// </summary>
        public int Unchanged { get; private set; }

        /// <summary>
        /// Garde les œuvres nouvelles ou modifiées du lot, avec les seules entités de référence (artistes, domaines,
        /// techniques, périodes, musées) qu'elles utilisent et qu'un lot précédent n'a pas déjà rendues
        /// </summary>
        public ParsingResult Filter(ParsingResult batch)
        {
            foreach (var artist in batch.Artists) _pendingArtists[artist.Id] = artist;
            _pendingDomains.UnionWith(batch.Domains);
            _pendingTechniques.UnionWith(batch.Techniques);
            _pendingPeriods.UnionWith(batch.Periods);
            _pendingMuseums.UnionWith(batch.Museums);

            var changed = new List<Artwork>();

            foreach (var artwork in batch.Artworks)
            {
                _seen.Add(artwork.Reference);

                var upToDate = artwork.ContentHash != null
                    && _existing.TryGetValue(artwork.Reference, out var storedHash)
                    && storedHash.HasValue
                    && storedHash.Value == new Guid(artwork.ContentHash);

                if (upToDate)
                    Unchanged++;
                else
                    changed.Add(artwork);
            }

            return new ParsingResult
            {
                Artworks = changed,
                Artists = changed.SelectMany(a => a.Artists).Select(link => link.ArtistId)
                    .Select(id => _pendingArtists.Remove(id, out var artist) ? artist : null).OfType<Artist>().ToList(),
                Domains = Release(changed.SelectMany(a => a.Domains), _pendingDomains),
                Techniques = Release(changed.SelectMany(a => a.Techniques), _pendingTechniques),
                Periods = Release(changed.SelectMany(a => a.Periods), _pendingPeriods),
                Museums = Release(changed.Where(a => a.Museum != null).Select(a => a.Museum!), _pendingMuseums)
            };
        }

        /// <summary>
        /// Références présentes en base mais jamais vues dans la source ; à appeler une fois le fichier lu en entier
        /// </summary>
        public IReadOnlyList<string> Missing() => _existing.Keys.Where(reference => !_seen.Contains(reference)).ToList();

        private static List<T> Release<T>(IEnumerable<T> used, HashSet<T> pending) =>
            used.Where(pending.Remove).ToList(); // Remove n'est vrai que la première fois : pas de doublon
    }
}
