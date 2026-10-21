using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;
using OpenJoconde.Infrastructure.Data.Repositories;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Upserts des entités de référence : lecture des existants par lot, et rejeu d'un lot refusé par la base (TECH-17).
/// </summary>
public class ReferenceUpsertTests : IAsyncLifetime
{
    private readonly ImportDatabase _db = new();

    public Task InitializeAsync() => _db.InitializeAsync();
    public Task DisposeAsync() => _db.DisposeAsync();

    /// <summary>Refuse toute écriture qui répond au prédicat, comme une base qui dépasse son délai sous charge.</summary>
    private sealed class RefusingInterceptor(Func<DbContext, bool> refuse) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context != null && refuse(eventData.Context))
                throw new TimeoutException("Le délai d'exécution a expiré (simulé)");
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private OpenJocondeDbContext ContextRefusing(Func<DbContext, bool> refuse) =>
        new(new DbContextOptionsBuilder<OpenJocondeDbContext>()
            .UseNpgsql(_db.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new RefusingInterceptor(refuse))
            .Options);

    private static List<Artist> Artists(int count) =>
        Enumerable.Range(1, count).Select(i => new Artist { LastName = $"NOM{i}", FirstName = "Prénom" }).ToList();

    private static int AddedArtists(DbContext context) =>
        context.ChangeTracker.Entries<Artist>().Count(e => e.State == EntityState.Added);

    [Fact]
    public async Task Artistes_LotRefuse_EstRejoueParMoities_EtToutEstEnregistre()
    {
        // Toute écriture de plus d'un artiste est refusée : seul le rejeu élément par élément aboutit
        await using var context = ContextRefusing(c => AddedArtists(c) > 1);
        var repository = new ArtistRepository(context, NullLogger<ArtistRepository>.Instance);

        var saved = await repository.BulkUpsertAsync(Artists(9));

        Assert.Equal(9, saved);
        await using var check = _db.CreateContext();
        Assert.Equal(9, await check.Artists.CountAsync());
    }

    [Fact]
    public async Task Artistes_UnSeulFautif_NePerdQueLui()
    {
        await using var context = ContextRefusing(c => c.ChangeTracker.Entries<Artist>().Any(e => e.State == EntityState.Added && e.Entity.LastName == "NOM5"));
        var repository = new ArtistRepository(context, NullLogger<ArtistRepository>.Instance);

        var saved = await repository.BulkUpsertAsync(Artists(9));

        Assert.Equal(8, saved);
        await using var check = _db.CreateContext();
        Assert.False(await check.Artists.AnyAsync(a => a.LastName == "NOM5"));
        Assert.Equal(8, await check.Artists.CountAsync());
    }

    [Fact]
    public async Task Techniques_Reimport_MetAJourSansDoublon_MemeSousUneAutreCasse()
    {
        await using (var context = _db.CreateContext())
        {
            var repository = new TechniqueRepository(context, NullLogger<TechniqueRepository>.Instance);
            var initial = Enumerable.Range(1, 250).Select(i => new Technique { Name = $"technique {i}", Description = "avant" }).ToList();
            Assert.Equal(250, await repository.BulkUpsertAsync(initial));
        }

        Dictionary<string, Guid> ids;
        await using (var context = _db.CreateContext())
            ids = await context.Techniques.ToDictionaryAsync(t => t.Name, t => t.Id);

        await using (var context = _db.CreateContext())
        {
            var repository = new TechniqueRepository(context, NullLogger<TechniqueRepository>.Instance);
            var second = Enumerable.Range(1, 250).Select(i => new Technique { Name = i == 7 ? "TECHNIQUE 7" : $"technique {i}", Description = "après" }).ToList();
            second.Add(new Technique { Name = "technique 251", Description = "après" });

            Assert.Equal(251, await repository.BulkUpsertAsync(second));
            Assert.Equal(ids["technique 7"], second[6].Id);
        }

        await using var check = _db.CreateContext();
        Assert.Equal(251, await check.Techniques.CountAsync());
        Assert.All(await check.Techniques.ToListAsync(), t => Assert.Equal("après", t.Description));
        Assert.Equal(ids["technique 100"], (await check.Techniques.SingleAsync(t => t.Name == "technique 100")).Id);
    }

    [Fact]
    public async Task Domaines_EtPeriodes_DoublonDansLeMemeLot_UnSeulEnregistrement()
    {
        await using var context = _db.CreateContext();
        var domains = new DomainRepository(context, NullLogger<DomainRepository>.Instance);
        var periods = new PeriodRepository(context, NullLogger<PeriodRepository>.Instance);

        Assert.Equal(2, await domains.BulkUpsertAsync(new[] { new Domain { Name = "peinture" }, new Domain { Name = "PEINTURE" } }));
        Assert.Equal(2, await periods.BulkUpsertAsync(new[] { new Period { Name = "XIXe siècle", StartYear = 1801 }, new Period { Name = "xixe siècle", StartYear = 1801 } }));

        await using var check = _db.CreateContext();
        Assert.Equal(1, await check.Domains.CountAsync());
        Assert.Equal(1, await check.Periods.CountAsync());
    }
}
