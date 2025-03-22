using System;
using System.Collections.Generic;

namespace OpenJoconde.Core.Models
{
    /// <summary>
    /// Represents a domain or category of artwork
    /// </summary>
    public class Domain
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the domain
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the domain
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Artworks in this domain
        /// </summary>
        public List<Artwork> Artworks { get; set; } = new List<Artwork>();
    }
}
