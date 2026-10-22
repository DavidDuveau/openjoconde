using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>Musées : recherche, CRUD, upsert par nom et ville, rejeu d'un lot refusé par la base (TECH-43)</summary>
public class MuseumRepositoryTests : IAsyncLifetime
{
    private readonly ImportDatabase _db = new();

    public Task InitializeAsync() => _db.InitializeAsync();
    public Task DisposeAsync() => _db.DisposeAsync();

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

    private static MuseumRepository Repository(OpenJocondeDbContext context) =>
        new(context, NullLogger<MuseumRepository>.Instance);

    private static int Count(DbContext context, EntityState state) =>
        context.ChangeTracker.Entries<Museum>().Count(e => e.State == state);

    [Fact]
    public async Task Constructeur_RefuseLesDependancesNulles()
    {
        await using var context = _db.CreateContext();

        Assert.Throws<ArgumentNullException>(() => new MuseumRepository(null!, NullLogger<MuseumRepository>.Instance));
        Assert.Throws<ArgumentNullException>(() => new MuseumRepository(context, null!));
    }

    [Fact]
    public async Task Crud_AjouteLitModifieEtSupprime()
    {
        await using var context = _db.CreateContext();
        var repository = Repository(context);

        var added = await repository.AddAsync(new Museum { Name = "Musée A", City = "Paris" });
        Assert.NotEqual(Guid.Empty, added.Id);
        Assert.Equal("Paris", (await repository.GetByIdAsync(added.Id))!.City);

        added.City = "Lyon";
        Assert.True(await repository.UpdateAsync(added));
        Assert.Equal("Lyon", (await repository.GetByIdAsync(added.Id))!.City);

        Assert.Equal(new HashSet<Guid> { added.Id }, await repository.GetExistingIdsAsync(new[] { added.Id, added.Id, Guid.NewGuid() }));

        Assert.True(await repository.DeleteAsync(added.Id));
        Assert.Null(await repository.GetByIdAsync(added.Id));
        Assert.False(await repository.DeleteAsync(added.Id));
    }

    [Fact]
    public async Task Ajout_UnIdentifiantFourni_EstConserve_EtUnDoublonDeNomEtVilleEstAccepte()
    {
        await using var context = _db.CreateContext();
        var id = Guid.NewGuid();

        var added = await Repository(context).AddAsync(new Museum { Id = id, Name = "Musée B" });

        Assert.Equal(id, added.Id);
    }

    [Fact]
    public async Task Ajout_RefuseParLaBase_Leve_EtModificationRefusee_RendFaux()
    {
        await using var refusing = ContextRefusing(c => Count(c, EntityState.Added) + Count(c, EntityState.Modified) > 0);
        var repository = Repository(refusing);

        await Assert.ThrowsAsync<TimeoutException>(() => repository.AddAsync(new Museum { Name = "Refusé" }));
        Assert.False(await repository.UpdateAsync(new Museum { Id = Guid.NewGuid(), Name = "Refusé" }));
    }

    [Fact]
    public async Task Suppression_RefuseeParLaBase_RendFaux()
    {
        Guid id;
        await using (var context = _db.CreateContext())
            id = (await Repository(context).AddAsync(new Museum { Name = "Protégé" })).Id;

        await using var refusing = ContextRefusing(c => Count(c, EntityState.Deleted) > 0);
        Assert.False(await Repository(refusing).DeleteAsync(id));
    }

    [Fact]
    public async Task Recherche_ParNomVilleOuDepartement_BorneLaPagination()
    {
        await using (var context = _db.CreateContext())
        {
            await Repository(context).BulkUpsertAsync(new[]
            {
                new Museum { Name = "Musée d'Orsay", City = "Paris", Department = "Paris" },
                new Museum { Name = "Musée Fabre", City = "Montpellier", Department = "Hérault" },
                new Museum { Name = "Musée sans lieu" }
            });
        }

        await using var read = _db.CreateContext();
        var repository = Repository(read);

        Assert.Equal(3, (await repository.SearchByNameAsync(null)).TotalItems);
        Assert.Equal(3, (await repository.SearchByNameAsync("  ")).TotalItems);
        Assert.Equal(new[] { "Musée d'Orsay" }, (await repository.SearchByNameAsync("orsay")).Items.Select(m => m.Name));
        Assert.Equal(new[] { "Musée Fabre" }, (await repository.SearchByNameAsync("montpellier")).Items.Select(m => m.Name));
        Assert.Equal(new[] { "Musée Fabre" }, (await repository.SearchByNameAsync("hérault")).Items.Select(m => m.Name));

        var bounded = await repository.SearchByNameAsync(null, page: -3, pageSize: 1000);
        Assert.Equal(1, bounded.Page);
        Assert.Equal(100, bounded.PageSize);
        var defaults = await repository.SearchByNameAsync(null, page: 1, pageSize: 0);
        Assert.Equal(10, defaults.PageSize);
        var second = await repository.SearchByNameAsync(null, page: 2, pageSize: 2);
        Assert.Single(second.Items);
    }

    [Fact]
    public async Task Upsert_Reimport_CompleteSansEffacerLesValeursAbsentes()
    {
        await using (var context = _db.CreateContext())
        {
            Assert.Equal(1, await Repository(context).BulkUpsertAsync(new[]
            {
                new Museum { Name = "Musée C", City = "Lille", Phone = "0102030405", Website = "https://c.test", Address = "1 rue", ZipCode = "59000", Email = "c@c.test", Description = "histoire", Region = "Hauts-de-France", Code = "M0001", Latitude = 50.6, Longitude = 3.0, Department = "Nord" }
            }));
        }

        Guid knownId;
        await using (var context = _db.CreateContext())
            knownId = (await context.Museums.SingleAsync()).Id;

        var reimport = new Museum { Name = "Musée C", City = "Lille", Department = "Nord (59)" };
        await using (var context = _db.CreateContext())
            Assert.Equal(2, await Repository(context).BulkUpsertAsync(new[] { reimport, new Museum { Name = "Musée D", City = "Lille" } }));
        Assert.Equal(knownId, reimport.Id);

        await using var check = _db.CreateContext();
        var museum = await check.Museums.SingleAsync(m => m.Name == "Musée C");
        Assert.Equal("Nord (59)", museum.Department);
        Assert.Equal("0102030405", museum.Phone);
        Assert.Equal("https://c.test", museum.Website);
        Assert.Equal("histoire", museum.Description);
        Assert.Equal(2, await check.Museums.CountAsync());
    }

    [Fact]
    public async Task Upsert_PlusDeCentMusees_EstEnregistreParLots()
    {
        await using var context = _db.CreateContext();

        var saved = await Repository(context).BulkUpsertAsync(Enumerable.Range(1, 230).Select(i => new Museum { Name = $"Musée {i}", City = "Ville" }));

        Assert.Equal(230, saved);
        await using var check = _db.CreateContext();
        Assert.Equal(230, await check.Museums.CountAsync());
    }

    [Fact]
    public async Task Upsert_LotRefuse_EstRejoueParElement_PourLesAjouts()
    {
        await using var context = ContextRefusing(c => Count(c, EntityState.Added) > 1);

        var saved = await Repository(context).BulkUpsertAsync(Enumerable.Range(1, 5).Select(i => new Museum { Name = $"Musée {i}", City = "Ville" }));

        Assert.Equal(5, saved);
        await using var check = _db.CreateContext();
        Assert.Equal(5, await check.Museums.CountAsync());
    }

    [Fact]
    public async Task Upsert_LotRefuse_EstRejoueParElement_PourLesMisesAJour()
    {
        await using (var context = _db.CreateContext())
            await Repository(context).BulkUpsertAsync(Enumerable.Range(1, 4).Select(i => new Museum { Name = $"Musée {i}", City = "Ville", Phone = "0102030405" }));

        await using var refusing = ContextRefusing(c => Count(c, EntityState.Modified) > 1);
        var saved = await Repository(refusing).BulkUpsertAsync(Enumerable.Range(1, 4).Select(i =>
            new Museum { Name = $"Musée {i}", City = "Ville", Department = new string('d', 150), ZipCode = "75001", Email = "a@b.test", Address = "adresse", Website = "https://w.test", Description = "desc" }));

        Assert.Equal(4, saved);
        await using var check = _db.CreateContext();
        var museums = await check.Museums.AsNoTracking().ToListAsync();
        Assert.All(museums, m =>
        {
            Assert.Equal("0102030405", m.Phone);
            Assert.Equal(100, m.Department!.Length);
            Assert.Equal("desc", m.Description);
        });
    }

    [Fact]
    public async Task Upsert_LotRefuse_TronqueLesChampsTropLongsDesNouveauxMusees()
    {
        await using var context = ContextRefusing(c => Count(c, EntityState.Added) > 1);
        var museums = new[]
        {
            new Museum { Name = new string('n', 250), City = new string('v', 150), Department = new string('d', 150), ZipCode = new string('z', 30), Phone = new string('p', 30), Email = new string('e', 150), Address = "a", Website = "w", Description = "d" },
            new Museum { Name = "Autre", City = "Ville" }
        };

        var saved = await Repository(context).BulkUpsertAsync(museums);

        Assert.Equal(2, saved);
        await using var check = _db.CreateContext();
        var truncated = await check.Museums.AsNoTracking().SingleAsync(m => m.Name.StartsWith("nnn"));
        Assert.Equal(200, truncated.Name.Length);
        Assert.Equal(100, truncated.City!.Length);
        Assert.Equal(20, truncated.ZipCode!.Length);
        Assert.Equal(20, truncated.Phone!.Length);
        Assert.Equal(100, truncated.Email!.Length);
    }

    [Fact]
    public async Task Upsert_UnSeulFautif_NePerdQueLui()
    {
        await using var context = ContextRefusing(c =>
            c.ChangeTracker.Entries<Museum>().Any(e => e.State == EntityState.Added && e.Entity.Name == "fautif"));

        var saved = await Repository(context).BulkUpsertAsync(new[]
        {
            new Museum { Name = "bon 1", City = "V" }, new Museum { Name = "fautif", City = "V" }, new Museum { Name = "bon 2", City = "V" }
        });

        Assert.Equal(2, saved);
        await using var check = _db.CreateContext();
        Assert.False(await check.Museums.AnyAsync(m => m.Name == "fautif"));
    }

    [Fact]
    public async Task Upsert_LotVide_NeFaitRien()
    {
        await using var context = _db.CreateContext();

        Assert.Equal(0, await Repository(context).BulkUpsertAsync(Array.Empty<Museum>()));
    }
}
