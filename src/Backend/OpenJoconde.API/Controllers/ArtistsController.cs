using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenJoconde.API.Dtos;
using OpenJoconde.Core.Dtos;
using OpenJoconde.API.Security;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private static ArtistDto ToDto(Artist artist, int artworksCount) => new()
        {
            Id = artist.Id,
            LastName = artist.LastName,
            FirstName = artist.FirstName,
            BirthDate = artist.BirthDate,
            DeathDate = artist.DeathDate,
            Nationality = artist.Nationality,
            Biography = artist.Biography,
            ArtworksCount = artworksCount
        };

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
        public async Task<ActionResult<PaginatedResult<ArtistDto>>> SearchArtists(
            [FromQuery] string? name = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Recherche d'artistes - Nom: {Name}, Page: {Page}, PageSize: {PageSize}",
                    name ?? "null", page, pageSize);

                var artists = await _artistRepository.SearchByNameAsync(name, page, pageSize);
                var counts = await _artistRepository.CountArtworksAsync(artists.Items.Select(a => a.Id).ToList());
                return Ok(new PaginatedResult<ArtistDto>
                {
                    Items = artists.Items.Select(a => ToDto(a, counts.GetValueOrDefault(a.Id))).ToList(),
                    TotalItems = artists.TotalItems,
                    Page = artists.Page,
                    PageSize = artists.PageSize
                });
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
        public async Task<ActionResult<ArtistDto>> GetArtist(Guid id)
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

                var counts = await _artistRepository.CountArtworksAsync(new[] { id });
                return Ok(ToDto(artist, counts.GetValueOrDefault(id)));
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
        /// <param name="input">L'artiste à créer</param>
        /// <returns>L'artiste créé</returns>
        /// <response code="201">Retourne l'artiste créé</response>
        /// <response code="400">Requête invalide</response>
        /// <response code="500">Erreur interne du serveur</response>
        [RequireApiKey]
        [EnableRateLimiting("ecriture")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ArtistDto>> CreateArtist([FromBody] ArtistInput input)
        {
            try
            {
                var artist = new Artist { CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                input.ApplyTo(artist);

                _logger.LogInformation("Création d'un nouvel artiste: {LastName}, {FirstName}",
                    artist.LastName, artist.FirstName);

                var createdArtist = await _artistRepository.AddAsync(artist);
                return CreatedAtAction(nameof(GetArtist), new { id = createdArtist.Id }, ToDto(createdArtist, 0));
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
        /// <param name="input">Les nouvelles informations de l'artiste</param>
        /// <returns>Aucun contenu en cas de succès</returns>
        /// <response code="204">L'artiste a été mis à jour avec succès</response>
        /// <response code="400">Requête invalide</response>
        /// <response code="404">Artiste non trouvé</response>
        /// <response code="500">Erreur interne du serveur</response>
        [RequireApiKey]
        [EnableRateLimiting("ecriture")]
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateArtist(Guid id, [FromBody] ArtistInput input)
        {
            try
            {
                var existingArtist = await _artistRepository.GetByIdAsync(id);
                if (existingArtist == null)
                {
                    return NotFound();
                }

                _logger.LogInformation("Mise à jour de l'artiste avec l'identifiant {Id}", id);

                input.ApplyTo(existingArtist);
                existingArtist.UpdatedAt = DateTime.UtcNow;
                var result = await _artistRepository.UpdateAsync(existingArtist);
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
        [RequireApiKey]
        [EnableRateLimiting("ecriture")]
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
