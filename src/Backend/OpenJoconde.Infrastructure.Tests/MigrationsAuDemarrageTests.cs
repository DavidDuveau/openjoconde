using System.Net;
using Microsoft.EntityFrameworkCore;
using OpenJoconde.Infrastructure.Data;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>Migrations appliquées au démarrage de l'API sur une base vide (TECH-47)</summary>
public class MigrationsAuDemarrageTests
{
    [Fact]
    public async Task ReglageActif_BaseVide_LeSchemaEstCree()
    {
        await using var factory = new ApiFactory { EmptyDatabase = true };
        factory.Settings["Database:MigrateOnStartup"] = "true";
        await factory.InitializeAsync();

        var response = await factory.CreateClient().GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var context = factory.Database.CreateContext();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task ReglageAbsent_BaseVide_RienNEstApplique()
    {
        await using var factory = new ApiFactory { EmptyDatabase = true };
        await factory.InitializeAsync();

        _ = factory.CreateClient();

        await using var context = factory.Database.CreateContext();
        Assert.NotEmpty(await context.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task DeuxInstancesQuiDemarrentEnsemble_LaMigrationNeEchoueNiNeSeDuplique()
    {
        await using var database = new ImportDatabase();
        await database.InitializeAsync(migrate: false);

        await Task.WhenAll(Enumerable.Range(0, 3).Select(async _ =>
        {
            await using var context = database.CreateContext();
            await DatabaseMigrator.MigrateAsync(context);
        }));

        await using var verification = database.CreateContext();
        Assert.Empty(await verification.Database.GetPendingMigrationsAsync());
        Assert.Equal(verification.Database.GetMigrations().Count(), (await verification.Database.GetAppliedMigrationsAsync()).Count());
    }
}
