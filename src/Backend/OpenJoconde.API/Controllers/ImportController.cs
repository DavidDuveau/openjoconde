using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenJoconde.API.Security;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.API.Controllers
{
    /// <summary>
    /// Import d'un fichier Joconde fourni par l'appelant. L'import du fichier publié par le ministère
    /// passe par la synchronisation (POST api/sync/start).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ImportController : ControllerBase
    {
        private static readonly string[] SupportedExtensions = { ".csv", ".json", ".xml" };

        private readonly ILogger<ImportController> _logger;
        private readonly IDataImportService _dataImportService;

        public ImportController(
            ILogger<ImportController> logger,
            IDataImportService dataImportService)
        {
            _logger = logger;
            _dataImportService = dataImportService;
        }

        /// <summary>
        /// Importe un fichier Joconde (CSV, JSON ou XML), le format étant déduit de l'extension
        /// </summary>
        [RequireApiKey]
        [EnableRateLimiting("ecriture")]
        [HttpPost("from-file")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ImportFromFile(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Le fichier n'est pas valide.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!SupportedExtensions.Contains(extension))
                return BadRequest("Le fichier doit être au format CSV, JSON ou XML.");

            var tempPath = Path.Combine(Path.GetTempPath(), $"joconde_{Guid.NewGuid():N}{extension}");

            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    await file.CopyToAsync(stream, cancellationToken);
                }

                _logger.LogInformation("Début de l'importation du fichier {FileName}", file.FileName);
                var report = await _dataImportService.ImportFromFileAsync(
                    tempPath,
                    (phase, current, total) => _logger.LogDebug("Progression de l'import ({Phase}): {Current}/{Total}", phase, current, total),
                    cancellationToken);

                return Ok(new
                {
                    message = report.Success ? "Importation réussie" : "Importation terminée avec des erreurs",
                    stats = report
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'importation des données");
                return StatusCode(500, "Une erreur est survenue lors de l'importation des données.");
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }
    }
}
