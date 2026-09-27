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
    /// Implementation of the domain repository
    /// </summary>
    public class DomainRepository : IDomainRepository
    {
        private readonly OpenJocondeDbContext _context;
        private readonly ILogger<DomainRepository> _logger;

        public DomainRepository(
            OpenJocondeDbContext context,
            ILogger<DomainRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get a domain by ID
        /// </summary>
        public async Task<Domain> GetByIdAsync(Guid id)
        {
            return await _context.Domains
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        /// <summary>
        /// Get all domains
        /// </summary>
        public async Task<IEnumerable<Domain>> GetAllAsync()
        {
            return await _context.Domains
                .OrderBy(d => d.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Add a new domain
        /// </summary>
        public async Task<Domain> AddAsync(Domain domain)
        {
            try
            {
                if (domain.Id == Guid.Empty)
                {
                    domain.Id = Guid.NewGuid();
                }

                _context.Domains.Add(domain);
                await _context.SaveChangesAsync();
                return domain;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding domain {Domain}", domain.Name);
                throw;
            }
        }

        /// <summary>
        /// Update an existing domain
        /// </summary>
        public async Task<bool> UpdateAsync(Domain domain)
        {
            try
            {
                _context.Domains.Update(domain);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating domain with ID {Id}", domain.Id);
                return false;
            }
        }

        /// <summary>
        /// Delete a domain
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var domain = await _context.Domains.FindAsync(id);
                if (domain == null)
                {
                    return false;
                }

                _context.Domains.Remove(domain);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting domain with ID {Id}", id);
                return false;
            }
        }
        
        /// <summary>
        /// Bulk upsert domains (insert or update) with individual fallback
        /// </summary>
        public async Task<int> BulkUpsertAsync(IEnumerable<Domain> domains)
        {
            var domainList = domains.ToList();
            _logger.LogInformation("Starting bulk upsert of {Count} domains", domainList.Count);
            
            int successCount = 0;
            int errorCount = 0;
            var pendingDomains = new List<Domain>();
            
            foreach (var domain in domainList)
            {
                try
                {
                    var existingDomain = await _context.Domains
                        .FirstOrDefaultAsync(d => d.Name == domain.Name);

                    if (existingDomain != null)
                    {
                        // Update existing domain
                        existingDomain.Description = domain.Description;
                        existingDomain.UpdatedAt = DateTime.UtcNow;
                        
                        _context.Domains.Update(existingDomain);
                        pendingDomains.Add(existingDomain);
                    }
                    else
                    {
                        // Add new domain
                        if (domain.Id == Guid.Empty)
                        {
                            domain.Id = Guid.NewGuid();
                        }
                        
                        domain.CreatedAt = DateTime.UtcNow;
                        domain.UpdatedAt = DateTime.UtcNow;
                        
                        _context.Domains.Add(domain);
                        pendingDomains.Add(domain);
                    }
                    
                    successCount++;
                    
                    // Save in batches of 100 to avoid memory issues
                    if (pendingDomains.Count >= 100)
                    {
                        var batchResult = await SaveBatchWithIndividualFallback(pendingDomains);
                        errorCount += batchResult.ErrorCount;
                        
                        if (batchResult.ErrorCount > 0)
                        {
                            _logger.LogWarning("Batch had {ErrorCount} errors out of {BatchSize} domains", 
                                batchResult.ErrorCount, pendingDomains.Count);
                        }
                        
                        pendingDomains.Clear();
                        _context.ChangeTracker.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing domain {Name}: {Message}", 
                        domain.Name, ex.Message);
                    errorCount++;
                }
            }
            
            // Save any remaining changes
            if (pendingDomains.Count > 0)
            {
                var batchResult = await SaveBatchWithIndividualFallback(pendingDomains);
                errorCount += batchResult.ErrorCount;
            }
            
            var finalSuccessCount = successCount - errorCount;
            _logger.LogInformation("Bulk upsert completed: {Success}/{Total} domains processed successfully, {Errors} errors", 
                finalSuccessCount, successCount, errorCount);
            
            return finalSuccessCount;
        }
        
        /// <summary>
        /// Saves a batch of domains with individual fallback on failure
        /// </summary>
        private async Task<(int ErrorCount, int SkippedCount)> SaveBatchWithIndividualFallback(List<Domain> domains)
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
                _logger.LogWarning(batchEx, "Batch save failed for {Count} domains. Attempting individual saves.", domains.Count);
                
                // Clear context and retry individually
                _context.ChangeTracker.Clear();
                
                foreach (var domain in domains)
                {
                    try
                    {
                        // Re-attach and process individual domain
                        var existingDomain = await _context.Domains
                            .FirstOrDefaultAsync(d => d.Id == domain.Id || d.Name == domain.Name);

                        if (existingDomain != null)
                        {
                            // Update existing
                            existingDomain.Description = domain.Description;
                            existingDomain.UpdatedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            // Insert new with data validation
                            var truncatedName = TruncateString(domain.Name ?? "Unknown Domain", 200);
                            var uniqueName = await EnsureUniqueName(truncatedName);
                            
                            var newDomain = new Domain
                            {
                                Id = domain.Id != Guid.Empty ? domain.Id : Guid.NewGuid(),
                                Name = uniqueName,
                                Description = domain.Description,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.Domains.Add(newDomain);
                        }
                        
                        await _context.SaveChangesAsync();
                        _context.ChangeTracker.Clear();
                    }
                    catch (Exception individualEx)
                    {
                        _logger.LogError(individualEx, "Failed to save domain {Name}: {Message}", 
                            domain.Name, individualEx.Message);
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
        /// Ensures a domain name is unique by appending a number if needed
        /// </summary>
        private async Task<string> EnsureUniqueName(string baseName)
        {
            var candidateName = baseName;
            var counter = 1;
            
            while (await _context.Domains.AnyAsync(d => d.Name == candidateName))
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
