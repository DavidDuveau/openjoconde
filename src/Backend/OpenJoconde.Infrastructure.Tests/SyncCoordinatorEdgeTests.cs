using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenJoconde.Infrastructure.Configuration;
using OpenJoconde.Infrastructure.Services;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>Le coordinateur de synchronisation refuse une configuration absente et une base injoignable (TECH-43)</summary>
public class SyncCoordinatorEdgeTests
{
    private static readonly IServiceScopeFactory Scopes = new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    private static readonly IOptions<SyncOptions> Options = Microsoft.Extensions.Options.Options.Create(new SyncOptions());

    private static IConfiguration Configuration(string? connectionString) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connectionString
        }).Build();

    [Fact]
    public void Constructeur_RefuseChaqueDependanceNulle()
    {
        ILogger<SyncCoordinator> logger = NullLogger<SyncCoordinator>.Instance;
        var configuration = Configuration("x");

        Assert.Throws<ArgumentNullException>(() => new SyncCoordinator(null!, configuration, Options, logger));
        Assert.Throws<ArgumentNullException>(() => new SyncCoordinator(Scopes, null!, Options, logger));
        Assert.Throws<ArgumentNullException>(() => new SyncCoordinator(Scopes, configuration, null!, logger));
        Assert.Throws<ArgumentNullException>(() => new SyncCoordinator(Scopes, configuration, Options, null!));
    }

    [Fact]
    public async Task SansChaineDeConnexion_LeDemarrageLeve()
    {
        var coordinator = new SyncCoordinator(Scopes, Configuration(null), Options, NullLogger<SyncCoordinator>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.TryStartAsync("Manual"));
    }

    [Fact]
    public async Task BaseInjoignable_LeDemarrageLeve_SansLaisserDeVerrou()
    {
        var coordinator = new SyncCoordinator(
            Scopes, Configuration("Host=localhost;Port=1;Username=x;Timeout=2;Pooling=false"), Options, NullLogger<SyncCoordinator>.Instance);

        await Assert.ThrowsAnyAsync<Exception>(() => coordinator.TryStartAsync("Manual"));
        Assert.False(coordinator.RequestCancel(Guid.NewGuid()));
    }
}
