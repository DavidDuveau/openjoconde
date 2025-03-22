using System;
using System.Collections.Generic;

namespace OpenJoconde.Core.Models
{
    /// <summary>
    /// Represents a technique used in artworks
    /// </summary>
    public class Technique
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the technique
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the technique
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Artworks using this technique
        /// </summary>
        public List<Artwork> Artworks { get; set; } = new List<Artwork>();
    }
}
