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
    }
}
