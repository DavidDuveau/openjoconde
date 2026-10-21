using Microsoft.EntityFrameworkCore;
using OpenJoconde.Core.Models;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Synchronisation incrémentale : seules les notices nouvelles ou modifiées sont écrites, les disparues sont supprimées
/// avec garde-fous (TECH-29).
/// </summary>
public class IncrementalImportTests : IAsyncLifetime
{
    private const int Notices = 300;

    private readonly ImportDatabase _db = new();
    private readonly List<string> _fichiers = new();

    public Task InitializeAsync() => _db.InitializeAsync();

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        foreach (var fichier in _fichiers)
            File.Delete(fichier);
    }

    private string Csv(IEnumerable<int> numeros, Func<int, string>? titre = null, Func<int, string>? domaine = null)
    {
        var chemin = GeneratedCsv.Create(numeros, titre ?? (i => $"titre {i}"), domain: domaine);
        _fichiers.Add(chemin);
        return chemin;
    }

    private async Task<ImportReport> ImporterAsync(string chemin, bool onlyChanges = true)
    {
        await using var context = _db.CreateContext();
        return await _db.CreateImportService(context).ImportFromFileAsync(chemin, onlyChanges: onlyChanges);
    }

    private async Task<Dictionary<string, DateTime>> DatesDeMajAsync()
    {
        await using var context = _db.CreateContext();
        return await context.Artworks.AsNoTracking().ToDictionaryAsync(a => a.Reference, a => a.UpdatedAt);
    }

    [Fact]
    public async Task ReimportIdentique_NeReecritAucuneOeuvre()
    {
        var fichier = Csv(Enumerable.Range(1, Notices));
        await ImporterAsync(fichier, onlyChanges: false);
        var avant = await DatesDeMajAsync();

        var report = await ImporterAsync(fichier);

        Assert.True(report.Success, report.ErrorMessage);
        Assert.Equal(Notices, report.SkippedArtworks);
        Assert.Equal(0, report.ImportedArtworks);
        Assert.Equal(0, report.RemovedArtworks);
        Assert.Equal(avant, await DatesDeMajAsync());
    }

    [Fact]
    public async Task NoticeModifiee_SeuleEstReecrite_AvecSesRelations()
    {
        await ImporterAsync(Csv(Enumerable.Range(1, Notices)), onlyChanges: false);
        var avant = await DatesDeMajAsync();

        var report = await ImporterAsync(Csv(Enumerable.Range(1, Notices), titre: i => i == 7 ? "titre modifié 7" : $"titre {i}"));

        Assert.True(report.Success, report.ErrorMessage);
        Assert.Equal(1, report.ImportedArtworks);
        Assert.Equal(Notices - 1, report.SkippedArtworks);

        var apres = await DatesDeMajAsync();
        Assert.True(apres["L000007"] > avant["L000007"]);
        Assert.All(apres.Where(a => a.Key != "L000007"), a => Assert.Equal(avant[a.Key], a.Value));

        await using var context = _db.CreateContext();
        var oeuvre = await context.Artworks.Include(a => a.Domains).Include(a => a.Artists).SingleAsync(a => a.Reference == "L000007");
        Assert.Equal("titre modifié 7", oeuvre.Title);
        Assert.Equal(2, oeuvre.Domains.Count);
        Assert.Single(oeuvre.Artists);
        Assert.Equal(Notices * 2, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM artwork_domain").SingleAsync());
    }

    [Fact]
    public async Task ChangementDeDomaineSeul_EstDetecte()
    {
        await ImporterAsync(Csv(Enumerable.Range(1, Notices)), onlyChanges: false);

        var report = await ImporterAsync(Csv(Enumerable.Range(1, Notices), domaine: i => i == 3 ? "domaine neuf" : $"domaine {i % 7};commun"));

        Assert.Equal(1, report.ImportedArtworks);
        await using var context = _db.CreateContext();
        var oeuvre = await context.Artworks.Include(a => a.Domains).SingleAsync(a => a.Reference == "L000003");
        Assert.Equal(new[] { "domaine neuf" }, oeuvre.Domains.Select(d => d.Name));
    }

    [Fact]
    public async Task NoticeAjoutee_EstEcrite_LesAutresIgnorees()
    {
        await ImporterAsync(Csv(Enumerable.Range(1, Notices)), onlyChanges: false);

        var report = await ImporterAsync(Csv(Enumerable.Range(1, Notices + 1)));

        Assert.True(report.Success, report.ErrorMessage);
        Assert.Equal(1, report.ImportedArtworks);
        Assert.Equal(Notices, report.SkippedArtworks);
        await using var context = _db.CreateContext();
        Assert.Equal(Notices + 1, await context.Artworks.CountAsync());
    }

    [Fact]
    public async Task NoticeDisparue_EstSupprimee_AvecSesLiens()
    {
        await ImporterAsync(Csv(Enumerable.Range(1, Notices)), onlyChanges: false);

        var report = await ImporterAsync(Csv(Enumerable.Range(1, Notices).Where(i => i != 5)));

        Assert.True(report.Success, report.ErrorMessage);
        Assert.Equal(1, report.RemovedArtworks);
        Assert.Equal(0, report.ImportedArtworks);
        await using var context = _db.CreateContext();
        Assert.Equal(Notices - 1, await context.Artworks.CountAsync());
        Assert.False(await context.Artworks.AnyAsync(a => a.Reference == "L000005"));
        // Les liens de l'œuvre supprimée sont partis avec elle
        Assert.Equal((Notices - 1) * 2, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM artwork_domain").SingleAsync());
        Assert.Equal(Notices - 1, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM artwork_artist").SingleAsync());
    }

    [Fact]
    public async Task DisparitionMassive_EstRefusee_RienNEstSupprime()
    {
        await ImporterAsync(Csv(Enumerable.Range(1, Notices)), onlyChanges: false);

        var report = await ImporterAsync(Csv(Enumerable.Range(1, Notices / 3))); // fichier tronqué : les deux tiers manquent

        Assert.False(report.Success);
        Assert.Equal(0, report.RemovedArtworks);
        Assert.Contains("Suppression refusée", report.ErrorMessage);
        await using var context = _db.CreateContext();
        Assert.Equal(Notices, await context.Artworks.CountAsync());
    }

    [Fact]
    public async Task ParsingIncomplet_NeSupprimeRien()
    {
        await ImporterAsync(Csv(Enumerable.Range(1, Notices)), onlyChanges: false);
        var fichier = Csv(Enumerable.Range(1, Notices).Where(i => i != 5));
        File.AppendAllText(fichier, "\"ligne tronquée\"\n"); // rejetée au parsing : on ignore si c'était la notice 5

        var report = await ImporterAsync(fichier);

        Assert.False(report.Success);
        Assert.Equal(0, report.RemovedArtworks);
        await using var context = _db.CreateContext();
        Assert.True(await context.Artworks.AnyAsync(a => a.Reference == "L000005"));
    }

    [Fact]
    public async Task OeuvreSansEmpreinte_EstReecriteUneFois_PuisIgnoree()
    {
        var fichier = Csv(Enumerable.Range(1, Notices));
        await ImporterAsync(fichier, onlyChanges: false);
        await using (var context = _db.CreateContext())
            await context.Database.ExecuteSqlRawAsync("UPDATE artwork SET content_hash = NULL"); // base d'avant les empreintes

        var premiere = await ImporterAsync(fichier);
        var seconde = await ImporterAsync(fichier);

        Assert.Equal(Notices, premiere.ImportedArtworks);
        Assert.Equal(0, seconde.ImportedArtworks);
        Assert.Equal(Notices, seconde.SkippedArtworks);
    }

    [Fact]
    public async Task ImportComplet_EcritLesEmpreintes()
    {
        await ImporterAsync(Csv(Enumerable.Range(1, 5)), onlyChanges: false);

        await using var context = _db.CreateContext();
        Assert.Equal(5, await context.Artworks.CountAsync(a => a.ContentHash != null));
    }
}
