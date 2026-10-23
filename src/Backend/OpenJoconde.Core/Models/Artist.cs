using System;
using System.Collections.Generic;

namespace OpenJoconde.Core.Models
{
    /// <summary>
    /// Represents an artist in the database
    /// </summary>
    public class Artist
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Last name of the artist
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// First name of the artist
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Birth date of the artist
        /// </summary>
        public string? BirthDate { get; set; }

        /// <summary>
        /// Death date of the artist
        /// </summary>
        public string? DeathDate { get; set; }

        /// <summary>
        /// Nationality of the artist
        /// </summary>
        public string? Nationality { get; set; }

        /// <summary>
        /// Biography of the artist
        /// </summary>
        public string? Biography { get; set; }

        /// <summary>
        /// Identifiant Wikidata de l'artiste (ex. Q296), quand le rapprochement avec la notice a été fait
        /// </summary>
        public string? WikidataId { get; set; }

        /// <summary>
        /// Adresse de l'article Wikipédia dont la biographie est tirée
        /// </summary>
        public string? WikipediaUrl { get; set; }

        /// <summary>
        /// Courant ou mouvement artistique de l'artiste, selon Wikidata
        /// </summary>
        public string? Movement { get; set; }

        /// <summary>
        /// Date du dernier examen par l'enrichissement Wikidata, qu'il ait trouvé une correspondance ou non
        /// </summary>
        public DateTime? EnrichmentAttemptedAt { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Artworks associated with this artist
        /// </summary>
        public List<ArtworkArtist> Artworks { get; set; } = new List<ArtworkArtist>();
    }
}
