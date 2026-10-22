using System;

namespace OpenJoconde.Core.Models
{
    /// <summary>
    /// Ce que demande une recherche d'œuvres : texte, filtres cumulables, tri et page
    /// </summary>
    public class ArtworkSearchCriteria
    {
        /// <summary>Tris connus : title (A-Z, par défaut), title_desc, date, date_desc, relevance, artist</summary>
        public static readonly string[] SortKeys = { "title", "title_desc", "date", "date_desc", "relevance", "artist" };

        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? Search { get; init; }
        public Guid? MuseumId { get; init; }
        public Guid? ArtistId { get; init; }
        public Guid? DomainId { get; init; }
        public Guid? TechniqueId { get; init; }
        public Guid? PeriodId { get; init; }
        public string? SortBy { get; init; }
    }

    /// <summary>
    /// Recherche impossible telle que demandée (tri inconnu, ou tri qui suppose une recherche ou un filtre absent)
    /// </summary>
    public class InvalidArtworkSearchException : Exception
    {
        public InvalidArtworkSearchException(string message) : base(message) { }
    }
}
