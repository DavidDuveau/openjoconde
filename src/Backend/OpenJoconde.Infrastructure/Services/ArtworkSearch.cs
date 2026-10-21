using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;
using System.Text.RegularExpressions;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Recherche d'œuvres : le titre et la description sont cherchés par mots et débuts de mots dans l'index plein texte
    /// de PostgreSQL (configuration française, sans accents ni casse) ; la référence et le numéro d'inventaire par début.
    /// </summary>
    public static partial class ArtworkSearch
    {
        private const int MaxWords = 8;

        [GeneratedRegex(@"[\p{L}\p{N}]+")]
        private static partial Regex WordPattern();

        /// <summary>
        /// Requête to_tsquery : chaque mot du terme, en préfixe, tous requis. Null quand le terme n'a aucun mot.
        /// Seuls des mots de lettres et de chiffres y entrent : la syntaxe saisie par l'utilisateur n'est jamais reprise.
        /// </summary>
        public static string? BuildCondition(string term)
        {
            var words = WordPattern().Matches(term).Select(m => m.Value).Take(MaxWords).ToList();
            return words.Count == 0 ? null : string.Join(" & ", words.Select(word => $"{word}:*"));
        }

        /// <summary>
        /// Restreint la requête aux œuvres qui correspondent au terme
        /// </summary>
        public static IQueryable<Artwork> Apply(IQueryable<Artwork> query, string term)
        {
            term = term.Trim();
            var condition = BuildCondition(term);
            // LIKE sur lower(...) plutôt qu'ILIKE : c'est ce que les index ix_artwork_*_prefixe savent servir
            var prefix = SearchPatterns.Prefix(term.ToLowerInvariant());

            if (condition == null)
            {
                return query.Where(a => EF.Functions.Like(a.Reference.ToLower(), prefix) ||
                    EF.Functions.Like(a.InventoryNumber!.ToLower(), prefix));
            }

            return query.Where(a =>
                EF.Property<NpgsqlTsVector>(a, OpenJocondeDbContext.SearchVector).Matches(EF.Functions.ToTsQuery("french_unaccent", condition)) ||
                EF.Functions.Like(a.Reference.ToLower(), prefix) ||
                EF.Functions.Like(a.InventoryNumber!.ToLower(), prefix));
        }
    }
}
