using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>Opérations d'un dépôt d'entité de référence (domaine, technique, période), qui n'ont pas d'interface commune.</summary>
public sealed record ReferenceOps<T>(
    Func<Guid, Task<T?>> GetById,
    Func<Task<IEnumerable<T>>> GetAll,
    Func<T, Task<T>> Add,
    Func<T, Task<bool>> Update,
    Func<Guid, Task<bool>> Delete,
    Func<IEnumerable<T>, Task<int>> Bulk) where T : class;

/// <summary>
/// Comportement commun des dépôts d'entités de référence : CRUD, upsert par nom, rejeu d'un lot refusé par la base.
/// </summary>
public abstract class ReferenceRepositoryTests<T> : IAsyncLifetime where T : class
{
    protected readonly ImportDatabase Db = new();

    public Task InitializeAsync() => Db.InitializeAsync();
    public Task DisposeAsync() => Db.DisposeAsync();

    protected abstract T Make(string name, string? description = null);
    protected abstract string NameOf(T item);
    protected abstract Guid IdOf(T item);
    protected abstract string? DescriptionOf(T item);
    protected abstract void SetDescription(T item, string? description);
    protected abstract ReferenceOps<T> Ops(OpenJocondeDbContext context);
    protected abstract DbSet<T> Set(OpenJocondeDbContext context);

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

    /// <summary>Lectures refusées : la base ne répond plus.</summary>
    private sealed class BrokenReadInterceptor : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) =>
            throw new TimeoutException("Lecture refusée (simulée)");
    }

    private OpenJocondeDbContext ContextWith(IInterceptor interceptor) =>
        new(new DbContextOptionsBuilder<OpenJocondeDbContext>()
            .UseNpgsql(Db.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(interceptor)
            .Options);

    private static int Count(DbContext context, EntityState state) =>
        context.ChangeTracker.Entries<T>().Count(e => e.State == state);

    private void SetId(T item, Guid id) => typeof(T).GetProperty("Id")!.SetValue(item, id);

    [Fact]
    public async Task Crud_AjouteLitModifieEtSupprime()
    {
        await using var context = Db.CreateContext();
        var ops = Ops(context);

        var added = await ops.Add(Make("Alpha", "première"));
        Assert.NotEqual(Guid.Empty, IdOf(added));
        Assert.Equal("première", DescriptionOf((await ops.GetById(IdOf(added)))!));

        await ops.Add(Make("Beta"));
        Assert.Equal(new[] { "Alpha", "Beta" }, (await ops.GetAll()).Select(NameOf).ToArray());

        SetDescription(added, "modifiée");
        Assert.True(await ops.Update(added));
        Assert.Equal("modifiée", DescriptionOf((await ops.GetById(IdOf(added)))!));

        Assert.True(await ops.Delete(IdOf(added)));
        Assert.Null(await ops.GetById(IdOf(added)));
        Assert.False(await ops.Delete(IdOf(added)));
    }

    [Fact]
    public async Task Ajout_UnIdentifiantFourni_EstConserve()
    {
        await using var context = Db.CreateContext();
        var item = Make("Fournie");
        var id = Guid.NewGuid();
        SetId(item, id);

        Assert.Equal(id, IdOf(await Ops(context).Add(item)));
    }

    [Fact]
    public async Task Ajout_EnDoublon_Leve_EtModificationDUneLigneInexistante_RendFaux()
    {
        await using (var context = Db.CreateContext())
            await Ops(context).Add(Make("Unique"));

        await using var other = Db.CreateContext();
        await Assert.ThrowsAnyAsync<Exception>(() => Ops(other).Add(Make("Unique")));

        await using var third = Db.CreateContext();
        var ghost = Make("Fantôme");
        SetId(ghost, Guid.NewGuid());
        Assert.False(await Ops(third).Update(ghost));
    }

    [Fact]
    public async Task Suppression_RefuseeParLaBase_RendFaux()
    {
        Guid id;
        await using (var context = Db.CreateContext())
            id = IdOf(await Ops(context).Add(Make("Protégée")));

        await using var refusing = ContextWith(new RefusingInterceptor(c => Count(c, EntityState.Deleted) > 0));
        Assert.False(await Ops(refusing).Delete(id));
    }

    [Fact]
    public async Task Upsert_Reimport_MetAJourLesExistants_EtAjouteLesNouveaux()
    {
        await using (var context = Db.CreateContext())
            Assert.Equal(2, await Ops(context).Bulk(new[] { Make("un", "avant"), Make("deux", "avant") }));

        await using (var context = Db.CreateContext())
            Assert.Equal(3, await Ops(context).Bulk(new[] { Make("UN", "après"), Make("deux", "après"), Make("trois", "après") }));

        await using var check = Db.CreateContext();
        var all = await Set(check).AsNoTracking().ToListAsync();
        Assert.Equal(3, all.Count);
        Assert.All(all, x => Assert.Equal("après", DescriptionOf(x)));
    }

    [Fact]
    public async Task Upsert_LotRefuse_EstRejoueParElement_PourLesAjouts()
    {
        await using var context = ContextWith(new RefusingInterceptor(c => Count(c, EntityState.Added) > 1));

        var saved = await Ops(context).Bulk(Enumerable.Range(1, 5).Select(i => Make($"nouveau {i}", "d")));

        Assert.Equal(5, saved);
        await using var check = Db.CreateContext();
        Assert.Equal(5, await Set(check).CountAsync());
    }

    [Fact]
    public async Task Upsert_LotRefuse_EstRejoueParElement_PourLesMisesAJour()
    {
        await using (var context = Db.CreateContext())
            await Ops(context).Bulk(Enumerable.Range(1, 4).Select(i => Make($"ancien {i}", "avant")));

        await using var refusing = ContextWith(new RefusingInterceptor(c => Count(c, EntityState.Modified) > 1));
        var saved = await Ops(refusing).Bulk(Enumerable.Range(1, 4).Select(i => Make($"ancien {i}", "après")));

        Assert.Equal(4, saved);
        await using var check = Db.CreateContext();
        Assert.All(await Set(check).AsNoTracking().ToListAsync(), x => Assert.Equal("après", DescriptionOf(x)));
    }

    [Fact]
    public async Task Upsert_LotRefuse_TronqueLesNomsTropLongs_EtLesRendUniques()
    {
        var prefix = new string('x', 200);
        await using var context = ContextWith(new RefusingInterceptor(c => Count(c, EntityState.Added) > 1));

        var saved = await Ops(context).Bulk(new[] { Make(prefix + "1"), Make(prefix + "2"), Make(prefix + "3") });

        Assert.Equal(3, saved);
        await using var check = Db.CreateContext();
        var names = (await Set(check).AsNoTracking().ToListAsync()).Select(NameOf).OrderBy(n => n).ToList();
        Assert.Equal(3, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(names, n => Assert.True(n.Length <= 200));
        Assert.Contains(prefix, names);
        Assert.Contains(names, n => n.EndsWith(" (1)"));
    }

    [Fact]
    public async Task Upsert_UnSeulFautif_NePerdQueLui()
    {
        await using var context = ContextWith(new RefusingInterceptor(c =>
            c.ChangeTracker.Entries<T>().Any(e => e.State == EntityState.Added && NameOf(e.Entity) == "fautif")));

        var saved = await Ops(context).Bulk(new[] { Make("bon 1"), Make("fautif"), Make("bon 2") });

        Assert.Equal(2, saved);
        await using var check = Db.CreateContext();
        var names = (await Set(check).AsNoTracking().ToListAsync()).Select(NameOf).ToList();
        Assert.DoesNotContain("fautif", names);
        Assert.Equal(2, names.Count);
    }

    [Fact]
    public async Task Upsert_LectureDesExistantsImpossible_CompteLeLotEnErreur()
    {
        await using var context = ContextWith(new BrokenReadInterceptor());

        var saved = await Ops(context).Bulk(new[] { Make("a"), Make("b") });

        Assert.Equal(0, saved);
    }
}

public class DomainRepositoryTests : ReferenceRepositoryTests<Domain>
{
    protected override Domain Make(string name, string? description = null) => new() { Name = name, Description = description };
    protected override string NameOf(Domain item) => item.Name;
    protected override Guid IdOf(Domain item) => item.Id;
    protected override string? DescriptionOf(Domain item) => item.Description;
    protected override void SetDescription(Domain item, string? description) => item.Description = description;
    protected override DbSet<Domain> Set(OpenJocondeDbContext context) => context.Domains;

    protected override ReferenceOps<Domain> Ops(OpenJocondeDbContext context)
    {
        var repository = new DomainRepository(context, NullLogger<DomainRepository>.Instance);
        return new(repository.GetByIdAsync, repository.GetAllAsync, repository.AddAsync, repository.UpdateAsync, repository.DeleteAsync, repository.BulkUpsertAsync);
    }
}

public class TechniqueRepositoryTests : ReferenceRepositoryTests<Technique>
{
    protected override Technique Make(string name, string? description = null) => new() { Name = name, Description = description };
    protected override string NameOf(Technique item) => item.Name;
    protected override Guid IdOf(Technique item) => item.Id;
    protected override string? DescriptionOf(Technique item) => item.Description;
    protected override void SetDescription(Technique item, string? description) => item.Description = description;
    protected override DbSet<Technique> Set(OpenJocondeDbContext context) => context.Techniques;

    protected override ReferenceOps<Technique> Ops(OpenJocondeDbContext context)
    {
        var repository = new TechniqueRepository(context, NullLogger<TechniqueRepository>.Instance);
        return new(repository.GetByIdAsync, repository.GetAllAsync, repository.AddAsync, repository.UpdateAsync, repository.DeleteAsync, repository.BulkUpsertAsync);
    }
}

public class PeriodRepositoryTests : ReferenceRepositoryTests<Period>
{
    protected override Period Make(string name, string? description = null) => new() { Name = name, Description = description, StartYear = 1800, EndYear = 1900 };
    protected override string NameOf(Period item) => item.Name;
    protected override Guid IdOf(Period item) => item.Id;
    protected override string? DescriptionOf(Period item) => item.Description;
    protected override void SetDescription(Period item, string? description) => item.Description = description;
    protected override DbSet<Period> Set(OpenJocondeDbContext context) => context.Periods;

    protected override ReferenceOps<Period> Ops(OpenJocondeDbContext context)
    {
        var repository = new PeriodRepository(context, NullLogger<PeriodRepository>.Instance);
        return new(repository.GetByIdAsync, repository.GetAllAsync, repository.AddAsync, repository.UpdateAsync, repository.DeleteAsync, repository.BulkUpsertAsync);
    }
}
