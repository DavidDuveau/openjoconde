using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using OpenJoconde.Core.Interfaces;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>Un artiste de la base, tel que le rapprochement le compare à Wikidata</summary>
    /// <param name="FullName">« Prénom Nom »</param>
    /// <param name="BirthYear">Année de naissance de la notice</param>
    /// <param name="DeathYear">Année de décès de la notice, quand elle est connue</param>
    public sealed record ArtistCandidate(string FullName, int BirthYear, int? DeathYear);

    /// <summary>
    /// Règles du rapprochement entre un artiste de la base et une entité Wikidata. Strictes : une biographie d'un homonyme
    /// est pire que pas de biographie.
    /// </summary>
    public static class ArtistMatcher
    {
        private static readonly Regex NotAPerson = new(
            @"(?<![\p{L}\p{N}])est (?:une?|la|le) (?:société|entreprise|marque|maison|manufacture|famille|dynastie)(?![\p{L}\p{N}])",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex Collective = new(
            @"[;()/]|anonyme|atelier|école|ecole|d'après|manufacture",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>Minuscules, sans accents ni ponctuation : la forme sous laquelle on compare deux noms</summary>
        public static string Normalize(string? text)
        {
            var decomposed = (text ?? string.Empty).Normalize(NormalizationForm.FormD);
            var plain = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    plain.Append(char.ToLowerInvariant(c));
            return Regex.Replace(plain.ToString(), "[^a-z0-9]+", " ").Trim();
        }

        /// <summary>
        /// « Claude » + « Monet » donne « Claude Monet » ; null pour ce qui n'est pas une personne identifiable (anonyme,
        /// atelier, « d'après », plusieurs auteurs...)
        /// </summary>
        public static string? FullName(string? firstName, string? lastName)
        {
            var name = $"{firstName} {lastName}".Trim();
            return name.Length == 0 || Collective.IsMatch(name) ? null : name;
        }

        /// <summary>Année d'une date Wikidata (+1840-11-14T00:00:00Z) ; null si elle n'a pas ce format (avant J.-C., inconnue)</summary>
        public static int? YearOf(string? date)
        {
            var match = Regex.Match(date ?? string.Empty, @"^\+(\d{4})(?:-|$)");
            return match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Vrai si l'entité est la personne de la notice : un humain de même nom, né la même année (et mort la même année
        /// si les deux la connaissent), avec un article Wikipédia en français
        /// </summary>
        public static bool Matches(WikidataEntity entity, ArtistCandidate candidate)
        {
            if (!entity.IsHuman || entity.FrenchWikipediaTitle == null)
                return false;

            var names = entity.Aliases.Prepend(entity.Label ?? string.Empty).Select(Normalize);
            if (!names.Contains(Normalize(candidate.FullName)))
                return false;
            if (!entity.BirthYears.Contains(candidate.BirthYear))
                return false;
            return candidate.DeathYear == null || entity.DeathYears.Count == 0 || entity.DeathYears.Contains(candidate.DeathYear.Value);
        }

        /// <summary>Faux quand le résumé d'un article parle d'une entreprise ou d'une famille portant le nom de la personne</summary>
        public static bool IsPersonSummary(string? summary) => !NotAPerson.IsMatch(summary ?? string.Empty);
    }
}
