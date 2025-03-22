using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Interface pour le repository gérant les relations entre les œuvres et les autres entités
    /// </summary>
    public interface IArtworkRelationsRepository
    {
        /// <summary>
        /// Crée ou met à jour les relations entre les œuvres et les artistes
        /// </summary>
        Task<int> SaveArtworkArtistRelationsAsync(Guid artworkId, IEnumerable<ArtworkArtist> relations);

        /// <summary>
        /// Crée ou met à jour les relations entre les œuvres et les domaines
        /// </summary>
        Task<int> SaveArtworkDomainRelationsAsync(Guid artworkId, IEnumerable<Guid> domainIds);

        /// <summary>
        /// Crée ou met à jour les relations entre les œuvres et les techniques
        /// </summary>
        Task<int> SaveArtworkTechniqueRelationsAsync(Guid artworkId, IEnumerable<Guid> techniqueIds);

        /// <summary>
        /// Crée ou met à jour les relations entre les œuvres et les périodes
        /// </summary>
        Task<int> SaveArtworkPeriodRelationsAsync(Guid artworkId, IEnumerable<Guid> periodIds);

        /// <summary>
        /// Supprime toutes les relations pour une œuvre
        /// </summary>
        Task<bool> DeleteArtworkRelationsAsync(Guid artworkId);
    }
}
