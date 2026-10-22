using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenJoconde.API.Dtos;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;
using OpenJoconde.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenJoconde.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArtworksController : ControllerBase
    {
        private readonly OpenJocondeDbContext _context;
        private readonly ILogger<ArtworksController> _logger;

        public ArtworksController(OpenJocondeDbContext context, ILogger<ArtworksController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Récupère une liste paginée d'oeuvres d'art
        /// </summary>
        /// <param name="page">Numéro de page (commence à 1)</param>
        /// <param name="pageSize">Nombre d'éléments par page</param>
        /// <param name="search">Terme de recherche : mots du titre ou de la description (début de mot accepté), début de référence ou de numéro d'inventaire</param>
        /// <param name="museumId">Ne garder que les œuvres de ce musée</param>
        /// <param name="artistId">Ne garder que les œuvres de cet artiste</param>
        /// <param name="domainId">Ne garder que les œuvres de ce domaine</param>
        /// <param name="techniqueId">Ne garder que les œuvres réalisées avec cette technique</param>
        /// <param name="periodId">Ne garder que les œuvres de cette période</param>
        /// <param name="sortBy">Tri : title (A-Z, par défaut) ou title_desc (Z-A) ; les œuvres sans titre viennent en dernier</param>
        /// <returns>Liste paginée d'oeuvres d'art</returns>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<Artwork>>> GetArtworks(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] Guid? museumId = null,
            [FromQuery] Guid? artistId = null,
            [FromQuery] Guid? domainId = null,
            [FromQuery] Guid? techniqueId = null,
            [FromQuery] Guid? periodId = null,
            [FromQuery] string? sortBy = null)
        {
            if (sortBy != null && sortBy != "title" && sortBy != "title_desc")
                return BadRequest("Tri inconnu : utiliser title ou title_desc.");

            try
            {
                _logger.LogInformation("Récupération des oeuvres d'art - Page: {Page}, PageSize: {PageSize}, Search: {Search}",
                    page, pageSize, search ?? "null");

                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                // Query de base
                IQueryable<Artwork> query = _context.Artworks;

                query = ArtworkFilter.Apply(query, museumId, artistId, domainId, techniqueId, periodId);
                var filtered = museumId.HasValue || artistId.HasValue || domainId.HasValue || techniqueId.HasValue || periodId.HasValue;

                // Appliquer la recherche si nécessaire
                var searching = !string.IsNullOrWhiteSpace(search);
                if (searching)
                {
                    query = ArtworkSearch.Apply(query, search!);
                }

                IQueryable<Artwork> ordered = ArtworkOrdering.ByTitle(query, searching || filtered, sortBy == "title_desc");

                // Calculer le nombre total d'éléments
                var totalItems = await ordered.CountAsync();

                // Appliquer la pagination
                var items = await ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Construire le résultat paginé
                var result = new PaginatedResult<Artwork>
                {
                    Items = items,
                    TotalItems = totalItems,
                    Page = page,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des oeuvres d'art: {Message}", ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des oeuvres d'art.");
            }
        }

        /// <summary>
        /// Récupère une oeuvre d'art par son identifiant
        /// </summary>
        /// <param name="id">Identifiant de l'oeuvre d'art</param>
        /// <returns>La fiche de l'oeuvre d'art, avec son musée, ses artistes, domaines, techniques et périodes</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ArtworkDetailDto>> GetArtwork(Guid id)
        {
            try
            {
                _logger.LogInformation("Récupération de l'oeuvre d'art avec l'identifiant {Id}", id);

                var artwork = await _context.Artworks
                    .Where(a => a.Id == id)
                    .AsNoTracking()
                    .Select(a => new ArtworkDetailDto
                    {
                        Id = a.Id,
                        Reference = a.Reference,
                        InventoryNumber = a.InventoryNumber,
                        Denomination = a.Denomination,
                        Title = a.Title,
                        Description = a.Description,
                        Dimensions = a.Dimensions,
                        CreationDate = a.CreationDate,
                        CreationPlace = a.CreationPlace,
                        ConservationPlace = a.ConservationPlace,
                        Copyright = a.Copyright,
                        ImageUrl = a.ImageUrl,
                        AssociatedSiteUrl = a.AssociatedSiteUrl,
                        ArtistUnderCopyright = a.ArtistUnderCopyright,
                        PublicDomainYear = a.PublicDomainYear,
                        Museum = a.Museum == null ? null : new NamedItemDto { Id = a.Museum.Id, Name = a.Museum.Name },
                        Artists = a.Artists
                            .Select(aa => new ArtworkArtistDto
                            {
                                Id = aa.Artist.Id,
                                FirstName = aa.Artist.FirstName,
                                LastName = aa.Artist.LastName,
                                Role = aa.Role
                            })
                            .ToList(),
                        Domains = a.Domains.Select(d => new NamedItemDto { Id = d.Id, Name = d.Name }).ToList(),
                        Techniques = a.Techniques.Select(t => new NamedItemDto { Id = t.Id, Name = t.Name }).ToList(),
                        Periods = a.Periods.Select(p => new NamedItemDto { Id = p.Id, Name = p.Name }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (artwork == null)
                {
                    _logger.LogWarning("Oeuvre d'art avec l'identifiant {Id} non trouvée", id);
                    return NotFound();
                }

                return Ok(artwork);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'oeuvre d'art {Id}: {Message}", id, ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'oeuvre d'art.");
            }
        }
    }
}
