using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Dtos;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Data
{
    /// <summary>
    /// Implementation of the museum repository
    /// </summary>
    public class MuseumRepository : IMuseumRepository
    {
        private readonly OpenJocondeDbContext _context;
        private readonly ILogger<MuseumRepository> _logger;

        public MuseumRepository(
            OpenJocondeDbContext context,
            ILogger<MuseumRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get a museum by ID
        /// </summary>
        public async Task<Museum?> GetByIdAsync(Guid id)
        {
            return await _context.Museums
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        /// <summary>
        /// Search museums by name
        /// </summary>
        public async Task<PaginatedResult<Museum>> SearchByNameAsync(string? name, int page = 1, int pageSize = 10, string? region = null, string? department = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Museums.AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                var pattern = SearchPatterns.Contains(name.Trim());
                query = query.Where(m =>
                    EF.Functions.ILike(m.Name, pattern) ||
                    (m.City != null && EF.Functions.ILike(m.City, pattern)) ||
                    (m.Department != null && EF.Functions.ILike(m.Department, pattern)));
            }

            if (!string.IsNullOrWhiteSpace(region))
                query = query.Where(m => m.Region == region);
            if (!string.IsNullOrWhiteSpace(department))
                query = query.Where(m => m.Department == department);

            var ordered = query
                .OrderBy(m => m.Name)
                .ThenBy(m => m.City);

            return new PaginatedResult<Museum>
            {
                Items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).AsNoTracking().ToListAsync(),
                TotalItems = await ordered.CountAsync(),
                Page = page,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Régions et départements qui ont des musées, avec leur nombre de musées ; les musées sans région ou sans
        /// département n'y figurent pas
        /// </summary>
        public async Task<List<RegionDto>> GetGeographyAsync()
        {
            var counts = await _context.Museums
                .Where(m => m.Region != null && m.Region != "" && m.Department != null && m.Department != "")
                .GroupBy(m => new { m.Region, m.Department })
                .Select(g => new { Region = g.Key.Region!, Department = g.Key.Department!, Museums = g.Count() })
                .ToListAsync();

            return counts
                .GroupBy(c => c.Region)
                .Select(g => new RegionDto
                {
                    Name = g.Key,
                    Museums = g.Sum(c => c.Museums),
                    Departments = g.OrderBy(c => c.Department, StringComparer.Create(new System.Globalization.CultureInfo("fr-FR"), true))
                        .Select(c => new DepartmentDto { Name = c.Department, Museums = c.Museums }).ToList()
                })
                .OrderBy(r => r.Name, StringComparer.Create(new System.Globalization.CultureInfo("fr-FR"), true))
                .ToList();
        }

        /// <summary>
        /// Les musées qui ont des coordonnées (nom et position seulement : la carte n'a pas besoin du reste), avec le
        /// nombre total de musées
        /// </summary>
        public async Task<MuseumLocationsDto> GetLocationsAsync()
        {
            var items = await _context.Museums
                .AsNoTracking()
                .Where(m => m.Latitude != null && m.Longitude != null)
                .OrderBy(m => m.Name)
                .Select(m => new MuseumLocationDto { Id = m.Id, Name = m.Name, Latitude = m.Latitude!.Value, Longitude = m.Longitude!.Value })
                .ToListAsync();
            return new MuseumLocationsDto { Total = await _context.Museums.CountAsync(), Items = items };
        }

        /// <summary>
        /// Add a new museum
        /// </summary>
        public async Task<Museum> AddAsync(Museum museum)
        {
            try
            {
                if (museum.Id == Guid.Empty)
                {
                    museum.Id = Guid.NewGuid();
                }

                _context.Museums.Add(museum);
                await _context.SaveChangesAsync();
                return museum;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding museum {Museum}", museum.Name);
                throw;
            }
        }

        /// <summary>
        /// Update an existing museum
        /// </summary>
        public async Task<bool> UpdateAsync(Museum museum)
        {
            try
            {
                _context.Museums.Update(museum);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating museum with ID {Id}", museum.Id);
                return false;
            }
        }

        /// <summary>
        /// Delete a museum
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var museum = await _context.Museums.FindAsync(id);
                if (museum == null)
                {
                    return false;
                }

                _context.Museums.Remove(museum);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting museum with ID {Id}", id);
                return false;
            }
        }
        
        /// <summary>
        /// Bulk upsert museums (insert or update) with individual fallback
        /// </summary>
        public async Task<int> BulkUpsertAsync(IEnumerable<Museum> museums)
        {
            var museumList = museums.ToList();
            _logger.LogInformation("Starting bulk upsert of {Count} museums", museumList.Count);
            
            int successCount = 0;
            int errorCount = 0;
            var pendingMuseums = new List<Museum>();
            
            foreach (var museum in museumList)
            {
                try
                {
                    var existingMuseum = await _context.Museums
                        .FirstOrDefaultAsync(m => 
                            m.Name == museum.Name && 
                            m.City == museum.City);

                    if (existingMuseum != null)
                    {
                        // L'appelant s'appuie sur l'Id réel pour rattacher les œuvres
                        museum.Id = existingMuseum.Id;

                        // Update existing museum
                        // Une valeur absente de l'import n'efface pas celle de la base : adresse, téléphone, site
                        // viennent de Muséofile, que l'import Joconde ne connaît pas
                        existingMuseum.Department = museum.Department ?? existingMuseum.Department;
                        existingMuseum.Region = museum.Region ?? existingMuseum.Region;
                        existingMuseum.Code = museum.Code ?? existingMuseum.Code;
                        existingMuseum.Latitude = museum.Latitude ?? existingMuseum.Latitude;
                        existingMuseum.Longitude = museum.Longitude ?? existingMuseum.Longitude;
                        existingMuseum.Address = museum.Address ?? existingMuseum.Address;
                        existingMuseum.ZipCode = museum.ZipCode ?? existingMuseum.ZipCode;
                        existingMuseum.Phone = museum.Phone ?? existingMuseum.Phone;
                        existingMuseum.Email = museum.Email ?? existingMuseum.Email;
                        existingMuseum.Website = museum.Website ?? existingMuseum.Website;
                        existingMuseum.Description = museum.Description ?? existingMuseum.Description;
                        existingMuseum.UpdatedAt = DateTime.UtcNow;
                        
                        _context.Museums.Update(existingMuseum);
                        pendingMuseums.Add(existingMuseum);
                    }
                    else
                    {
                        // Add new museum
                        if (museum.Id == Guid.Empty)
                        {
                            museum.Id = Guid.NewGuid();
                        }
                        
                        museum.CreatedAt = DateTime.UtcNow;
                        museum.UpdatedAt = DateTime.UtcNow;
                        
                        _context.Museums.Add(museum);
                        pendingMuseums.Add(museum);
                    }
                    
                    successCount++;
                    
                    // Save in batches of 100 to avoid memory issues
                    if (pendingMuseums.Count >= 100)
                    {
                        var batchResult = await SaveBatchWithIndividualFallback(pendingMuseums);
                        errorCount += batchResult.ErrorCount;
                        
                        if (batchResult.ErrorCount > 0)
                        {
                            _logger.LogWarning("Batch had {ErrorCount} errors out of {BatchSize} museums", 
                                batchResult.ErrorCount, pendingMuseums.Count);
                        }
                        
                        pendingMuseums.Clear();
                        _context.ChangeTracker.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing museum {Name}, {City}: {Message}", 
                        museum.Name, museum.City, ex.Message);
                    errorCount++;
                }
            }
            
            // Save any remaining changes
            if (pendingMuseums.Count > 0)
            {
                var batchResult = await SaveBatchWithIndividualFallback(pendingMuseums);
                errorCount += batchResult.ErrorCount;
            }
            
            var finalSuccessCount = successCount - errorCount;
            _logger.LogInformation("Bulk upsert completed: {Success}/{Total} museums processed successfully, {Errors} errors", 
                finalSuccessCount, successCount, errorCount);
            
            return finalSuccessCount;
        }
        
        /// <summary>
        /// Rend ceux des identifiants donnés qui existent en base
        /// </summary>
        public async Task<HashSet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids)
        {
            var wanted = ids.Distinct().ToList();
            return (await _context.Museums.Where(m => wanted.Contains(m.Id)).Select(m => m.Id).ToListAsync()).ToHashSet();
        }

        /// <summary>
        /// Saves a batch of museums with individual fallback on failure
        /// </summary>
        private async Task<(int ErrorCount, int SkippedCount)> SaveBatchWithIndividualFallback(List<Museum> museums)
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
                _logger.LogWarning(batchEx, "Batch save failed for {Count} museums. Attempting individual saves.", museums.Count);
                
                // Clear context and retry individually
                _context.ChangeTracker.Clear();
                
                foreach (var museum in museums)
                {
                    try
                    {
                        // Re-attach and process individual museum
                        var existingMuseum = await _context.Museums
                            .FirstOrDefaultAsync(m => m.Id == museum.Id || 
                                (m.Name == museum.Name && m.City == museum.City));

                        if (existingMuseum != null)
                        {
                            // Update existing
                            existingMuseum.Department = TruncateString(museum.Department, 100) ?? existingMuseum.Department;
                            existingMuseum.Address = museum.Address ?? existingMuseum.Address;
                            existingMuseum.ZipCode = TruncateString(museum.ZipCode, 20) ?? existingMuseum.ZipCode;
                            existingMuseum.Phone = TruncateString(museum.Phone, 20) ?? existingMuseum.Phone;
                            existingMuseum.Email = TruncateString(museum.Email, 100) ?? existingMuseum.Email;
                            existingMuseum.Website = museum.Website ?? existingMuseum.Website;
                            existingMuseum.Description = museum.Description ?? existingMuseum.Description;
                            existingMuseum.UpdatedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            // Insert new with data validation
                            var newMuseum = new Museum
                            {
                                Id = museum.Id != Guid.Empty ? museum.Id : Guid.NewGuid(),
                                Name = TruncateString(museum.Name ?? "Unknown Museum", 200),
                                City = TruncateString(museum.City, 100),
                                Department = TruncateString(museum.Department, 100),
                                Address = museum.Address,
                                ZipCode = TruncateString(museum.ZipCode, 20),
                                Phone = TruncateString(museum.Phone, 20),
                                Email = TruncateString(museum.Email, 100),
                                Website = museum.Website,
                                Description = museum.Description,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.Museums.Add(newMuseum);
                        }
                        
                        await _context.SaveChangesAsync();
                        _context.ChangeTracker.Clear();
                    }
                    catch (Exception individualEx)
                    {
                        _logger.LogError(individualEx, "Failed to save museum {Name}, {City}: {Message}", 
                            museum.Name, museum.City, individualEx.Message);
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
        [return: NotNullIfNotNull(nameof(value))]
        private string? TruncateString(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;
                
            if (value.Length <= maxLength)
                return value;
                
            return value.Substring(0, maxLength);
        }
    }
}
