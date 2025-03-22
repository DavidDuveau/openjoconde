using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Interface pour le repository des techniques artistiques
    /// </summary>
    public interface ITechniqueRepository
    {
        /// <summary>
        /// Récupère une technique par son identifiant
        /// </summary>
        Task<Technique> GetByIdAsync(Guid id);

        /// <summary>
        /// Récupère toutes les techniques
        /// </summary>
        Task<IEnumerable<Technique>> GetAllAsync();

        /// <summary>
        /// Ajoute une nouvelle technique
        /// </summary>
        Task<Technique> AddAsync(Technique technique);

        /// <summary>
        /// Met à jour une technique existante
        /// </summary>
        Task<bool> UpdateAsync(Technique technique);

        /// <summary>
        /// Supprime une technique
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
        
        /// <summary>
        /// Importe en masse des techniques (insertion ou mise à jour)
        /// </summary>
        Task<int> BulkUpsertAsync(IEnumerable<Technique> techniques);
    }
}
