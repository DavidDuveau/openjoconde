using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.API.Controllers;
using OpenJoconde.API.Dtos;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;
using OpenJoconde.Infrastructure.Data.Repositories;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Les routes d'écriture des musées et des artistes ne prennent que des DTO d'entrée : l'identifiant, les dates et les
/// œuvres liées restent au serveur, et les valeurs sont contrôlées avant d'atteindre la base (TECH-06).
/// </summary>
public class EntreesTests : IAsyncLifetime
{
    private readonly ImportDatabase _db = new();

    public Task InitializeAsync() => _db.InitializeAsync();
    public Task DisposeAsync() => _db.DisposeAsync();

    private MuseumsController Musees() =>
        new(new MuseumRepository(_db.CreateContext(), NullLogger<MuseumRepository>.Instance), NullLogger<MuseumsController>.Instance);

    private ArtistsController Artistes() =>
        new(new ArtistRepository(_db.CreateContext(), NullLogger<ArtistRepository>.Instance), NullLogger<ArtistsController>.Instance);

    private static IList<ValidationResult> Erreurs(object dto)
    {
        var resultats = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), resultats, validateAllProperties: true);
        return resultats;
    }

    [Fact]
    public async Task CreerUnMusee_LIdentifiantEtLesDatesViennentDuServeur()
    {
        var avant = DateTime.UtcNow.AddSeconds(-1);

        var reponse = await Musees().CreateMuseum(new MuseumInput { Name = "  Musée test  ", City = "Lyon", Website = "https://musee.example" });

        var cree = Assert.IsType<Museum>(Assert.IsType<CreatedAtActionResult>(reponse.Result).Value);
        Assert.NotEqual(Guid.Empty, cree.Id);
        Assert.Equal("Musée test", cree.Name);
        Assert.True(cree.CreatedAt >= avant && cree.UpdatedAt >= avant);
        Assert.Equal("Lyon", (await _db.CreateContext().Museums.AsNoTracking().SingleAsync()).City);
    }

    [Fact]
    public async Task ModifierUnMusee_GardeLaDateDeCreation_EtRemplaceLesChampsFournis()
    {
        var cree = Assert.IsType<Museum>(Assert.IsType<CreatedAtActionResult>((await Musees().CreateMuseum(new MuseumInput { Name = "Ancien", City = "Lyon", Phone = "01" })).Result).Value);

        var reponse = await Musees().UpdateMuseum(cree.Id, new MuseumInput { Name = "Nouveau" });

        Assert.IsType<NoContentResult>(reponse);
        var modifie = await _db.CreateContext().Museums.AsNoTracking().SingleAsync();
        Assert.Equal("Nouveau", modifie.Name);
        Assert.Null(modifie.City);
        Assert.Null(modifie.Phone);
        Assert.Equal(cree.Id, modifie.Id);
        Assert.Equal(cree.CreatedAt, modifie.CreatedAt, TimeSpan.FromMilliseconds(1));
        Assert.True(modifie.UpdatedAt >= cree.UpdatedAt);
    }

    [Fact]
    public async Task ModifierUnMuseeInconnu_Repond404() =>
        Assert.IsType<NotFoundResult>(await Musees().UpdateMuseum(Guid.NewGuid(), new MuseumInput { Name = "x" }));

    [Fact]
    public async Task CreerUnArtiste_LIdentifiantEtLesDatesViennentDuServeur()
    {
        var reponse = await Artistes().CreateArtist(new ArtistInput { LastName = "DUPONT", FirstName = "Jean" });

        var cree = Assert.IsType<Artist>(Assert.IsType<CreatedAtActionResult>(reponse.Result).Value);
        Assert.NotEqual(Guid.Empty, cree.Id);
        Assert.NotEqual(default, cree.CreatedAt);
        Assert.Empty(cree.Artworks);
    }

    [Fact]
    public async Task ModifierUnArtiste_NeToucheNiLesOeuvresLieesNiLaDateDeCreation()
    {
        var fichier = GeneratedCsv.Create(Enumerable.Range(1, 3), i => $"Titre {i}", author: _ => "MARTIN Paul");
        try
        {
            await using (var context = _db.CreateContext())
                Assert.True((await _db.CreateImportService(context).ImportFromFileAsync(fichier)).Success);
        }
        finally { File.Delete(fichier); }
        var avant = await _db.CreateContext().Artists.AsNoTracking().SingleAsync();

        var reponse = await Artistes().UpdateArtist(avant.Id, new ArtistInput { LastName = "MARTIN", FirstName = "Pauline", Nationality = "française" });

        Assert.IsType<NoContentResult>(reponse);
        var apres = await _db.CreateContext().Artists.AsNoTracking().SingleAsync();
        Assert.Equal("Pauline", apres.FirstName);
        Assert.Equal("française", apres.Nationality);
        Assert.Equal(avant.CreatedAt, apres.CreatedAt, TimeSpan.FromMilliseconds(1));
        Assert.Equal(3, await _db.CreateContext().Set<ArtworkArtist>().CountAsync(l => l.ArtistId == avant.Id));
    }

    [Fact]
    public void UnMuseeSansNom_OuAuxChampsTropLongs_OuAuxCoordonneesAbsurdes_EstRefuse()
    {
        Assert.NotEmpty(Erreurs(new MuseumInput()));
        Assert.NotEmpty(Erreurs(new MuseumInput { Name = new string('x', 201) }));
        Assert.NotEmpty(Erreurs(new MuseumInput { Name = "ok", ZipCode = new string('1', 21) }));
        Assert.NotEmpty(Erreurs(new MuseumInput { Name = "ok", Latitude = 91 }));
        Assert.Empty(Erreurs(new MuseumInput { Name = "ok", Latitude = 45.5, Longitude = 4.8 }));
    }

    [Fact]
    public void UnArtisteSansNom_OuAuxChampsTropLongs_EstRefuse()
    {
        Assert.NotEmpty(Erreurs(new ArtistInput()));
        Assert.NotEmpty(Erreurs(new ArtistInput { LastName = "ok", BirthDate = new string('1', 101) }));
        Assert.Empty(Erreurs(new ArtistInput { LastName = "ok" }));
    }

    [Fact]
    public void LesDtoNeLaissentPasPasserIdentifiantNiDates()
    {
        Assert.DoesNotContain(typeof(MuseumInput).GetProperties(), p => p.Name is "Id" or "CreatedAt" or "UpdatedAt");
        Assert.DoesNotContain(typeof(ArtistInput).GetProperties(), p => p.Name is "Id" or "CreatedAt" or "UpdatedAt" or "Artworks");
    }
}
