using System;

namespace OpenJoconde.Core.Models
{
    /// <summary>
    /// Ce que demande une recherche d'œuvres : texte, filtres cumulables, tri et page
    /// </summary>
    public class ArtworkSearchCriteria
    {
        /// <summary>Tris connus : title (A-Z, par défaut), title_desc, date, date_desc, relevance, artist, distance (proche d'abord)</summary>
        public static readonly string[] SortKeys = { "title", "title_desc", "date", "date_desc", "relevance", "artist", "distance" };

        /// <summary>Numéro de la page, à partir de 1</summary>
        public int Page { get; init; } = 1;
        /// <summary>Nombre d'œuvres par page</summary>
        public int PageSize { get; init; } = 10;
        /// <summary>Texte cherché : mots du titre et de la description, référence ou numéro d'inventaire</summary>
        public string? Search { get; init; }
        /// <summary>Ne garde que les œuvres de ce musée</summary>
        public Guid? MuseumId { get; init; }
        /// <summary>Ne garde que les œuvres de cet artiste</summary>
        public Guid? ArtistId { get; init; }
        /// <summary>Ne garde que les œuvres de ce domaine</summary>
        public Guid? DomainId { get; init; }
        /// <summary>Ne garde que les œuvres de cette technique</summary>
        public Guid? TechniqueId { get; init; }
        /// <summary>Ne garde que les œuvres de cette période</summary>
        public Guid? PeriodId { get; init; }
        /// <summary>Ne garde que les œuvres des musées de cette région (nom exact, comme le rend la liste géographique)</summary>
        public string? Region { get; init; }
        /// <summary>Ne garde que les œuvres des musées de ce département (nom exact)</summary>
        public string? Department { get; init; }
        /// <summary>Latitude de l'utilisateur, en degrés : sert au tri par distance</summary>
        public double? Latitude { get; init; }
        /// <summary>Longitude de l'utilisateur, en degrés : sert au tri par distance</summary>
        public double? Longitude { get; init; }
        /// <summary>Clé de tri (voir <see cref="SortKeys"/>) ; titre A-Z si absente</summary>
        public string? SortBy { get; init; }
    }

    /// <summary>
    /// Recherche impossible telle que demandée (tri inconnu, ou tri qui suppose une recherche ou un filtre absent)
    /// </summary>
    public class InvalidArtworkSearchException : Exception
    {
        /// <summary>Crée l'exception avec le motif du refus</summary>
        public InvalidArtworkSearchException(string message) : base(message) { }
    }
}
