using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Services;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Enrichissement des musées par Muséofile (TECH-36) : adresse, code postal, téléphone, site, histoire, rapprochés par
/// le code du musée. L'extrait est réel ; les cas limites (adresses douteuses) passent par un petit fichier écrit à part.
/// </summary>
public class MuseofileTests : IAsyncLifetime
{
    private static readonly string Extrait = ImportDatabase.FixturePath("museofile_extrait_reel.csv");

    private readonly ImportDatabase _db = new();
    private readonly List<string> _fichiers = new();

    public Task InitializeAsync() => _db.InitializeAsync();

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        _fichiers.ForEach(File.Delete);
    }

    private async Task<MuseofileReport> EnrichirAsync(string fichier)
    {
        await using var context = _db.CreateContext();
        var service = new MuseofileService(context, new HttpClient(), NullLogger<MuseofileService>.Instance);
        return await service.EnrichFromFileAsync(fichier);
    }

    private async Task AjouterAsync(params Museum[] musees)
    {
        await using var context = _db.CreateContext();
        context.Museums.AddRange(musees);
        await context.SaveChangesAsync();
    }

    private async Task<Museum> MuseeAsync(string code)
    {
        await using var context = _db.CreateContext();
        return await context.Museums.AsNoTracking().SingleAsync(m => m.Code == code);
    }

    private static Museum Musee(string code, string? name = null) => new() { Id = Guid.NewGuid(), Name = name ?? $"musée {code}", Code = code };

    /// <summary>Fichier Muséofile minimal : une ligne par couple (code, adresse Internet)</summary>
    private string FichierUrls(params (string Code, string Url)[] lignes)
    {
        var chemin = Path.Combine(Path.GetTempPath(), $"museofile_test_{Guid.NewGuid():N}.csv");
        File.WriteAllLines(chemin, new[] { "Identifiant|URL" }.Concat(lignes.Select(l => $"\"{l.Code}\"|\"{l.Url}\"")));
        _fichiers.Add(chemin);
        return chemin;
    }

    [Fact]
    public async Task Enrichit_AdresseCodePostalTelephoneSiteEtHistoire()
    {
        await AjouterAsync(Musee("M1128"));

        var rapport = await EnrichirAsync(Extrait);

        var musee = await MuseeAsync("M1128");
        Assert.Equal("358 avenue de Champagne", musee.Address);
        Assert.Equal("69009", musee.ZipCode);
        Assert.Equal("04 72 17 54 54", musee.Phone);
        Assert.Equal("https://museepompiers.com/", musee.Website); // Muséofile donne « museepompiers.com/ » sans schéma
        Assert.StartsWith("Le  musée des sapeurs-pompiers de Lyon", musee.Description);
        Assert.Equal(7, rapport.Entries);
        Assert.Equal(1, rapport.Matched);
        Assert.Equal(1, rapport.Updated);
    }

    [Fact]
    public async Task Enrichit_CategorieDomainesThematiquesEtPointsForts()
    {
        await AjouterAsync(Musee("M0003"), Musee("M1128"));

        await EnrichirAsync(Extrait);

        var hanau = await MuseeAsync("M0003");
        Assert.Equal("écomusée ; musée en territoire rural", hanau.Category);
        Assert.Equal("Archéologie;Ethnologie;Histoire;Sciences de la nature", hanau.ThematicDomains);
        Assert.StartsWith("Histoire du pays de Hanau", hanau.Highlights);
        // Sans catégorie dans le fichier, la valeur reste vide
        var pompiers = await MuseeAsync("M1128");
        Assert.Null(pompiers.Category);
        Assert.Equal("Ethnologie;Histoire;Technique et industrie", pompiers.ThematicDomains);
    }

    [Fact]
    public async Task UneCategorieAbsenteDuFichier_NEffacePasCelleDeLaBase()
    {
        var musee = Musee("M1128");
        musee.Category = "musée d'art";
        await AjouterAsync(musee);

        await EnrichirAsync(Extrait);

        Assert.Equal("musée d'art", (await MuseeAsync("M1128")).Category);
    }

    [Fact]
    public async Task LeRapprochementIgnoreLaCasseDuCode()
    {
        await AjouterAsync(Musee("m1128"));

        var rapport = await EnrichirAsync(Extrait);

        Assert.Equal(1, rapport.Matched);
        Assert.Equal("69009", (await MuseeAsync("m1128")).ZipCode);
    }

    [Fact]
    public async Task UneAdresseAvecEspaceEnFin_EstRogneeEtUneAdresseCompleteEstGardee()
    {
        await AjouterAsync(Musee("M0146"), Musee("M0004"));

        await EnrichirAsync(Extrait);

        Assert.Equal("https://saulieu.fr/le-musee-francois-pompon/", (await MuseeAsync("M0146")).Website);
        Assert.Equal("https://haguenau.fr/fr/page/musee-alsacien", (await MuseeAsync("M0004")).Website);
    }

    [Fact]
    public async Task SansAdresseInternetDansLeFichier_LeSiteDeLaBaseResteIntact()
    {
        await AjouterAsync(new Museum { Id = Guid.NewGuid(), Name = "musée sans site", Code = "M0053", Website = "https://ancien.example" });

        await EnrichirAsync(Extrait);

        var musee = await MuseeAsync("M0053");
        Assert.Equal("https://ancien.example", musee.Website);
        Assert.Equal("05 53 51 82 60", musee.Phone); // le reste est bien appliqué
    }

    [Fact]
    public async Task UnTelephoneTropLong_EstCoupeEntreDeuxNumeros()
    {
        await AjouterAsync(Musee("M0077"));

        await EnrichirAsync(Extrait);

        var telephone = (await MuseeAsync("M0077")).Phone!;
        var original = File.ReadAllLines(Extrait).First(l => l.StartsWith("\"M0077\""));
        Assert.True(telephone.Length <= 50);
        Assert.StartsWith("05 58 74 12 91", telephone);
        Assert.Contains(telephone, original);
        Assert.Contains(original[(original.IndexOf(telephone, StringComparison.Ordinal) + telephone.Length)..][0], " /;,");
    }

    [Fact]
    public async Task LocalisationEtCoordonneesDeJoconde_SontGardees_MuseofileNeLesCompleteQueSiEllesManquent()
    {
        await AjouterAsync(
            new Museum { Id = Guid.NewGuid(), Name = "complet", Code = "M1128", City = "Villeurbanne", Department = "Ain", Latitude = 1.5, Longitude = 2.5 },
            new Museum { Id = Guid.NewGuid(), Name = "vide", Code = "M0003" });

        await EnrichirAsync(Extrait);

        var complet = await MuseeAsync("M1128");
        Assert.Equal("Villeurbanne", complet.City);
        Assert.Equal("Ain", complet.Department);
        Assert.Equal(1.5, complet.Latitude);
        Assert.Equal(2.5, complet.Longitude);
        var vide = await MuseeAsync("M0003");
        Assert.Equal("Bas-Rhin", vide.Department);
        Assert.Null(vide.City); // la ville sert de clé à l'import Joconde : Muséofile n'y touche jamais
        Assert.Equal(48.824977, vide.Latitude);
        Assert.Equal(7.482915, vide.Longitude);
    }

    [Fact]
    public async Task UnSecondPassage_NeChangeRien()
    {
        await AjouterAsync(Musee("M1128"), Musee("M0003"));
        await EnrichirAsync(Extrait);

        var rapport = await EnrichirAsync(Extrait);

        Assert.Equal(2, rapport.Matched);
        Assert.Equal(0, rapport.Updated);
    }

    [Fact]
    public async Task UnMuseeSansCorrespondance_RestePointe_SansEtreModifie()
    {
        await AjouterAsync(Musee("X0000", "Roubaix ; archives"), new Museum { Id = Guid.NewGuid(), Name = "sans code" });

        var rapport = await EnrichirAsync(Extrait);

        Assert.Equal(0, rapport.Matched);
        Assert.Equal(2, rapport.Unmatched);
        Assert.Null((await MuseeAsync("X0000")).Website);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://musee.example/fichier")]
    [InlineData("www.musee.fr et www.autre.fr")]
    [InlineData("")]
    public async Task UneAdresseInternetNonHttp_OuDouteuse_EstIgnoree(string url)
    {
        await AjouterAsync(Musee("M7777"));

        await EnrichirAsync(FichierUrls(("M7777", url)));

        Assert.Null((await MuseeAsync("M7777")).Website);
    }

    [Fact]
    public async Task UneAdresseInternetAvecHttp_EstGardeeTelleQuelle()
    {
        await AjouterAsync(Musee("M7777"));

        await EnrichirAsync(FichierUrls(("M7777", "http://musee.example/accueil")));

        Assert.Equal("http://musee.example/accueil", (await MuseeAsync("M7777")).Website);
    }

    [Fact]
    public async Task UnFichierSansColonneIdentifiant_EstRefuse()
    {
        var chemin = Path.Combine(Path.GetTempPath(), $"museofile_test_{Guid.NewGuid():N}.csv");
        File.WriteAllText(chemin, "Nom|URL\n\"x\"|\"y\"\n");
        _fichiers.Add(chemin);

        await Assert.ThrowsAsync<InvalidDataException>(() => EnrichirAsync(chemin));
    }

    [Fact]
    public async Task LImportJoconde_NEffacePasCeQueMuseofileApporte()
    {
        var fichier = GeneratedCsv.Create(
            Enumerable.Range(1, 6), i => $"Titre {i}",
            extra: i => new Dictionary<string, string> { ["Code_Museofile"] = "M1128" });
        _fichiers.Add(fichier);
        await using (var context = _db.CreateContext())
            Assert.True((await _db.CreateImportService(context).ImportFromFileAsync(fichier)).Success);
        await EnrichirAsync(Extrait);

        // Le même fichier arrive à nouveau (synchronisation complète) : ses musées sont réécrits
        await using (var context = _db.CreateContext())
            Assert.True((await _db.CreateImportService(context).ImportFromFileAsync(fichier)).Success);

        var musees = await _db.CreateContext().Museums.AsNoTracking().Where(m => m.Code == "M1128").ToListAsync();
        Assert.NotEmpty(musees);
        Assert.All(musees, m =>
        {
            Assert.Equal("358 avenue de Champagne", m.Address);
            Assert.Equal("https://museepompiers.com/", m.Website);
            Assert.Equal("04 72 17 54 54", m.Phone);
        });
    }
}
