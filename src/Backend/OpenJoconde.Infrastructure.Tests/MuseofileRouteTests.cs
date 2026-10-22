using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenJoconde.Infrastructure.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.API.Controllers;
using OpenJoconde.Core.Interfaces;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// POST api/museofile/sync : applique Muséofile sans synchroniser Joconde (TECH-36)
/// </summary>
public class MuseofileRouteTests
{
    private static MuseofileController Controleur(FakeMuseofile service, string? url = "https://exemple.test/museofile.csv")
    {
        return new MuseofileController(service, Options.Create(new SyncOptions { MuseofileSourceUrl = url }), NullLogger<MuseofileController>.Instance);
    }

    [Fact]
    public async Task Rend_LeBilanDeLEnrichissement()
    {
        var service = new FakeMuseofile { Report = new MuseofileReport(1216, 523, 523, 8) };

        var reponse = await Controleur(service).Sync(CancellationToken.None);

        Assert.Equal(new MuseofileReport(1216, 523, 523, 8), Assert.IsType<OkObjectResult>(reponse).Value);
        Assert.Equal("https://exemple.test/museofile.csv", service.LastUrl);
    }

    [Fact]
    public async Task SansUrlConfiguree_Repond503()
    {
        var reponse = await Controleur(new FakeMuseofile(), url: null).Sync(CancellationToken.None);

        Assert.Equal(503, Assert.IsType<ObjectResult>(reponse).StatusCode);
    }

    [Fact]
    public async Task UneErreurDeLecture_Repond502_SansLeMessageDeLException()
    {
        var service = new FakeMuseofile { Failure = new HttpRequestException("détail interne à ne pas exposer") };

        var reponse = await Controleur(service).Sync(CancellationToken.None);

        var objet = Assert.IsType<ObjectResult>(reponse);
        Assert.Equal(502, objet.StatusCode);
        Assert.DoesNotContain("interne", (string)objet.Value!);
    }

    private sealed class FakeMuseofile : IMuseofileService
    {
        public MuseofileReport Report { get; set; } = new(0, 0, 0, 0);
        public Exception? Failure { get; set; }
        public string? LastUrl { get; private set; }

        public Task<MuseofileReport> EnrichFromUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            LastUrl = url;
            return Failure != null ? Task.FromException<MuseofileReport>(Failure) : Task.FromResult(Report);
        }

        public Task<MuseofileReport> EnrichFromFileAsync(string csvFilePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
