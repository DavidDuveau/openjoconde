using System;
using System.Collections.Generic;

namespace OpenJoconde.Core.Models
{
    /// <summary>
    /// Represents an artwork from the Joconde database
    /// </summary>
    public class Artwork
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Reference in the Joconde database (REF in XML)
        /// </summary>
        public string Reference { get; set; }

        /// <summary>
        /// Inventory number of the artwork (INV in XML)
        /// </summary>
        public string InventoryNumber { get; set; }

        /// <summary>
        /// Denomination of the artwork
        /// </summary>
        public string Denomination { get; set; }

        /// <summary>
        /// Title of the artwork
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Description of the artwork
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Dimensions of the artwork
        /// </summary>
        public string Dimensions { get; set; }

        /// <summary>
        /// Creation date of the artwork
        /// </summary>
        public string CreationDate { get; set; }

        /// <summary>
        /// Place where the artwork was created
        /// </summary>
        public string CreationPlace { get; set; }

        /// <summary>
        /// Place where the artwork is conserved
        /// </summary>
        public string ConservationPlace { get; set; }

        /// <summary>
        /// Copyright information
        /// </summary>
        public string Copyright { get; set; }

        /// <summary>
        /// URL to the image of the artwork
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Flag indicating if the artwork is deleted
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Artists associated with this artwork
        /// </summary>
        public List<ArtworkArtist> Artists { get; set; } = new List<ArtworkArtist>();

        /// <summary>
        /// Domains associated with this artwork
        /// </summary>
        public List<Domain> Domains { get; set; } = new List<Domain>();

        /// <summary>
        /// Techniques used for this artwork
        /// </summary>
        public List<Technique> Techniques { get; set; } = new List<Technique>();

        /// <summary>
        /// Periods associated with this artwork
        /// </summary>
        public List<Period> Periods { get; set; } = new List<Period>();
    }
}
