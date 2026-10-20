using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenJoconde.API.Controllers;
using OpenJoconde.API.Dtos;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// GET api/artworks/{id} rend la fiche complète : musée, artistes, domaines, techniques, périodes (TECH-31).
/// </summary>
public class FicheOeuvreTests : IAsyncLifetime
{
    private readonly ImportDatabase _db = new();

    public Task InitializeAsync() => _db.InitializeAsync();
    public Task DisposeAsync() => _db.DisposeAsync();

    private async Task<ArtworksController> ControleurApresImportAsync()
    {
        // L'extrait réel porte musées, domaines, techniques et périodes ; le fichier de cas particuliers, des auteurs
        foreach (var fixture in new[] { "joconde_extrait_reel.csv", "joconde_cas_particuliers.csv" })
        {
            await using var import = _db.CreateContext();
            var report = await _db.CreateImportService(import).ImportFromFileAsync(ImportDatabase.FixturePath(fixture));
            // Le fichier de cas particuliers contient une ligne tronquée, rejetée exprès : seul compte ce qui est écrit
            Assert.True(report.ImportedArtworks > 0, report.ErrorMessage);
        }

        return new ArtworksController(_db.CreateContext(), NullLogger<ArtworksController>.Instance);
    }

    /// <summary>La fiche de l'œuvre la mieux reliée parmi celles qui vérifient le filtre, comparée à ses relations en base.</summary>
    private async Task<ArtworkDetailDto> VerifierFicheAsync(System.Linq.Expressions.Expression<Func<OpenJoconde.Core.Models.Artwork, bool>> filtre)
    {
        var controleur = await ControleurApresImportAsync();
        await using var lecture = _db.CreateContext();
        var id = await lecture.Artworks.Where(filtre)
            .OrderByDescending(a => a.Artists.Count + a.Domains.Count + a.Techniques.Count + a.Periods.Count)
            .Select(a => a.Id).FirstAsync();
        var attendu = await lecture.Artworks.Include(a => a.Museum).Include(a => a.Artists).ThenInclude(aa => aa.Artist)
            .Include(a => a.Domains).Include(a => a.Techniques).Include(a => a.Periods).SingleAsync(a => a.Id == id);

        var reponse = await controleur.GetArtwork(id);

        var fiche = Assert.IsType<ArtworkDetailDto>(Assert.IsType<OkObjectResult>(reponse.Result).Value);
        Assert.Equal(attendu.Reference, fiche.Reference);
        Assert.Equal(attendu.Title, fiche.Title);
        Assert.Equal(attendu.Museum?.Name, fiche.Museum?.Name);
        Assert.Equal(attendu.Museum?.Id, fiche.Museum?.Id);
        Assert.Equal(attendu.Artists.Select(a => (a.Artist.LastName, (string?)a.Role)).OrderBy(n => n), fiche.Artists.Select(a => (a.LastName, a.Role)).OrderBy(n => n));
        Assert.Equal(attendu.Domains.Select(d => d.Name).OrderBy(n => n), fiche.Domains.Select(d => d.Name).OrderBy(n => n));
        Assert.Equal(attendu.Techniques.Select(t => t.Name).OrderBy(n => n), fiche.Techniques.Select(t => t.Name).OrderBy(n => n));
        Assert.Equal(attendu.Periods.Select(p => p.Name).OrderBy(n => n), fiche.Periods.Select(p => p.Name).OrderBy(n => n));
        return fiche;
    }

    [Fact]
    public async Task Fiche_RendMuseeDomainesTechniquesEtPeriodes()
    {
        var fiche = await VerifierFicheAsync(a => a.MuseumId != null && a.Techniques.Any() && a.Periods.Any());

        Assert.NotNull(fiche.Museum);
        Assert.NotEmpty(fiche.Domains);
        Assert.NotEmpty(fiche.Techniques);
        Assert.NotEmpty(fiche.Periods);
    }

    [Fact]
    public async Task Fiche_RendLesArtistesEtLeurRole()
    {
        var fiche = await VerifierFicheAsync(a => a.Artists.Any());

        Assert.NotEmpty(fiche.Artists);
        Assert.All(fiche.Artists, a => Assert.False(string.IsNullOrEmpty(a.LastName)));
    }

    [Fact]
    public async Task Fiche_OeuvreInconnue_Rend404()
    {
        var controleur = await ControleurApresImportAsync();

        var reponse = await controleur.GetArtwork(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(reponse.Result);
    }
}
