using Microsoft.Extensions.Options;
using OpenJoconde.Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenJoconde.API.Security;
using OpenJoconde.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.API.Controllers
{
    /// <summary>
    /// Mise à jour des musées depuis Muséofile, sans passer par une synchronisation complète de Joconde
    /// </summary>
    [Route("api/museofile")]
    [ApiController]
    public class MuseofileController : ControllerBase
    {
        private readonly IMuseofileService _museofile;
        private readonly SyncOptions _options;
        private readonly ILogger<MuseofileController> _logger;

        /// <summary>Crée le contrôleur avec ses dépendances</summary>
        public MuseofileController(IMuseofileService museofile, IOptions<SyncOptions> options, ILogger<MuseofileController> logger)
        {
            _museofile = museofile ?? throw new ArgumentNullException(nameof(museofile));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Télécharge le fichier Muséofile (<c>Sync:MuseofileSourceUrl</c>) et complète les musées : adresse, code postal,
        /// téléphone, site Internet, histoire. Même opération que celle qui suit chaque synchronisation de Joconde.
        /// </summary>
        /// <returns>Nombre de musées lus, retrouvés, mis à jour et sans correspondance</returns>
        [RequireApiKey]
        [EnableRateLimiting("ecriture")]
        [HttpPost("sync")]
        public async Task<IActionResult> Sync(CancellationToken cancellationToken)
        {
            var url = _options.MuseofileSourceUrl;
            if (string.IsNullOrWhiteSpace(url))
                return StatusCode(503, "L'URL source de Muséofile n'est pas configurée.");

            try
            {
                return Ok(await _museofile.EnrichFromUrlAsync(url, cancellationToken));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Les musées n'ont pas pu être complétés avec Muséofile");
                return StatusCode(502, "Le fichier Muséofile n'a pas pu être lu.");
            }
        }
    }
}
