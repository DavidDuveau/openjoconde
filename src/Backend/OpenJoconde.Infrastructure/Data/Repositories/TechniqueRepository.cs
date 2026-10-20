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
        /// Insère ou met à jour des techniques, identifiés par leur nom. Les existants de chaque lot de 100 sont lus en une
        /// requête ; un lot refusé par la base est rejoué élément par élément.
        /// </summary>
        /// <returns>Nombre d'éléments enregistrés</returns>
        public async Task<int> BulkUpsertAsync(IEnumerable<Technique> techniques)
        {
            const int batchSize = 100;
            var items = techniques.ToList();
            _logger.LogInformation("Starting bulk upsert of {Count} techniques", items.Count);

            int successCount = 0;
            int errorCount = 0;

            foreach (var chunk in items.Chunk(batchSize))
            {
                var pending = new List<Technique>();
                var names = chunk.Where(i => i.Name != null).Select(i => i.Name).Distinct().ToList();
                Dictionary<string, Technique> known;
                try
                {
                    known = new Dictionary<string, Technique>(StringComparer.OrdinalIgnoreCase);
                    foreach (var existing in await _context.Techniques.Where(e => names.Contains(e.Name)).ToListAsync())
                    {
                        known.TryAdd(existing.Name, existing);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lecture des techniques existants impossible pour un lot de {Count}", chunk.Length);
                    errorCount += chunk.Length;
                    _context.ChangeTracker.Clear();
                    continue;
                }

                foreach (var item in chunk)
                {
                    if (item.Name != null && known.TryGetValue(item.Name, out var existing))
                    {
                        // Aussi pour un élément ajouté plus tôt dans ce lot sous une casse différente
                        item.Id = existing.Id;
                        if (!ReferenceEquals(existing, item))
                        {
                            existing.Description = item.Description;
                            existing.UpdatedAt = DateTime.UtcNow;
                            pending.Add(existing);
                        }
                    }
                    else
                    {
                        if (item.Id == Guid.Empty)
                        {
                            item.Id = Guid.NewGuid();
                        }

                        item.CreatedAt = DateTime.UtcNow;
                        item.UpdatedAt = DateTime.UtcNow;
                        _context.Techniques.Add(item);
                        pending.Add(item);
                        if (item.Name != null)
                            known[item.Name] = item;
                    }

                    successCount++;
                }

                var batchResult = await SaveBatchWithIndividualFallback(pending);
                errorCount += batchResult.ErrorCount;
                if (batchResult.ErrorCount > 0)
                {
                    _logger.LogWarning("Batch had {ErrorCount} errors out of {BatchSize} techniques", batchResult.ErrorCount, pending.Count);
                }

                _context.ChangeTracker.Clear();
            }

            var finalSuccessCount = successCount - errorCount;
            _logger.LogInformation("Bulk upsert completed: {Success}/{Total} techniques processed successfully, {Errors} errors",
                finalSuccessCount, successCount, errorCount);

            return finalSuccessCount;
        }

        /// <summary>
        /// Saves a batch of techniques with individual fallback on failure
        /// </summary>
        private async Task<(int ErrorCount, int SkippedCount)> SaveBatchWithIndividualFallback(List<Technique> techniques)
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
                _logger.LogWarning(batchEx, "Batch save failed for {Count} techniques. Attempting individual saves.", techniques.Count);
                
                // Clear context and retry individually
                _context.ChangeTracker.Clear();
                
                foreach (var technique in techniques)
                {
                    try
                    {
                        // Re-attach and process individual technique
                        var existingTechnique = await _context.Techniques
                            .FirstOrDefaultAsync(t => t.Id == technique.Id || t.Name == technique.Name);

                        if (existingTechnique != null)
                        {
                            // Update existing
                            existingTechnique.Description = technique.Description;
                            existingTechnique.UpdatedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            // Insert new with data validation
                            var truncatedName = TruncateString(technique.Name ?? "Unknown Technique", 200);
                            var uniqueName = await EnsureUniqueName(truncatedName);
                            
                            var newTechnique = new Technique
                            {
                                Id = technique.Id != Guid.Empty ? technique.Id : Guid.NewGuid(),
                                Name = uniqueName,
                                Description = technique.Description,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.Techniques.Add(newTechnique);
                        }
                        
                        await _context.SaveChangesAsync();
                        _context.ChangeTracker.Clear();
                    }
                    catch (Exception individualEx)
                    {
                        _logger.LogError(individualEx, "Failed to save technique {Name}: {Message}", 
                            technique.Name, individualEx.Message);
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
        /// Ensures a technique name is unique by appending a number if needed
        /// </summary>
        private async Task<string> EnsureUniqueName(string baseName)
        {
            var candidateName = baseName;
            var counter = 1;
            
            while (await _context.Techniques.AnyAsync(t => t.Name == candidateName))
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
