using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using OpenJoconde.Core.Parsers;
using OpenJoconde.Infrastructure.Services;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Service d'import sur des dépôts et des parseurs simulés : pannes d'écriture, écritures partielles, suppressions
/// refusées, téléchargement (TECH-43). Les imports de bout en bout sur PostgreSQL sont dans les autres classes.
/// </summary>
public sealed class DataImportServiceUnitTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"openjoconde_imp_{Guid.NewGuid():N}");

    public DataImportServiceUnitTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private sealed class Wiring
    {
        public (string, Func<object?[], object?>)[] Artworks { get; set; } = [];
        public (string, Func<object?[], object?>)[] Museums { get; set; } = [];
        public (string, Func<object?[], object?>)[] Domains { get; set; } = [];
        public ParsingResult? Batch { get; set; }
        public ParsingSummary Summary { get; set; } = new();
        public HttpMessageHandler? Http { get; set; }
    }

    private static Func<object?[], object?> Count(int written) => _ => Task.FromResult(written);

    private static DataImportService Service(Wiring w)
    {
        var parser = new FakeParser(w.Batch, w.Summary);
        return new DataImportService(
            NullLogger<DataImportService>.Instance,
            Stub.Create<IArtworkRepository>(w.Artworks),
            Stub.Create<IArtistRepository>(("BulkUpsertAsync", Count(1))),
            Stub.Create<IMuseumRepository>(w.Museums),
            Stub.Create<IDomainRepository>(w.Domains.Length > 0 ? w.Domains : [("BulkUpsertAsync", Count(1))]),
            Stub.Create<ITechniqueRepository>(("BulkUpsertAsync", Count(1))),
            Stub.Create<IPeriodRepository>(("BulkUpsertAsync", Count(1))),
            Stub.Create<IArtworkRelationsRepository>(("SaveBatchAsync", Count(1))),
            parser, parser,
            new HttpClient(w.Http ?? new FakeHandler(HttpStatusCode.OK, [1])));
    }

    private sealed class FakeParser(ParsingResult? batch, ParsingSummary summary) : IJocondeJsonParser, IJocondeCsvParser
    {
        public async Task<ParsingSummary> ParseInBatchesAsync(string path, int batchSize, Func<ParsingResult, Task> onBatch,
            Action<int, int>? progressCallback = null, CancellationToken cancellationToken = default)
        {
            progressCallback?.Invoke(0, 10);
            if (batch != null)
                await onBatch(batch);
            return summary;
        }
    }

    private sealed class FakeHandler(HttpStatusCode status, byte[] content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new ByteArrayContent(content) });
    }

    private static Artwork ArtworkOf(string reference, Museum? museum = null) =>
        new() { Id = Guid.NewGuid(), Reference = reference, Title = reference, Museum = museum, ContentHash = Guid.NewGuid().ToByteArray() };

    private string FileOf(string name)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, "x");
        return path;
    }

    [Fact]
    public void Constructeur_RefuseChaqueDependanceNulle()
    {
        ILogger<DataImportService> logger = NullLogger<DataImportService>.Instance;
        var artworks = Stub.Create<IArtworkRepository>();
        var artists = Stub.Create<IArtistRepository>();
        var museums = Stub.Create<IMuseumRepository>();
        var domains = Stub.Create<IDomainRepository>();
        var techniques = Stub.Create<ITechniqueRepository>();
        var periods = Stub.Create<IPeriodRepository>();
        var relations = Stub.Create<IArtworkRelationsRepository>();
        var parser = new FakeParser(null, new ParsingSummary());
        var http = new HttpClient();

        Assert.Throws<ArgumentNullException>(() => new DataImportService(null!, artworks, artists, museums, domains, techniques, periods, relations, parser, parser, http));
        Assert.Throws<ArgumentNullException>(() => new DataImportService(logger, null!, artists, museums, domains, techniques, periods, relations, parser, parser, http));
        Assert.Throws<ArgumentNullException>(() => new DataImportService(logger, artworks, null!, museums, domains, techniques, periods, relations, parser, parser, http));
        Assert.Throws<ArgumentNullException>(() => new DataImportService(logger, artworks, artists, null!, domains, techniques, periods, relations, parser, parser, http));
        Assert.Throws<ArgumentNullException>(() => new DataImportService(logger, artworks, artists, museums, null!, techniques, periods, relations, parser, parser, http));
        Assert.Throws<ArgumentNullException>(() => new DataImportService(logger, artworks, artists, museums, domains, null!, periods, relations, parser, parser, http));
        Assert.Throws<ArgumentNullException>(() => new DataImportService(logger, artworks, artists, museums, domains, techniques, null!, relations, parser, parser, http));
        Assert.Throws<ArgumentNullException>(() => new DataImportService(logger, artworks, artists, museums, domains, techniques, periods, null!, parser, parser, http));
        Assert.Throws<ArgumentNullException>(() => new DataImportService(logger, artworks, artists, museums, domains, techniques, periods, relations, null!, parser, http));
        Assert.Throws<ArgumentNullException>(() => new DataImportService(logger, artworks, artists, museums, domains, techniques, periods, relations, parser, null!, http));
        Assert.Throws<ArgumentNullException>(() => new DataImportService(logger, artworks, artists, museums, domains, techniques, periods, relations, parser, parser, null!));
    }

    [Fact]
    public async Task ImportData_NoticesRejeteesEtEcritureDomainesEnPanne_SontCompteesDansLesErreurs()
    {
        var service = Service(new Wiring { Domains = [Stub.Throws("BulkUpsertAsync")] });
        var parsing = new ParsingResult { RejectedRecords = 3, Domains = { new Domain { Name = "peinture" } } };

        var stats = await service.ImportDataAsync(parsing);

        Assert.Equal(0, stats.DomainsImported);
        Assert.Equal(4, stats.Errors);
        Assert.Contains("3 notice(s) rejetée(s) au parsing", stats.ErrorMessages);
        Assert.Contains(stats.ErrorMessages, m => m.StartsWith("Domaines : "));
    }

    [Fact]
    public async Task ImportData_EcritureDomainesPartielle_SignaleLeManque_EtLaProgression()
    {
        var service = Service(new Wiring { Domains = [("BulkUpsertAsync", Count(1))] });
        var parsing = new ParsingResult { Domains = { new Domain { Name = "a" }, new Domain { Name = "b" }, new Domain { Name = "c" } } };
        var progress = new List<string>();

        var stats = await service.ImportDataAsync(parsing, (phase, _, _) => progress.Add(phase));

        Assert.Equal(1, stats.DomainsImported);
        Assert.Equal(2, stats.Errors);
        Assert.Contains("Domaines : 2 sur 3 non enregistré(s)", stats.ErrorMessages);
        Assert.Contains("Domaines", progress);
    }

    [Fact]
    public async Task ImportData_MuseeNonEnregistre_LesOeuvresNeLeReferencentPas()
    {
        IEnumerable<Artwork>? written = null;
        var museum = new Museum { Id = Guid.NewGuid(), Name = "Perdu" };
        var service = Service(new Wiring
        {
            Museums = [("BulkUpsertAsync", Count(0)), ("GetExistingIdsAsync", _ => Task.FromResult(new HashSet<Guid>()))],
            Artworks = [("BulkUpsertAsync", args => { written = (IEnumerable<Artwork>)args[0]!; return Task.FromResult(1); })]
        });

        var stats = await service.ImportDataAsync(new ParsingResult
        {
            Museums = { museum },
            Artworks = { ArtworkOf("A1", museum) }
        });

        Assert.Equal(1, stats.ArtworksImported);
        Assert.Equal(1, stats.Errors);
        Assert.Null(Assert.Single(written!).MuseumId);
    }

    [Fact]
    public async Task ImportData_OeuvresEcritesEnPartie_SignaleLesManquantes()
    {
        var service = Service(new Wiring { Artworks = [("BulkUpsertAsync", Count(1))] });

        var stats = await service.ImportDataAsync(new ParsingResult { Artworks = { ArtworkOf("A1"), ArtworkOf("A2"), ArtworkOf("A3") } });

        Assert.Equal(1, stats.ArtworksImported);
        Assert.Contains("Œuvres : 2 sur 3 non enregistrée(s)", stats.ErrorMessages);
    }

    [Fact]
    public async Task ImportData_Annule_LeveUneAnnulation()
    {
        var service = Service(new Wiring { Domains = [("BulkUpsertAsync", Count(1))] });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ImportDataAsync(new ParsingResult { Domains = { new Domain { Name = "a" } } }, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task Fichier_Inexistant_EchoueProprement()
    {
        var report = await Service(new Wiring()).ImportFromFileAsync(Path.Combine(_directory, "absent.csv"));

        Assert.False(report.Success);
        Assert.Contains("n'existe pas", report.ErrorMessage);
        Assert.Equal(1, report.Errors);
    }

    [Fact]
    public async Task Fichier_FormatInconnu_Leve()
    {
        await Assert.ThrowsAsync<NotSupportedException>(() => Service(new Wiring()).ImportFromFileAsync(FileOf("x.xml")));
    }

    [Fact]
    public async Task Fichier_JsonEtCsv_PassentParLeurParseur_EtRapportentLesTotauxEtLesNoticesRejetees()
    {
        var summary = new ParsingSummary { Artworks = 5, Artists = 4, Museums = 3, Domains = 2, Techniques = 1, Periods = 6, RejectedRecords = 2 };
        var service = Service(new Wiring { Summary = summary });
        var progress = new List<string>();

        var json = await service.ImportFromJsonFileAsync(FileOf("a.json"), (phase, _, _) => progress.Add(phase));
        var csv = await service.ImportFromCsvFileAsync(FileOf("a.csv"));

        Assert.Equal(5, json.TotalArtworks);
        Assert.Equal(6, csv.TotalPeriods);
        Assert.False(json.Success);
        Assert.Contains("2 notice(s) rejetée(s) au parsing", json.ErrorMessage);
        Assert.Contains("Parsing", progress);
    }

    private Wiring Incremental(string[] stored, int rejected, (string, Func<object?[], object?>)? deletion = null)
    {
        var hashes = stored.ToDictionary(r => r, _ => (Guid?)Guid.NewGuid());
        return new Wiring
        {
            Summary = new ParsingSummary { Artworks = 1, RejectedRecords = rejected },
            Batch = new ParsingResult { Artworks = { ArtworkOf("PRESENTE") } },
            Artworks =
            [
                ("GetContentHashesAsync", _ => Task.FromResult(hashes)),
                ("BulkUpsertAsync", args => Task.FromResult(((IEnumerable<Artwork>)args[0]!).Count())),
                deletion ?? ("DeleteByReferencesAsync", args => Task.FromResult(((IReadOnlyCollection<string>)args[0]!).Count))
            ]
        };
    }

    private static string[] Stored(int count) => Enumerable.Range(0, count).Select(i => $"REF{i}").Prepend("PRESENTE").ToArray();

    [Fact]
    public async Task Incremental_TropDAbsentes_RienNEstSupprime()
    {
        var report = await Service(Incremental(Stored(30).Concat(new[] { "ABSENTE" }).ToArray(), rejected: 0))
            .ImportFromFileAsync(FileOf("a.csv"), onlyChanges: true);

        // 30 absentes sur 32 : au-delà du seuil de 10 %
        Assert.Equal(0, report.RemovedArtworks);
        Assert.Contains("Suppression refusée", report.ErrorMessage);
    }

    [Fact]
    public async Task Incremental_UneSeuleAbsenteSurBeaucoup_EstSupprimee()
    {
        var stored = Enumerable.Range(0, 50).Select(i => $"PRESENTE{i}").Append("PRESENTE").Append("ABSENTE").ToArray();
        var wiring = Incremental(stored, rejected: 0);
        wiring.Batch = new ParsingResult { Artworks = Enumerable.Range(0, 50).Select(i => ArtworkOf($"PRESENTE{i}")).Append(ArtworkOf("PRESENTE")).ToList() };

        var report = await Service(wiring).ImportFromFileAsync(FileOf("a.csv"), onlyChanges: true);

        Assert.True(report.Success, report.ErrorMessage);
        Assert.Equal(1, report.RemovedArtworks);
    }

    [Fact]
    public async Task Incremental_NoticesRejetees_RienNEstSupprime()
    {
        var report = await Service(Incremental(Stored(3), rejected: 2)).ImportFromFileAsync(FileOf("a.csv"), onlyChanges: true);

        Assert.Equal(0, report.RemovedArtworks);
        Assert.Contains("rejetée(s) au parsing", report.ErrorMessage);
        Assert.Contains("non supprimée(s)", report.ErrorMessage);
    }

    [Fact]
    public async Task Incremental_SuppressionEnPanne_EstSignalee()
    {
        var stored = Enumerable.Range(0, 50).Select(i => $"PRESENTE{i}").Append("PRESENTE").Append("ABSENTE").ToArray();
        var wiring = Incremental(stored, rejected: 0, deletion: Stub.Throws("DeleteByReferencesAsync"));
        wiring.Batch = new ParsingResult { Artworks = Enumerable.Range(0, 50).Select(i => ArtworkOf($"PRESENTE{i}")).Append(ArtworkOf("PRESENTE")).ToList() };

        var report = await Service(wiring).ImportFromFileAsync(FileOf("a.csv"), onlyChanges: true);

        Assert.False(report.Success);
        Assert.Contains("Suppression des œuvres absentes de la source", report.ErrorMessage);
    }

    [Fact]
    public async Task Incremental_Annule_RendUnRapportAnnule()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var report = await Service(Incremental(Stored(1), rejected: 0))
            .ImportFromFileAsync(FileOf("a.csv"), cancellationToken: cts.Token, onlyChanges: true);

        Assert.False(report.Success);
        Assert.Equal("Importation annulée", report.ErrorMessage);
    }

    [Fact]
    public async Task Telechargement_EcritLeFichier_EtRefuseLesArgumentsVides()
    {
        var service = Service(new Wiring { Http = new FakeHandler(HttpStatusCode.OK, [9, 8, 7]) });
        var destination = Path.Combine(_directory, "dl", "joconde.csv");

        Assert.Equal(destination, await service.DownloadJocondeDataAsync("https://exemple.test/x", destination));
        Assert.Equal(new byte[] { 9, 8, 7 }, await File.ReadAllBytesAsync(destination));
        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadJocondeDataAsync("", destination));
        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadJocondeDataAsync("https://exemple.test/x", ""));
    }

    [Fact]
    public async Task Telechargement_ReponseEnErreur_Leve()
    {
        var service = Service(new Wiring { Http = new FakeHandler(HttpStatusCode.BadGateway, []) });

        await Assert.ThrowsAsync<HttpRequestException>(() => service.DownloadJocondeDataAsync("https://exemple.test/x", Path.Combine(_directory, "dl2.csv")));
    }
}
