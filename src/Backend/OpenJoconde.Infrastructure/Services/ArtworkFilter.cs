using Microsoft.EntityFrameworkCore;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Filtres sur les œuvres : musée, région ou département du musée, artiste, domaine, technique, période. Les filtres posés se cumulent (ET) ; chacun
    /// passe par les index des tables de liens.
    /// </summary>
    public static class ArtworkFilter
    {
        /// <summary>
        /// Restreint la requête aux œuvres qui portent tous les critères renseignés
        /// </summary>
        public static IQueryable<Artwork> Apply(
            IQueryable<Artwork> query, Guid? museumId, Guid? artistId, Guid? domainId, Guid? techniqueId, Guid? periodId,
            string? region = null, string? department = null, int? yearFrom = null, int? yearTo = null)
        {
            if (!string.IsNullOrWhiteSpace(region))
                query = query.Where(a => a.Museum != null && a.Museum.Region == region);
            if (!string.IsNullOrWhiteSpace(department))
                query = query.Where(a => a.Museum != null && a.Museum.Department == department);
            if (museumId.HasValue)
                query = query.Where(a => a.MuseumId == museumId.Value);
            if (artistId.HasValue)
                query = query.Where(a => a.Artists.Any(aa => aa.ArtistId == artistId.Value));
            if (domainId.HasValue)
                query = query.Where(a => a.Domains.Any(d => d.Id == domainId.Value));
            if (techniqueId.HasValue)
                query = query.Where(a => a.Techniques.Any(t => t.Id == techniqueId.Value));
            if (periodId.HasValue)
                query = query.Where(a => a.Periods.Any(p => p.Id == periodId.Value));
            // Année de création lue dans la date en texte libre par la base ; sans lecture possible (NULL), l'œuvre n'est dans aucune plage
            if (yearFrom.HasValue)
                query = query.Where(a => EF.Property<int?>(a, OpenJocondeDbContext.CreationYear) >= yearFrom.Value);
            if (yearTo.HasValue)
                query = query.Where(a => EF.Property<int?>(a, OpenJocondeDbContext.CreationYear) <= yearTo.Value);
            return query;
        }
    }
}
