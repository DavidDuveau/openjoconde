using Microsoft.EntityFrameworkCore;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Tri alphabétique des œuvres par titre, sans tenir compte de la ponctuation initiale ; les œuvres sans titre
    /// viennent en dernier. Le titre égal est départagé par l'identifiant : la pagination reste stable.
    /// </summary>
    public static class ArtworkOrdering
    {
        /// <param name="searching">
        /// Avec une recherche, le tri porte sur lower(clé), sans index : sinon PostgreSQL parcourt tout l'index du
        /// titre en filtrant au fil de l'eau, 5 s pour un terme rare. Le résultat d'une recherche est court.
        /// </param>
        public static IOrderedQueryable<Artwork> ByTitle(IQueryable<Artwork> query, bool searching) =>
            searching
                ? query.OrderBy(a => EF.Property<string>(a, OpenJocondeDbContext.TitleSort).ToLower()).ThenBy(a => a.Id)
                : query.OrderBy(a => EF.Property<string>(a, OpenJocondeDbContext.TitleSort)).ThenBy(a => a.Id);
    }
}
