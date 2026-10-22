using Microsoft.EntityFrameworkCore;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Première année lue dans une date de création en texte libre (fonction SQL oj_creation_year, colonne creation_year),
/// sur les écritures de l'export Joconde, dont les dates avant J.-C. (TECH-38).
/// </summary>
public class DateDeCreationTests : IAsyncLifetime
{
    private readonly ImportDatabase _db = new();

    public Task InitializeAsync() => _db.InitializeAsync();

    public Task DisposeAsync() => _db.DisposeAsync();

    [Theory]
    // Après J.-C.
    [InlineData("1850", 1850)]
    [InlineData("vers 1720-1750", 1720)]
    [InlineData("4e quart 19e siècle", 1875)]
    [InlineData("1ère moitié 20e siècle", 1900)]
    [InlineData("2e moitié 20e siècle", 1950)]
    [InlineData("19e-20e siècle", 1800)]
    [InlineData("1er siècle", 0)]
    [InlineData("2e quart 17e siècle ; 1er quart 18e siècle", 1625)]
    // Avant J.-C. : années négatives, début de l'intervalle
    [InlineData("5e siècle av JC", -500)]
    [InlineData("5e siècle av JC ; 4e siècle av JC", -500)]
    [InlineData("1er siècle av JC", -100)]
    [InlineData("1er siècle av. J.-C.", -100)]
    [InlineData("4e quart 2e siècle av JC ; 1ère moitié 1er siècle av JC", -125)]
    [InlineData("2e moitié 4e siècle av JC", -350)]
    [InlineData("1er quart 5e siècle av JC", -500)]
    [InlineData("1er siècle av JC ; 5e siècle", -100)]
    [InlineData("5e millénaire av JC", -5000)]
    [InlineData("2e millénaire av JC ; 1er millénaire av JC", -2000)]
    [InlineData("40000 av JC,10000 av JC", -40000)]
    [InlineData("672 av JC-330 av JC,332 av JC-30 av JC", -672)]
    [InlineData("300 av. J.-C.", -300)]
    // Sans lecture possible : NULL, ces œuvres viennent en dernier
    [InlineData("Avant 1870", null)]
    [InlineData("après J.-C.", null)]
    [InlineData("néolithique", null)]
    [InlineData("avec Jean Chardin", null)]
    [InlineData("", null)]
    public async Task AnneeDeCreation_LitLEcritureDeLExport(string date, int? attendue)
    {
        await using var context = _db.CreateContext();

        var annee = await context.Database
            .SqlQueryRaw<int?>("SELECT oj_creation_year({0}) AS \"Value\"", date)
            .SingleAsync();

        Assert.Equal(attendue, annee);
    }
}
