using System;

namespace OpenJoconde.Core.Dtos
{
    /// <summary>
    /// Artiste tel que l'API le montre : ses champs et le nombre de ses œuvres, sans la liste des œuvres (elle se lit
    /// par la recherche d'œuvres filtrée par artiste, paginée)
    /// </summary>
    public class ArtistDto
    {
        /// <summary>Identifiant de l'artiste</summary>
        public Guid Id { get; init; }
        /// <summary>Nom de famille</summary>
        public string LastName { get; init; } = string.Empty;
        /// <summary>Prénom</summary>
        public string? FirstName { get; init; }
        /// <summary>Date de naissance, en texte libre</summary>
        public string? BirthDate { get; init; }
        /// <summary>Date de décès, en texte libre</summary>
        public string? DeathDate { get; init; }
        /// <summary>Nationalité</summary>
        public string? Nationality { get; init; }
        /// <summary>Biographie</summary>
        public string? Biography { get; init; }
        /// <summary>Courant ou mouvement artistique</summary>
        public string? Movement { get; init; }
        /// <summary>Article Wikipédia dont la biographie est tirée (à citer avec elle)</summary>
        public string? WikipediaUrl { get; init; }
        /// <summary>Nombre d'œuvres de l'artiste</summary>
        public int ArtworksCount { get; init; }
    }
}
