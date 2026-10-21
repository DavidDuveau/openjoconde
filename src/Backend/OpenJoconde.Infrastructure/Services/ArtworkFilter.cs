using OpenJoconde.Core.Models;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Filtres sur les œuvres : musée, artiste, domaine, technique, période. Les filtres posés se cumulent (ET) ; chacun
    /// passe par les index des tables de liens.
    /// </summary>
    public static class ArtworkFilter
    {
        /// <summary>
        /// Restreint la requête aux œuvres qui portent tous les critères renseignés
        /// </summary>
        public static IQueryable<Artwork> Apply(
            IQueryable<Artwork> query, Guid? museumId, Guid? artistId, Guid? domainId, Guid? techniqueId, Guid? periodId)
        {
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
            return query;
        }
    }
}
