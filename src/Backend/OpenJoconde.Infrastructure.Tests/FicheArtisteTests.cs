using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.API.Controllers;
using OpenJoconde.Core.Dtos;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;
using OpenJoconde.Infrastructure.Data.Repositories;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// La fiche et la liste des artistes disent combien d'œuvres chacun a ; les œuvres elles-mêmes se lisent par la
/// recherche d'œuvres filtrée par artiste (TECH-40).
/// </summary>
public class FicheArtisteTests : IAsyncLifetime
{
    private readonly ImportDatabase _db = new();
    private string? _fichier;

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();
        // 6 notices : trois de DUPONT, deux de MARTIN, une de BERNARD
        var auteurs = new[] { "DUPONT Jean", "DUPONT Jean", "DUPONT Jean", "MARTIN Paul", "MARTIN Paul", "BERNARD Luc" };
        _fichier = GeneratedCsv.Create(Enumerable.Range(1, 6), i => $"Titre {i}", author: i => auteurs[i - 1]);
        await using var context = _db.CreateContext();
        Assert.True((await _db.CreateImportService(context).ImportFromFileAsync(_fichier)).Success);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        if (_fichier != null)
            File.Delete(_fichier);
    }

    private ArtistsController Controleur() =>
        new(new ArtistRepository(_db.CreateContext(), NullLogger<ArtistRepository>.Instance), NullLogger<ArtistsController>.Instance);

    private async Task<PaginatedResult<ArtistDto>> ListerAsync(string? nom = null) =>
        Assert.IsType<PaginatedResult<ArtistDto>>(Assert.IsType<OkObjectResult>((await Controleur().SearchArtists(nom)).Result).Value);

    [Fact]
    public async Task LaListe_DitCombienDOeuvresAChaqueArtiste()
    {
        var artistes = await ListerAsync();

        Assert.Equal(new[] { 1, 3, 2 }.OrderBy(n => n), artistes.Items.Select(a => a.ArtworksCount).OrderBy(n => n));
        Assert.Equal(3, artistes.Items.Single(a => a.LastName == "DUPONT").ArtworksCount);
    }

    [Fact]
    public async Task LaFiche_DitCombienDOeuvresALArtiste()
    {
        var dupont = (await ListerAsync("dupont")).Items.Single();

        var reponse = await Controleur().GetArtist(dupont.Id);

        var fiche = Assert.IsType<ArtistDto>(Assert.IsType<OkObjectResult>(reponse.Result).Value);
        Assert.Equal("DUPONT", fiche.LastName);
        Assert.Equal(3, fiche.ArtworksCount);
    }

    [Fact]
    public async Task LesOeuvresDeLArtiste_SeLisentParLaRechercheFiltreeParArtiste()
    {
        var martin = (await ListerAsync("martin")).Items.Single();

        var reponse = await _db.CreateArtworksController().GetArtworks(artistId: martin.Id);

        var oeuvres = Assert.IsType<PaginatedResult<ArtworkListItemDto>>(Assert.IsType<OkObjectResult>(reponse.Result).Value);
        Assert.Equal(martin.ArtworksCount, oeuvres.TotalItems);
        Assert.Equal(2, oeuvres.TotalItems);
    }

    [Fact]
    public async Task LaFiche_DonneLeContexteEnrichi_BiographieCourantEtSource()
    {
        var bernard = (await ListerAsync("bernard")).Items.Single();
        await using (var context = _db.CreateContext())
        {
            var artiste = await context.Artists.FindAsync(bernard.Id);
            artiste!.Biography = "Peintre français.";
            artiste.Movement = "impressionnisme";
            artiste.WikipediaUrl = "https://fr.wikipedia.org/wiki/Luc_Bernard";
            await context.SaveChangesAsync();
        }

        var fiche = Assert.IsType<ArtistDto>(Assert.IsType<OkObjectResult>((await Controleur().GetArtist(bernard.Id)).Result).Value);

        Assert.Equal("Peintre français.", fiche.Biography);
        Assert.Equal("impressionnisme", fiche.Movement);
        Assert.Equal("https://fr.wikipedia.org/wiki/Luc_Bernard", fiche.WikipediaUrl);
    }

    [Fact]
    public async Task UneFicheInconnue_Repond404() =>
        Assert.IsType<NotFoundResult>((await Controleur().GetArtist(Guid.NewGuid())).Result);
}
