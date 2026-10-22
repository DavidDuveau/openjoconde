using System;
using System.Collections.Generic;

namespace OpenJoconde.Core.Dtos
{
    /// <summary>
    /// Fiche complète d'une œuvre : ses champs et ses relations (musée, artistes, domaines, techniques, périodes)
    /// </summary>
    public class ArtworkDetailDto
    {
        public Guid Id { get; init; }
        public string Reference { get; init; } = string.Empty;
        public string? InventoryNumber { get; init; }
        public string? Denomination { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public string? Dimensions { get; init; }
        public string? CreationDate { get; init; }
        public string? CreationPlace { get; init; }
        public string? ConservationPlace { get; init; }
        public string? Copyright { get; init; }
        public string? ImageUrl { get; init; }
        public string? AssociatedSiteUrl { get; init; }
        public bool? ArtistUnderCopyright { get; init; }
        public int? PublicDomainYear { get; init; }
        public bool? HasImage { get; init; }
        public NamedItemDto? Museum { get; init; }
        public List<ArtworkArtistDto> Artists { get; init; } = new();
        public List<NamedItemDto> Domains { get; init; } = new();
        public List<NamedItemDto> Techniques { get; init; } = new();
        public List<NamedItemDto> Periods { get; init; } = new();
    }

    /// <summary>
    /// Élément référencé par son identifiant et son nom
    /// </summary>
    public class NamedItemDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    /// <summary>
    /// Artiste d'une œuvre, avec son rôle
    /// </summary>
    public class ArtworkArtistDto
    {
        public Guid Id { get; init; }
        public string? FirstName { get; init; }
        public string LastName { get; init; } = string.Empty;
        public string? Role { get; init; }
    }
}
