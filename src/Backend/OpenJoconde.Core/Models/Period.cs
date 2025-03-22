using System;
using System.Collections.Generic;

namespace OpenJoconde.Core.Models
{
    /// <summary>
    /// Represents a historical period
    /// </summary>
    public class Period
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the period
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Start year of the period
        /// </summary>
        public int? StartYear { get; set; }

        /// <summary>
        /// End year of the period
        /// </summary>
        public int? EndYear { get; set; }

        /// <summary>
        /// Description of the period
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Artworks associated with this period
        /// </summary>
        public List<Artwork> Artworks { get; set; } = new List<Artwork>();
    }
}
