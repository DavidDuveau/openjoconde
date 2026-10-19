using Microsoft.EntityFrameworkCore;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Le rapport d'import dit ce qui a réellement été écrit et signale ce qui a été perdu (TECH-04).
/// </summary>
public class ImportRapportTests : IAsyncLifetime
{
    private readonly ImportDatabase _db = new();

    public Task InitializeAsync() => _db.InitializeAsync();

    public Task DisposeAsync() => _db.DisposeAsync();

    private async Task<ImportReport> ImporterAsync(
        string fixture,
        IArtworkRepository? artworks = null,
        IArtworkRelationsRepository? relations = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = _db.CreateContext();
        return await _db.CreateImportService(context, artworks, relations)
            .ImportFromFileAsync(ImportDatabase.FixturePath(fixture), cancellationToken: cancellationToken);
    }

    [Fact]
    public async Task Rapport_ImportReussi_CompteCeQuiEstEnBase()
    {
        var report = await ImporterAsync("joconde_extrait_reel.csv");

        Assert.True(report.Success, report.ErrorMessage);
        Assert.Equal(0, report.Errors);
        await using var context = _db.CreateContext();
        Assert.Equal(await context.Artworks.CountAsync(), report.ImportedArtworks);
        Assert.Equal(await context.Museums.CountAsync(), report.ImportedMuseums);
        Assert.Equal(await context.Domains.CountAsync(), report.ImportedDomains);
        Assert.Equal(await context.Techniques.CountAsync(), report.ImportedTechniques);
        Assert.Equal(await context.Artists.CountAsync(), report.ImportedArtists);
    }

    [Fact]
    public async Task Rapport_EcritureDesOeuvresRefusee_NeCompteAucuneOeuvreEtSignaleLesErreurs()
    {
        var report = await ImporterAsync("joconde_extrait_reel.csv", artworks: new FailingArtworkRepository());

        Assert.False(report.Success);
        Assert.Equal(0, report.ImportedArtworks);
        Assert.Equal(20, report.Errors);
        Assert.Contains("écriture des œuvres refusée", report.ErrorMessage);
        await using var context = _db.CreateContext();
        Assert.Equal(0, await context.Artworks.CountAsync());
    }

    [Fact]
    public async Task Rapport_EcritureDesRelationsRefusee_SignaleChaqueRelationPerdue()
    {
        // Une œuvre avec artiste, domaine, technique et période : quatre écritures de relations
        var report = await ImporterAsync("joconde_une_oeuvre.json", relations: new FailingRelationsRepository());

        Assert.False(report.Success);
        Assert.Equal(1, report.ImportedArtworks);
        Assert.Equal(4, report.Errors);
        Assert.Contains("écriture des relations refusée", report.ErrorMessage);
    }

    [Fact]
    public async Task Rapport_NoticeRejeteeAuParsing_EstComptee()
    {
        // La dernière ligne du fichier est tronquée : elle ne doit pas disparaître sans trace
        var report = await ImporterAsync("joconde_cas_particuliers.csv");

        Assert.False(report.Success);
        Assert.Equal(4, report.ImportedArtworks);
        Assert.Equal(1, report.Errors);
        Assert.Contains("rejetée", report.ErrorMessage);
    }

    [Fact]
    public async Task Rapport_ImportAnnule_NEstPasDeclareReussi()
    {
        using var annulation = new CancellationTokenSource();
        annulation.Cancel();

        var report = await ImporterAsync("joconde_extrait_reel.csv", cancellationToken: annulation.Token);

        Assert.False(report.Success);
        Assert.Contains("annulée", report.ErrorMessage);
        Assert.Equal(0, report.ImportedArtworks);
        await using var context = _db.CreateContext();
        Assert.Equal(0, await context.Artworks.CountAsync());
    }
}
