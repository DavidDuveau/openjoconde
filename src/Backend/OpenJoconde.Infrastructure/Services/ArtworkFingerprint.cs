using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Empreinte du contenu d'une notice telle que l'import l'écrit : champs de l'œuvre, musée, auteur, domaines,
    /// techniques et périodes. Une notice dont l'empreinte n'a pas changé depuis la dernière synchronisation n'est pas réécrite.
    /// </summary>
    public static class ArtworkFingerprint
    {
        /// <summary>
        /// À incrémenter dès que le mappage d'une notice change ce qu'il écrit en base : toutes les œuvres
        /// sont alors réécrites une fois à la synchronisation suivante.
        /// </summary>
        private const string MappingVersion = "2";

        public const int Length = 16;

        public static byte[] Compute(Artwork artwork, Artist? author)
        {
            var text = new StringBuilder(MappingVersion);

            void Add(object? value) => text.Append('\u001F').Append(value);

            void AddNames(IEnumerable<string> names, char kind)
            {
                text.Append('\u001E').Append(kind);
                foreach (var name in names.OrderBy(n => n, StringComparer.Ordinal))
                    Add(name);
            }

            Add(artwork.InventoryNumber);
            Add(artwork.Denomination);
            Add(artwork.Title);
            Add(artwork.Description);
            Add(artwork.Dimensions);
            Add(artwork.CreationDate);
            Add(artwork.CreationPlace);
            Add(artwork.ConservationPlace);
            Add(artwork.Copyright);
            Add(artwork.ImageUrl);
            Add(artwork.AssociatedSiteUrl);
            Add(artwork.ArtistUnderCopyright);
            Add(artwork.PublicDomainYear);
            Add(artwork.HasImage);

            var museum = artwork.Museum;
            Add(museum?.Name);
            Add(museum?.Code);
            Add(museum?.City);
            Add(museum?.Department);
            Add(museum?.Region);
            Add(museum?.Latitude);
            Add(museum?.Longitude);

            if (author != null)
            {
                Add(author.LastName);
                Add(author.FirstName);
                Add(author.BirthDate);
                Add(author.DeathDate);
                Add(author.Nationality);
                foreach (var role in artwork.Artists.Select(a => a.Role).OrderBy(r => r, StringComparer.Ordinal))
                    Add(role);
            }

            AddNames(artwork.Domains.Select(d => d.Name), 'D');
            AddNames(artwork.Techniques.Select(t => t.Name), 'T');
            AddNames(artwork.Periods.Select(p => p.Name), 'P');

            return SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())).AsSpan(0, Length).ToArray();
        }
    }
}
