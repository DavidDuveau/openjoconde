using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.Infrastructure.Services;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Téléchargement du fichier Joconde et lecture de la version de la source, sur un serveur simulé (TECH-43)
/// </summary>
public sealed class JocondeDownloaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"openjoconde_dl_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public List<HttpMethod> Methods { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Methods.Add(request.Method);
            return respond(request, cancellationToken);
        }
    }

    /// <summary>Contenu sans taille connue (transfert par morceaux) et dont la lecture traîne</summary>
    private sealed class SlowContent(byte[] bytes, TimeSpan delay) : HttpContent
    {
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await Task.Delay(delay);
            await stream.WriteAsync(bytes);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private static JocondeDownloader Downloader(FakeHandler handler) =>
        new(NullLogger<JocondeDownloader>.Instance, new HttpClient(handler));

    private static FakeHandler Ok(byte[] content) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(content) }));

    [Fact]
    public async Task Telecharge_EcritLeFichier_EtCreeLeDossier()
    {
        var destination = Path.Combine(_directory, "sous", "joconde.csv");
        var content = Enumerable.Range(0, 20000).Select(i => (byte)(i % 251)).ToArray();

        var path = await Downloader(Ok(content)).DownloadJocondeDataAsync("https://exemple.test/joconde.csv", destination);

        Assert.Equal(destination, path);
        Assert.Equal(content, await File.ReadAllBytesAsync(destination));
    }

    [Fact]
    public async Task Telecharge_VersUnNomSansDossier_FonctionneAussi()
    {
        var name = $"openjoconde_dl_{Guid.NewGuid():N}.csv";
        try
        {
            await Downloader(Ok([1, 2, 3])).DownloadJocondeDataAsync("https://exemple.test/x", name);
            Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(name));
        }
        finally
        {
            File.Delete(name);
        }
    }

    [Fact]
    public async Task Telecharge_UrlOuDestinationVide_Leve()
    {
        var downloader = Downloader(Ok([1]));

        await Assert.ThrowsAsync<ArgumentException>(() => downloader.DownloadJocondeDataAsync("", Path.Combine(_directory, "a")));
        await Assert.ThrowsAsync<ArgumentException>(() => downloader.DownloadJocondeDataAsync("https://exemple.test/x", ""));
    }

    [Fact]
    public async Task Telecharge_ReponseEnErreur_Leve()
    {
        var handler = new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            Downloader(handler).DownloadJocondeDataAsync("https://exemple.test/x", Path.Combine(_directory, "a")));
    }

    [Fact]
    public async Task Telecharge_DelaiDepasse_LeveUneTimeoutException()
    {
        var handler = new FakeHandler((_, _) => throw new TaskCanceledException("délai", new TimeoutException()));

        await Assert.ThrowsAsync<TimeoutException>(() =>
            Downloader(handler).DownloadJocondeDataAsync("https://exemple.test/x", Path.Combine(_directory, "a")));
    }

    [Fact]
    public async Task Telecharge_Annule_LaisseRemonterLAnnulation()
    {
        using var cts = new CancellationTokenSource();
        var handler = new FakeHandler((_, token) => { cts.Cancel(); token.ThrowIfCancellationRequested(); return Task.FromResult(new HttpResponseMessage()); });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Downloader(handler).DownloadJocondeDataAsync("https://exemple.test/x", Path.Combine(_directory, "a"), cts.Token));
    }

    [Fact]
    public async Task Telecharge_Lent_JournaliseLaProgression_AvecEtSansTailleConnue()
    {
        // Plus de 5 secondes de flux : la progression est journalisée, avec le pourcentage si la taille est connue
        var sized = new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new SlowSizedContent([1, 2, 3, 4]) }));
        var unsized = new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new SlowContent([1, 2, 3, 4], TimeSpan.FromSeconds(5.2)) }));

        var results = await Task.WhenAll(
            Downloader(sized).DownloadJocondeDataAsync("https://exemple.test/x", Path.Combine(_directory, "avec")),
            Downloader(unsized).DownloadJocondeDataAsync("https://exemple.test/x", Path.Combine(_directory, "sans")));

        Assert.Equal(8, File.ReadAllBytes(results[0]).Length);
        Assert.Equal(4, File.ReadAllBytes(results[1]).Length);
    }

    /// <summary>Taille annoncée de 8 octets, 4 reçus après 5 secondes, puis le reste : la progression a un pourcentage</summary>
    private sealed class SlowSizedContent(byte[] first) : HttpContent
    {
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await stream.WriteAsync(first);
            await stream.FlushAsync();
            await Task.Delay(TimeSpan.FromSeconds(5.2));
            await stream.WriteAsync(new byte[] { 5, 6, 7, 8 });
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 8;
            return true;
        }
    }

    [Fact]
    public async Task Version_LitLETagParUneRequeteHead()
    {
        var handler = new FakeHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.ETag = new EntityTagHeaderValue("\"abc\"");
            return Task.FromResult(response);
        });

        Assert.Equal("\"abc\"", await Downloader(handler).GetSourceVersionAsync("https://exemple.test/x"));
        Assert.Equal(HttpMethod.Head, Assert.Single(handler.Methods));
    }

    [Fact]
    public async Task Version_SansETagOuEnErreur_EstInconnue()
    {
        var noEtag = new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var failing = new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var unreachable = new FakeHandler((_, _) => throw new HttpRequestException("réseau coupé"));
        var timeout = new FakeHandler((_, _) => throw new TaskCanceledException("délai"));

        Assert.Null(await Downloader(noEtag).GetSourceVersionAsync("https://exemple.test/x"));
        Assert.Null(await Downloader(failing).GetSourceVersionAsync("https://exemple.test/x"));
        Assert.Null(await Downloader(unreachable).GetSourceVersionAsync("https://exemple.test/x"));
        Assert.Null(await Downloader(timeout).GetSourceVersionAsync("https://exemple.test/x"));
    }

    [Fact]
    public async Task Version_Annulee_LaisseRemonterLAnnulation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var handler = new FakeHandler((_, token) => throw new TaskCanceledException("annulé", null, token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Downloader(handler).GetSourceVersionAsync("https://exemple.test/x", cts.Token));
    }
}
