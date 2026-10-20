using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Interface for the artwork repository
    /// </summary>
    public interface IArtworkRepository
    {
        /// <summary>
        /// Bulk insert or update artworks
        /// </summary>
        /// <param name="artworks">List of artworks to upsert</param>
        /// <returns>Number of affected records</returns>
        Task<int> BulkUpsertAsync(IEnumerable<Artwork> artworks);

        /// <summary>
        /// Référence et empreinte de contenu de chaque œuvre en base (null pour une œuvre importée avant les empreintes)
        /// </summary>
        Task<Dictionary<string, Guid?>> GetContentHashesAsync();

        /// <summary>
        /// Supprime les œuvres de ces références, avec leurs liens
        /// </summary>
        /// <returns>Nombre d'œuvres supprimées</returns>
        Task<int> DeleteByReferencesAsync(IReadOnlyCollection<string> references);
    }
}
