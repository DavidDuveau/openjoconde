using Microsoft.EntityFrameworkCore;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Services;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Tri des listes (TECH-35) : la ponctuation initiale des titres ne les met pas en tête, les œuvres sans titre et les
/// artistes sans vrai nom viennent en dernier.
/// </summary>
public class OrderingTests : IAsyncLifetime
{
    private static readonly string[] Titres =
    {
        "- Zèbre au galop", "Ane", "'Bœuf", "(12) Chat", "Dromadaire", ""
    };

    private readonly ImportDatabase _db = new();
    private string? _fichier;

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();
        _fichier = GeneratedCsv.Create(Enumerable.Range(1, Titres.Length), i => Titres[i - 1]);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Oeuvres_SontTrieesSansLaPonctuationInitiale_LesSansTitreEnDernier(bool recherche)
    {
        await using var context = _db.CreateContext();

        var titres = await ArtworkOrdering.ByTitle(context.Artworks.AsNoTracking(), recherche)
            .Select(a => a.Title).ToListAsync();

        Assert.Equal(new[] { "(12) Chat", "Ane", "'Bœuf", "Dromadaire", "- Zèbre au galop" }, titres.Take(5));
        Assert.Null(titres[5]);
    }

    [Fact]
    public async Task Artistes_SansVraiNom_ViennentEnDernier()
    {
        await using var context = _db.CreateContext();
        context.Artists.AddRange(
            new Artist { Id = Guid.NewGuid(), LastName = "?" },
            new Artist { Id = Guid.NewGuid(), LastName = "Zola", FirstName = "Émile" },
            new Artist { Id = Guid.NewGuid(), LastName = "Monet", FirstName = "Claude" },
            new Artist { Id = Guid.NewGuid(), LastName = "(attribué à) Giotto" },
            new Artist { Id = Guid.NewGuid(), LastName = "Ané" });
        await context.SaveChangesAsync();

        var noms = (await ArtistOrdering.ByName(context.Artists.AsNoTracking()).Select(a => a.LastName).ToListAsync())
            .Where(n => !n.StartsWith("AUTEUR")) // artistes créés par l'import du fichier de test
            .ToList();

        Assert.Equal(new[] { "Ané", "Monet", "Zola" }, noms.Take(3));
        Assert.Equal(new[] { "(attribué à) Giotto", "?" }, noms.Skip(3).OrderBy(n => n, StringComparer.Ordinal));
    }
}
