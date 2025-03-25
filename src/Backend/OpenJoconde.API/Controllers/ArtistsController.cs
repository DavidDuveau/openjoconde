using Microsoft.AspNetCore.Http;
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
    [Produces("application/json")]
    public class ArtistsController : ControllerBase
    {
        private readonly IArtistRepository _artistRepository;
        private readonly ILogger<ArtistsController> _logger;

        public ArtistsController(
            IArtistRepository artistRepository,
            ILogger<ArtistsController> logger)
        {
            _artistRepository = artistRepository ?? throw new ArgumentNullException(nameof(artistRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Recherche des artistes par nom
        /// </summary>
        /// <param name="name">Nom ou partie du nom de l'artiste à rechercher</param>
        /// <param name="page">Numéro de page (commence à 1)</param>
        /// <param name="pageSize">Nombre d'éléments par page</param>
        /// <returns>Liste des artistes correspondant aux critères de recherche</returns>
        /// <response code="200">Retourne la liste des artistes</response>
        /// <response code="500">Erreur interne du serveur</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<Artist>>> SearchArtists(
            [FromQuery] string? name = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Recherche d'artistes - Nom: {Name}, Page: {Page}, PageSize: {PageSize}",
                    name ?? "null", page, pageSize);

                var artists = await _artistRepository.SearchByNameAsync(name, page, pageSize);
                return Ok(artists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche d'artistes: {Message}", ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de la recherche d'artistes.");
            }
        }

        /// <summary>
        /// Récupère un artiste par son identifiant
        /// </summary>
        /// <param name="id">Identifiant de l'artiste</param>
        /// <returns>L'artiste correspondant à l'identifiant</returns>
        /// <response code="200">Retourne l'artiste</response>
        /// <response code="404">Artiste non trouvé</response>
        /// <response code="500">Erreur interne du serveur</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Artist>> GetArtist(Guid id)
        {
            try
            {
                _logger.LogInformation("Récupération de l'artiste avec l'identifiant {Id}", id);

                var artist = await _artistRepository.GetByIdAsync(id);

                if (artist == null)
                {
                    _logger.LogWarning("Artiste avec l'identifiant {Id} non trouvé", id);
                    return NotFound();
                }

                return Ok(artist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'artiste {Id}: {Message}", id, ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'artiste.");
            }
        }

        /// <summary>
        /// Crée un nouvel artiste
        /// </summary>
        /// <param name="artist">L'artiste à créer</param>
        /// <returns>L'artiste créé</returns>
        /// <response code="201">Retourne l'artiste créé</response>
        /// <response code="400">Requête invalide</response>
        /// <response code="500">Erreur interne du serveur</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Artist>> CreateArtist([FromBody] Artist artist)
        {
            try
            {
                if (artist == null)
                {
                    return BadRequest("L'artiste ne peut pas être null.");
                }

                _logger.LogInformation("Création d'un nouvel artiste: {LastName}, {FirstName}",
                    artist.LastName, artist.FirstName);

                var createdArtist = await _artistRepository.AddAsync(artist);
                return CreatedAtAction(nameof(GetArtist), new { id = createdArtist.Id }, createdArtist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'artiste: {Message}", ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de la création de l'artiste.");
            }
        }

        /// <summary>
        /// Met à jour un artiste existant
        /// </summary>
        /// <param name="id">Identifiant de l'artiste à mettre à jour</param>
        /// <param name="artist">Les nouvelles informations de l'artiste</param>
        /// <returns>Aucun contenu en cas de succès</returns>
        /// <response code="204">L'artiste a été mis à jour avec succès</response>
        /// <response code="400">Requête invalide</response>
        /// <response code="404">Artiste non trouvé</response>
        /// <response code="500">Erreur interne du serveur</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateArtist(Guid id, [FromBody] Artist artist)
        {
            try
            {
                if (artist == null)
                {
                    return BadRequest("L'artiste ne peut pas être null.");
                }

                if (id != artist.Id)
                {
                    return BadRequest("L'identifiant dans l'URL ne correspond pas à l'identifiant de l'artiste.");
                }

                var existingArtist = await _artistRepository.GetByIdAsync(id);
                if (existingArtist == null)
                {
                    return NotFound();
                }

                _logger.LogInformation("Mise à jour de l'artiste avec l'identifiant {Id}", id);

                var result = await _artistRepository.UpdateAsync(artist);
                if (!result)
                {
                    return StatusCode(500, "La mise à jour de l'artiste a échoué.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'artiste {Id}: {Message}", id, ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de l'artiste.");
            }
        }

        /// <summary>
        /// Supprime un artiste
        /// </summary>
        /// <param name="id">Identifiant de l'artiste à supprimer</param>
        /// <returns>Aucun contenu en cas de succès</returns>
        /// <response code="204">L'artiste a été supprimé avec succès</response>
        /// <response code="404">Artiste non trouvé</response>
        /// <response code="500">Erreur interne du serveur</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteArtist(Guid id)
        {
            try
            {
                _logger.LogInformation("Suppression de l'artiste avec l'identifiant {Id}", id);

                var artist = await _artistRepository.GetByIdAsync(id);
                if (artist == null)
                {
                    return NotFound();
                }

                var result = await _artistRepository.DeleteAsync(id);
                if (!result)
                {
                    return StatusCode(500, "La suppression de l'artiste a échoué.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'artiste {Id}: {Message}", id, ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de l'artiste.");
            }
        }
    }
}
