using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Infrastructure.Data;
using OpenJoconde.Infrastructure.Data.Repositories;
using OpenJoconde.Infrastructure.Services;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Base PostgreSQL jetable et chaîne d'import réelle (parseur streaming, repositories, service d'import).
/// SQLite est exclu : <see cref="ArtworkRelationsRepository"/> utilise SqlConnection directement.
/// </summary>
public sealed class ImportDatabase : IAsyncLifetime
{
    private readonly string _databaseName = $"openjoconde_tests_{Guid.NewGuid():N}";

    // Variable d'environnement OPENJOCONDE_TEST_POSTGRES : connexion à un serveur PostgreSQL sans nom de base
    // (intégration continue). Par défaut, le serveur local, utilisateur postgres, mot de passe lu dans pgpass.conf.
    private static readonly string ServerConnection =
        Environment.GetEnvironmentVariable("OPENJOCONDE_TEST_POSTGRES")?.TrimEnd(';')
        ?? "Host=localhost;Port=5432;Username=postgres";

    public string ConnectionString => $"{ServerConnection};Database={_databaseName}";

    /// <summary>Options du contexte comme dans l'API : PostgreSQL, noms en snake_case</summary>
    public static DbContextOptionsBuilder<OpenJocondeDbContext> Configure(DbContextOptionsBuilder<OpenJocondeDbContext> builder, string connectionString) =>
        builder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();

    public static string FixturePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    public API.Controllers.ArtworksController CreateArtworksController() =>
        new(new ArtworkRepository(CreateContext(), NullLogger<ArtworkRepository>.Instance), NullLogger<API.Controllers.ArtworksController>.Instance);

    public OpenJocondeDbContext CreateContext() =>
        new(Configure(new DbContextOptionsBuilder<OpenJocondeDbContext>(), ConnectionString).Options);

    private async Task RunOnServerAsync(string sql)
    {
        await using var connection = new NpgsqlConnection($"{ServerConnection};Database=postgres;Pooling=false");
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public Task InitializeAsync() => InitializeAsync(migrate: true);

    /// <summary>Crée la base jetable ; sans migration, elle reste vide comme celle d'un premier déploiement</summary>
    public async Task InitializeAsync(bool migrate)
    {
        await RunOnServerAsync($"CREATE DATABASE {_databaseName}");
        if (!migrate)
            return;
        await using var context = CreateContext();
        // Les migrations, pas EnsureCreated : c'est le schéma que créera vraiment `dotnet ef database update`
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        // Les connexions poolées de la base empêcheraient de la supprimer
        NpgsqlConnection.ClearAllPools();
        await RunOnServerAsync($"DROP DATABASE IF EXISTS {_databaseName} WITH (FORCE)");
    }

    /// <summary>Assemble le service d'import comme le fait l'injection de dépendances de l'API.</summary>
    public DataImportService CreateImportService(
        OpenJocondeDbContext context,
        IArtworkRepository? artworks = null,
        IArtworkRelationsRepository? relations = null) =>
        new(
            NullLogger<DataImportService>.Instance,
            artworks ?? new ArtworkRepository(context, NullLogger<ArtworkRepository>.Instance),
            new ArtistRepository(context, NullLogger<ArtistRepository>.Instance),
            new MuseumRepository(context, NullLogger<MuseumRepository>.Instance),
            new DomainRepository(context, NullLogger<DomainRepository>.Instance),
            new TechniqueRepository(context, NullLogger<TechniqueRepository>.Instance),
            new PeriodRepository(context, NullLogger<PeriodRepository>.Instance),
            relations ?? new ArtworkRelationsRepository(ConnectionString, NullLogger<ArtworkRelationsRepository>.Instance),
            new StreamingJocondeJsonParserService(NullLogger<StreamingJocondeJsonParserService>.Instance),
            new StreamingJocondeCsvParserService(NullLogger<StreamingJocondeCsvParserService>.Instance),
            new HttpClient());
}
