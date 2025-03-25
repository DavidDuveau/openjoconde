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
    public class MuseumsController : ControllerBase
    {
        private readonly IMuseumRepository _museumRepository;
        private readonly ILogger<MuseumsController> _logger;

        public MuseumsController(
            IMuseumRepository museumRepository,
            ILogger<MuseumsController> logger)
        {
            _museumRepository = museumRepository ?? throw new ArgumentNullException(nameof(museumRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Recherche des musées par nom ou localisation
        /// </summary>
        /// <param name="name">Nom ou localisation partielle du musée à rechercher</param>
        /// <param name="page">Numéro de page (commence à 1)</param>
        /// <param name="pageSize">Nombre d'éléments par page</param>
        /// <returns>Liste des musées correspondant aux critères de recherche</returns>
        /// <response code="200">Retourne la liste des musées</response>
        /// <response code="500">Erreur interne du serveur</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<Museum>>> SearchMuseums(
            [FromQuery] string? name = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Recherche de musées - Nom/Localisation: {Name}, Page: {Page}, PageSize: {PageSize}",
                    name ?? "null", page, pageSize);

                var museums = await _museumRepository.SearchByNameAsync(name, page, pageSize);
                return Ok(museums);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche de musées: {Message}", ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de la recherche de musées.");
            }
        }

        /// <summary>
        /// Récupère un musée par son identifiant
        /// </summary>
        /// <param name="id">Identifiant du musée</param>
        /// <returns>Le musée correspondant à l'identifiant</returns>
        /// <response code="200">Retourne le musée</response>
        /// <response code="404">Musée non trouvé</response>
        /// <response code="500">Erreur interne du serveur</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Museum>> GetMuseum(Guid id)
        {
            try
            {
                _logger.LogInformation("Récupération du musée avec l'identifiant {Id}", id);

                var museum = await _museumRepository.GetByIdAsync(id);

                if (museum == null)
                {
                    _logger.LogWarning("Musée avec l'identifiant {Id} non trouvé", id);
                    return NotFound();
                }

                return Ok(museum);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du musée {Id}: {Message}", id, ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du musée.");
            }
        }

        /// <summary>
        /// Crée un nouveau musée
        /// </summary>
        /// <param name="museum">Le musée à créer</param>
        /// <returns>Le musée créé</returns>
        /// <response code="201">Retourne le musée créé</response>
        /// <response code="400">Requête invalide</response>
        /// <response code="500">Erreur interne du serveur</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Museum>> CreateMuseum([FromBody] Museum museum)
        {
            try
            {
                if (museum == null)
                {
                    return BadRequest("Le musée ne peut pas être null.");
                }

                _logger.LogInformation("Création d'un nouveau musée: {Name}, {City}",
                    museum.Name, museum.City);

                var createdMuseum = await _museumRepository.AddAsync(museum);
                return CreatedAtAction(nameof(GetMuseum), new { id = createdMuseum.Id }, createdMuseum);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du musée: {Message}", ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de la création du musée.");
            }
        }

        /// <summary>
        /// Met à jour un musée existant
        /// </summary>
        /// <param name="id">Identifiant du musée à mettre à jour</param>
        /// <param name="museum">Les nouvelles informations du musée</param>
        /// <returns>Aucun contenu en cas de succès</returns>
        /// <response code="204">Le musée a été mis à jour avec succès</response>
        /// <response code="400">Requête invalide</response>
        /// <response code="404">Musée non trouvé</response>
        /// <response code="500">Erreur interne du serveur</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateMuseum(Guid id, [FromBody] Museum museum)
        {
            try
            {
                if (museum == null)
                {
                    return BadRequest("Le musée ne peut pas être null.");
                }

                if (id != museum.Id)
                {
                    return BadRequest("L'identifiant dans l'URL ne correspond pas à l'identifiant du musée.");
                }

                var existingMuseum = await _museumRepository.GetByIdAsync(id);
                if (existingMuseum == null)
                {
                    return NotFound();
                }

                _logger.LogInformation("Mise à jour du musée avec l'identifiant {Id}", id);

                var result = await _museumRepository.UpdateAsync(museum);
                if (!result)
                {
                    return StatusCode(500, "La mise à jour du musée a échoué.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du musée {Id}: {Message}", id, ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour du musée.");
            }
        }

        /// <summary>
        /// Supprime un musée
        /// </summary>
        /// <param name="id">Identifiant du musée à supprimer</param>
        /// <returns>Aucun contenu en cas de succès</returns>
        /// <response code="204">Le musée a été supprimé avec succès</response>
        /// <response code="404">Musée non trouvé</response>
        /// <response code="500">Erreur interne du serveur</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteMuseum(Guid id)
        {
            try
            {
                _logger.LogInformation("Suppression du musée avec l'identifiant {Id}", id);

                var museum = await _museumRepository.GetByIdAsync(id);
                if (museum == null)
                {
                    return NotFound();
                }

                var result = await _museumRepository.DeleteAsync(id);
                if (!result)
                {
                    return StatusCode(500, "La suppression du musée a échoué.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du musée {Id}: {Message}", id, ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du musée.");
            }
        }
    }
}
