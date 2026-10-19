using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Interface pour la gestion des relations entre les œuvres et les autres entités
    /// </summary>
    public interface IArtworkRelationsRepository
    {
        /// <summary>
        /// Remplace les relations (artistes, domaines, techniques, périodes) d'un groupe d'œuvres, en une seule transaction :
        /// les relations existantes de ces œuvres sont supprimées puis celles fournies sont écrites. Tout ou rien.
        /// </summary>
        /// <param name="relations">Relations de chaque œuvre, identifiées par leur Id en base</param>
        /// <returns>Nombre de lignes de relation écrites</returns>
        Task<int> SaveBatchAsync(IReadOnlyCollection<ArtworkRelationSet> relations);

        /// <summary>
        /// Supprime toutes les relations pour une œuvre
        /// </summary>
        /// <param name="artworkId">Identifiant de l'œuvre</param>
        /// <returns>True si les relations ont été supprimées</returns>
        Task<bool> DeleteArtworkRelationsAsync(Guid artworkId);
    }
}
