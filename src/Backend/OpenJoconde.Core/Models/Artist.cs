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
        public string LastName { get; set; }

        /// <summary>
        /// First name of the artist
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Birth date of the artist
        /// </summary>
        public string BirthDate { get; set; }

        /// <summary>
        /// Death date of the artist
        /// </summary>
        public string DeathDate { get; set; }

        /// <summary>
        /// Nationality of the artist
        /// </summary>
        public string Nationality { get; set; }

        /// <summary>
        /// Biography of the artist
        /// </summary>
        public string Biography { get; set; }

        /// <summary>
        /// Artworks associated with this artist
        /// </summary>
        public List<ArtworkArtist> Artworks { get; set; } = new List<ArtworkArtist>();
    }
}
