using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Infrastructure.Data;
using OpenJoconde.Infrastructure.Data.Repositories;
using OpenJoconde.Infrastructure.Services;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Base SQL Server LocalDB jetable et chaîne d'import réelle (parseur streaming, repositories, service d'import).
/// SQLite est exclu : <see cref="ArtworkRelationsRepository"/> utilise SqlConnection directement.
/// </summary>
public sealed class ImportDatabase : IAsyncLifetime
{
    private readonly string _databaseName = $"OpenJoconde_Tests_{Guid.NewGuid():N}";

    public string ConnectionString =>
        $@"Server=(localdb)\MSSQLLocalDB;Database={_databaseName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    public static string FixturePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    public OpenJocondeDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<OpenJocondeDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    /// <summary>Assemble le service d'import comme le fait l'injection de dépendances de l'API.</summary>
    public AdvancedDataImportService CreateImportService(
        OpenJocondeDbContext context,
        IArtworkRepository? artworks = null,
        IArtworkRelationsRepository? relations = null) =>
        new(
            NullLogger<AdvancedDataImportService>.Instance,
            artworks ?? new ArtworkRepository(context, NullLogger<ArtworkRepository>.Instance),
            new ArtistRepository(context, NullLogger<ArtistRepository>.Instance),
            new MuseumRepository(context, NullLogger<MuseumRepository>.Instance),
            new DomainRepository(context, NullLogger<DomainRepository>.Instance),
            new TechniqueRepository(context, NullLogger<TechniqueRepository>.Instance),
            new PeriodRepository(context, NullLogger<PeriodRepository>.Instance),
            relations ?? new ArtworkRelationsRepository(ConnectionString, NullLogger<ArtworkRelationsRepository>.Instance),
            new JocondeXmlParserService(NullLogger<JocondeXmlParserService>.Instance),
            new StreamingJocondeJsonParserService(NullLogger<StreamingJocondeJsonParserService>.Instance),
            new StreamingJocondeCsvParserService(NullLogger<StreamingJocondeCsvParserService>.Instance),
            new HttpClient());
}
