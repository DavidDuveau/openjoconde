using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
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

        /// <summary>
        /// Par date de création (première année lue dans le texte libre) ; les œuvres sans date lisible (« néolithique »)
        /// viennent en dernier, dans les deux sens. Le plus ancien d'abord par défaut, sur l'index.
        /// </summary>
        public static IOrderedQueryable<Artwork> ByDate(IQueryable<Artwork> query, bool descending = false)
        {
            if (descending)
                return query
                    .OrderBy(a => EF.Property<int?>(a, OpenJocondeDbContext.CreationYear) == null)
                    .ThenByDescending(a => EF.Property<int?>(a, OpenJocondeDbContext.CreationYear))
                    .ThenBy(a => a.Id);

            return query.OrderBy(a => EF.Property<int?>(a, OpenJocondeDbContext.CreationYear)).ThenBy(a => a.Id);
        }

        /// <summary>
        /// Par pertinence de la recherche plein texte (ts_rank sur titre et description), la plus forte d'abord
        /// </summary>
        /// <param name="tsQueryCondition">Condition to_tsquery de la recherche (<see cref="ArtworkSearch.BuildCondition"/>)</param>
        public static IOrderedQueryable<Artwork> ByRelevance(IQueryable<Artwork> query, string tsQueryCondition) =>
            query
                .OrderByDescending(a => EF.Property<NpgsqlTsVector>(a, OpenJocondeDbContext.SearchVector)
                    .Rank(EF.Functions.ToTsQuery("french_unaccent", tsQueryCondition)))
                .ThenBy(a => a.Id);

        /// <summary>
        /// Par nom du premier artiste, les œuvres sans artiste en dernier. Une jointure par œuvre : à réserver aux
        /// résultats déjà restreints par une recherche ou un filtre.
        /// </summary>
        public static IOrderedQueryable<Artwork> ByArtist(IQueryable<Artwork> query) =>
            query
                .OrderBy(a => !a.Artists.Any())
                .ThenBy(a => a.Artists.Select(x => x.Artist.LastName.ToLower()).Min())
                .ThenBy(a => a.Artists.Select(x => x.Artist.FirstName!.ToLower()).Min())
                .ThenBy(a => a.Id);
    }
}
