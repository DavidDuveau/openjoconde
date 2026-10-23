using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Infrastructure.Configuration;
using OpenJoconde.Infrastructure.Services;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>Wikidata et Wikipédia de substitution : réponses écrites d'avance, pannes à la demande.</summary>
internal sealed class FakeWikidataClient : IWikidataClient
{
    public Dictionary<string, string[]> Searches { get; } = new();
    public Dictionary<string, WikidataEntity> Entities { get; } = new();
    public Dictionary<string, string?> Summaries { get; } = new();
    public HashSet<string> FailingNames { get; } = new();
    public List<string> SearchedNames { get; } = new();

    public Task<IReadOnlyList<string>> SearchAsync(string name, CancellationToken cancellationToken = default)
    {
        SearchedNames.Add(name);
        if (FailingNames.Contains(name))
            throw new HttpRequestException("Wikidata est hors d'atteinte");
        return Task.FromResult<IReadOnlyList<string>>(Searches.GetValueOrDefault(name) ?? Array.Empty<string>());
    }

    public Task<IReadOnlyDictionary<string, WikidataEntity>> GetEntitiesAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<string, WikidataEntity>>(
            ids.Where(Entities.ContainsKey).ToDictionary(id => id, id => Entities[id]));

    public Task<string?> GetSummaryAsync(string title, CancellationToken cancellationToken = default) =>
        Task.FromResult(Summaries.GetValueOrDefault(title));

    public static WikidataEntity Person(string id, string label, int birth, int? death, string title,
        string[]? citizenship = null, string[]? movements = null, bool human = true) =>
        new(id, label, Array.Empty<string>(), human, new[] { birth }, death == null ? Array.Empty<int>() : new[] { death.Value },
            title, citizenship ?? Array.Empty<string>(), movements ?? Array.Empty<string>());

    public static WikidataEntity Label(string id, string label) =>
        new(id, label, Array.Empty<string>(), false, Array.Empty<int>(), Array.Empty<int>(), null, Array.Empty<string>(), Array.Empty<string>());
}

/// <summary>
/// Enrichissement des artistes par Wikidata et Wikipédia (TECH-44) : rapprochement strict, ordre des candidats, pannes,
/// et rien n'est redemandé une fois examiné. Base PostgreSQL réelle, Wikimedia remplacé par <see cref="FakeWikidataClient"/>.
/// </summary>
public class EnrichissementArtistesTests : IAsyncLifetime
{
    private readonly ImportDatabase _db = new();
    private readonly FakeWikidataClient _wikidata = new();
    private string? _fichier;

    // MONET : 4 notices, RENOIR : 3, ROUSSEAU : 1 ; un anonyme (2) et un artiste sans année de naissance (2)
    private static readonly string[] Auteurs =
    {
        "MONET Claude", "MONET Claude", "MONET Claude", "MONET Claude",
        "RENOIR Auguste", "RENOIR Auguste", "RENOIR Auguste",
        "ROUSSEAU Henri",
        "Anonyme Inconnu", "Anonyme Inconnu",
        "SANSDATE Paul", "SANSDATE Paul"
    };

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();
        _fichier = GeneratedCsv.Create(Enumerable.Range(1, Auteurs.Length), i => $"Titre {i}", author: i => Auteurs[i - 1]);
        await using var context = _db.CreateContext();
        Assert.True((await _db.CreateImportService(context).ImportFromFileAsync(_fichier)).Success);

        foreach (var (nom, naissance, deces) in new[] { ("MONET", "1840", "1926"), ("RENOIR", "1841", "1919"), ("ROUSSEAU", "1844", "1910"), ("Anonyme", "1700", null) })
        {
            var artiste = await context.Artists.SingleAsync(a => a.LastName == nom);
            artiste.BirthDate = naissance;
            artiste.DeathDate = deces;
        }
        await context.SaveChangesAsync();

        _wikidata.Searches["Claude MONET"] = new[] { "Q296" };
        _wikidata.Entities["Q296"] = FakeWikidataClient.Person("Q296", "Claude Monet", 1840, 1926, "Claude Monet", new[] { "Q142" }, new[] { "Q40415" });
        _wikidata.Entities["Q142"] = FakeWikidataClient.Label("Q142", "France");
        _wikidata.Entities["Q40415"] = FakeWikidataClient.Label("Q40415", "impressionnisme");
        _wikidata.Summaries["Claude Monet"] = "Claude Monet est un peintre français.";
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        if (_fichier != null)
            File.Delete(_fichier);
    }

    private static EnrichmentOptions Options(int batch = 100, int minimum = 3) =>
        new() { BatchSize = batch, MinimumArtworks = minimum, PauseBetweenArtistsMilliseconds = 0 };

    private async Task<ArtistEnrichmentReport> EnrichirAsync(EnrichmentOptions? options = null)
    {
        await using var context = _db.CreateContext();
        var service = new ArtistEnrichmentService(context, _wikidata, Microsoft.Extensions.Options.Options.Create(options ?? Options()),
            NullLogger<ArtistEnrichmentService>.Instance);
        return await service.EnrichBatchAsync();
    }

    private async Task<OpenJoconde.Core.Models.Artist> ArtisteAsync(string nom)
    {
        await using var context = _db.CreateContext();
        return await context.Artists.AsNoTracking().SingleAsync(a => a.LastName == nom);
    }

    [Fact]
    public async Task UnArtisteRapprocheDeFaconSure_ReçoitBiographieSourceNationaliteEtCourant()
    {
        var bilan = await EnrichirAsync();

        var monet = await ArtisteAsync("MONET");
        Assert.Equal("Claude Monet est un peintre français.", monet.Biography);
        Assert.Equal("Q296", monet.WikidataId);
        Assert.Equal("https://fr.wikipedia.org/wiki/Claude_Monet", monet.WikipediaUrl);
        Assert.Equal("France", monet.Nationality);
        Assert.Equal("impressionnisme", monet.Movement);
        Assert.NotNull(monet.EnrichmentAttemptedAt);
        Assert.Equal(1, bilan.Enriched);
    }

    [Fact]
    public async Task LaNationaliteDeLaNotice_EstGardee()
    {
        await using (var context = _db.CreateContext())
        {
            (await context.Artists.SingleAsync(a => a.LastName == "MONET")).Nationality = "française";
            await context.SaveChangesAsync();
        }

        await EnrichirAsync();

        Assert.Equal("française", (await ArtisteAsync("MONET")).Nationality);
    }

    [Fact]
    public async Task UnArtisteSansCorrespondance_EstMarqueExamineSansRienRecevoir()
    {
        _wikidata.Searches["Auguste RENOIR"] = new[] { "Q1" };
        _wikidata.Entities["Q1"] = FakeWikidataClient.Person("Q1", "Auguste Renoir", 1840, 1919, "Auguste Renoir"); // né une autre année

        await EnrichirAsync();

        var renoir = await ArtisteAsync("RENOIR");
        Assert.Null(renoir.Biography);
        Assert.Null(renoir.WikidataId);
        Assert.NotNull(renoir.EnrichmentAttemptedAt);
    }

    [Fact]
    public async Task DeuxCorrespondancesPossibles_NeDonnentRien()
    {
        _wikidata.Searches["Auguste RENOIR"] = new[] { "Q1", "Q2" };
        _wikidata.Entities["Q1"] = FakeWikidataClient.Person("Q1", "Auguste Renoir", 1841, 1919, "Auguste Renoir");
        _wikidata.Entities["Q2"] = FakeWikidataClient.Person("Q2", "Auguste Renoir", 1841, null, "Auguste Renoir (homonyme)");
        _wikidata.Summaries["Auguste Renoir"] = "Auguste Renoir est un peintre.";

        await EnrichirAsync();

        var renoir = await ArtisteAsync("RENOIR");
        Assert.Null(renoir.Biography);
        Assert.NotNull(renoir.EnrichmentAttemptedAt);
    }

    [Fact]
    public async Task UnArticleSurUneEntreprise_NestPasUneBiographie()
    {
        _wikidata.Searches["Auguste RENOIR"] = new[] { "Q1" };
        _wikidata.Entities["Q1"] = FakeWikidataClient.Person("Q1", "Auguste Renoir", 1841, 1919, "Renoir (société)");
        _wikidata.Summaries["Renoir (société)"] = "Renoir est une société de peinture.";

        await EnrichirAsync();

        Assert.Null((await ArtisteAsync("RENOIR")).Biography);
    }

    [Fact]
    public async Task UnArticleSansResume_NeDonneRien()
    {
        _wikidata.Searches["Auguste RENOIR"] = new[] { "Q1" };
        _wikidata.Entities["Q1"] = FakeWikidataClient.Person("Q1", "Auguste Renoir", 1841, 1919, "Auguste Renoir");

        await EnrichirAsync();

        Assert.Null((await ArtisteAsync("RENOIR")).Biography);
    }

    [Fact]
    public async Task SeulsLesArtistesAssezFournisEtDatesSontExamines()
    {
        var bilan = await EnrichirAsync();

        // MONET et RENOIR (≥ 3 œuvres) seulement : ROUSSEAU n'a qu'une œuvre, SANSDATE aucune naissance connue, l'anonyme 2 œuvres
        Assert.Equal(2, bilan.Examined);
        Assert.Null((await ArtisteAsync("ROUSSEAU")).EnrichmentAttemptedAt);
        Assert.Null((await ArtisteAsync("SANSDATE")).EnrichmentAttemptedAt);
        Assert.DoesNotContain("Henri ROUSSEAU", _wikidata.SearchedNames);
    }

    [Fact]
    public async Task LesNoticesCollectives_SontExamineesSansInterrogerWikidata()
    {
        var bilan = await EnrichirAsync(Options(minimum: 2));

        Assert.Equal(3, bilan.Examined);
        Assert.NotNull((await ArtisteAsync("Anonyme")).EnrichmentAttemptedAt);
        Assert.DoesNotContain(_wikidata.SearchedNames, n => n.Contains("Anonyme"));
    }

    [Fact]
    public async Task LesPlusFournisPassentLesPremiers_EtLeLotEstBorne()
    {
        var bilan = await EnrichirAsync(Options(batch: 1));

        Assert.Equal(1, bilan.Examined);
        Assert.NotNull((await ArtisteAsync("MONET")).EnrichmentAttemptedAt);
        Assert.Null((await ArtisteAsync("RENOIR")).EnrichmentAttemptedAt);
    }

    [Fact]
    public async Task UnArtisteExamine_NestPasRedemande()
    {
        await EnrichirAsync();
        _wikidata.SearchedNames.Clear();

        var bilan = await EnrichirAsync();

        Assert.Equal(0, bilan.Examined);
        Assert.Empty(_wikidata.SearchedNames);
    }

    [Fact]
    public async Task UnArtisteDejaPourvuDeBiographie_NestPasExamine()
    {
        await using (var context = _db.CreateContext())
        {
            (await context.Artists.SingleAsync(a => a.LastName == "MONET")).Biography = "Saisie à la main.";
            await context.SaveChangesAsync();
        }

        await EnrichirAsync();

        Assert.Equal("Saisie à la main.", (await ArtisteAsync("MONET")).Biography);
        Assert.DoesNotContain("Claude MONET", _wikidata.SearchedNames);
    }

    [Fact]
    public async Task UnePanne_LaisseLArtisteVierge_PourUneProchaineFois()
    {
        _wikidata.FailingNames.Add("Auguste RENOIR");

        var bilan = await EnrichirAsync();

        Assert.Equal(1, bilan.Failed);
        Assert.Equal(1, bilan.Enriched);
        Assert.False(bilan.Aborted);
        var renoir = await ArtisteAsync("RENOIR");
        Assert.Null(renoir.EnrichmentAttemptedAt);

        _wikidata.FailingNames.Clear();
        Assert.Equal(1, (await EnrichirAsync()).Examined);
    }

    [Fact]
    public async Task CinqPannesDeSuite_InterrompentLeLot()
    {
        var artistes = Enumerable.Range(1, 6).Select(i => new OpenJoconde.Core.Models.Artist
        {
            Id = Guid.NewGuid(), LastName = $"PANNE{i}", FirstName = "Z", BirthDate = "1900"
        }).ToList();
        await using (var context = _db.CreateContext())
        {
            context.Artists.AddRange(artistes);
            // Cinq œuvres chacun : ils passent avant MONET (4) et ne sont pas séparés par un artiste qui réussirait
            var oeuvres = await context.Artworks.Take(5).ToListAsync();
            foreach (var artiste in artistes)
                foreach (var oeuvre in oeuvres)
                    context.Set<OpenJoconde.Core.Models.ArtworkArtist>().Add(new() { ArtworkId = oeuvre.Id, ArtistId = artiste.Id, Role = "auteur" });
            await context.SaveChangesAsync();
        }
        foreach (var artiste in artistes)
            _wikidata.FailingNames.Add($"Z {artiste.LastName}");

        var bilan = await EnrichirAsync(Options(minimum: 1, batch: 6));

        Assert.True(bilan.Aborted);
        Assert.Equal(5, bilan.Failed);
        await using var context2 = _db.CreateContext();
        Assert.Equal(0, await context2.Artists.CountAsync(a => a.LastName.StartsWith("PANNE") && a.EnrichmentAttemptedAt != null));
    }
}
