using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.API.Controllers;
using OpenJoconde.Core.Dtos;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Recherche géographique : filtres par région et département du musée, tri par distance, liste géographique.
/// Quatre musées (Paris, Lyon, Marseille, un sans coordonnées) conservent chacun deux œuvres (RECHERCHE-05).
/// </summary>
public class GeographieTests : IAsyncLifetime
{
    private readonly ImportDatabase _db = new();

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();
        await using var context = _db.CreateContext();
        var musees = new[]
        {
            Musee("Paris", "Ile-de-France", "Paris", 48.8566, 2.3522),
            Musee("Lyon", "Auvergne-Rhône-Alpes", "Rhône", 45.7640, 4.8357),
            Musee("Marseille", "Provence-Alpes-Côte-d'Azur", "Bouches-du-Rhône", 43.2965, 5.3698),
            Musee("Sans carte", "Auvergne-Rhône-Alpes", "Ain", null, null)
        };
        context.Museums.AddRange(musees);
        for (var m = 0; m < musees.Length; m++)
            for (var i = 1; i <= 2; i++)
                context.Artworks.Add(new Artwork { Reference = $"{musees[m].Name}-{i}", Title = $"Titre {musees[m].Name} {i}", MuseumId = musees[m].Id });
        context.Artworks.Add(new Artwork { Reference = "Sans-musee", Title = "Titre sans musée" });
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => _db.DisposeAsync();

    private static Museum Musee(string nom, string region, string departement, double? latitude, double? longitude) =>
        new() { Id = Guid.NewGuid(), Name = nom, Region = region, Department = departement, Latitude = latitude, Longitude = longitude };

    private async Task<List<string>> ListerAsync(string? tri = null, string? region = null, string? departement = null, double? latitude = null, double? longitude = null)
    {
        var reponse = await _db.CreateArtworksController().GetArtworks(1, 100, null, null, null, null, null, null, tri, region, departement, latitude, longitude);
        var page = Assert.IsType<PaginatedResult<OpenJoconde.Core.Dtos.ArtworkListItemDto>>(Assert.IsType<OkObjectResult>(reponse.Result).Value);
        return page.Items.Select(a => a.Reference).ToList();
    }

    [Fact]
    public async Task FiltreParRegion_NeGardeQueLesOeuvresDesMuseesDeLaRegion()
    {
        var references = await ListerAsync(region: "Auvergne-Rhône-Alpes");

        Assert.Equal(new[] { "Lyon-1", "Lyon-2", "Sans carte-1", "Sans carte-2" }.Order(), references.Order());
    }

    [Fact]
    public async Task FiltreParDepartement_NeGardeQueLesOeuvresDesMuseesDuDepartement()
    {
        var references = await ListerAsync(departement: "Paris");

        Assert.Equal(new[] { "Paris-1", "Paris-2" }, references.Order());
    }

    [Fact]
    public async Task FiltresRegionEtDepartement_SeCumulent()
    {
        Assert.Equal(new[] { "Lyon-1", "Lyon-2" }, (await ListerAsync(region: "Auvergne-Rhône-Alpes", departement: "Rhône")).Order());
        Assert.Empty(await ListerAsync(region: "Ile-de-France", departement: "Rhône"));
    }

    [Fact]
    public async Task TriParDistance_PlaceLesMuseesProchesAvantLesLointains_EtLesNonLocalisesALaFin()
    {
        // Depuis Dijon (47,32 ; 5,04) : Lyon (190 km) avant Paris (260 km) avant Marseille (410 km)
        var references = await ListerAsync("distance", latitude: 47.32, longitude: 5.04);

        var musees = references.Select(r => r.Split('-')[0]).Distinct().ToList();
        Assert.Equal(new[] { "Lyon", "Paris", "Marseille" }, musees.Take(3));
        // Musée non localisé et œuvre sans musée : même rang, départagés par identifiant
        Assert.Equal(new[] { "Sans", "Sans carte" }, musees.Skip(3).Order());
        Assert.Equal(9, references.Count);
    }

    [Fact]
    public async Task TriParDistance_SeCumuleAvecUnFiltre()
    {
        var references = await ListerAsync("distance", region: "Auvergne-Rhône-Alpes", latitude: 45.0, longitude: 5.0);

        Assert.Equal(new[] { "Lyon", "Sans carte" }, references.Select(r => r.Split('-')[0]).Distinct());
    }

    [Fact]
    public async Task TriParDistance_SansPosition_EstRefuse()
    {
        var reponse = await _db.CreateArtworksController().GetArtworks(1, 10, null, null, null, null, null, null, "distance");

        Assert.Contains("latitude", Assert.IsType<BadRequestObjectResult>(reponse.Result).Value!.ToString());
    }

    [Fact]
    public async Task MuseesLocalises_RendLesMuseesAvecCoordonneesEtLeTotal()
    {
        var controleur = new MuseumsController(new MuseumRepository(_db.CreateContext(), NullLogger<MuseumRepository>.Instance), NullLogger<MuseumsController>.Instance);

        var locations = Assert.IsType<MuseumLocationsDto>(Assert.IsType<OkObjectResult>((await controleur.GetLocations()).Result).Value);

        Assert.Equal(4, locations.Total);
        Assert.Equal(new[] { "Lyon", "Marseille", "Paris" }, locations.Items.Select(m => m.Name));
        var paris = locations.Items.Single(m => m.Name == "Paris");
        Assert.Equal((48.8566, 2.3522), (paris.Latitude, paris.Longitude));
    }

    [Fact]
    public async Task ListeGeographique_RangeLesDepartementsParRegionAvecLeurNombreDeMusees()
    {
        var controleur = new MuseumsController(new MuseumRepository(_db.CreateContext(), NullLogger<MuseumRepository>.Instance), NullLogger<MuseumsController>.Instance);

        var regions = Assert.IsType<List<RegionDto>>(Assert.IsType<OkObjectResult>((await controleur.GetGeography()).Result).Value);

        Assert.Equal(new[] { "Auvergne-Rhône-Alpes", "Ile-de-France", "Provence-Alpes-Côte-d'Azur" }, regions.Select(r => r.Name));
        var auvergne = regions[0];
        Assert.Equal(2, auvergne.Museums);
        Assert.Equal(new[] { ("Ain", 1), ("Rhône", 1) }, auvergne.Departments.Select(d => (d.Name, d.Museums)));
    }
}

/// <summary>Dépôt de musées dont chaque lecture géographique échoue (base indisponible).</summary>
internal sealed class FailingMuseumRepository : OpenJoconde.Core.Interfaces.IMuseumRepository
{
    public Task<List<RegionDto>> GetGeographyAsync() => throw new InvalidOperationException("base indisponible");
    public Task<MuseumLocationsDto> GetLocationsAsync() => throw new InvalidOperationException("base indisponible");

    public Task<Museum?> GetByIdAsync(Guid id) => throw new NotSupportedException();
    public Task<PaginatedResult<Museum>> SearchByNameAsync(string? name, int page = 1, int pageSize = 10) => throw new NotSupportedException();
    public Task<Museum> AddAsync(Museum museum) => throw new NotSupportedException();
    public Task<bool> UpdateAsync(Museum museum) => throw new NotSupportedException();
    public Task<bool> DeleteAsync(Guid id) => throw new NotSupportedException();
    public Task<HashSet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids) => throw new NotSupportedException();
    public Task<int> BulkUpsertAsync(IEnumerable<Museum> museums) => throw new NotSupportedException();
}

/// <summary>Pannes de la lecture géographique : l'API répond 500 avec un message, sans fuite de détail.</summary>
public class GeographiePanneTests
{
    private static MuseumsController Controleur() => new(new FailingMuseumRepository(), NullLogger<MuseumsController>.Instance);

    [Fact]
    public async Task ListeGeographique_EnPanne_Repond500()
    {
        var reponse = await Controleur().GetGeography();

        var resultat = Assert.IsType<ObjectResult>(reponse.Result);
        Assert.Equal(500, resultat.StatusCode);
        Assert.DoesNotContain("base indisponible", resultat.Value!.ToString());
    }

    [Fact]
    public async Task MuseesLocalises_EnPanne_Repond500()
    {
        var reponse = await Controleur().GetLocations();

        var resultat = Assert.IsType<ObjectResult>(reponse.Result);
        Assert.Equal(500, resultat.StatusCode);
        Assert.DoesNotContain("base indisponible", resultat.Value!.ToString());
    }
}
