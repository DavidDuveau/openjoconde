using Microsoft.AspNetCore.Mvc;
using OpenJoconde.Core.Dtos;
using OpenJoconde.Core.Models;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>Filtre par plage d'années de création (US-RECHERCHE-02), lue dans la date en texte libre.</summary>
public class FiltreParDateTests : IAsyncLifetime
{
    // Notices 1 à 8 : 1850, -500 (5e siècle av JC), 1900, 1920, 1720, illisible, vide, 1850 (l'indice 0 n'est pas utilisé)
    private static readonly string[] Dates = { "", "1850", "5e siècle av JC", "1ère moitié 20e siècle", "1920", "vers 1720-1750", "néolithique", "", "1850" };

    private readonly ImportDatabase _db = new();
    private string? _fichier;

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();
        _fichier = GeneratedCsv.Create(Enumerable.Range(1, 8), i => $"Titre {i}",
            extra: i => new Dictionary<string, string> { ["Periode_de_creation"] = Dates[i] });
        await using var context = _db.CreateContext();
        Assert.True((await _db.CreateImportService(context).ImportFromFileAsync(_fichier)).Success);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        if (_fichier != null)
            File.Delete(_fichier);
    }

    private async Task<List<string>> ListerAsync(int? de = null, int? a = null, string? tri = "date")
    {
        var reponse = await _db.CreateArtworksController().GetArtworks(1, 100, null, null, null, null, null, null, tri, yearFrom: de, yearTo: a);
        var page = Assert.IsType<PaginatedResult<ArtworkListItemDto>>(Assert.IsType<OkObjectResult>(reponse.Result).Value);
        return page.Items.Select(o => o.Reference).ToList();
    }

    private static string Ref(int i) => $"L{i:D6}";

    private static IEnumerable<string> Attendues(params int[] numeros) => numeros.Select(Ref).Order();

    [Fact]
    public async Task UneBorneDeDebut_GardeLesOeuvresCreeesDepuisCetteAnnee() =>
        Assert.Equal(Attendues(1, 3, 4, 8), (await ListerAsync(de: 1850)).Order());

    [Fact]
    public async Task UneBorneDeFin_GardeLesOeuvresCreeesJusquaCetteAnnee() =>
        Assert.Equal(Attendues(2, 5), (await ListerAsync(a: 1800)).Order());

    [Fact]
    public async Task UnePlage_GardeLesOeuvresDedans_BornesIncluses()
    {
        Assert.Equal(Attendues(1, 3, 4, 8), (await ListerAsync(de: 1850, a: 1920)).Order());
        Assert.Equal(Attendues(4), (await ListerAsync(de: 1920, a: 1920)).Order());
    }

    [Fact]
    public async Task LesAnneesAvantJesusChrist_SontNegatives() =>
        Assert.Equal(new[] { Ref(2) }, await ListerAsync(a: -400));

    [Fact]
    public async Task UneDateIllisibleOuVide_NEstDansAucunePlage()
    {
        var toutes = await ListerAsync(de: -100000, a: 100000);

        Assert.Equal(Attendues(1, 2, 3, 4, 5, 8), toutes.Order());
    }

    [Fact]
    public async Task SansBorne_ToutesLesOeuvresRestent_MemeCellesSansDateLisible() =>
        Assert.Equal(8, (await ListerAsync()).Count);

    [Fact]
    public async Task LaPlageSeCumuleAvecLeTri()
    {
        Assert.Equal(new[] { Ref(3), Ref(4) }, await ListerAsync(de: 1900, tri: "date"));
        Assert.Equal(new[] { Ref(4), Ref(3) }, await ListerAsync(de: 1900, tri: "date_desc"));
    }

    [Fact]
    public async Task UneBorneDeDebutApresLaBorneDeFin_EstRefusee()
    {
        var reponse = await _db.CreateArtworksController().GetArtworks(1, 10, null, null, null, null, null, null, null, yearFrom: 1900, yearTo: 1800);

        var erreur = Assert.IsType<BadRequestObjectResult>(reponse.Result);
        Assert.Contains("année de début", erreur.Value?.ToString());
    }
}
