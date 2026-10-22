using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.Core.Models;
using OpenJoconde.Core.Parsers;
using OpenJoconde.Infrastructure.Services;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Import en flux : le fichier est lu et écrit par lots, jamais gardé entier en mémoire (TECH-17).
/// 4 500 notices font trois lots de lecture (2 000, 2 000, 500) : les artistes, domaines et musées partagés
/// sont créés dans le premier lot et cités par les suivants.
/// </summary>
public class ImportEnFluxTests : IAsyncLifetime
{
    private const int Notices = 4500;

    private readonly ImportDatabase _db = new();
    private readonly List<string> _fichiers = new();

    public Task InitializeAsync() => _db.InitializeAsync();

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        foreach (var fichier in _fichiers)
            File.Delete(fichier);
    }

    private string Csv(IEnumerable<int> numeros, Func<int, string>? titre = null)
    {
        var chemin = GeneratedCsv.Create(numeros, titre ?? (i => $"titre {i}"));
        _fichiers.Add(chemin);
        return chemin;
    }

    private async Task<ImportReport> ImporterAsync(string chemin, bool onlyChanges = false)
    {
        await using var context = _db.CreateContext();
        return await _db.CreateImportService(context).ImportFromFileAsync(chemin, onlyChanges: onlyChanges);
    }

    [Fact]
    public async Task Parseur_RendLesNoticesParLots_EtChaqueEntiteUneSeuleFois()
    {
        var parseur = new StreamingJocondeCsvParserService(NullLogger<StreamingJocondeCsvParserService>.Instance);
        var lots = new List<ParsingResult>();

        var resume = await parseur.ParseInBatchesAsync(Csv(Enumerable.Range(1, 250)), 100, lot =>
        {
            lots.Add(lot);
            return Task.CompletedTask;
        });

        Assert.Equal(new[] { 100, 100, 50 }, lots.Select(l => l.Artworks.Count));
        Assert.Equal(250, resume.Artworks);

        // 40 auteurs, 8 domaines, 3 musées au total ; aucun n'est rendu deux fois
        Assert.Equal(40, resume.Artists);
        Assert.Equal(40, lots.SelectMany(l => l.Artists).Select(a => a.Id).Distinct().Count());
        Assert.Equal(40, lots.Sum(l => l.Artists.Count));
        Assert.Equal(8, lots.Sum(l => l.Domains.Count));
        Assert.Equal(3, lots.Sum(l => l.Museums.Count));
        Assert.Empty(lots[2].Artists);
    }

    [Fact]
    public async Task Import_PlusieursLotsDeLecture_LieChaqueOeuvreAuBonArtisteEtAuBonMusee()
    {
        var report = await ImporterAsync(Csv(Enumerable.Range(1, Notices)));

        Assert.True(report.Success, report.ErrorMessage);
        Assert.Equal(Notices, report.TotalArtworks);
        Assert.Equal(Notices, report.ImportedArtworks);

        await using var context = _db.CreateContext();
        Assert.Equal(40, await context.Artists.CountAsync());
        Assert.Equal(8, await context.Domains.CountAsync());
        Assert.Equal(3, await context.Museums.CountAsync());
        Assert.Equal(Notices, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM artwork_artist").SingleAsync());
        Assert.Equal(Notices, await context.Artworks.CountAsync(a => a.MuseumId != null));

        // Une œuvre du dernier lot, dont l'artiste et le musée ont été créés dans le premier
        var derniere = await context.Artworks.Include(a => a.Museum).Include(a => a.Artists).ThenInclude(aa => aa.Artist)
            .SingleAsync(a => a.Reference == $"L{Notices:D6}");
        Assert.Equal($"AUTEUR{Notices % 40}", Assert.Single(derniere.Artists).Artist.LastName);
        Assert.Equal($"musée {Notices % 3}", derniere.Museum!.Name);
    }

    [Fact]
    public async Task Synchro_ReecritSeulementLesOeuvresModifieesDeChaqueLot()
    {
        await ImporterAsync(Csv(Enumerable.Range(1, Notices)));

        // Une notice modifiée dans chacun des trois lots de lecture
        var modifiees = new[] { 10, 2500, 4400 };
        var report = await ImporterAsync(
            Csv(Enumerable.Range(1, Notices), i => modifiees.Contains(i) ? $"titre {i} corrigé" : $"titre {i}"),
            onlyChanges: true);

        Assert.True(report.Success, report.ErrorMessage);
        Assert.Equal(modifiees.Length, report.ImportedArtworks);
        Assert.Equal(Notices - modifiees.Length, report.SkippedArtworks);
        Assert.Equal(0, report.RemovedArtworks);

        await using var context = _db.CreateContext();
        Assert.Equal(Notices, await context.Artworks.CountAsync());
        Assert.Equal("titre 2500 corrigé", (await context.Artworks.SingleAsync(a => a.Reference == "L002500")).Title);
        Assert.Equal(Notices, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM artwork_artist").SingleAsync());
    }

    [Fact]
    public async Task Synchro_SupprimeLesOeuvresAbsentesDeToutLeFichier()
    {
        await ImporterAsync(Csv(Enumerable.Range(1, Notices)));

        // Les 100 dernières notices ont disparu de la source : elles ne sont vues dans aucun lot
        var report = await ImporterAsync(Csv(Enumerable.Range(1, Notices - 100)), onlyChanges: true);

        Assert.True(report.Success, report.ErrorMessage);
        Assert.Equal(100, report.RemovedArtworks);
        Assert.Equal(Notices - 100, report.SkippedArtworks);
        await using var context = _db.CreateContext();
        Assert.Equal(Notices - 100, await context.Artworks.CountAsync());
    }
}
