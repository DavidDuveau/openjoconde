using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Data
{
    /// <summary>
    /// Repository implementation for artwork entities
    /// </summary>
    public class ArtworkRepository : IArtworkRepository
    {
        private readonly OpenJocondeDbContext _context;
        private readonly ILogger<ArtworkRepository> _logger;

        public ArtworkRepository(
            OpenJocondeDbContext context,
            ILogger<ArtworkRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all artworks with pagination
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>List of artworks</returns>
        public async Task<IEnumerable<Artwork>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Artworks
                .Where(a => !a.IsDeleted)
                .OrderBy(a => a.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(a => a.Artists)
                    .ThenInclude(aa => aa.Artist)
                .Include(a => a.Domains)
                .Include(a => a.Techniques)
                .Include(a => a.Periods)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Get artwork by ID
        /// </summary>
        /// <param name="id">Artwork ID</param>
        /// <returns>Artwork if found, null otherwise</returns>
        public async Task<Artwork> GetByIdAsync(Guid id)
        {
            return await _context.Artworks
                .Where(a => a.Id == id && !a.IsDeleted)
                .Include(a => a.Artists)
                    .ThenInclude(aa => aa.Artist)
                .Include(a => a.Domains)
                .Include(a => a.Techniques)
                .Include(a => a.Periods)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get artwork by reference
        /// </summary>
        /// <param name="reference">Artwork reference in Joconde database</param>
        /// <returns>Artwork if found, null otherwise</returns>
        public async Task<Artwork> GetByReferenceAsync(string reference)
        {
            return await _context.Artworks
                .Where(a => a.Reference == reference && !a.IsDeleted)
                .Include(a => a.Artists)
                    .ThenInclude(aa => aa.Artist)
                .Include(a => a.Domains)
                .Include(a => a.Techniques)
                .Include(a => a.Periods)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

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
        public async Task<IEnumerable<Artwork>> SearchAsync(
            string searchText = null,
            Guid? artistId = null,
            Guid? domainId = null,
            Guid? techniqueId = null,
            Guid? periodId = null,
            Guid? museumId = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Artworks.Where(a => !a.IsDeleted);

            // Apply search text filter
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(a =>
                    a.Title.Contains(searchText) ||
                    a.Reference.Contains(searchText) ||
                    a.InventoryNumber.Contains(searchText) ||
                    a.Description.Contains(searchText) ||
                    a.Denomination.Contains(searchText)
                );
            }

            // Apply artist filter
            if (artistId.HasValue)
            {
                query = query.Where(a => a.Artists.Any(aa => aa.ArtistId == artistId.Value));
            }

            // Apply domain filter
            if (domainId.HasValue)
            {
                query = query.Where(a => a.Domains.Any(d => d.Id == domainId.Value));
            }

            // Apply technique filter
            if (techniqueId.HasValue)
            {
                query = query.Where(a => a.Techniques.Any(t => t.Id == techniqueId.Value));
            }

            // Apply period filter
            if (periodId.HasValue)
            {
                query = query.Where(a => a.Periods.Any(p => p.Id == periodId.Value));
            }

            // TODO: Add museumId filter when Museum is linked to Artwork

            return await query
                .OrderBy(a => a.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(a => a.Artists)
                    .ThenInclude(aa => aa.Artist)
                .Include(a => a.Domains)
                .Include(a => a.Techniques)
                .Include(a => a.Periods)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Add a new artwork
        /// </summary>
        /// <param name="artwork">Artwork to add</param>
        /// <returns>Added artwork</returns>
        public async Task<Artwork> AddAsync(Artwork artwork)
        {
            try
            {
                artwork.Id = Guid.NewGuid();
                artwork.UpdatedAt = DateTime.UtcNow;

                _context.Artworks.Add(artwork);
                await _context.SaveChangesAsync();

                return artwork;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding artwork");
                throw;
            }
        }

        /// <summary>
        /// Update an existing artwork
        /// </summary>
        /// <param name="artwork">Artwork to update</param>
        /// <returns>true if successful, false otherwise</returns>
        public async Task<bool> UpdateAsync(Artwork artwork)
        {
            try
            {
                artwork.UpdatedAt = DateTime.UtcNow;

                _context.Artworks.Update(artwork);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating artwork with ID {Id}", artwork.Id);
                return false;
            }
        }

        /// <summary>
        /// Delete an artwork
        /// </summary>
        /// <param name="id">ID of the artwork to delete</param>
        /// <returns>true if successful, false otherwise</returns>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var artwork = await _context.Artworks.FindAsync(id);
                if (artwork == null)
                {
                    return false;
                }

                // Soft delete
                artwork.IsDeleted = true;
                artwork.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting artwork with ID {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// Get the total count of artworks
        /// </summary>
        /// <returns>Number of artworks</returns>
        public async Task<int> GetCountAsync()
        {
            return await _context.Artworks.CountAsync(a => !a.IsDeleted);
        }

        /// <summary>
        /// Bulk insert or update artworks
        /// </summary>
        /// <param name="artworks">List of artworks to upsert</param>
        /// <returns>Number of affected records</returns>
        public async Task<int> BulkUpsertAsync(IEnumerable<Artwork> artworks)
        {
            try
            {
                // This is a simple implementation. For production, use a bulk library like EFCore.BulkExtensions
                foreach (var artwork in artworks)
                {
                    var existing = await _context.Artworks
                        .FirstOrDefaultAsync(a => a.Reference == artwork.Reference);

                    if (existing != null)
                    {
                        // Update existing
                        // Map properties one by one or use AutoMapper
                        existing.Title = artwork.Title;
                        existing.Description = artwork.Description;
                        existing.Denomination = artwork.Denomination;
                        // ... map other properties
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        // Add new
                        artwork.Id = Guid.NewGuid();
                        artwork.UpdatedAt = DateTime.UtcNow;
                        _context.Artworks.Add(artwork);
                    }
                }

                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk upserting artworks");
                throw;
            }
        }
    }
}
