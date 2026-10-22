using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.API.Controllers;
using OpenJoconde.Core.Models;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Tris par date de création, pertinence et artiste (TECH-38).
/// </summary>
public class TrisTests : IAsyncLifetime
{
    private readonly ImportDatabase _db = new();
    private readonly List<string> _fichiers = new();

    public Task InitializeAsync() => _db.InitializeAsync();

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        _fichiers.ForEach(File.Delete);
    }

    private async Task ImporterAsync(int nombre, Func<int, string> titre, Func<int, IDictionary<string, string>>? extra = null, Func<int, string>? auteur = null)
    {
        var fichier = GeneratedCsv.Create(Enumerable.Range(1, nombre), titre, author: auteur, extra: extra);
        _fichiers.Add(fichier);
        await using var context = _db.CreateContext();
        var rapport = await _db.CreateImportService(context).ImportFromFileAsync(fichier);
        Assert.True(rapport.Success, rapport.ErrorMessage);
    }

    private async Task<ActionResult<PaginatedResult<Artwork>>> ListerAsync(string? tri, string? recherche = null)
    {
        var controleur = new ArtworksController(_db.CreateContext(), NullLogger<ArtworksController>.Instance);
        return await controleur.GetArtworks(1, 100, recherche, null, null, null, null, null, tri);
    }

    private static List<string> References(ActionResult<PaginatedResult<Artwork>> reponse) =>
        Assert.IsType<PaginatedResult<Artwork>>(Assert.IsType<OkObjectResult>(reponse.Result).Value).Items.Select(a => a.Reference).ToList();

    private static string Ref(int i) => $"L{i:D6}";

    private static readonly string?[] Dates =
    {
        null, "1850", "4e quart 19e siècle", "1ère moitié 20e siècle", "néolithique", "18e siècle",
        "19e-20e siècle", "vers 1720-1750", "", "2e quart 17e siècle ; 1er quart 18e siècle"
    };

    private Task ImporterDatesAsync() =>
        ImporterAsync(9, i => $"Titre {i}", i => new Dictionary<string, string> { ["Periode_de_creation"] = Dates[i]! });

    [Fact]
    public async Task TriParDate_PlusAncienDAbord_SansDateLisibleALaFin()
    {
        await ImporterDatesAsync();

        var references = References(await ListerAsync("date"));

        Assert.Equal(new[] { 9, 5, 7, 6, 1, 2, 3 }.Select(Ref), references.Take(7));
        Assert.Equal(new[] { Ref(4), Ref(8) }.OrderBy(r => r), references.Skip(7).OrderBy(r => r));
    }

    [Fact]
    public async Task TriParDateDecroissante_PlusRecentDAbord_SansDateLisibleALaFin()
    {
        await ImporterDatesAsync();

        var references = References(await ListerAsync("date_desc"));

        Assert.Equal(new[] { 3, 2, 1, 6, 7, 5, 9 }.Select(Ref), references.Take(7));
        Assert.Equal(new[] { Ref(4), Ref(8) }.OrderBy(r => r), references.Skip(7).OrderBy(r => r));
    }

    [Fact]
    public async Task TriParPertinence_MetEnPremierLOeuvreOuLeMotRevientLePlusSouvent()
    {
        await ImporterAsync(3, i => i switch { 1 => "Vase", 2 => "Vase vase vase grand vase", _ => "Cruche" });

        var references = References(await ListerAsync("relevance", "vase"));

        Assert.Equal(new[] { Ref(2), Ref(1) }, references);
    }

    [Fact]
    public async Task TriParArtiste_SuitLeNomDuPremierArtiste_SansArtisteALaFin()
    {
        await ImporterAsync(3, i => $"Titre {i}", auteur: i => i switch { 1 => "BETA Bob", 2 => "ALPHA Anne", _ => "" });

        var references = References(await ListerAsync("artist", "description"));

        Assert.Equal(new[] { Ref(2), Ref(1), Ref(3) }, references);
    }

    [Theory]
    [InlineData("relevance", null)]
    [InlineData("relevance", "!!")]
    [InlineData("artist", null)]
    [InlineData("hasard", null)]
    public async Task UnTriImpossible_EstRefuse(string tri, string? recherche)
    {
        var reponse = await ListerAsync(tri, recherche);

        Assert.IsType<BadRequestObjectResult>(reponse.Result);
    }
}
