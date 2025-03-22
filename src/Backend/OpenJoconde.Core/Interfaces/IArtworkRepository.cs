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
        /// Get all artworks with pagination
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>List of artworks</returns>
        Task<IEnumerable<Artwork>> GetAllAsync(int page = 1, int pageSize = 10);

        /// <summary>
        /// Get artwork by ID
        /// </summary>
        /// <param name="id">Artwork ID</param>
        /// <returns>Artwork if found, null otherwise</returns>
        Task<Artwork> GetByIdAsync(Guid id);

        /// <summary>
        /// Get artwork by reference
        /// </summary>
        /// <param name="reference">Artwork reference in Joconde database</param>
        /// <returns>Artwork if found, null otherwise</returns>
        Task<Artwork> GetByReferenceAsync(string reference);

        /// <summary>
        /// Search artworks based on criteria
        /// </summary>
        /// <param name="searchText">Text to search in title, description, etc.</param>
        /// <param name="artistId">Filter by artist ID</param>
        /// <param name="domainId">Filter by domain ID</param>
        /// <param name="techniqueId">Filter by technique ID</param>
        /// <param name="periodId">Filter by period ID</param>
        /// <param name="museumId">Filter by museum ID</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>List of artworks matching criteria</returns>
        Task<IEnumerable<Artwork>> SearchAsync(
            string searchText = null,
            Guid? artistId = null,
            Guid? domainId = null, 
            Guid? techniqueId = null,
            Guid? periodId = null,
            Guid? museumId = null,
            int page = 1,
            int pageSize = 10);

        /// <summary>
        /// Add a new artwork
        /// </summary>
        /// <param name="artwork">Artwork to add</param>
        /// <returns>Added artwork</returns>
        Task<Artwork> AddAsync(Artwork artwork);

        /// <summary>
        /// Update an existing artwork
        /// </summary>
        /// <param name="artwork">Artwork to update</param>
        /// <returns>true if successful, false otherwise</returns>
        Task<bool> UpdateAsync(Artwork artwork);

        /// <summary>
        /// Delete an artwork
        /// </summary>
        /// <param name="id">ID of the artwork to delete</param>
        /// <returns>true if successful, false otherwise</returns>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// Get the total count of artworks
        /// </summary>
        /// <returns>Number of artworks</returns>
        Task<int> GetCountAsync();

        /// <summary>
        /// Bulk insert or update artworks
        /// </summary>
        /// <param name="artworks">List of artworks to upsert</param>
        /// <returns>Number of affected records</returns>
        Task<int> BulkUpsertAsync(IEnumerable<Artwork> artworks);
    }
}
