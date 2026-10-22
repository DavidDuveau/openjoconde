using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenJoconde.Core.Interfaces;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// L'API réelle (Program, injection de dépendances, filtres, limitation de débit) sur une base PostgreSQL jetable.
/// Les appels réseau sortants et le coordinateur de synchronisation peuvent être remplacés.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ApiKey = "cle-de-test";

    private readonly ImportDatabase _db = new();
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"openjoconde_api_{Guid.NewGuid():N}");

    /// <summary>Remplacements de services, appliqués à la construction de l'hôte</summary>
    public Action<IServiceCollection>? Overrides { get; init; }

    /// <summary>Réglages ajoutés ou remplacés (ex. « Sync:CheckForUpdatesOnStartup »)</summary>
    public Dictionary<string, string?> Settings { get; } = new();

    /// <summary>Environnement de l'hôte ; la production par défaut, comme le déploiement</summary>
    public string Environment { get; init; } = "Production";

    public ImportDatabase Database => _db;

    public Task InitializeAsync() => _db.InitializeAsync();

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    /// <summary>Arrête l'API, supprime la base jetable et le dossier temporaire</summary>
    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environment);
        builder.UseSetting("ConnectionStrings:DefaultConnection", _db.ConnectionString);
        builder.UseSetting("Security:ApiKey", ApiKey);
        builder.UseSetting("Sync:TempDirectory", _tempDirectory);
        foreach (var (key, value) in Settings)
            builder.UseSetting(key, value);

        builder.ConfigureTestServices(services => Overrides?.Invoke(services));
    }

    /// <summary>Client qui présente la clé d'API</summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        return client;
    }

    /// <summary>Remplace un service de l'API (portée scoped)</summary>
    public static Action<IServiceCollection> Replacing<T>(T instance) where T : class =>
        services => services.Replace(ServiceDescriptor.Scoped(_ => instance));

    /// <summary>Remplace le coordinateur de synchronisation (singleton)</summary>
    public static Action<IServiceCollection> WithCoordinator(ISyncCoordinator coordinator) =>
        services => services.Replace(ServiceDescriptor.Singleton(coordinator));
}
