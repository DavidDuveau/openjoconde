using System;

namespace OpenJoconde.Core.Models
{
    /// <summary>
    /// Join entity for the many-to-many relationship between Artwork and Artist
    /// </summary>
    public class ArtworkArtist
    {
        /// <summary>
        /// Foreign key to Artwork
        /// </summary>
        public Guid ArtworkId { get; set; }

        /// <summary>
        /// Reference to the Artwork
        /// </summary>
        public Artwork Artwork { get; set; }

        /// <summary>
        /// Foreign key to Artist
        /// </summary>
        public Guid ArtistId { get; set; }

        /// <summary>
        /// Reference to the Artist
        /// </summary>
        public Artist Artist { get; set; }

        /// <summary>
        /// Role of the artist for this artwork (e.g., "painter", "sculptor")
        /// </summary>
        public string Role { get; set; }
    }
}
