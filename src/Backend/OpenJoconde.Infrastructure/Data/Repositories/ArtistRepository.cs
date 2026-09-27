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
                    (a.FirstName != null && a.FirstName.ToLower().Contains(name)));
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
        /// Bulk upsert artists (insert or update) with individual fallback
        /// </summary>
        public async Task<int> BulkUpsertAsync(IEnumerable<Artist> artists)
        {
            var artistList = artists.ToList();
            _logger.LogInformation("Starting bulk upsert of {Count} artists", artistList.Count);
            
            int successCount = 0;
            int errorCount = 0;
            var pendingArtists = new List<Artist>();
            
            foreach (var artist in artistList)
            {
                try
                {
                    var existingArtist = await _context.Artists
                        .FirstOrDefaultAsync(a => 
                            a.LastName == artist.LastName && 
                            ((a.FirstName == null && artist.FirstName == null) || 
                             (a.FirstName != null && a.FirstName == artist.FirstName)));

                    if (existingArtist != null)
                    {
                        // Update existing artist
                        existingArtist.Nationality = artist.Nationality;
                        existingArtist.BirthDate = artist.BirthDate;
                        existingArtist.DeathDate = artist.DeathDate;
                        existingArtist.Biography = artist.Biography;
                        existingArtist.UpdatedAt = DateTime.UtcNow;
                        
                        _context.Artists.Update(existingArtist);
                        pendingArtists.Add(existingArtist);
                    }
                    else
                    {
                        // Add new artist
                        if (artist.Id == Guid.Empty)
                        {
                            artist.Id = Guid.NewGuid();
                        }
                        
                        artist.CreatedAt = DateTime.UtcNow;
                        artist.UpdatedAt = DateTime.UtcNow;
                        
                        _context.Artists.Add(artist);
                        pendingArtists.Add(artist);
                    }
                    
                    successCount++;
                    
                    // Save in batches of 100 to avoid memory issues
                    if (pendingArtists.Count >= 100)
                    {
                        var batchResult = await SaveBatchWithIndividualFallback(pendingArtists);
                        errorCount += batchResult.ErrorCount;
                        
                        if (batchResult.ErrorCount > 0)
                        {
                            _logger.LogWarning("Batch had {ErrorCount} errors out of {BatchSize} artists", 
                                batchResult.ErrorCount, pendingArtists.Count);
                        }
                        
                        pendingArtists.Clear();
                        _context.ChangeTracker.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing artist {LastName}, {FirstName}: {Message}", 
                        artist.LastName, artist.FirstName, ex.Message);
                    errorCount++;
                }
            }
            
            // Save any remaining changes
            if (pendingArtists.Count > 0)
            {
                var batchResult = await SaveBatchWithIndividualFallback(pendingArtists);
                errorCount += batchResult.ErrorCount;
            }
            
            var finalSuccessCount = successCount - errorCount;
            _logger.LogInformation("Bulk upsert completed: {Success}/{Total} artists processed successfully, {Errors} errors", 
                finalSuccessCount, successCount, errorCount);
            
            return finalSuccessCount;
        }
        
        /// <summary>
        /// Saves a batch of artists with individual fallback on failure
        /// </summary>
        private async Task<(int ErrorCount, int SkippedCount)> SaveBatchWithIndividualFallback(List<Artist> artists)
        {
            int errorCount = 0;
            int skippedCount = 0;
            
            try
            {
                // Attempt batch save
                await _context.SaveChangesAsync();
                return (0, 0); // Success
            }
            catch (Exception batchEx)
            {
                _logger.LogWarning(batchEx, "Batch save failed for {Count} artists. Attempting individual saves.", artists.Count);
                
                // Clear context and retry individually
                _context.ChangeTracker.Clear();
                
                foreach (var artist in artists)
                {
                    try
                    {
                        // Re-attach and process individual artist
                        var existingArtist = await _context.Artists
                            .FirstOrDefaultAsync(a => a.Id == artist.Id || 
                                (a.LastName == artist.LastName && 
                                 ((a.FirstName == null && artist.FirstName == null) || 
                                  (a.FirstName != null && a.FirstName == artist.FirstName))));

                        if (existingArtist != null)
                        {
                            // Update existing
                            existingArtist.Nationality = TruncateString(artist.Nationality, 200);
                            existingArtist.BirthDate = TruncateString(artist.BirthDate, 100);
                            existingArtist.DeathDate = TruncateString(artist.DeathDate, 100);
                            existingArtist.Biography = artist.Biography; // NVARCHAR(MAX) - no limit
                            existingArtist.UpdatedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            // Insert new with data validation
                            var newArtist = new Artist
                            {
                                Id = artist.Id != Guid.Empty ? artist.Id : Guid.NewGuid(),
                                LastName = TruncateString(artist.LastName ?? "Unknown", 500),
                                FirstName = TruncateString(artist.FirstName, 500),
                                Nationality = TruncateString(artist.Nationality, 200),
                                BirthDate = TruncateString(artist.BirthDate, 100),
                                DeathDate = TruncateString(artist.DeathDate, 100),
                                Biography = artist.Biography, // NVARCHAR(MAX) - no limit
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.Artists.Add(newArtist);
                        }
                        
                        await _context.SaveChangesAsync();
                        _context.ChangeTracker.Clear();
                    }
                    catch (Exception individualEx)
                    {
                        _logger.LogError(individualEx, "Failed to save artist {LastName}, {FirstName}: {Message}", 
                            artist.LastName, artist.FirstName, individualEx.Message);
                        errorCount++;
                        _context.ChangeTracker.Clear();
                    }
                }
                
                return (errorCount, skippedCount);
            }
        }
        
        /// <summary>
        /// Truncates a string to the specified maximum length
        /// </summary>
        private string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;
                
            if (value.Length <= maxLength)
                return value;
                
            return value.Substring(0, maxLength);
        }
    }
}
