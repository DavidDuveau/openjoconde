using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;
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
        /// <param name="search">Terme de recherche (titre, référence, etc.)</param>
        /// <returns>Liste paginée d'oeuvres d'art</returns>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<Artwork>>> GetArtworks(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            try
            {
                _logger.LogInformation("Récupération des oeuvres d'art - Page: {Page}, PageSize: {PageSize}, Search: {Search}",
                    page, pageSize, search ?? "null");

                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                // Query de base
                IQueryable<Artwork> query = _context.Artworks
                    .Where(a => !a.IsDeleted)
                    .OrderBy(a => a.Title);

                // Appliquer la recherche si nécessaire
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim().ToLower();
                    query = query.Where(a =>
                        a.Title.ToLower().Contains(search) ||
                        a.Reference.ToLower().Contains(search) ||
                        a.InventoryNumber.ToLower().Contains(search) ||
                        a.Description.ToLower().Contains(search));
                }

                // Calculer le nombre total d'éléments
                var totalItems = await query.CountAsync();

                // Appliquer la pagination
                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Construire le résultat paginé
                var result = new PaginatedResult<Artwork>
                {
                    Items = items,
                    TotalItems = totalItems,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
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
        /// <returns>L'oeuvre d'art</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Artwork>> GetArtwork(Guid id)
        {
            try
            {
                _logger.LogInformation("Récupération de l'oeuvre d'art avec l'identifiant {Id}", id);

                var artwork = await _context.Artworks
                    .Where(a => a.Id == id && !a.IsDeleted)
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

    /// <summary>
    /// Résultat paginé pour les listes d'éléments
    /// </summary>
    /// <typeparam name="T">Type d'élément</typeparam>
    public class PaginatedResult<T>
    {
        /// <summary>
        /// Éléments de la page courante
        /// </summary>
        public required IEnumerable<T> Items { get; set; }

        /// <summary>
        /// Nombre total d'éléments (toutes pages confondues)
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Numéro de la page courante
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Nombre d'éléments par page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Nombre total de pages
        /// </summary>
        public int TotalPages { get; set; }
    }
}
