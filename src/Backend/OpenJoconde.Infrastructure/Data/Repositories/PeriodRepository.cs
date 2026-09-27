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
        /// Bulk upsert periods (insert or update) with individual fallback
        /// </summary>
        public async Task<int> BulkUpsertAsync(IEnumerable<Period> periods)
        {
            var periodList = periods.ToList();
            _logger.LogInformation("Starting bulk upsert of {Count} periods", periodList.Count);
            
            int successCount = 0;
            int errorCount = 0;
            var pendingPeriods = new List<Period>();
            
            foreach (var period in periodList)
            {
                try
                {
                    var existingPeriod = await _context.Periods
                        .FirstOrDefaultAsync(p => p.Name == period.Name);

                    if (existingPeriod != null)
                    {
                        // Update existing period
                        existingPeriod.StartYear = period.StartYear;
                        existingPeriod.EndYear = period.EndYear;
                        existingPeriod.Description = period.Description;
                        existingPeriod.UpdatedAt = DateTime.UtcNow;
                        
                        _context.Periods.Update(existingPeriod);
                        pendingPeriods.Add(existingPeriod);
                    }
                    else
                    {
                        // Add new period
                        if (period.Id == Guid.Empty)
                        {
                            period.Id = Guid.NewGuid();
                        }
                        
                        period.CreatedAt = DateTime.UtcNow;
                        period.UpdatedAt = DateTime.UtcNow;
                        
                        _context.Periods.Add(period);
                        pendingPeriods.Add(period);
                    }
                    
                    successCount++;
                    
                    // Save in batches of 100 to avoid memory issues
                    if (pendingPeriods.Count >= 100)
                    {
                        var batchResult = await SaveBatchWithIndividualFallback(pendingPeriods);
                        errorCount += batchResult.ErrorCount;
                        
                        if (batchResult.ErrorCount > 0)
                        {
                            _logger.LogWarning("Batch had {ErrorCount} errors out of {BatchSize} periods", 
                                batchResult.ErrorCount, pendingPeriods.Count);
                        }
                        
                        pendingPeriods.Clear();
                        _context.ChangeTracker.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing period {Name}: {Message}", 
                        period.Name, ex.Message);
                    errorCount++;
                }
            }
            
            // Save any remaining changes
            if (pendingPeriods.Count > 0)
            {
                var batchResult = await SaveBatchWithIndividualFallback(pendingPeriods);
                errorCount += batchResult.ErrorCount;
            }
            
            var finalSuccessCount = successCount - errorCount;
            _logger.LogInformation("Bulk upsert completed: {Success}/{Total} periods processed successfully, {Errors} errors", 
                finalSuccessCount, successCount, errorCount);
            
            return finalSuccessCount;
        }
        
        /// <summary>
        /// Saves a batch of periods with individual fallback on failure
        /// </summary>
        private async Task<(int ErrorCount, int SkippedCount)> SaveBatchWithIndividualFallback(List<Period> periods)
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
                _logger.LogWarning(batchEx, "Batch save failed for {Count} periods. Attempting individual saves.", periods.Count);
                
                // Clear context and retry individually
                _context.ChangeTracker.Clear();
                
                foreach (var period in periods)
                {
                    try
                    {
                        // Re-attach and process individual period
                        var existingPeriod = await _context.Periods
                            .FirstOrDefaultAsync(p => p.Id == period.Id || p.Name == period.Name);

                        if (existingPeriod != null)
                        {
                            // Update existing
                            existingPeriod.StartYear = period.StartYear;
                            existingPeriod.EndYear = period.EndYear;
                            existingPeriod.Description = period.Description;
                            existingPeriod.UpdatedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            // Insert new with data validation
                            var truncatedName = TruncateString(period.Name ?? "Unknown Period", 200);
                            var uniqueName = await EnsureUniqueName(truncatedName);
                            
                            var newPeriod = new Period
                            {
                                Id = period.Id != Guid.Empty ? period.Id : Guid.NewGuid(),
                                Name = uniqueName,
                                StartYear = period.StartYear,
                                EndYear = period.EndYear,
                                Description = period.Description,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.Periods.Add(newPeriod);
                        }
                        
                        await _context.SaveChangesAsync();
                        _context.ChangeTracker.Clear();
                    }
                    catch (Exception individualEx)
                    {
                        _logger.LogError(individualEx, "Failed to save period {Name}: {Message}", 
                            period.Name, individualEx.Message);
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
        
        /// <summary>
        /// Ensures a period name is unique by appending a number if needed
        /// </summary>
        private async Task<string> EnsureUniqueName(string baseName)
        {
            var candidateName = baseName;
            var counter = 1;
            
            while (await _context.Periods.AnyAsync(p => p.Name == candidateName))
            {
                var suffix = $" ({counter})";
                var maxBaseLength = 200 - suffix.Length;
                
                if (baseName.Length > maxBaseLength)
                {
                    candidateName = baseName.Substring(0, maxBaseLength) + suffix;
                }
                else
                {
                    candidateName = baseName + suffix;
                }
                
                counter++;
                
                // Safety check to avoid infinite loop
                if (counter > 1000)
                {
                    candidateName = Guid.NewGuid().ToString();
                    break;
                }
            }
            
            return candidateName;
        }
    }
}
