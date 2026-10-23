using System;
using System.Collections.Generic;

namespace OpenJoconde.Core.Models
{
    /// <summary>
    /// Represents a museum
    /// </summary>
    public class Museum
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the museum
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// City where the museum is located
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Department where the museum is located
        /// </summary>
        public string? Department { get; set; }

        /// <summary>
        /// Address of the museum
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Zip code of the museum
        /// </summary>
        public string? ZipCode { get; set; }

        /// <summary>
        /// Phone number of the museum
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// Email of the museum
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Website of the museum
        /// </summary>
        public string? Website { get; set; }

        /// <summary>
        /// Description of the museum
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Region where the museum is located
        /// </summary>
        public string? Region { get; set; }

        /// <summary>
        /// Museofile code
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Catégorie du musée selon Muséofile (« écomusée ; musée en territoire rural »), valeurs séparées par « ; »
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Domaines thématiques des collections selon Muséofile (« Archéologie;Beaux-Arts;Histoire »), séparés par « ; »
        /// </summary>
        public string? ThematicDomains { get; set; }

        /// <summary>
        /// Points forts du musée selon Muséofile, en texte libre
        /// </summary>
        public string? Highlights { get; set; }

        /// <summary>
        /// Longitude of the museum location
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// Latitude of the museum location
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
