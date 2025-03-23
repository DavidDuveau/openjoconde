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
        public async Task<Museum> GetByIdAsync(Guid id)
        {
            return await _context.Museums
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        /// <summary>
        /// Search museums by name
        /// </summary>
        public async Task<IEnumerable<Museum>> SearchByNameAsync(string name, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Museums.AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.ToLower();
                query = query.Where(m => 
                    m.Name.ToLower().Contains(name) || 
                    m.City.ToLower().Contains(name) ||
                    m.Department.ToLower().Contains(name));
            }

            return await query
                .OrderBy(m => m.Name)
                .ThenBy(m => m.City)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
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
        /// Bulk upsert museums (insert or update)
        /// </summary>
        public async Task<int> BulkUpsertAsync(IEnumerable<Museum> museums)
        {
            try
            {
                // This is a simple implementation. For production, consider using a library like EFCore.BulkExtensions
                int count = 0;
                
                foreach (var museum in museums)
                {
                    var existingMuseum = await _context.Museums
                        .FirstOrDefaultAsync(m => 
                            m.Name == museum.Name && 
                            m.City == museum.City);

                    if (existingMuseum != null)
                    {
                        // Update existing museum
                        existingMuseum.Department = museum.Department;
                        existingMuseum.Address = museum.Address;
                        existingMuseum.ZipCode = museum.ZipCode;
                        existingMuseum.Phone = museum.Phone;
                        existingMuseum.Email = museum.Email;
                        existingMuseum.Website = museum.Website;
                        existingMuseum.Description = museum.Description;
                        
                        _context.Museums.Update(existingMuseum);
                    }
                    else
                    {
                        // Add new museum
                        if (museum.Id == Guid.Empty)
                        {
                            museum.Id = Guid.NewGuid();
                        }
                        
                        _context.Museums.Add(museum);
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
                _logger.LogError(ex, "Error bulk upserting museums");
                throw;
            }
        }
    }
}
