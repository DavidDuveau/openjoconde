using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Interface pour le repository des musées
    /// </summary>
    public interface IMuseumRepository
    {
        /// <summary>
        /// Récupère un musée par son identifiant
        /// </summary>
        Task<Museum> GetByIdAsync(Guid id);

        /// <summary>
        /// Recherche des musées par nom
        /// </summary>
        Task<IEnumerable<Museum>> SearchByNameAsync(string name, int page = 1, int pageSize = 10);

        /// <summary>
        /// Ajoute un nouveau musée
        /// </summary>
        Task<Museum> AddAsync(Museum museum);

        /// <summary>
        /// Met à jour un musée existant
        /// </summary>
        Task<bool> UpdateAsync(Museum museum);

        /// <summary>
        /// Supprime un musée
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
        
        /// <summary>
        /// Importe en masse des musées (insertion ou mise à jour)
        /// </summary>
        Task<int> BulkUpsertAsync(IEnumerable<Museum> museums);
    }
}
