using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Dtos;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenJoconde.API.Controllers
{
    /// <summary>
    /// Recherche et consultation des œuvres
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ArtworksController : ControllerBase
    {
        private readonly IArtworkRepository _artworkRepository;
        private readonly ILogger<ArtworksController> _logger;

        /// <summary>Crée le contrôleur avec ses dépendances</summary>
        public ArtworksController(IArtworkRepository artworkRepository, ILogger<ArtworksController> logger)
        {
            _artworkRepository = artworkRepository ?? throw new ArgumentNullException(nameof(artworkRepository));
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
        /// <param name="region">Ne garder que les œuvres des musées de cette région (nom exact, voir /api/museums/geography)</param>
        /// <param name="department">Ne garder que les œuvres des musées de ce département (nom exact)</param>
        /// <param name="latitude">Latitude de l'utilisateur, en degrés, pour le tri par distance</param>
        /// <param name="longitude">Longitude de l'utilisateur, en degrés, pour le tri par distance</param>
        /// <param name="sortBy">
        /// Tri : title (A-Z, par défaut), title_desc (Z-A), date (plus ancien d'abord), date_desc, relevance (pertinence,
        /// avec une recherche) ou artist (nom du premier artiste, avec une recherche ou un filtre) ou distance (musée le plus proche d'abord, avec latitude et longitude) ; les œuvres sans
        /// titre, sans date lisible ou sans artiste viennent en dernier
        /// </param>
        /// <returns>Liste paginée d'oeuvres d'art</returns>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<ArtworkListItemDto>>> GetArtworks(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] Guid? museumId = null,
            [FromQuery] Guid? artistId = null,
            [FromQuery] Guid? domainId = null,
            [FromQuery] Guid? techniqueId = null,
            [FromQuery] Guid? periodId = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? region = null,
            [FromQuery] string? department = null,
            [FromQuery] double? latitude = null,
            [FromQuery] double? longitude = null)
        {
            try
            {
                _logger.LogInformation("Récupération des oeuvres d'art - Page: {Page}, PageSize: {PageSize}, Search: {Search}",
                    page, pageSize, search ?? "null");

                var result = await _artworkRepository.SearchAsync(new ArtworkSearchCriteria
                {
                    Page = page,
                    PageSize = pageSize,
                    Search = search,
                    MuseumId = museumId,
                    ArtistId = artistId,
                    DomainId = domainId,
                    TechniqueId = techniqueId,
                    PeriodId = periodId,
                    SortBy = sortBy,
                    Region = region,
                    Department = department,
                    Latitude = latitude,
                    Longitude = longitude
                });

                return Ok(result);
            }
            catch (InvalidArtworkSearchException ex)
            {
                return BadRequest(ex.Message);
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

                var artwork = await _artworkRepository.GetDetailAsync(id);

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
