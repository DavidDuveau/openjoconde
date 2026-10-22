using System;

namespace OpenJoconde.Core.Dtos
{
    /// <summary>
    /// Œuvre telle qu'une liste la montre : ses champs, sans relations ni données internes (empreinte, dates de gestion)
    /// </summary>
    public class ArtworkListItemDto
    {
        /// <summary>Identifiant de l'œuvre</summary>
        public Guid Id { get; init; }
        /// <summary>Référence de la notice dans la base Joconde</summary>
        public string Reference { get; init; } = string.Empty;
        /// <summary>Numéro d'inventaire dans le musée</summary>
        public string? InventoryNumber { get; init; }
        /// <summary>Dénomination de l'objet (tableau, estampe, sculpture…)</summary>
        public string? Denomination { get; init; }
        /// <summary>Titre de l'œuvre ; absent pour une notice sans titre</summary>
        public string? Title { get; init; }
        /// <summary>Description de la notice</summary>
        public string? Description { get; init; }
        /// <summary>Mesures de l'œuvre</summary>
        public string? Dimensions { get; init; }
        /// <summary>Date de création, en texte libre (« 4e quart 19e siècle »)</summary>
        public string? CreationDate { get; init; }
        /// <summary>Lieu de création ou d'utilisation</summary>
        public string? CreationPlace { get; init; }
        /// <summary>Lieu de conservation</summary>
        public string? ConservationPlace { get; init; }
        /// <summary>Mention de droits de la notice</summary>
        public string? Copyright { get; init; }
        /// <summary>Adresse d'une image ; l'export Joconde n'en fournit pas</summary>
        public string? ImageUrl { get; init; }
        /// <summary>Adresse de la notice sur le site du musée</summary>
        public string? AssociatedSiteUrl { get; init; }
        /// <summary>L'artiste est encore sous droits d'auteur ; null si la notice ne le dit pas</summary>
        public bool? ArtistUnderCopyright { get; init; }
        /// <summary>Année d'entrée de l'œuvre dans le domaine public</summary>
        public int? PublicDomainYear { get; init; }
        /// <summary>La notice annonce une image (visible sur POP) ; null si elle ne le dit pas</summary>
        public bool? HasImage { get; init; }
        /// <summary>Identifiant du musée qui conserve l'œuvre</summary>
        public Guid? MuseumId { get; init; }
        /// <summary>Date de dernière modification en base</summary>
        public DateTime UpdatedAt { get; init; }
    }
}
