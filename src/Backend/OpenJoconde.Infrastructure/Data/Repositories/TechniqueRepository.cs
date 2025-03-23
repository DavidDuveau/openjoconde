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
    /// Implementation of the technique repository
    /// </summary>
    public class TechniqueRepository : ITechniqueRepository
    {
        private readonly OpenJocondeDbContext _context;
        private readonly ILogger<TechniqueRepository> _logger;

        public TechniqueRepository(
            OpenJocondeDbContext context,
            ILogger<TechniqueRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get a technique by ID
        /// </summary>
        public async Task<Technique> GetByIdAsync(Guid id)
        {
            return await _context.Techniques
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        /// <summary>
        /// Get all techniques
        /// </summary>
        public async Task<IEnumerable<Technique>> GetAllAsync()
        {
            return await _context.Techniques
                .OrderBy(t => t.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Add a new technique
        /// </summary>
        public async Task<Technique> AddAsync(Technique technique)
        {
            try
            {
                if (technique.Id == Guid.Empty)
                {
                    technique.Id = Guid.NewGuid();
                }

                _context.Techniques.Add(technique);
                await _context.SaveChangesAsync();
                return technique;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding technique {Technique}", technique.Name);
                throw;
            }
        }

        /// <summary>
        /// Update an existing technique
        /// </summary>
        public async Task<bool> UpdateAsync(Technique technique)
        {
            try
            {
                _context.Techniques.Update(technique);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating technique with ID {Id}", technique.Id);
                return false;
            }
        }

        /// <summary>
        /// Delete a technique
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var technique = await _context.Techniques.FindAsync(id);
                if (technique == null)
                {
                    return false;
                }

                _context.Techniques.Remove(technique);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting technique with ID {Id}", id);
                return false;
            }
        }
        
        /// <summary>
        /// Bulk upsert techniques (insert or update)
        /// </summary>
        public async Task<int> BulkUpsertAsync(IEnumerable<Technique> techniques)
        {
            try
            {
                // This is a simple implementation. For production, consider using a library like EFCore.BulkExtensions
                int count = 0;
                
                foreach (var technique in techniques)
                {
                    var existingTechnique = await _context.Techniques
                        .FirstOrDefaultAsync(t => t.Name == technique.Name);

                    if (existingTechnique != null)
                    {
                        // Update existing technique if necessary
                        existingTechnique.Description = technique.Description;
                        
                        _context.Techniques.Update(existingTechnique);
                    }
                    else
                    {
                        // Add new technique
                        if (technique.Id == Guid.Empty)
                        {
                            technique.Id = Guid.NewGuid();
                        }
                        
                        _context.Techniques.Add(technique);
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
                _logger.LogError(ex, "Error bulk upserting techniques");
                throw;
            }
        }
    }
}
