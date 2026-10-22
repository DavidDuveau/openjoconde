using System;
using System.Collections.Generic;

namespace OpenJoconde.Core.Models
{
    /// <summary>
    /// Toutes les relations d'une œuvre déjà enregistrée, à écrire en une fois avec celles de son lot
    /// </summary>
    public class ArtworkRelationSet
    {
        /// <summary>
        /// Identifiant de l'œuvre en base
        /// </summary>
        public Guid ArtworkId { get; set; }

        /// <summary>
        /// Artistes de l'œuvre (identifiant de l'artiste en base et rôle)
        /// </summary>
        public List<ArtworkArtist> Artists { get; set; } = new List<ArtworkArtist>();

        /// <summary>Identifiants des domaines de l'œuvre</summary>
        public List<Guid> DomainIds { get; set; } = new List<Guid>();

        /// <summary>Identifiants des techniques de l'œuvre</summary>
        public List<Guid> TechniqueIds { get; set; } = new List<Guid>();

        /// <summary>Identifiants des périodes de création de l'œuvre</summary>
        public List<Guid> PeriodIds { get; set; } = new List<Guid>();

        /// <summary>
        /// Nombre de lignes de relation que cette œuvre représente
        /// </summary>
        public int RelationCount => Artists.Count + DomainIds.Count + TechniqueIds.Count + PeriodIds.Count;
    }
}
