using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Interface pour le repository des périodes historiques
    /// </summary>
    public interface IPeriodRepository
    {
        /// <summary>
        /// Récupère une période par son identifiant
        /// </summary>
        Task<Period> GetByIdAsync(Guid id);

        /// <summary>
        /// Récupère toutes les périodes
        /// </summary>
        Task<IEnumerable<Period>> GetAllAsync();

        /// <summary>
        /// Ajoute une nouvelle période
        /// </summary>
        Task<Period> AddAsync(Period period);

        /// <summary>
        /// Met à jour une période existante
        /// </summary>
        Task<bool> UpdateAsync(Period period);

        /// <summary>
        /// Supprime une période
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
        
        /// <summary>
        /// Importe en masse des périodes (insertion ou mise à jour)
        /// </summary>
        Task<int> BulkUpsertAsync(IEnumerable<Period> periods);
    }
}
