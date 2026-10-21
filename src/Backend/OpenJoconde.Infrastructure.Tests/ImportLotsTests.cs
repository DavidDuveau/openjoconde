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

    private string CreerCsv(int nombre, Func<int, string> titre, string suffixe = "", Func<int, string>? auteur = null)
    {
        var chemin = GeneratedCsv.Create(Enumerable.Range(1, nombre), titre, suffixe, auteur);
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
        var liens = await context.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM artwork_domain").SingleAsync();
        Assert.Equal(Notices * 2, liens);
        Assert.Equal(Notices, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM artwork_technique").SingleAsync());
        Assert.Equal(Notices, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM artwork_period").SingleAsync());
        Assert.Equal(Notices, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM artwork_artist").SingleAsync());

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

        Assert.Equal(Notices * 2, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM artwork_domain").SingleAsync());
        Assert.Equal(Notices, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM artwork_artist").SingleAsync());
    }

    [Fact]
    public async Task Import_AuteursAuNomIdentiqueDansLeMemeLot_NeFontQuUnArtiste()
    {
        // Le parseur distingue ces deux graphies (espaces), la base les confond : un seul artiste, les deux œuvres liées
        var report = await ImporterAsync(CreerCsv(2, _ => "titre", auteur: i => i == 1 ? "MARTIN  Paul" : "MARTIN Paul"));

        Assert.True(report.Success, report.ErrorMessage);
        await using var context = _db.CreateContext();
        Assert.Equal(1, await context.Artists.CountAsync());
        var artisteId = await context.Artists.Select(a => a.Id).SingleAsync();
        var liens = await context.Database.SqlQueryRaw<Guid>("SELECT artist_id AS \"Value\" FROM artwork_artist").ToListAsync();
        Assert.Equal(new[] { artisteId, artisteId }, liens);
    }

    [Fact]
    public async Task Import_RattacheChaqueOeuvreASonMusee()
    {
        var report = await ImporterAsync(CreerCsv(Notices, i => $"titre {i}"));

        Assert.True(report.Success, report.ErrorMessage);
        await using var context = _db.CreateContext();
        Assert.Equal(3, await context.Museums.CountAsync());
        var oeuvres = await context.Artworks.Include(a => a.Museum).ToListAsync();
        Assert.All(oeuvres, a =>
        {
            var numero = int.Parse(a.Reference.TrimStart('L'));
            Assert.Equal($"musée {numero % 3}", a.Museum?.Name);
        });
    }

    [Fact]
    public async Task Reimport_GardeLesMuseesEtLeurRattachement()
    {
        await ImporterAsync(CreerCsv(Notices, i => $"titre {i}"));
        List<Guid> avant;
        await using (var context = _db.CreateContext())
            avant = await context.Museums.OrderBy(m => m.Name).Select(m => m.Id).ToListAsync();

        var report = await ImporterAsync(CreerCsv(Notices, i => $"titre {i}", " v2"));

        Assert.True(report.Success, report.ErrorMessage);
        await using var verif = _db.CreateContext();
        Assert.Equal(avant, await verif.Museums.OrderBy(m => m.Name).Select(m => m.Id).ToListAsync());
        Assert.Equal(Notices, await verif.Artworks.CountAsync(a => a.MuseumId != null));
    }

    [Fact]
    public async Task Oeuvres_FiltrablesParMusee()
    {
        await ImporterAsync(CreerCsv(Notices, i => $"titre {i}"));
        await using var context = _db.CreateContext();
        var musee = await context.Museums.SingleAsync(m => m.Name == "musée 1");
        var controleur = new OpenJoconde.API.Controllers.ArtworksController(
            context, Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenJoconde.API.Controllers.ArtworksController>.Instance);

        var reponse = await controleur.GetArtworks(page: 1, pageSize: 50, museumId: musee.Id);

        var page = Assert.IsType<OpenJoconde.Core.Models.PaginatedResult<OpenJoconde.Core.Models.Artwork>>(
            Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(reponse.Result).Value);
        Assert.Equal(Notices / 3, page.TotalItems);
        Assert.Equal(50, page.Items.Count);
        Assert.All(page.Items, a => Assert.Equal(musee.Id, a.MuseumId));
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
