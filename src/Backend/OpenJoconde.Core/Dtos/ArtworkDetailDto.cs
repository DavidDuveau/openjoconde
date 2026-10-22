using System;
using System.Collections.Generic;

namespace OpenJoconde.Core.Dtos
{
    /// <summary>
    /// Fiche complète d'une œuvre : ses champs et ses relations (musée, artistes, domaines, techniques, périodes)
    /// </summary>
    public class ArtworkDetailDto
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
        /// <summary>Musée qui conserve l'œuvre</summary>
        public NamedItemDto? Museum { get; init; }
        /// <summary>Artistes de l'œuvre, avec leur rôle</summary>
        public List<ArtworkArtistDto> Artists { get; init; } = new();
        /// <summary>Domaines de l'œuvre</summary>
        public List<NamedItemDto> Domains { get; init; } = new();
        /// <summary>Matériaux et techniques de l'œuvre</summary>
        public List<NamedItemDto> Techniques { get; init; } = new();
        /// <summary>Périodes de création de l'œuvre</summary>
        public List<NamedItemDto> Periods { get; init; } = new();
    }

    /// <summary>
    /// Élément référencé par son identifiant et son nom
    /// </summary>
    public class NamedItemDto
    {
        /// <summary>Identifiant de l'élément</summary>
        public Guid Id { get; init; }
        /// <summary>Nom de l'élément</summary>
        public string Name { get; init; } = string.Empty;
    }

    /// <summary>
    /// Artiste d'une œuvre, avec son rôle
    /// </summary>
    public class ArtworkArtistDto
    {
        /// <summary>Identifiant de l'artiste</summary>
        public Guid Id { get; init; }
        /// <summary>Prénom</summary>
        public string? FirstName { get; init; }
        /// <summary>Nom de famille</summary>
        public string LastName { get; init; } = string.Empty;
        /// <summary>Rôle de l'artiste dans l'œuvre (ex. Créateur)</summary>
        public string? Role { get; init; }
    }
}
