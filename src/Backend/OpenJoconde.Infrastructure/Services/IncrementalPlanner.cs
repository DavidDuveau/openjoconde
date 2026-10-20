using OpenJoconde.Core.Models;
using OpenJoconde.Core.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Résultat de la comparaison entre le fichier source et la base
    /// </summary>
    public sealed class IncrementalPlan
    {
        /// <summary>
        /// Œuvres nouvelles ou modifiées, avec les seules entités de référence (artistes, domaines, techniques,
        /// périodes, musées) qu'elles utilisent
        /// </summary>
        public required ParsingResult Changed { get; init; }

        /// <summary>
        /// Nombre d'œuvres de la source déjà à jour en base
        /// </summary>
        public int Unchanged { get; init; }

        /// <summary>
        /// Références présentes en base mais plus dans la source
        /// </summary>
        public required IReadOnlyList<string> Missing { get; init; }
    }

    /// <summary>
    /// Compare les notices du fichier aux empreintes de la base pour ne garder que ce qui doit être écrit
    /// </summary>
    public static class IncrementalPlanner
    {
        /// <param name="source">Résultat complet du parsing</param>
        /// <param name="existing">Référence et empreinte de chaque œuvre en base</param>
        public static IncrementalPlan Plan(ParsingResult source, IReadOnlyDictionary<string, Guid?> existing)
        {
            var changed = new List<Artwork>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var unchanged = 0;

            foreach (var artwork in source.Artworks)
            {
                seen.Add(artwork.Reference);

                var upToDate = artwork.ContentHash != null
                    && existing.TryGetValue(artwork.Reference, out var storedHash)
                    && storedHash.HasValue
                    && storedHash.Value == new Guid(artwork.ContentHash);

                if (upToDate)
                    unchanged++;
                else
                    changed.Add(artwork);
            }

            var artistIds = changed.SelectMany(a => a.Artists).Select(link => link.ArtistId).ToHashSet();
            var domains = changed.SelectMany(a => a.Domains).ToHashSet();
            var techniques = changed.SelectMany(a => a.Techniques).ToHashSet();
            var periods = changed.SelectMany(a => a.Periods).ToHashSet();
            var museums = changed.Where(a => a.Museum != null).Select(a => a.Museum!).ToHashSet();

            return new IncrementalPlan
            {
                Changed = new ParsingResult
                {
                    Artworks = changed,
                    Artists = source.Artists.Where(a => artistIds.Contains(a.Id)).ToList(),
                    Domains = source.Domains.Where(domains.Contains).ToList(),
                    Techniques = source.Techniques.Where(techniques.Contains).ToList(),
                    Periods = source.Periods.Where(periods.Contains).ToList(),
                    Museums = source.Museums.Where(museums.Contains).ToList(),
                    RejectedRecords = source.RejectedRecords
                },
                Unchanged = unchanged,
                Missing = existing.Keys.Where(reference => !seen.Contains(reference)).ToList()
            };
        }
    }
}
