using Microsoft.EntityFrameworkCore;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Tri alphabétique des œuvres par titre, sans tenir compte de la ponctuation initiale ; les œuvres sans titre
    /// viennent en dernier, dans les deux sens. Le titre égal est départagé par l'identifiant : la pagination reste stable.
    /// </summary>
    public static class ArtworkOrdering
    {
        /// <param name="restricted">
        /// Avec une recherche ou un filtre, le tri porte sur lower(clé), sans index : sinon PostgreSQL parcourt tout
        /// l'index du titre en filtrant au fil de l'eau, 5 s pour un terme rare. Le résultat restreint est court.
        /// </param>
        /// <param name="descending">Z-A au lieu de A-Z (sans index : tri de toute la table sans restriction)</param>
        public static IOrderedQueryable<Artwork> ByTitle(IQueryable<Artwork> query, bool restricted, bool descending = false)
        {
            if (descending)
                return query
                    .OrderBy(a => EF.Property<string>(a, OpenJocondeDbContext.TitleSort) == null)
                    .ThenByDescending(a => EF.Property<string>(a, OpenJocondeDbContext.TitleSort).ToLower())
                    .ThenByDescending(a => a.Id);

            return restricted
                ? query.OrderBy(a => EF.Property<string>(a, OpenJocondeDbContext.TitleSort).ToLower()).ThenBy(a => a.Id)
                : query.OrderBy(a => EF.Property<string>(a, OpenJocondeDbContext.TitleSort)).ThenBy(a => a.Id);
        }
    }
}
