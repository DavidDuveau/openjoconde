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
        /// Bulk upsert domains (insert or update)
        /// </summary>
        public async Task<int> BulkUpsertAsync(IEnumerable<Domain> domains)
        {
            try
            {
                // This is a simple implementation. For production, consider using a library like EFCore.BulkExtensions
                int count = 0;
                
                foreach (var domain in domains)
                {
                    var existingDomain = await _context.Domains
                        .FirstOrDefaultAsync(d => d.Name == domain.Name);

                    if (existingDomain != null)
                    {
                        // Update existing domain if necessary
                        existingDomain.Description = domain.Description;
                        
                        _context.Domains.Update(existingDomain);
                    }
                    else
                    {
                        // Add new domain
                        if (domain.Id == Guid.Empty)
                        {
                            domain.Id = Guid.NewGuid();
                        }
                        
                        _context.Domains.Add(domain);
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
                _logger.LogError(ex, "Error bulk upserting domains");
                throw;
            }
        }
    }
}
