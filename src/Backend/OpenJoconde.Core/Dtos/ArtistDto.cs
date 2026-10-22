using System;

namespace OpenJoconde.Core.Dtos
{
    /// <summary>
    /// Artiste tel que l'API le montre : ses champs et le nombre de ses œuvres, sans la liste des œuvres (elle se lit
    /// par la recherche d'œuvres filtrée par artiste, paginée)
    /// </summary>
    public class ArtistDto
    {
        public Guid Id { get; init; }
        public string LastName { get; init; } = string.Empty;
        public string? FirstName { get; init; }
        public string? BirthDate { get; init; }
        public string? DeathDate { get; init; }
        public string? Nationality { get; init; }
        public string? Biography { get; init; }
        public int ArtworksCount { get; init; }
    }
}
