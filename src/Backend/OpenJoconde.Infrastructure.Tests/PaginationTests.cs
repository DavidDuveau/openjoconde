using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;
using OpenJoconde.Infrastructure.Data.Repositories;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Les listes de musées et d'artistes rendent l'enveloppe commune (éléments, total, page) dont les vues ont besoin (TECH-12, TECH-13).
/// </summary>
public class PaginationTests : IAsyncLifetime
{
    private readonly ImportDatabase _db = new();

    public Task InitializeAsync() => _db.InitializeAsync();
    public Task DisposeAsync() => _db.DisposeAsync();

    private async Task<MuseumRepository> MuseumsAsync(int nombre)
    {
        var context = _db.CreateContext();
        for (var i = 1; i <= nombre; i++)
        {
            context.Museums.Add(new Museum
            {
                Id = Guid.NewGuid(),
                Name = i % 2 == 0 ? $"Musée du Louvre {i:D2}" : $"Musée d'Orsay {i:D2}",
                City = i % 2 == 0 ? "Paris" : "Lyon",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();
        return new MuseumRepository(context, NullLogger<MuseumRepository>.Instance);
    }

    [Fact]
    public async Task Musees_RendentLaPageDemandeeEtLeTotal()
    {
        var musees = await MuseumsAsync(25);

        var page = await musees.SearchByNameAsync(null, page: 3, pageSize: 10);

        Assert.Equal(5, page.Items.Count);
        Assert.Equal(25, page.TotalItems);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(3, page.Page);
        Assert.Equal(10, page.PageSize);
    }

    [Fact]
    public async Task Musees_FiltresParNom_CompteSeulementLesCorrespondants()
    {
        var musees = await MuseumsAsync(25);

        var page = await musees.SearchByNameAsync("louvre", page: 1, pageSize: 10);

        Assert.Equal(12, page.TotalItems);
        Assert.Equal(10, page.Items.Count);
        Assert.All(page.Items, m => Assert.Contains("Louvre", m.Name));
    }

    [Fact]
    public async Task Artistes_RendentLaPageDemandeeEtLeTotal()
    {
        await using var context = _db.CreateContext();
        for (var i = 1; i <= 12; i++)
            context.Artists.Add(new Artist { Id = Guid.NewGuid(), LastName = $"DUPONT{i:D2}", FirstName = "Jean", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
        var artistes = new ArtistRepository(context, NullLogger<ArtistRepository>.Instance);

        var page = await artistes.SearchByNameAsync("dupont", page: 2, pageSize: 5);

        Assert.Equal(12, page.TotalItems);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(new[] { "DUPONT06", "DUPONT07", "DUPONT08", "DUPONT09", "DUPONT10" }, page.Items.Select(a => a.LastName));
    }
}
