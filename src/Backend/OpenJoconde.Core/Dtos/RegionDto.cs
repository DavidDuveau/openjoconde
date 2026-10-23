using System.Collections.Generic;

namespace OpenJoconde.Core.Dtos
{
    /// <summary>
    /// Une région et ses départements, avec le nombre de musées de chacun : de quoi proposer les filtres géographiques
    /// et situer les musées sans interroger chaque fiche
    /// </summary>
    public class RegionDto
    {
        /// <summary>Nom de la région, tel que la recherche d'œuvres l'attend</summary>
        public string Name { get; init; } = string.Empty;
        /// <summary>Nombre de musées de la région</summary>
        public int Museums { get; init; }
        /// <summary>Départements de la région qui ont des musées, par ordre alphabétique</summary>
        public List<DepartmentDto> Departments { get; init; } = new();
    }

    /// <summary>Un département et son nombre de musées</summary>
    public class DepartmentDto
    {
        /// <summary>Nom du département, tel que la recherche d'œuvres l'attend</summary>
        public string Name { get; init; } = string.Empty;
        /// <summary>Nombre de musées du département</summary>
        public int Museums { get; init; }
    }
}
