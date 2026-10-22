using Microsoft.EntityFrameworkCore;
using OpenJoconde.Core.Models;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Lien du site associé, artiste sous droits et année d'entrée dans le domaine public sont lus dans l'export (TECH-33).
/// </summary>
public class DroitsEtLiensTests : IAsyncLifetime
{
    private readonly ImportDatabase _db = new();
    private readonly List<string> _fichiers = new();

    public Task InitializeAsync() => _db.InitializeAsync();

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        _fichiers.ForEach(File.Delete);
    }

    private static IDictionary<string, string> Colonnes(int i) => i switch
    {
        1 => new Dictionary<string, string>
        {
            ["Lien_site_associe"] = "https://webmuseo.com/ws/musee-crozatier/app/collection/record/17872",
            ["Artiste_sous_droits"] = "oui ; oui", // deux auteurs, espace insécable comme dans l'export
            ["Date_entree_dans_le_domaine_public"] = "2046",
            ["Presence_image"] = "oui"
        },
        2 => new Dictionary<string, string>
        {
            ["Lien_site_associe"] = "javascript:alert(1)", // jamais proposé en lien
            ["Artiste_sous_droits"] = "non",
            ["Date_entree_dans_le_domaine_public"] = "2031 ; 2056",
            ["Presence_image"] = "Non"
        },
        3 => new Dictionary<string, string> { ["Lien_site_associe"] = "pas une adresse", ["Date_entree_dans_le_domaine_public"] = "bientôt", ["Presence_image"] = "peut-être" },
        _ => new Dictionary<string, string>()
    };

    private async Task ImporterAsync()
    {
        var fichier = GeneratedCsv.Create(Enumerable.Range(1, 4), i => $"Titre {i}", extra: Colonnes);
        _fichiers.Add(fichier);
        await using var context = _db.CreateContext();
        var report = await _db.CreateImportService(context).ImportFromFileAsync(fichier);
        Assert.True(report.Success, report.ErrorMessage);
    }

    private async Task<Artwork> OeuvreAsync(string reference)
    {
        await using var context = _db.CreateContext();
        return await context.Artworks.AsNoTracking().SingleAsync(a => a.Reference == reference);
    }

    [Fact]
    public async Task LesTroisChamps_SontEcritsEnBase()
    {
        await ImporterAsync();

        var premiere = await OeuvreAsync("L000001");
        Assert.Equal("https://webmuseo.com/ws/musee-crozatier/app/collection/record/17872", premiere.AssociatedSiteUrl);
        Assert.True(premiere.ArtistUnderCopyright);
        Assert.Equal(2046, premiere.PublicDomainYear);
    }

    [Fact]
    public async Task AdresseNonWeb_EstEcartee_EtLAnneeLaPlusTardiveEstRetenue()
    {
        await ImporterAsync();

        var deuxieme = await OeuvreAsync("L000002");
        Assert.Null(deuxieme.AssociatedSiteUrl);
        Assert.False(deuxieme.ArtistUnderCopyright);
        Assert.Equal(2056, deuxieme.PublicDomainYear);

        var troisieme = await OeuvreAsync("L000003");
        Assert.Null(troisieme.AssociatedSiteUrl);
        Assert.Null(troisieme.PublicDomainYear);
    }

    [Fact]
    public async Task NoticeSansCesChamps_LesLaisseVides()
    {
        await ImporterAsync();

        var quatrieme = await OeuvreAsync("L000004");
        Assert.Null(quatrieme.AssociatedSiteUrl);
        Assert.Null(quatrieme.ArtistUnderCopyright);
        Assert.Null(quatrieme.PublicDomainYear);
    }

    [Fact]
    public async Task PresenceImage_EstLue_OuiNonEtLeResteInconnu()
    {
        await ImporterAsync();

        Assert.True((await OeuvreAsync("L000001")).HasImage);
        Assert.False((await OeuvreAsync("L000002")).HasImage);
        Assert.Null((await OeuvreAsync("L000003")).HasImage);
        Assert.Null((await OeuvreAsync("L000004")).HasImage);
    }

    [Fact]
    public async Task PresenceImage_EstDansLaListeEtLaFiche()
    {
        await ImporterAsync();
        var id = (await OeuvreAsync("L000001")).Id;
        var controleur = _db.CreateArtworksController();

        var fiche = Assert.IsType<OpenJoconde.Core.Dtos.ArtworkDetailDto>(Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>((await controleur.GetArtwork(id)).Result).Value);
        var liste = Assert.IsType<PaginatedResult<OpenJoconde.Core.Dtos.ArtworkListItemDto>>(Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>((await controleur.GetArtworks(search: "L000001")).Result).Value);

        Assert.True(fiche.HasImage);
        Assert.True(liste.Items.Single().HasImage);
    }
}
