using Microsoft.EntityFrameworkCore;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Import en plusieurs lots : œuvres et relations écrites par groupes, réimportation incluse (TECH-17).
/// </summary>
public class ImportLotsTests : IAsyncLifetime
{
    private const int Notices = 1200; // trois lots de 500, 500 et 200

    private readonly ImportDatabase _db = new();
    private readonly List<string> _fichiers = new();

    public Task InitializeAsync() => _db.InitializeAsync();

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        foreach (var fichier in _fichiers)
            File.Delete(fichier);
    }

    /// <summary>CSV de test : en-tête réel, une notice par numéro, avec domaine, technique, période et auteur.</summary>
    private string CreerCsv(int nombre, Func<int, string> titre, string suffixe = "")
    {
        var colonnes = File.ReadLines(ImportDatabase.FixturePath("joconde_cas_particuliers.csv")).First().Split('|');
        string Ligne(int i)
        {
            var valeurs = new Dictionary<string, string>
            {
                ["reference"] = $"L{i:D6}",
                ["titre"] = titre(i),
                ["description"] = $"description {i}{suffixe}",
                ["domaine"] = $"domaine {i % 7};commun",
                ["materiaux_techniques"] = $"technique {i % 13}",
                ["periode_de_creation"] = $"période {i % 5}",
                ["auteur"] = $"AUTEUR{i % 40} Prénom",
                ["nom_officiel_musee"] = $"musée {i % 3}",
                ["localisation"] = $"lieu {i}{suffixe}"
            };
            return string.Join('|', colonnes.Select(c => "\"" + valeurs.GetValueOrDefault(c.ToLowerInvariant(), "").Replace("\"", "\"\"") + "\""));
        }

        var chemin = Path.Combine(Path.GetTempPath(), $"joconde_lots_{Guid.NewGuid():N}.csv");
        File.WriteAllLines(chemin, new[] { string.Join('|', colonnes) }.Concat(Enumerable.Range(1, nombre).Select(Ligne)));
        _fichiers.Add(chemin);
        return chemin;
    }

    private async Task<OpenJoconde.Core.Models.ImportReport> ImporterAsync(string chemin)
    {
        await using var context = _db.CreateContext();
        return await _db.CreateImportService(context).ImportFromFileAsync(chemin);
    }

    [Fact]
    public async Task Import_PlusieursLots_EcritToutesLesOeuvresEtLeursRelations()
    {
        var report = await ImporterAsync(CreerCsv(Notices, i => $"titre {i}"));

        Assert.True(report.Success, report.ErrorMessage);
        Assert.Equal(Notices, report.ImportedArtworks);
        await using var context = _db.CreateContext();
        Assert.Equal(Notices, await context.Artworks.CountAsync());

        // Chaque notice : 2 domaines, 1 technique, 1 période, 1 auteur
        var liens = await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM ArtworkDomain").SingleAsync();
        Assert.Equal(Notices * 2, liens);
        Assert.Equal(Notices, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM ArtworkTechnique").SingleAsync());
        Assert.Equal(Notices, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM ArtworkPeriod").SingleAsync());
        Assert.Equal(Notices, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM ArtworkArtist").SingleAsync());

        // Les relations pointent la bonne œuvre : celle du dernier lot en témoigne
        var derniere = await context.Artworks.Include(a => a.Domains).Include(a => a.Artists).ThenInclude(aa => aa.Artist)
            .SingleAsync(a => a.Reference == $"L{Notices:D6}");
        Assert.Equal(new[] { "commun", $"domaine {Notices % 7}" }.OrderBy(n => n), derniere.Domains.Select(d => d.Name).OrderBy(n => n));
        Assert.Equal($"AUTEUR{Notices % 40}", Assert.Single(derniere.Artists).Artist.LastName);
    }

    [Fact]
    public async Task Reimport_MetAJourTousLesChampsSansDupliquerLesLiens()
    {
        await ImporterAsync(CreerCsv(Notices, i => $"titre {i}"));
        var report = await ImporterAsync(CreerCsv(Notices, i => $"nouveau titre {i}", " v2"));

        Assert.True(report.Success, report.ErrorMessage);
        await using var context = _db.CreateContext();
        Assert.Equal(Notices, await context.Artworks.CountAsync());

        var oeuvre = await context.Artworks.SingleAsync(a => a.Reference == "L000777");
        Assert.Equal("nouveau titre 777", oeuvre.Title);
        Assert.Equal("lieu 777 v2", oeuvre.ConservationPlace);
        Assert.Equal("description 777 v2", oeuvre.Description);

        Assert.Equal(Notices * 2, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM ArtworkDomain").SingleAsync());
        Assert.Equal(Notices, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM ArtworkArtist").SingleAsync());
    }

    [Fact]
    public async Task Reimport_ReprendLesChampsModifiesDeLaNotice()
    {
        var chemin = Path.Combine(Path.GetTempPath(), $"joconde_modifie_{Guid.NewGuid():N}.json");
        _fichiers.Add(chemin);
        var original = await File.ReadAllTextAsync(ImportDatabase.FixturePath("joconde_une_oeuvre.json"));

        await ImporterAsync(ImportDatabase.FixturePath("joconde_une_oeuvre.json"));
        await File.WriteAllTextAsync(chemin, original
            .Replace("Grotesques ; Grotesques (verso)", "Titre corrigé")
            .Replace("PM 454", "PM 999")
            .Replace("H. 28.9 ; L. 41.5", "H. 30"));
        await ImporterAsync(chemin);

        await using var context = _db.CreateContext();
        var oeuvre = await context.Artworks.SingleAsync();
        Assert.Equal("Titre corrigé", oeuvre.Title);
        Assert.Equal("PM 999", oeuvre.InventoryNumber);
        Assert.Equal("H. 30", oeuvre.Dimensions);
    }
}
