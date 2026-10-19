using Microsoft.EntityFrameworkCore;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Tests de caractérisation de l'import JSON de bout en bout (TECH-01).
/// Chaque test attend le comportement correct ; ceux qui échouent portent la référence du backlog
/// technique qui les corrigera.
/// </summary>
public class ImportCaracterisationTests : IAsyncLifetime
{
    // Une base neuve par test : les tests ne partagent aucun état.
    private readonly ImportDatabase _db = new();

    public Task InitializeAsync() => _db.InitializeAsync();

    public Task DisposeAsync() => _db.DisposeAsync();

    private async Task<OpenJoconde.Core.Models.ImportReport> ImporterAsync(string fixture)
    {
        await using var context = _db.CreateContext();
        return await _db.CreateImportService(context).ImportFromJsonFileAsync(ImportDatabase.FixturePath(fixture));
    }

    [Fact]
    public async Task Import_UneOeuvre_EcritLOeuvreEnBase()
    {
        var report = await ImporterAsync("joconde_une_oeuvre.json");

        Assert.True(report.Success, report.ErrorMessage);
        await using var context = _db.CreateContext();
        var oeuvre = await context.Artworks.SingleAsync(a => a.Reference == "50510016755");
        Assert.Equal("Grotesques ; Grotesques (verso)", oeuvre.Title);
        Assert.Equal("PM 454", oeuvre.InventoryNumber);
    }

    [Fact]
    public async Task Import_UneOeuvre_ConserveDateEtLieuDeConservation()
    {
        await ImporterAsync("joconde_une_oeuvre.json");

        await using var context = _db.CreateContext();
        var oeuvre = await context.Artworks.SingleAsync(a => a.Reference == "50510016755");
        Assert.Equal("2e moitié 16e siècle", oeuvre.CreationDate);
        Assert.Equal("Paris ; musée de l'Ecole nationale supérieure des beaux-arts", oeuvre.ConservationPlace);
    }

    [Fact]
    public async Task Import_UneOeuvre_EcritLeMuseeAvecSesCoordonnees()
    {
        await ImporterAsync("joconde_une_oeuvre.json");

        await using var context = _db.CreateContext();
        var musee = await context.Museums.SingleAsync();
        Assert.Equal("musée de l'Ecole nationale supérieure des beaux-arts", musee.Name);
        Assert.Equal("M5051", musee.Code);
        Assert.Equal(2.33167, musee.Longitude);
        Assert.Equal(48.845748, musee.Latitude);
    }

    [Fact]
    [Trait("Backlog", "TECH-02")]
    public async Task Import_UneOeuvre_LieLArtisteALOeuvre()
    {
        await ImporterAsync("joconde_une_oeuvre.json");

        await using var context = _db.CreateContext();
        var oeuvre = await context.Artworks.Include(a => a.Artists).ThenInclude(aa => aa.Artist)
            .SingleAsync(a => a.Reference == "50510016755");
        var lien = Assert.Single(oeuvre.Artists);
        Assert.Equal("DUBREUIL", lien.Artist.LastName);
        Assert.Equal("Toussaint", lien.Artist.FirstName);
    }

    [Fact]
    public async Task Import_UneOeuvre_LieDomaineEtTechniqueALOeuvre()
    {
        await ImporterAsync("joconde_une_oeuvre.json");

        await using var context = _db.CreateContext();
        var oeuvre = await context.Artworks.Include(a => a.Domains).Include(a => a.Techniques)
            .SingleAsync(a => a.Reference == "50510016755");
        Assert.Equal("dessin", Assert.Single(oeuvre.Domains).Name);
        Assert.Single(oeuvre.Techniques);
    }

    [Fact]
    public async Task Import_DeuxFois_ConserveLesRelationsSansLesDupliquer()
    {
        await ImporterAsync("joconde_une_oeuvre.json");
        await ImporterAsync("joconde_une_oeuvre.json");

        await using var context = _db.CreateContext();
        var oeuvre = await context.Artworks
            .Include(a => a.Artists).Include(a => a.Domains).Include(a => a.Techniques)
            .SingleAsync(a => a.Reference == "50510016755");
        Assert.Single(oeuvre.Artists);
        Assert.Single(oeuvre.Domains);
        Assert.Single(oeuvre.Techniques);
    }

    [Fact]
    public async Task Import_UneOeuvre_UtiliseMateriauxTechniquesCommeTechnique()
    {
        await ImporterAsync("joconde_une_oeuvre.json");

        await using var context = _db.CreateContext();
        var technique = await context.Techniques.SingleAsync();
        Assert.Equal("plume, encre, lavis brun, sanguine", technique.Name);
    }

    [Fact]
    public async Task Import_AccoladesDansLeTexte_NePerdAucuneOeuvre()
    {
        var report = await ImporterAsync("joconde_accolades_dans_texte.json");

        await using var context = _db.CreateContext();
        var references = await context.Artworks.Select(a => a.Reference).OrderBy(r => r).ToListAsync();
        Assert.Equal(new[] { "A1", "A2", "A3" }, references);
    }

    [Fact]
    public async Task Import_DeuxFois_NeDuplique_PasLesOeuvres()
    {
        await ImporterAsync("joconde_une_oeuvre.json");
        await ImporterAsync("joconde_une_oeuvre.json");

        await using var context = _db.CreateContext();
        Assert.Equal(1, await context.Artworks.CountAsync());
        Assert.Equal(1, await context.Artists.CountAsync());
    }

    [Fact]
    public async Task Rapport_ImportedArtworks_CorrespondAuxLignesEnBase()
    {
        var report = await ImporterAsync("joconde_accolades_dans_texte.json");

        await using var context = _db.CreateContext();
        Assert.Equal(await context.Artworks.CountAsync(), report.ImportedArtworks);
    }

    [Fact]
    public async Task Import_ChampsMultivalues_EtCoordonneesEnTexte_SontDecoupes()
    {
        // Échantillon réel de la base : listes séparées par « ; », coordonnées « lat, lon »
        await ImporterAsync("joconde_echantillon_csv.json");

        await using var context = _db.CreateContext();
        Assert.Equal(2, await context.Artworks.CountAsync());
        Assert.Equal(new[] { "archéologie", "gallo-romain" }, await context.Domains.Select(d => d.Name).OrderBy(n => n).ToListAsync());
        Assert.Equal(5, await context.Periods.CountAsync());

        var musee = await context.Museums.SingleAsync();
        Assert.Equal("M0988", musee.Code);
        Assert.Equal(44.931125, musee.Latitude);
        Assert.Equal(4.889563, musee.Longitude);

        var oeuvre = await context.Artworks.Include(a => a.Periods).Include(a => a.Domains).Include(a => a.Techniques)
            .SingleAsync(a => a.Reference == "09880004412");
        Assert.Equal(5, oeuvre.Periods.Count);
        Assert.Equal(2, oeuvre.Domains.Count);
        Assert.Equal("Os", Assert.Single(oeuvre.Techniques).Name);
        Assert.Equal("1er siècle av JC ; 1er siècle ; 2e siècle ; 3e siècle ; 4e siècle", oeuvre.CreationDate);
    }

    [Fact]
    public async Task Import_FichierSansTableauRacine_EchoueAvecUnMessageExplicite()
    {
        var chemin = Path.Combine(Path.GetTempPath(), $"joconde_{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(chemin, "{ \"results\": [] }");
        try
        {
            await using var context = _db.CreateContext();
            var report = await _db.CreateImportService(context).ImportFromJsonFileAsync(chemin);

            Assert.False(report.Success);
            Assert.Contains("Erreur lors du parsing", report.ErrorMessage);
        }
        finally
        {
            File.Delete(chemin);
        }
    }
}
