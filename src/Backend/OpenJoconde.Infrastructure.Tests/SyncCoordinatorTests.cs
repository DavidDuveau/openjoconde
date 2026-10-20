using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;
using OpenJoconde.Infrastructure.Services;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Synchronisation : journal persisté, un seul passage à la fois, annulation réelle, lignes « Running » orphelines (TECH-11).
/// Le téléchargement et l'import sont simulés ; le journal et le verrou passent par une vraie base.
/// </summary>
public class SyncCoordinatorTests : IAsyncLifetime
{
    private readonly ImportDatabase _db = new();
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"sync_tests_{Guid.NewGuid():N}");
    private readonly FakeImportService _import = new();
    private SyncCoordinator _coordinator = null!;

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = _db.ConnectionString,
            ["JocondeData:SourceUrl"] = "https://exemple.test/joconde.csv",
            ["JocondeData:TempDirectory"] = _tempDirectory
        }).Build();

        var services = new ServiceCollection()
            .AddDbContext<OpenJocondeDbContext>(options => options.UseSqlServer(_db.ConnectionString))
            .AddSingleton<IJocondeDataService, FakeDownloadService>()
            .AddSingleton<IDataImportService>(_import)
            .BuildServiceProvider();

        _coordinator = new SyncCoordinator(
            services.GetRequiredService<IServiceScopeFactory>(), configuration, NullLogger<SyncCoordinator>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    private async Task<DataSyncLog> LogAsync(Guid id)
    {
        await using var context = _db.CreateContext();
        return await context.DataSyncLogs.AsNoTracking().SingleAsync(log => log.Id == id);
    }

    [Fact]
    public async Task Synchronisation_Reussie_EstJournalisee_EtLeFichierSupprime()
    {
        _import.OnImport = (_, _) => Task.FromResult(new ImportReport { Success = true, ImportedArtworks = 120, ImportedArtists = 30 });

        var result = await _coordinator.TryStartAsync("Manual");
        await result.Completion;

        Assert.True(result.Started);
        var log = await LogAsync(result.SyncId!.Value);
        Assert.Equal("Completed", log.Status);
        Assert.True(log.Success);
        Assert.Equal(120, log.ArtworksProcessed);
        Assert.Equal(30, log.ArtistsProcessed);
        Assert.Equal("Manual", log.TriggeredBy);
        Assert.NotNull(log.CompletedAt);
        Assert.Empty(Directory.GetFiles(_tempDirectory));
    }

    [Fact]
    public async Task Synchronisation_AvecPertes_EstMarqueeCompletedWithErrors()
    {
        _import.OnImport = (_, _) => Task.FromResult(new ImportReport
        {
            Success = false, ImportedArtworks = 100, Errors = 3, ErrorMessage = "Techniques : 3 sur 10 non enregistré(s)"
        });

        var result = await _coordinator.TryStartAsync("Automatic");
        await result.Completion;

        var log = await LogAsync(result.SyncId!.Value);
        Assert.Equal("CompletedWithErrors", log.Status);
        Assert.False(log.Success);
        Assert.Equal(3, log.ErrorCount);
        Assert.Equal("Techniques : 3 sur 10 non enregistré(s)", log.ErrorMessage);
        Assert.Equal("Automatic", log.TriggeredBy);
    }

    [Fact]
    public async Task UneSynchronisationEnCours_RefuseLaSuivante_PuisLaLibere()
    {
        var release = new TaskCompletionSource();
        _import.OnImport = async (_, _) =>
        {
            await release.Task;
            return new ImportReport { Success = true };
        };

        var first = await _coordinator.TryStartAsync("Manual");
        var second = await _coordinator.TryStartAsync("Automatic");

        Assert.True(first.Started);
        Assert.False(second.Started);
        Assert.NotNull(second.RunningSince);
        await using (var context = _db.CreateContext())
            Assert.Equal(1, await context.DataSyncLogs.CountAsync()); // le refus ne laisse pas de ligne

        release.SetResult();
        await first.Completion;

        var third = await _coordinator.TryStartAsync("Manual");
        await third.Completion;
        Assert.True(third.Started);
    }

    [Fact]
    public async Task Annulation_ArreteLImport_EtMarqueCanceled()
    {
        var importStarted = new TaskCompletionSource();
        _import.OnImport = async (_, token) =>
        {
            importStarted.SetResult();
            await Task.Delay(Timeout.Infinite, token);
            return new ImportReport();
        };

        var result = await _coordinator.TryStartAsync("Manual");
        await importStarted.Task;

        Assert.False(_coordinator.RequestCancel(Guid.NewGuid()));
        Assert.True(_coordinator.RequestCancel(result.SyncId!.Value));
        await result.Completion;

        var log = await LogAsync(result.SyncId.Value);
        Assert.Equal("Canceled", log.Status);
        Assert.False(log.Success);
        Assert.NotNull(log.CompletedAt);
        Assert.Empty(Directory.GetFiles(_tempDirectory));
        Assert.False(_coordinator.RequestCancel(result.SyncId.Value)); // plus en cours
    }

    [Fact]
    public async Task Echec_EstJournalise_SansExposerLeDetailDeLException()
    {
        _import.OnImport = (_, _) => throw new InvalidOperationException("détail interne : Server=prod;Password=secret");

        var result = await _coordinator.TryStartAsync("Manual");
        await result.Completion;

        var log = await LogAsync(result.SyncId!.Value);
        Assert.Equal("Failed", log.Status);
        Assert.DoesNotContain("secret", log.ErrorMessage);
        Assert.Empty(Directory.GetFiles(_tempDirectory));
    }

    [Fact]
    public async Task LigneRunningOrpheline_EstMarqueeInterrupted_AuProchainDemarrage()
    {
        Guid orphelineId;
        await using (var context = _db.CreateContext())
        {
            var orpheline = new DataSyncLog
            {
                Id = Guid.NewGuid(), SyncType = "Full", TriggeredBy = "Manual", Status = "Running",
                StartedAt = DateTime.UtcNow.AddHours(-3), CreatedAt = DateTime.UtcNow.AddHours(-3)
            };
            context.DataSyncLogs.Add(orpheline);
            await context.SaveChangesAsync();
            orphelineId = orpheline.Id;
        }
        _import.OnImport = (_, _) => Task.FromResult(new ImportReport { Success = true });

        var result = await _coordinator.TryStartAsync("Manual");
        await result.Completion;

        Assert.True(result.Started);
        var orphelineApres = await LogAsync(orphelineId);
        Assert.Equal("Interrupted", orphelineApres.Status);
        Assert.NotNull(orphelineApres.CompletedAt);
        Assert.Equal("Completed", (await LogAsync(result.SyncId!.Value)).Status);
    }

    private sealed class FakeDownloadService : IJocondeDataService
    {
        public Task<string> DownloadJocondeDataAsync(string url, string destinationPath, CancellationToken cancellationToken = default)
        {
            File.WriteAllText(destinationPath, "Reference|Titre");
            return Task.FromResult(destinationPath);
        }
    }

    private sealed class FakeImportService : IDataImportService
    {
        public Func<string, CancellationToken, Task<ImportReport>> OnImport { get; set; } = (_, _) => Task.FromResult(new ImportReport());

        public Task<ImportReport> ImportFromFileAsync(string filePath, Action<string, int, int>? progressCallback = null, CancellationToken cancellationToken = default) =>
            OnImport(filePath, cancellationToken);

        public Task<ImportStatistics> ImportDataAsync(OpenJoconde.Core.Parsers.ParsingResult parsingResult, Action<string, int, int>? progressCallback = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string> DownloadJocondeDataAsync(string url, string destinationPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ImportReport> ImportFromJsonFileAsync(string jsonFilePath, Action<string, int, int>? progressCallback = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ImportReport> ImportFromCsvFileAsync(string csvFilePath, Action<string, int, int>? progressCallback = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
