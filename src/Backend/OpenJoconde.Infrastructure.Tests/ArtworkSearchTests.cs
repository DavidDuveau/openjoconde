using Microsoft.EntityFrameworkCore;
using OpenJoconde.Infrastructure.Services;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Recherche d'œuvres par titre, description, référence et numéro d'inventaire (TECH-16), sur l'index plein texte
/// de PostgreSQL.
/// </summary>
public class ArtworkSearchTests : IAsyncLifetime
{
    private static readonly string[] Titres =
    {
        "Nocturne sur la Seine", "Paysage de neige", "Étude de femme", "Portrait de la reine", "Nature morte aux pommes"
    };

    private readonly ImportDatabase _db = new();
    private string? _fichier;

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();
        _fichier = GeneratedCsv.Create(Enumerable.Range(1, Titres.Length), i => Titres[i - 1]);
        await using var context = _db.CreateContext();
        var report = await _db.CreateImportService(context).ImportFromFileAsync(_fichier);
        Assert.True(report.Success, report.ErrorMessage);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        if (_fichier != null)
            File.Delete(_fichier);
    }

    private async Task<List<string>> ChercherAsync(string terme)
    {
        await using var context = _db.CreateContext();
        return await ArtworkSearch.Apply(context.Artworks.AsNoTracking(), terme)
            .OrderBy(a => a.Reference).Select(a => a.Title!).ToListAsync();
    }

    [Theory]
    [InlineData("nocturne", "Nocturne sur la Seine")]
    [InlineData("PAYSAGE", "Paysage de neige")]
    [InlineData("neige", "Paysage de neige")]
    [InlineData("pommes", "Nature morte aux pommes")]
    [InlineData("pomme", "Nature morte aux pommes")] // racine : singulier et pluriel se rejoignent
    public async Task TermeDuTitre_TrouveLOeuvre_SansTenirCompteDeLaCasse(string terme, string attendu) =>
        Assert.Equal(new[] { attendu }, await ChercherAsync(terme));

    [Fact]
    public async Task Description_EstCherchee()
    {
        // GeneratedCsv écrit « description {i} » : chaque notice a donc le mot « description »
        Assert.Equal(Titres.Length, (await ChercherAsync("description")).Count);
    }

    [Fact]
    public async Task Reference_EstCherchee_ParDebut_SansTenirCompteDeLaCasse()
    {
        Assert.Equal(new[] { "Paysage de neige" }, await ChercherAsync("L000002"));
        Assert.Equal(new[] { "Paysage de neige" }, await ChercherAsync("l000002"));
        Assert.Equal(Titres.Length, (await ChercherAsync("L00000")).Count);
    }

    [Fact]
    public async Task TermeAbsent_NeTrouveRien() =>
        Assert.Empty(await ChercherAsync("introuvable"));

    [Fact]
    public async Task PleinTexte_IgnoreLesAccents_EtAccepteUnDebutDeMot()
    {
        Assert.Equal(new[] { "Étude de femme" }, await ChercherAsync("etude"));
        Assert.Equal(new[] { "Portrait de la reine" }, await ChercherAsync("port")); // début de mot
        Assert.Empty(await ChercherAsync("ortrait")); // milieu de mot : non trouvé par l'index
        Assert.Equal(new[] { "Nocturne sur la Seine" }, await ChercherAsync("nocturne seine")); // tous les mots requis
        Assert.Empty(await ChercherAsync("nocturne neige")); // jamais ensemble
    }

    [Theory]
    [InlineData("\"; DROP TABLE artwork; --")]
    [InlineData("neige AND")]
    [InlineData("neige | & !")]
    [InlineData("\"")]
    [InlineData("*")]
    [InlineData("100% (a_b)")]
    [InlineData("'")]
    [InlineData("a\\b")]
    public async Task CaracteresSpeciaux_NeFontPasEchouerLaRecherche(string terme)
    {
        await ChercherAsync(terme); // aucune exception, quelle que soit la syntaxe saisie
        await using var context = _db.CreateContext();
        Assert.Equal(Titres.Length, await context.Artworks.CountAsync());
    }

    [Theory]
    [InlineData("paysage neige", "paysage:* & neige:*")]
    [InlineData("  La Joconde  ", "La:* & Joconde:*")]
    [InlineData("\"; DROP--", "DROP:*")]
    [InlineData("étude", "étude:*")]
    public void Condition_EstConstruiteMotParMot_SansSyntaxeDeLUtilisateur(string terme, string attendue) =>
        Assert.Equal(attendue, ArtworkSearch.BuildCondition(terme));

    [Theory]
    [InlineData("")]
    [InlineData("***")]
    [InlineData("\" ' - ;")]
    public void Condition_SansMot_EstNulle(string terme) =>
        Assert.Null(ArtworkSearch.BuildCondition(terme));
}
