using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenJoconde.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArtworksController : ControllerBase
    {
        private readonly ILogger<ArtworksController> _logger;
        private readonly IArtworkRepository _artworkRepository;

        public ArtworksController(
            ILogger<ArtworksController> logger,
            IArtworkRepository artworkRepository)
        {
            _logger = logger;
            _artworkRepository = artworkRepository;
        }

        /// <summary>
        /// Get all artworks with pagination
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 10)</param>
        /// <returns>List of artworks</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Artwork>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var artworks = await _artworkRepository.GetAllAsync(page, pageSize);
                var totalCount = await _artworkRepository.GetCountAsync();

                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                return Ok(artworks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting artworks");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Get artwork by ID
        /// </summary>
        /// <param name="id">Artwork ID</param>
        /// <returns>Artwork if found</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Artwork>> GetById(Guid id)
        {
            try
            {
                var artwork = await _artworkRepository.GetByIdAsync(id);
                if (artwork == null)
                {
                    return NotFound();
                }

                return Ok(artwork);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting artwork with ID {Id}", id);
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        /// <summary>
        /// Search artworks based on criteria
        /// </summary>
        /// <param name="searchText">Text to search in title, description, etc.</param>
        /// <param name="artistId">Filter by artist ID</param>
        /// <param name="domainId">Filter by domain ID</param>
        /// <param name="techniqueId">Filter by technique ID</param>
        /// <param name="periodId">Filter by period ID</param>
        /// <param name="museumId">Filter by museum ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 10)</param>
        /// <returns>List of artworks matching criteria</returns>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Artwork>>> Search(
            [FromQuery] string searchText = null,
            [FromQuery] Guid? artistId = null,
            [FromQuery] Guid? domainId = null,
            [FromQuery] Guid? techniqueId = null,
            [FromQuery] Guid? periodId = null,
            [FromQuery] Guid? museumId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var artworks = await _artworkRepository.SearchAsync(
                    searchText, artistId, domainId, techniqueId, periodId, museumId, page, pageSize);

                return Ok(artworks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching artworks");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
    }
}
