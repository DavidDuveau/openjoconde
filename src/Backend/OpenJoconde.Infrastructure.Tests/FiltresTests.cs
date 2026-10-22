using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.API.Controllers;
using OpenJoconde.API.Dtos;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Filtres (artiste, domaine, technique, période), tri par titre et listes de référence de l'API (TECH-12).
/// Le fichier de test porte 40 notices : domaine « domaine i%7 », technique « technique i%13 », période « période i%5 »,
/// auteur « AUTEUR(i%40) Prénom ».
/// </summary>
public class FiltresTests : IAsyncLifetime
{
    private const int Notices = 40;

    private readonly ImportDatabase _db = new();
    private string? _fichier;

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();
        _fichier = GeneratedCsv.Create(Enumerable.Range(1, Notices), i => i % 10 == 0 ? "" : $"Titre {i:D2}");
        await using var context = _db.CreateContext();
        var report = await _db.CreateImportService(context).ImportFromFileAsync(_fichier);
        Assert.True(report.Success, report.ErrorMessage);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        if (_fichier != null)
            File.Delete(_fichier);
    }

    private static HashSet<string> References(Func<int, bool> condition) =>
        Enumerable.Range(1, Notices).Where(condition).Select(i => $"L{i:D6}").ToHashSet();

    private static PaginatedResult<T> Resultat<T>(ActionResult<PaginatedResult<T>> reponse) =>
        Assert.IsType<PaginatedResult<T>>(Assert.IsType<OkObjectResult>(reponse.Result).Value);

    private async Task<PaginatedResult<Artwork>> ListerAsync(
        Guid? artistId = null, Guid? domainId = null, Guid? techniqueId = null, Guid? periodId = null,
        string? search = null, string? sortBy = null)
    {
        var controleur = new ArtworksController(_db.CreateContext(), NullLogger<ArtworksController>.Instance);
        return Resultat(await controleur.GetArtworks(1, 100, search, null, artistId, domainId, techniqueId, periodId, sortBy));
    }

    private async Task<Guid> IdAsync<T>(Func<OpenJocondeDbContext, IQueryable<T>> table, Func<T, Guid> id, Func<T, string> nom, string valeur) where T : class
    {
        await using var context = _db.CreateContext();
        return (await table(context).AsNoTracking().ToListAsync()).Where(x => nom(x) == valeur).Select(id).Single();
    }

    private Task<Guid> DomaineAsync(string nom) => IdAsync(c => c.Domains, d => d.Id, d => d.Name, nom);
    private Task<Guid> TechniqueAsync(string nom) => IdAsync(c => c.Techniques, t => t.Id, t => t.Name, nom);
    private Task<Guid> PeriodeAsync(string nom) => IdAsync(c => c.Periods, p => p.Id, p => p.Name, nom);

    [Fact]
    public async Task FiltreParDomaine_NeGardeQueLesOeuvresDuDomaine()
    {
        var resultat = await ListerAsync(domainId: await DomaineAsync("domaine 3"));

        Assert.Equal(References(i => i % 7 == 3), resultat.Items.Select(a => a.Reference).ToHashSet());
        Assert.Equal(resultat.Items.Count, resultat.TotalItems);
    }

    [Fact]
    public async Task FiltreParDomaineCommun_GardeToutesLesOeuvres()
    {
        var resultat = await ListerAsync(domainId: await DomaineAsync("commun"));

        Assert.Equal(Notices, resultat.TotalItems);
    }

    [Fact]
    public async Task FiltresCumules_GardentLIntersection()
    {
        var resultat = await ListerAsync(
            domainId: await DomaineAsync("domaine 3"),
            techniqueId: await TechniqueAsync("technique 11"),
            periodId: await PeriodeAsync("période 4"));

        Assert.Equal(References(i => i % 7 == 3 && i % 13 == 11 && i % 5 == 4), resultat.Items.Select(a => a.Reference).ToHashSet());
        Assert.NotEmpty(resultat.Items); // i = 24 : sinon le test ne prouverait rien
    }

    [Fact]
    public async Task FiltreParArtiste_NeGardeQueSesOeuvres()
    {
        await using var context = _db.CreateContext();
        var artiste = await context.Artists.AsNoTracking().SingleAsync(a => a.LastName == "AUTEUR7");

        var resultat = await ListerAsync(artistId: artiste.Id);

        Assert.Equal(References(i => i % 40 == 7), resultat.Items.Select(a => a.Reference).ToHashSet());
    }

    [Fact]
    public async Task FiltreEtRecherche_SeCumulent()
    {
        var resultat = await ListerAsync(domainId: await DomaineAsync("domaine 3"), search: "Titre 1");

        Assert.Equal(References(i => i % 7 == 3 && i % 10 != 0 && $"Titre {i:D2}".StartsWith("Titre 1")),
            resultat.Items.Select(a => a.Reference).ToHashSet());
    }

    [Fact]
    public async Task FiltreSansCorrespondance_RendUneListeVide()
    {
        var resultat = await ListerAsync(domainId: Guid.NewGuid());

        Assert.Empty(resultat.Items);
        Assert.Equal(0, resultat.TotalItems);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TriDecroissant_PlaceLesTitresDeZAAEtLesSansTitreEnDernier(bool filtre)
    {
        var resultat = await ListerAsync(
            domainId: filtre ? await DomaineAsync("commun") : null, sortBy: "title_desc");

        var titres = resultat.Items.Select(a => a.Title).ToList();
        var avecTitre = titres.Where(t => t != null).ToList();
        Assert.Equal(avecTitre.OrderByDescending(t => t, StringComparer.Ordinal), avecTitre);
        Assert.Equal("Titre 39", titres[0]);
        Assert.All(titres.TakeLast(Notices / 10), Assert.Null);
    }

    [Fact]
    public async Task TriInconnu_EstRefuse()
    {
        var controleur = new ArtworksController(_db.CreateContext(), NullLogger<ArtworksController>.Instance);

        var reponse = await controleur.GetArtworks(sortBy: "relevance");

        Assert.IsType<BadRequestObjectResult>(reponse.Result);
    }

    [Fact]
    public async Task Techniques_SePaginentEtSeCherchentParUnePartieDuNom()
    {
        var controleur = new ReferentielsController(_db.CreateContext());

        var tout = Resultat(await controleur.GetTechniques(pageSize: 5));
        var cherche = Resultat(await controleur.GetTechniques(name: "NIQUE 1", pageSize: 100));

        Assert.Equal(13, tout.TotalItems);
        Assert.Equal(5, tout.Items.Count);
        Assert.Equal(new[] { "technique 1", "technique 10", "technique 11", "technique 12" }, cherche.Items.Select(t => t.Name));
    }

    [Fact]
    public async Task DomainesEtPeriodes_SontListes()
    {
        var controleur = new ReferentielsController(_db.CreateContext());

        var domaines = Resultat(await controleur.GetDomains(pageSize: 100));
        var periodes = Resultat(await controleur.GetPeriods(name: "période 4"));

        Assert.Equal(8, domaines.TotalItems); // domaine 0 à 6 et « commun »
        Assert.Equal("période 4", Assert.Single(periodes.Items).Name);
    }
}
