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
    /// Implementation of the artist repository
    /// </summary>
    public class ArtistRepository : IArtistRepository
    {
        private readonly OpenJocondeDbContext _context;
        private readonly ILogger<ArtistRepository> _logger;

        public ArtistRepository(
            OpenJocondeDbContext context,
            ILogger<ArtistRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get an artist by ID
        /// </summary>
        public async Task<Artist> GetByIdAsync(Guid id)
        {
            return await _context.Artists
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        /// <summary>
        /// Search artists by name
        /// </summary>
        public async Task<IEnumerable<Artist>> SearchByNameAsync(string name, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Artists.AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.ToLower();
                query = query.Where(a => 
                    a.LastName.ToLower().Contains(name) || 
                    a.FirstName.ToLower().Contains(name));
            }

            return await query
                .OrderBy(a => a.LastName)
                .ThenBy(a => a.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Add a new artist
        /// </summary>
        public async Task<Artist> AddAsync(Artist artist)
        {
            try
            {
                if (artist.Id == Guid.Empty)
                {
                    artist.Id = Guid.NewGuid();
                }

                _context.Artists.Add(artist);
                await _context.SaveChangesAsync();
                return artist;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding artist {Artist}", artist.LastName);
                throw;
            }
        }

        /// <summary>
        /// Update an existing artist
        /// </summary>
        public async Task<bool> UpdateAsync(Artist artist)
        {
            try
            {
                _context.Artists.Update(artist);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating artist with ID {Id}", artist.Id);
                return false;
            }
        }

        /// <summary>
        /// Delete an artist
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var artist = await _context.Artists.FindAsync(id);
                if (artist == null)
                {
                    return false;
                }

                _context.Artists.Remove(artist);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting artist with ID {Id}", id);
                return false;
            }
        }
        
        /// <summary>
        /// Bulk upsert artists (insert or update)
        /// </summary>
        public async Task<int> BulkUpsertAsync(IEnumerable<Artist> artists)
        {
            try
            {
                // This is a simple implementation. For production, consider using a library like EFCore.BulkExtensions
                int count = 0;
                
                foreach (var artist in artists)
                {
                    var existingArtist = await _context.Artists
                        .FirstOrDefaultAsync(a => 
                            a.LastName == artist.LastName && 
                            a.FirstName == artist.FirstName);

                    if (existingArtist != null)
                    {
                        // Update existing artist
                        existingArtist.Nationality = artist.Nationality;
                        existingArtist.BirthDate = artist.BirthDate;
                        existingArtist.DeathDate = artist.DeathDate;
                        existingArtist.Biography = artist.Biography;
                        
                        _context.Artists.Update(existingArtist);
                    }
                    else
                    {
                        // Add new artist
                        if (artist.Id == Guid.Empty)
                        {
                            artist.Id = Guid.NewGuid();
                        }
                        
                        _context.Artists.Add(artist);
                    }
                    
                    count++;
                    
                    // Save in batches of 100 to avoid memory issues
                    if (count % 100 == 0)
                    {
                        await _context.SaveChangesAsync();
                    }
                }
                
                // Save any remaining changes
                if (count % 100 != 0)
                {
                    await _context.SaveChangesAsync();
                }
                
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk upserting artists");
                throw;
            }
        }
    }
}
