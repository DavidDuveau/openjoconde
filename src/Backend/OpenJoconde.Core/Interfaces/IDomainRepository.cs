using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Interface pour le repository des domaines artistiques
    /// </summary>
    public interface IDomainRepository
    {
        /// <summary>
        /// Récupère un domaine par son identifiant
        /// </summary>
        Task<Domain> GetByIdAsync(Guid id);

        /// <summary>
        /// Récupère tous les domaines
        /// </summary>
        Task<IEnumerable<Domain>> GetAllAsync();

        /// <summary>
        /// Ajoute un nouveau domaine
        /// </summary>
        Task<Domain> AddAsync(Domain domain);

        /// <summary>
        /// Met à jour un domaine existant
        /// </summary>
        Task<bool> UpdateAsync(Domain domain);

        /// <summary>
        /// Supprime un domaine
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
        
        /// <summary>
        /// Importe en masse des domaines (insertion ou mise à jour)
        /// </summary>
        Task<int> BulkUpsertAsync(IEnumerable<Domain> domains);
    }
}
