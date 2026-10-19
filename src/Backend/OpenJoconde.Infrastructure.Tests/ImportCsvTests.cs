using Microsoft.EntityFrameworkCore;
using OpenJoconde.Core.Models;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Import de bout en bout du CSV Joconde, format de l'export publié sur data.gouv.fr (TECH-28).
/// </summary>
public class ImportCsvTests : IAsyncLifetime
{
    private readonly ImportDatabase _db = new();

    public Task InitializeAsync() => _db.InitializeAsync();

    public Task DisposeAsync() => _db.DisposeAsync();

    private async Task<ImportReport> ImporterAsync(string fixture)
    {
        await using var context = _db.CreateContext();
        return await _db.CreateImportService(context).ImportFromFileAsync(ImportDatabase.FixturePath(fixture));
    }

    [Fact]
    public async Task Import_ExtraitReel_EcritOeuvresMuseesDomainesEtTechniques()
    {
        // 20 premières notices du fichier publié : 3 musées, 11 domaines, 18 techniques, aucun auteur
        var report = await ImporterAsync("joconde_extrait_reel.csv");

        Assert.True(report.Success, report.ErrorMessage);
        await using var context = _db.CreateContext();
        Assert.Equal(20, await context.Artworks.CountAsync());
        Assert.Equal(3, await context.Museums.CountAsync());
        Assert.Equal(11, await context.Domains.CountAsync());
        Assert.Equal(18, await context.Techniques.CountAsync());
        Assert.Equal(0, await context.Artists.CountAsync());
    }

    [Fact]
    public async Task Import_ExtraitReel_LitLesCoordonneesEnTexteLatLon()
    {
        await ImporterAsync("joconde_extrait_reel.csv");

        await using var context = _db.CreateContext();
        var musee = await context.Museums.SingleAsync(m => m.Code == "M0988");
        Assert.Equal("musée des beaux-arts", musee.Name);
        Assert.Equal("Valence", musee.City);
        Assert.Equal("Auvergne-Rhône-Alpes", musee.Region);
        Assert.Equal(44.931125, musee.Latitude);
        Assert.Equal(4.889563, musee.Longitude);
    }

    [Fact]
    public async Task Import_ExtraitReel_LieChaqueOeuvreAuxDomainesDeSaNotice()
    {
        await ImporterAsync("joconde_extrait_reel.csv");

        await using var context = _db.CreateContext();
        var oeuvre = await context.Artworks.Include(a => a.Domains).SingleAsync(a => a.Reference == "09880004412");
        Assert.Equal(new[] { "archéologie", "gallo-romain" }, oeuvre.Domains.Select(d => d.Name).OrderBy(n => n));
        Assert.Equal("jeton", oeuvre.Denomination);
        Assert.Equal("D 989.4.15", oeuvre.InventoryNumber);
    }

    [Fact]
    public async Task Import_CasParticuliers_GardeGuillemetsBarresEtRetoursALaLigne()
    {
        var report = await ImporterAsync("joconde_cas_particuliers.csv");

        await using var context = _db.CreateContext();
        var references = await context.Artworks.Select(a => a.Reference).OrderBy(r => r).ToListAsync();
        Assert.Equal(new[] { "T1", "T2", "T3", "T5" }, references);

        var oeuvre = await context.Artworks.Include(a => a.Domains).SingleAsync(a => a.Reference == "T2");
        Assert.Equal("Titre avec \"guillemets\" et | barre", oeuvre.Title);
        Assert.Equal("ligne 1\nligne 2 avec } et {", oeuvre.Description);
        Assert.Equal(new[] { "peinture", "sculpture" }, oeuvre.Domains.Select(d => d.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task Import_LigneTronquee_EstIgnoreeSansPerdreLesAutres()
    {
        // La dernière ligne du fichier n'a que 2 valeurs sur 68 : elle est rejetée, le reste est importé
        var report = await ImporterAsync("joconde_cas_particuliers.csv");

        Assert.True(report.Success, report.ErrorMessage);
        Assert.Equal(4, report.TotalArtworks);
        await using var context = _db.CreateContext();
        Assert.False(await context.Artworks.AnyAsync(a => a.Reference == "T4"));
    }

    [Fact]
    public async Task Import_ValeursPlusLonguesQueLesColonnes_SontTronqueesSansPerdreLeLot()
    {
        // Dans l'export réel : titres > 500 (57 notices), n° d'inventaire > 500, techniques à 395 caractères, auteurs > 5 000
        var report = await ImporterAsync("joconde_cas_particuliers.csv");

        Assert.True(report.Success, report.ErrorMessage);
        await using var context = _db.CreateContext();
        Assert.Equal(4, await context.Artworks.CountAsync());

        var oeuvre = await context.Artworks.Include(a => a.Artists).ThenInclude(aa => aa.Artist).Include(a => a.Techniques)
            .SingleAsync(a => a.Reference == "T5");
        Assert.Equal(500, oeuvre.Title!.Length);
        Assert.Equal(500, oeuvre.InventoryNumber!.Length);
        Assert.Equal(500, Assert.Single(oeuvre.Artists).Artist.FirstName!.Length);

        // Deux techniques qui ne diffèrent qu'après 200 caractères n'en font qu'une
        Assert.Equal(200, Assert.Single(oeuvre.Techniques).Name.Length);
    }

    [Fact]
    public async Task ImportFromFile_ExtensionInconnue_EstRefusee()
    {
        await using var context = _db.CreateContext();
        var service = _db.CreateImportService(context);

        await Assert.ThrowsAsync<NotSupportedException>(() => service.ImportFromFileAsync("donnees.txt"));
    }
}
