using System;

namespace OpenJoconde.Core.Dtos
{
    /// <summary>
    /// Œuvre telle qu'une liste la montre : ses champs, sans relations ni données internes (empreinte, dates de gestion)
    /// </summary>
    public class ArtworkListItemDto
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
        public Guid? MuseumId { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
