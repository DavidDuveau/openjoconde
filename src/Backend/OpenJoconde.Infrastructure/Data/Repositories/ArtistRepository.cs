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
        public async Task<PaginatedResult<Artist>> SearchByNameAsync(string? name, int page = 1, int pageSize = 10)
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

            var ordered = query
                .OrderBy(a => a.LastName)
                .ThenBy(a => a.FirstName);

            return new PaginatedResult<Artist>
            {
                Items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).AsNoTracking().ToListAsync(),
                TotalItems = await ordered.CountAsync(),
                Page = page,
                PageSize = pageSize
            };
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
        /// Insère ou met à jour des artistes, identifiés par leur nom et prénom (sans tenir compte de la casse).
        /// Les artistes existants de chaque lot sont lus en une requête ; l'Id de chaque artiste est remplacé par
        /// celui de la base. Un lot refusé par la base est compté en erreurs et les lots suivants continuent.
        /// </summary>
        /// <returns>Nombre d'artistes enregistrés</returns>
        public async Task<int> BulkUpsertAsync(IEnumerable<Artist> artists)
        {
            const int batchSize = 500;
            var artistList = artists.ToList();
            _logger.LogInformation("Starting bulk upsert of {Count} artists", artistList.Count);

            int saved = 0;
            foreach (var batch in artistList.Chunk(batchSize))
            {
                saved += await SaveBatchAsync(batch);
            }

            var errorCount = artistList.Count - saved;
            _logger.LogInformation("Bulk upsert completed: {Success}/{Total} artists processed successfully, {Errors} errors",
                saved, artistList.Count, errorCount);

            return saved;
        }

        /// <summary>
        /// Enregistre un lot ; s'il est refusé (délai dépassé sous charge, par exemple), le rejoue en deux moitiés,
        /// jusqu'à isoler l'artiste fautif. Renvoie le nombre d'artistes enregistrés.
        /// </summary>
        private async Task<int> SaveBatchAsync(Artist[] batch)
        {
            try
            {
                var lastNames = batch.Select(a => TruncateString(a.LastName ?? "Unknown", 500)).Distinct().ToList();
                var known = new Dictionary<(string, string), Artist>();
                foreach (var existing in await _context.Artists.Where(a => lastNames.Contains(a.LastName)).ToListAsync())
                {
                    known.TryAdd(NameKey(existing.LastName, existing.FirstName), existing);
                }

                foreach (var artist in batch)
                {
                    artist.LastName = TruncateString(artist.LastName ?? "Unknown", 500);
                    artist.FirstName = TruncateString(artist.FirstName, 500);
                    var key = NameKey(artist.LastName, artist.FirstName);

                    if (known.TryGetValue(key, out var existing))
                    {
                        // Aussi pour un artiste ajouté plus tôt dans ce lot sous une casse différente
                        artist.Id = existing.Id;
                        if (!ReferenceEquals(existing, artist))
                        {
                            existing.Nationality = TruncateString(artist.Nationality, 200);
                            existing.BirthDate = TruncateString(artist.BirthDate, 100);
                            existing.DeathDate = TruncateString(artist.DeathDate, 100);
                            existing.Biography = artist.Biography;
                            existing.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        if (artist.Id == Guid.Empty)
                        {
                            artist.Id = Guid.NewGuid();
                        }

                        artist.Nationality = TruncateString(artist.Nationality, 200);
                        artist.BirthDate = TruncateString(artist.BirthDate, 100);
                        artist.DeathDate = TruncateString(artist.DeathDate, 100);
                        artist.CreatedAt = DateTime.UtcNow;
                        artist.UpdatedAt = DateTime.UtcNow;

                        _context.Artists.Add(artist);
                        known[key] = artist;
                    }
                }

                await _context.SaveChangesAsync();
                return batch.Length;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _context.ChangeTracker.Clear();

                if (batch.Length == 1)
                {
                    _logger.LogError(ex, "Artist {LastName} {FirstName} could not be saved: {Message}", batch[0].LastName, batch[0].FirstName, ex.Message);
                    return 0;
                }

                _logger.LogWarning(ex, "Batch of {Count} artists could not be saved ({Message}) : retry in two halves", batch.Length, ex.Message);
                var half = batch.Length / 2;
                return await SaveBatchAsync(batch[..half]) + await SaveBatchAsync(batch[half..]);
            }
            finally
            {
                _context.ChangeTracker.Clear();
            }
        }

        private static (string, string) NameKey(string lastName, string firstName) =>
            (lastName.ToLowerInvariant(), (firstName ?? string.Empty).ToLowerInvariant());

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
