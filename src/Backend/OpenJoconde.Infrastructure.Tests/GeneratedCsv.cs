namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// CSV de test : en-tête réel du fichier Joconde, une notice par numéro (référence L000001…), avec domaine, technique,
/// période, auteur et musée. Chaque appel écrit un fichier temporaire, à supprimer par l'appelant.
/// </summary>
internal static class GeneratedCsv
{
    public static string Create(
        IEnumerable<int> numbers,
        Func<int, string> title,
        string suffix = "",
        Func<int, string>? author = null,
        Func<int, string>? domain = null,
        Func<int, IDictionary<string, string>>? extra = null)
    {
        var columns = File.ReadLines(ImportDatabase.FixturePath("joconde_cas_particuliers.csv")).First().Split('|');

        string Line(int i)
        {
            var values = new Dictionary<string, string>
            {
                ["reference"] = $"L{i:D6}",
                ["titre"] = title(i),
                ["description"] = $"description {i}{suffix}",
                ["domaine"] = domain?.Invoke(i) ?? $"domaine {i % 7};commun",
                ["materiaux_techniques"] = $"technique {i % 13}",
                ["periode_de_creation"] = $"période {i % 5}",
                ["auteur"] = author?.Invoke(i) ?? $"AUTEUR{i % 40} Prénom",
                ["nom_officiel_musee"] = $"musée {i % 3}",
                ["localisation"] = $"lieu {i}{suffix}"
            };
            foreach (var (column, value) in extra?.Invoke(i) ?? new Dictionary<string, string>())
                values[column.ToLowerInvariant()] = value;
            return string.Join('|', columns.Select(c => "\"" + values.GetValueOrDefault(c.ToLowerInvariant(), "").Replace("\"", "\"\"") + "\""));
        }

        var path = Path.Combine(Path.GetTempPath(), $"joconde_gen_{Guid.NewGuid():N}.csv");
        File.WriteAllLines(path, new[] { string.Join('|', columns) }.Concat(numbers.Select(Line)));
        return path;
    }
}
