using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Interface pour le repository des artistes
    /// </summary>
    public interface IArtistRepository
    {
        /// <summary>
        /// Récupère un artiste par son identifiant
        /// </summary>
        Task<Artist> GetByIdAsync(Guid id);

        /// <summary>
        /// Recherche des artistes par nom
        /// </summary>
        Task<IEnumerable<Artist>> SearchByNameAsync(string name, int page = 1, int pageSize = 10);

        /// <summary>
        /// Ajoute un nouvel artiste
        /// </summary>
        Task<Artist> AddAsync(Artist artist);

        /// <summary>
        /// Met à jour un artiste existant
        /// </summary>
        Task<bool> UpdateAsync(Artist artist);

        /// <summary>
        /// Supprime un artiste
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
        
        /// <summary>
        /// Importe en masse des artistes (insertion ou mise à jour)
        /// </summary>
        Task<int> BulkUpsertAsync(IEnumerable<Artist> artists);
    }
}
