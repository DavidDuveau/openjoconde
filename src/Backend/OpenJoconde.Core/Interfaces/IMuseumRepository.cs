using OpenJoconde.Core.Dtos;
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
        Task<Museum?> GetByIdAsync(Guid id);

        /// <summary>
        /// Recherche des musées par nom, ville ou département, restreinte à une région et à un département quand ils sont donnés
        /// </summary>
        Task<PaginatedResult<Museum>> SearchByNameAsync(string? name, int page = 1, int pageSize = 10, string? region = null, string? department = null);

        /// <summary>
        /// Régions et départements qui ont des musées, avec leur nombre de musées
        /// </summary>
        Task<List<RegionDto>> GetGeographyAsync();

        /// <summary>
        /// Les musées qui ont des coordonnées, avec le nombre total de musées
        /// </summary>
        Task<MuseumLocationsDto> GetLocationsAsync();

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
        /// Rend ceux des identifiants donnés qui existent en base
        /// </summary>
        Task<HashSet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids);

        /// <summary>
        /// Importe en masse des musées (insertion ou mise à jour)
        /// </summary>
        Task<int> BulkUpsertAsync(IEnumerable<Museum> museums);
    }
}
