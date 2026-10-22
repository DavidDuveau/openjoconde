using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.Core.Parsers;
using OpenJoconde.Infrastructure.Services;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>Lecture en flux d'un fichier JSON Joconde : formes de valeurs, notices rejetées, erreurs (TECH-43)</summary>
public sealed class JsonParserTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"openjoconde_json_{Guid.NewGuid():N}");
    private readonly StreamingJocondeJsonParserService _parser = new(NullLogger<StreamingJocondeJsonParserService>.Instance);

    public JsonParserTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private string Write(string json)
    {
        var path = Path.Combine(_directory, $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }

    private async Task<(ParsingSummary Summary, List<ParsingResult> Batches)> Parse(string json, int batchSize = 100, Action<int, int>? progress = null)
    {
        var batches = new List<ParsingResult>();
        var summary = await _parser.ParseInBatchesAsync(Write(json), batchSize, batch => { batches.Add(batch); return Task.CompletedTask; }, progress);
        return (summary, batches);
    }

    [Fact]
    public void Constructeur_RefuseUnJournalNul() =>
        Assert.Throws<ArgumentNullException>(() => new StreamingJocondeJsonParserService(null!));

    [Fact]
    public async Task CheminVideOuFichierAbsent_Leve()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _parser.ParseInBatchesAsync("", 10, _ => Task.CompletedTask));
        await Assert.ThrowsAsync<FileNotFoundException>(() => _parser.ParseInBatchesAsync(Path.Combine(_directory, "absent.json"), 10, _ => Task.CompletedTask));
    }

    [Fact]
    public async Task ValeursEnTexteEnNombreEnTableau_SontLuesCommeDesChaines()
    {
        var progress = new List<(int, int)>();
        var (summary, batches) = await Parse("""
            [
              { "reference": 123, "titre": "  Titre avec espaces  ", "domaine": ["peinture", "", "peinture", "dessin"],
                "materiaux_techniques": "huile ; toile", "periode_de_creation": true, "denomination": null,
                "coordonnees": { "lat": 48.8, "lon": 2.3 } },
              { "reference": "B", "titre": "Sans coordonnées" },
              { "reference": "C", "titre": "Coordonnées texte", "coordonnees": "48.1,2.2" },
              { "reference": "D", "titre": "Coordonnées incomplètes", "coordonnees": { "lat": 48.8 } },
              { "reference": "E", "titre": "Coordonnées d'un autre type", "coordonnees": 12 },
              { "reference": "F", "titre": "Latitude illisible", "coordonnees": { "lat": "x", "lon": 2.3 } },
            ]
            """, progress: (current, total) => progress.Add((current, total)));

        var artworks = Assert.Single(batches).Artworks;
        Assert.Equal(6, summary.Artworks);
        Assert.Equal("123", artworks[0].Reference);
        Assert.Equal("Titre avec espaces", artworks[0].Title);
        Assert.Equal(2, summary.Domains);
        Assert.Equal(2, artworks[0].Domains.Count);
        Assert.Equal(2, artworks[0].Techniques.Count);
        Assert.NotEmpty(progress);
        Assert.Equal(progress[^1].Item1, progress[^1].Item2);
    }

    [Fact]
    public async Task NoticeQuiNEstPasUnObjet_EstRejeteeSansInterrompreLeFichier()
    {
        var (summary, batches) = await Parse("""[ 42, "texte", null, { "reference": "OK", "titre": "Bonne" } ]""");

        Assert.Equal(3, summary.RejectedRecords);
        Assert.Equal("OK", Assert.Single(Assert.Single(batches).Artworks).Reference);
    }

    [Fact]
    public async Task PlusDeNoticesQueLaTailleDeLot_RendPlusieursLots()
    {
        var (summary, batches) = await Parse("""
            [ { "reference": "A", "titre": "a" }, { "reference": "B", "titre": "b" }, { "reference": "C", "titre": "c" } ]
            """, batchSize: 2);

        Assert.Equal(3, summary.Artworks);
        Assert.Equal(new[] { 2, 1 }, batches.Select(b => b.Artworks.Count));
    }

    [Fact]
    public async Task FichierSansNotice_NeRendAucunLot()
    {
        var (summary, batches) = await Parse("[]");

        Assert.Empty(batches);
        Assert.Equal(0, summary.Artworks);
    }

    [Fact]
    public async Task JsonInvalide_LeveUneErreurExplicite()
    {
        var error = await Assert.ThrowsAsync<Exception>(() => Parse("""[ { "reference": "A" """));

        Assert.StartsWith("Erreur lors du parsing du fichier JSON", error.Message);
    }

    [Fact]
    public async Task Annule_LaisseRemonterLAnnulation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _parser.ParseInBatchesAsync(Write("""[ { "reference": "A", "titre": "a" } ]"""), 10, _ => Task.CompletedTask, null, cts.Token));
    }
}
