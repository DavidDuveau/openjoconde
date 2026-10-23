using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Infrastructure.Configuration;
using OpenJoconde.Infrastructure.Services;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>Règles du rapprochement d'un artiste avec Wikidata : strictes, un doute vaut un refus.</summary>
public class ArtistMatcherTests
{
    private static readonly ArtistCandidate Monet = new("Claude Monet", 1840, 1926);

    private static WikidataEntity Entity(string label = "Claude Monet", int[]? birth = null, int[]? death = null,
        bool human = true, string? title = "Claude Monet", string[]? aliases = null) =>
        new("Q1", label, aliases ?? Array.Empty<string>(), human, birth ?? new[] { 1840 }, death ?? new[] { 1926 },
            title, Array.Empty<string>(), Array.Empty<string>());

    [Fact]
    public void UnHumainDeMemeNomEtDesMemesAnnees_Correspond() =>
        Assert.True(ArtistMatcher.Matches(Entity(), Monet));

    [Fact]
    public void LeNomSeComparePartoutSansAccentNiCasse() =>
        Assert.True(ArtistMatcher.Matches(Entity("CLAUDE MONÉT"), Monet));

    [Fact]
    public void UnAutreNom_NeCorrespondPas() =>
        Assert.False(ArtistMatcher.Matches(Entity("Édouard Manet"), Monet));

    [Fact]
    public void UnAliasDeMemeNom_Correspond() =>
        Assert.True(ArtistMatcher.Matches(Entity("Oscar-Claude Monet", aliases: new[] { "Claude Monet" }), Monet));

    [Fact]
    public void UneAutreAnneeDeNaissance_NeCorrespondPas() =>
        Assert.False(ArtistMatcher.Matches(Entity(birth: new[] { 1841 }), Monet));

    [Fact]
    public void UneAnneeDeDecesDifferente_NeCorrespondPas_MaisUnDecesInconnuOui()
    {
        Assert.False(ArtistMatcher.Matches(Entity(death: new[] { 1930 }), Monet));
        Assert.True(ArtistMatcher.Matches(Entity(death: Array.Empty<int>()), Monet));
        Assert.True(ArtistMatcher.Matches(Entity(), Monet with { DeathYear = null }));
    }

    [Fact]
    public void CeQuiNEstPasUnHumain_OuSansArticleFrancais_NeCorrespondPas()
    {
        Assert.False(ArtistMatcher.Matches(Entity(human: false), Monet));
        Assert.False(ArtistMatcher.Matches(Entity(title: null), Monet));
    }

    [Fact]
    public void UneEntiteSansNom_NeCorrespondPas() =>
        Assert.False(ArtistMatcher.Matches(Entity() with { Label = null }, Monet));

    [Theory]
    [InlineData("Claude", "Monet", "Claude Monet")]
    [InlineData(null, "Monet", "Monet")]
    [InlineData("Jean-François (d'après);anonyme", "Millet", null)]
    [InlineData("", "Atelier de Rubens", null)]
    [InlineData("", "", null)]
    public void LeNomComplet_ÉcarteLesNoticesCollectives(string? prenom, string? nom, string? attendu) =>
        Assert.Equal(attendu, ArtistMatcher.FullName(prenom, nom));

    [Theory]
    [InlineData("+1840-11-14T00:00:00Z", 1840)]
    [InlineData("+1840-00-00T00:00:00Z", 1840)]
    [InlineData("-0500-01-01T00:00:00Z", null)]
    [InlineData("actif 1940", null)]
    [InlineData(null, null)]
    public void LAnneeDUneDateWikidata(string? date, int? attendu) =>
        Assert.Equal(attendu, ArtistMatcher.YearOf(date));

    [Theory]
    [InlineData("Claude Monet est un peintre français.", true)]
    [InlineData("Charles Jourdan est une société spécialisée dans la chaussure.", false)]
    [InlineData("Les Dupont est une famille d'artistes.", false)]
    [InlineData(null, true)]
    public void UnResumeDEntreprise_NestPasUneBiographie(string? resume, bool attendu) =>
        Assert.Equal(attendu, ArtistMatcher.IsPersonSummary(resume));
}

/// <summary>Le client lit les réponses réelles de Wikidata et de Wikipédia et se présente avec un contact.</summary>
public class WikidataClientTests
{
    private sealed class Reponses : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requetes { get; } = new();
        public Func<Uri, (HttpStatusCode, string)> Repondre { get; set; } = _ => (HttpStatusCode.OK, "{}");

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requetes.Add(request);
            var (code, corps) = Repondre(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(code) { Content = new StringContent(corps) });
        }
    }

    private const string Monet = """
        {"entities": {
          "Q296": {
            "labels": {"fr": {"language": "fr", "value": "Claude Monet"}},
            "aliases": {"fr": [{"language": "fr", "value": "Oscar-Claude Monet"}]},
            "sitelinks": {"frwiki": {"site": "frwiki", "title": "Claude Monet"}},
            "claims": {
              "P31": [{"mainsnak": {"datavalue": {"value": {"id": "Q5"}}}}],
              "P569": [{"mainsnak": {"datavalue": {"value": {"time": "+1840-11-14T00:00:00Z"}}}}],
              "P570": [{"mainsnak": {"datavalue": {"value": {"time": "+1926-12-05T00:00:00Z"}}}}],
              "P27": [{"mainsnak": {"datavalue": {"value": {"id": "Q142"}}}}, {"mainsnak": {"snaktype": "novalue"}}],
              "P135": [{"mainsnak": {"datavalue": {"value": {"id": "Q40415"}}}}]
            }
          },
          "Q999": {"id": "Q999", "missing": ""},
          "Q142": {"labels": {"fr": {"value": "France"}}}
        }}
        """;

    private static (WikidataClient Client, Reponses Reponses) Creer()
    {
        var reponses = new Reponses();
        var client = new WikidataClient(new HttpClient(reponses), Options.Create(new EnrichmentOptions { ContactUrl = "https://exemple.test/contact" }));
        return (client, reponses);
    }

    [Fact]
    public async Task LaRecherche_RendLesIdentifiantsDesPremiersResultats()
    {
        var (client, reponses) = Creer();
        reponses.Repondre = _ => (HttpStatusCode.OK, """{"search": [{"id": "Q296"}, {"id": "Q2"}, {"label": "sans id"}]}""");

        var ids = await client.SearchAsync("Claude Monet & fils");

        Assert.Equal(new[] { "Q296", "Q2" }, ids);
        var url = reponses.Requetes.Single().RequestUri!.AbsoluteUri;
        Assert.Contains("action=wbsearchentities", url);
        Assert.Contains("search=Claude%20Monet%20%26%20fils", url);
        Assert.Contains("language=fr", url);
    }

    [Fact]
    public async Task UneRechercheSansResultat_RendUneListeVide()
    {
        var (client, reponses) = Creer();
        reponses.Repondre = _ => (HttpStatusCode.OK, "{}");

        Assert.Empty(await client.SearchAsync("Personne"));
    }

    [Fact]
    public async Task LesEntites_SontLuesAvecNomAliasAnneesArticleNationaliteEtCourant()
    {
        var (client, reponses) = Creer();
        reponses.Repondre = _ => (HttpStatusCode.OK, Monet);

        var entites = await client.GetEntitiesAsync(new[] { "Q296", "Q999", "Q142", "Q296" });

        Assert.Equal(new[] { "Q142", "Q296" }, entites.Keys.OrderBy(k => k));
        var monet = entites["Q296"];
        Assert.Equal("Claude Monet", monet.Label);
        Assert.Equal(new[] { "Oscar-Claude Monet" }, monet.Aliases);
        Assert.True(monet.IsHuman);
        Assert.Equal(new[] { 1840 }, monet.BirthYears);
        Assert.Equal(new[] { 1926 }, monet.DeathYears);
        Assert.Equal("Claude Monet", monet.FrenchWikipediaTitle);
        Assert.Equal(new[] { "Q142" }, monet.CitizenshipIds);
        Assert.Equal(new[] { "Q40415" }, monet.MovementIds);
        Assert.Equal("France", entites["Q142"].Label);
        Assert.False(entites["Q142"].IsHuman);
        Assert.Null(entites["Q142"].FrenchWikipediaTitle);
        Assert.Single(reponses.Requetes);
        Assert.Contains("ids=Q296%7CQ999%7CQ142", reponses.Requetes[0].RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task PlusDeCinquanteEntites_SontDemandeesParLotsDeCinquante()
    {
        var (client, reponses) = Creer();
        reponses.Repondre = _ => (HttpStatusCode.OK, """{"entities": {}}""");

        await client.GetEntitiesAsync(Enumerable.Range(1, 120).Select(i => $"Q{i}").ToList());

        Assert.Equal(3, reponses.Requetes.Count);
    }

    [Fact]
    public async Task UneReponseSansEntites_RendUnResultatVide()
    {
        var (client, reponses) = Creer();
        reponses.Repondre = _ => (HttpStatusCode.OK, "{}");

        Assert.Empty(await client.GetEntitiesAsync(new[] { "Q1" }));
    }

    [Fact]
    public async Task LeResume_EstLExtraitDeLArticleFrancais()
    {
        var (client, reponses) = Creer();
        reponses.Repondre = _ => (HttpStatusCode.OK, """{"extract": "  Claude Monet est un peintre français.\n"}""");

        var resume = await client.GetSummaryAsync("Claude Monet");

        Assert.Equal("Claude Monet est un peintre français.", resume);
        Assert.Equal("https://fr.wikipedia.org/api/rest_v1/page/summary/Claude_Monet", reponses.Requetes.Single().RequestUri!.AbsoluteUri);
    }

    [Theory]
    [InlineData("""{"extract": "  "}""")]
    [InlineData("{}")]
    public async Task UnArticleSansExtrait_NaPasDeResume(string corps)
    {
        var (client, reponses) = Creer();
        reponses.Repondre = _ => (HttpStatusCode.OK, corps);

        Assert.Null(await client.GetSummaryAsync("Claude Monet"));
    }

    [Fact]
    public async Task UnArticleAvecDesCaracteresSpeciaux_EstDemandeEncode()
    {
        var (client, reponses) = Creer();

        await client.GetSummaryAsync("Renoir (peintre)");

        Assert.EndsWith("/Renoir_%28peintre%29", reponses.Requetes.Single().RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task UneErreurHttp_EstSignaleeParUneException()
    {
        var (client, reponses) = Creer();
        reponses.Repondre = _ => (HttpStatusCode.TooManyRequests, "");

        await Assert.ThrowsAsync<HttpRequestException>(() => client.SearchAsync("Claude Monet"));
    }

    [Fact]
    public async Task LeClientSePresenteAvecUnContact()
    {
        var (client, reponses) = Creer();

        await client.SearchAsync("Claude Monet");

        var agent = reponses.Requetes.Single().Headers.UserAgent.ToString();
        Assert.StartsWith("openjoconde/1.0", agent);
        Assert.Contains("https://exemple.test/contact", agent);
    }
}

public class EnrichmentOptionsTests
{
    private static ValidateOptionsResult Valider(EnrichmentOptions options) => new EnrichmentOptionsValidator().Validate(null, options);

    [Fact]
    public void LesReglagesParDefaut_SontValidesEtLEnrichissementEstDesactive()
    {
        var options = new EnrichmentOptions();

        Assert.False(options.Enabled);
        Assert.True(Valider(options).Succeeded);
    }

    [Theory]
    [InlineData(nameof(EnrichmentOptions.BatchSize), 0)]
    [InlineData(nameof(EnrichmentOptions.BatchSize), 5000)]
    [InlineData(nameof(EnrichmentOptions.MinimumArtworks), 0)]
    [InlineData(nameof(EnrichmentOptions.PauseBetweenArtistsMilliseconds), -1)]
    [InlineData(nameof(EnrichmentOptions.PauseBetweenBatchesSeconds), -5)]
    [InlineData(nameof(EnrichmentOptions.IdleHours), 0)]
    [InlineData(nameof(EnrichmentOptions.StartupDelaySeconds), -1)]
    public void UneValeurAbsurde_EstRefuseeEtNommee(string reglage, int valeur)
    {
        var options = new EnrichmentOptions();
        typeof(EnrichmentOptions).GetProperty(reglage)!.SetValue(options, valeur);

        Assert.Contains($"Enrichment:{reglage}", Valider(options).FailureMessage);
    }

    [Fact]
    public void LeContactEstObligatoire() =>
        Assert.Contains("Enrichment:ContactUrl", Valider(new EnrichmentOptions { ContactUrl = " " }).FailureMessage);
}

/// <summary>La tâche de fond enchaîne les lots, et sait attendre après un lot vide, une panne ou une interruption.</summary>
public class EnrichissementEnFondTests
{
    private sealed class ServiceDeLots : IArtistEnrichmentService
    {
        private int _appels;
        public int Appels => _appels;
        public Func<int, ArtistEnrichmentReport> Lot { get; init; } = _ => new ArtistEnrichmentReport(0, 0, 0, false);

        public Task<ArtistEnrichmentReport> EnrichBatchAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Lot(Interlocked.Increment(ref _appels)));
    }

    private static async Task<int> LancerAsync(ServiceDeLots lots, int minimumAppels, int pauseEntreLotsSecondes = 0)
    {
        var services = new ServiceCollection().AddSingleton<IArtistEnrichmentService>(lots).BuildServiceProvider();
        var options = Options.Create(new EnrichmentOptions { StartupDelaySeconds = 0, PauseBetweenBatchesSeconds = pauseEntreLotsSecondes, IdleHours = 1 });
        using var tache = new ArtistEnrichmentBackgroundService(services.GetRequiredService<IServiceScopeFactory>(), options,
            NullLogger<ArtistEnrichmentBackgroundService>.Instance);

        await tache.StartAsync(CancellationToken.None);
        var delai = DateTime.UtcNow.AddSeconds(10);
        while (lots.Appels < minimumAppels && DateTime.UtcNow < delai)
            await Task.Delay(10);
        await Task.Delay(100); // laisse le temps d'un éventuel lot de trop
        await tache.StopAsync(CancellationToken.None);
        return lots.Appels;
    }

    [Fact]
    public async Task TantQuIlResteDesArtistes_LesLotsSEnchainent() =>
        Assert.True(await LancerAsync(new ServiceDeLots { Lot = _ => new ArtistEnrichmentReport(10, 4, 0, false) }, minimumAppels: 3) >= 3);

    [Fact]
    public async Task UnLotVide_FaitAttendreLongtemps() =>
        Assert.Equal(1, await LancerAsync(new ServiceDeLots(), minimumAppels: 1));

    [Fact]
    public async Task UnLotInterrompuParDesPannes_FaitAttendreLongtemps() =>
        Assert.Equal(1, await LancerAsync(new ServiceDeLots { Lot = _ => new ArtistEnrichmentReport(2, 0, 5, true) }, minimumAppels: 1));

    [Fact]
    public async Task UneErreurInattendue_NArretePasLaTache_ElleReessaiePlusTard() =>
        Assert.Equal(1, await LancerAsync(new ServiceDeLots { Lot = _ => throw new InvalidOperationException("base indisponible") }, minimumAppels: 1));

    [Fact]
    public async Task L_ArretDeLApplication_InterrompTrancheLAttente()
    {
        var lots = new ServiceDeLots { Lot = _ => new ArtistEnrichmentReport(10, 4, 0, false) };

        // Une pause de 100 s entre deux lots : l'arrêt doit la couper net
        var appels = await LancerAsync(lots, minimumAppels: 1, pauseEntreLotsSecondes: 100);

        Assert.Equal(1, appels);
    }
}

/// <summary>L'API réelle ne lance l'enrichissement que s'il est activé, et il enrichit la base.</summary>
public class EnrichissementIntegrationTests
{
    private static async Task SemerAsync(ApiFactory factory)
    {
        var fichier = GeneratedCsv.Create(Enumerable.Range(1, 3), i => $"Titre {i}", author: _ => "MONET Claude");
        try
        {
            await using var context = factory.Database.CreateContext();
            Assert.True((await factory.Database.CreateImportService(context).ImportFromFileAsync(fichier)).Success);
            (await context.Artists.SingleAsync()).BirthDate = "1840";
            await context.SaveChangesAsync();
        }
        finally
        {
            File.Delete(fichier);
        }
    }

    private static FakeWikidataClient Wikidata()
    {
        var wikidata = new FakeWikidataClient();
        wikidata.Searches["Claude MONET"] = new[] { "Q296" };
        wikidata.Entities["Q296"] = FakeWikidataClient.Person("Q296", "Claude Monet", 1840, null, "Claude Monet");
        wikidata.Summaries["Claude Monet"] = "Claude Monet est un peintre français.";
        return wikidata;
    }

    private static Action<IServiceCollection> Remplacer(IWikidataClient client) =>
        services => services.Replace(ServiceDescriptor.Singleton(client));

    [Fact]
    public async Task Active_LApiEnrichitLesArtistesEnArrierePlan()
    {
        await using var factory = new ApiFactory { Overrides = Remplacer(Wikidata()) };
        factory.Settings["Enrichment:Enabled"] = "true";
        factory.Settings["Enrichment:StartupDelaySeconds"] = "0";
        factory.Settings["Enrichment:PauseBetweenArtistsMilliseconds"] = "0";
        await factory.InitializeAsync();
        await SemerAsync(factory);

        using var client = factory.CreateClient(); // démarre l'hôte, donc la tâche de fond

        string? biographie = null;
        for (var i = 0; i < 200 && biographie == null; i++)
        {
            await using var context = factory.Database.CreateContext();
            biographie = (await context.Artists.AsNoTracking().SingleAsync()).Biography;
            if (biographie == null)
                await Task.Delay(50);
        }
        Assert.Equal("Claude Monet est un peintre français.", biographie);
    }

    [Fact]
    public async Task ParDefaut_LApiNAppelleAucunServiceExterne()
    {
        var wikidata = Wikidata();
        await using var factory = new ApiFactory { Overrides = Remplacer(wikidata) };
        factory.Settings["Enrichment:StartupDelaySeconds"] = "0";
        await factory.InitializeAsync();
        await SemerAsync(factory);

        using var client = factory.CreateClient();
        await Task.Delay(500);

        Assert.Empty(wikidata.SearchedNames);
        await using var context = factory.Database.CreateContext();
        Assert.Null((await context.Artists.AsNoTracking().SingleAsync()).Biography);
    }
}
