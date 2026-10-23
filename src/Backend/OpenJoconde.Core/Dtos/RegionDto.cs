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

    /// <summary>Un type de collection (domaine thématique de Muséofile) et le nombre de musées qui l'ont</summary>
    public class CollectionTypeDto
    {
        /// <summary>Nom du type, tel que la liste des musées l'attend dans son filtre « collection »</summary>
        public string Name { get; init; } = string.Empty;
        /// <summary>Nombre de musées qui ont ce type de collection</summary>
        public int Museums { get; init; }
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

namespace OpenJoconde.Core.Dtos
{
    /// <summary>Un musée placé sur la carte : de quoi dessiner son point et ouvrir sa fiche</summary>
    public class MuseumLocationDto
    {
        /// <summary>Identifiant du musée</summary>
        public System.Guid Id { get; init; }
        /// <summary>Nom du musée</summary>
        public string Name { get; init; } = string.Empty;
        /// <summary>Latitude, en degrés</summary>
        public double Latitude { get; init; }
        /// <summary>Longitude, en degrés</summary>
        public double Longitude { get; init; }
    }

    /// <summary>Les musées que la carte peut placer, et le nombre total de musées (placés ou non)</summary>
    public class MuseumLocationsDto
    {
        /// <summary>Nombre de musées en base, avec ou sans coordonnées</summary>
        public int Total { get; init; }
        /// <summary>Musées qui ont des coordonnées</summary>
        public List<MuseumLocationDto> Items { get; init; } = new();
    }
}
