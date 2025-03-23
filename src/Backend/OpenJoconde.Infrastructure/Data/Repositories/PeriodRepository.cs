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
    /// Implementation of the period repository
    /// </summary>
    public class PeriodRepository : IPeriodRepository
    {
        private readonly OpenJocondeDbContext _context;
        private readonly ILogger<PeriodRepository> _logger;

        public PeriodRepository(
            OpenJocondeDbContext context,
            ILogger<PeriodRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get a period by ID
        /// </summary>
        public async Task<Period> GetByIdAsync(Guid id)
        {
            return await _context.Periods
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        /// <summary>
        /// Get all periods
        /// </summary>
        public async Task<IEnumerable<Period>> GetAllAsync()
        {
            return await _context.Periods
                .OrderBy(p => p.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Add a new period
        /// </summary>
        public async Task<Period> AddAsync(Period period)
        {
            try
            {
                if (period.Id == Guid.Empty)
                {
                    period.Id = Guid.NewGuid();
                }

                _context.Periods.Add(period);
                await _context.SaveChangesAsync();
                return period;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding period {Period}", period.Name);
                throw;
            }
        }

        /// <summary>
        /// Update an existing period
        /// </summary>
        public async Task<bool> UpdateAsync(Period period)
        {
            try
            {
                _context.Periods.Update(period);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating period with ID {Id}", period.Id);
                return false;
            }
        }

        /// <summary>
        /// Delete a period
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var period = await _context.Periods.FindAsync(id);
                if (period == null)
                {
                    return false;
                }

                _context.Periods.Remove(period);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting period with ID {Id}", id);
                return false;
            }
        }
        
        /// <summary>
        /// Bulk upsert periods (insert or update)
        /// </summary>
        public async Task<int> BulkUpsertAsync(IEnumerable<Period> periods)
        {
            try
            {
                // This is a simple implementation. For production, consider using a library like EFCore.BulkExtensions
                int count = 0;
                
                foreach (var period in periods)
                {
                    var existingPeriod = await _context.Periods
                        .FirstOrDefaultAsync(p => p.Name == period.Name);

                    if (existingPeriod != null)
                    {
                        // Update existing period if necessary
                        existingPeriod.StartYear = period.StartYear;
                        existingPeriod.EndYear = period.EndYear;
                        existingPeriod.Description = period.Description;
                        
                        _context.Periods.Update(existingPeriod);
                    }
                    else
                    {
                        // Add new period
                        if (period.Id == Guid.Empty)
                        {
                            period.Id = Guid.NewGuid();
                        }
                        
                        _context.Periods.Add(period);
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
                _logger.LogError(ex, "Error bulk upserting periods");
                throw;
            }
        }
    }
}
