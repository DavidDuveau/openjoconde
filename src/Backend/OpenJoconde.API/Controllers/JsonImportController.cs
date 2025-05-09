using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JsonImportController : ControllerBase
    {
        private readonly ILogger<JsonImportController> _logger;
        private readonly IJocondeJsonParser _jsonParser;
        private readonly IDataImportService _dataImportService;

        public JsonImportController(
            ILogger<JsonImportController> logger,
            IJocondeJsonParser jsonParser,
            IDataImportService dataImportService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonParser = jsonParser ?? throw new ArgumentNullException(nameof(jsonParser));
            _dataImportService = dataImportService ?? throw new ArgumentNullException(nameof(dataImportService));
        }

        /// <summary>
        /// Importe les données à partir d'un fichier JSON téléchargé en utilisant le parser optimisé
        /// </summary>
        [HttpPost("from-file")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ImportFromJsonFile(IFormFile file, CancellationToken cancellationToken)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("Le fichier n'est pas valide.");

                // Valider l'extension du fichier
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".json")
                    return BadRequest("Le fichier doit être au format JSON.");

                // Récupération du chemin temporaire pour stocker le fichier
                var tempDirectory = Path.Combine(Path.GetTempPath(), "OpenJoconde", "Json");
                if (!Directory.Exists(tempDirectory))
                    Directory.CreateDirectory(tempDirectory);

                var tempFilePath = Path.Combine(tempDirectory, $"joconde_{DateTime.Now:yyyyMMdd_HHmmss}.json");

                try
                {
                    // Sauvegarde du fichier reçu
                    _logger.LogInformation("Enregistrement du fichier JSON téléchargé vers {FilePath}", tempFilePath);
                    using (var stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                    {
                        await file.CopyToAsync(stream, cancellationToken);
                    }

                    // Utilisation du parser JSON optimisé
                    _logger.LogInformation("Début du parsing optimisé du fichier JSON: {FilePath}", tempFilePath);
                    int processedItems = 0;

                    var parsingResult = await _jsonParser.ParseAsync(tempFilePath, 
                        progressCallback: (current, total) =>
                        {
                            processedItems = current;
                            if (current % 1000 == 0 || current == total)
                            {
                                _logger.LogInformation("Progression du parsing JSON: {Current}/{Total} éléments traités", current, total);
                            }
                        },
                        cancellationToken: cancellationToken);

                    _logger.LogInformation("Parsing JSON terminé: {Count} œuvres extraites", parsingResult.Artworks.Count);

                    // Importation des données en base
                    _logger.LogInformation("Début de l'importation des données en base");
                    var importStats = await _dataImportService.ImportDataAsync(parsingResult, 
                        progressCallback: (phase, current, total) =>
                        {
                            _logger.LogDebug("Progression de l'import ({Phase}): {Current}/{Total}", phase, current, total);
                        },
                        cancellationToken: cancellationToken);

                    return Ok(new
                    {
                        message = "Importation réussie avec le parser streaming",
                        processed = processedItems,
                        parsed = new
                        {
                            artworks = parsingResult.Artworks.Count,
                            artists = parsingResult.Artists.Count,
                            domains = parsingResult.Domains.Count,
                            techniques = parsingResult.Techniques.Count,
                            periods = parsingResult.Periods.Count,
                            museums = parsingResult.Museums.Count
                        },
                        imported = importStats
                    });
                }
                finally
                {
                    // Nettoyage du fichier temporaire - commenté pour le débogage
                    // if (System.IO.File.Exists(tempFilePath))
                    // {
                    //     System.IO.File.Delete(tempFilePath);
                    // }
                    
                    // Laisser le fichier pour inspection manuelle si nécessaire
                    _logger.LogInformation("Fichier JSON temporaire conservé pour analyse: {FilePath}", tempFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'importation du JSON avec le parser optimisé: {Message}", ex.Message);
                return StatusCode(500, new { error = "Une erreur est survenue lors de l'importation des données.", details = ex.Message });
            }
        }

        /// <summary>
        /// Importe un fichier JSON déjà existant sur le serveur
        /// </summary>
        [HttpPost("from-server-file")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ImportFromServerFile(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return BadRequest("Le chemin du fichier ne peut pas être vide.");

                if (!System.IO.File.Exists(filePath))
                    return BadRequest($"Le fichier n'existe pas: {filePath}");

                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (extension != ".json")
                    return BadRequest("Le fichier doit être au format JSON.");

                // Utilisation du parser JSON optimisé
                _logger.LogInformation("Début du parsing optimisé du fichier JSON: {FilePath}", filePath);
                int processedItems = 0;

                var parsingResult = await _jsonParser.ParseAsync(filePath, 
                    progressCallback: (current, total) =>
                    {
                        processedItems = current;
                        if (current % 1000 == 0 || current == total)
                        {
                            _logger.LogInformation("Progression du parsing JSON: {Current}/{Total} éléments traités", current, total);
                        }
                    },
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Parsing JSON terminé: {Count} œuvres extraites", parsingResult.Artworks.Count);

                // Importation des données en base
                _logger.LogInformation("Début de l'importation des données en base");
                var importStats = await _dataImportService.ImportDataAsync(parsingResult, 
                    progressCallback: (phase, current, total) =>
                    {
                        _logger.LogDebug("Progression de l'import ({Phase}): {Current}/{Total}", phase, current, total);
                    },
                    cancellationToken: cancellationToken);

                return Ok(new
                {
                    message = "Importation réussie avec le parser streaming",
                    filePath = filePath,
                    fileSize = new FileInfo(filePath).Length,
                    processed = processedItems,
                    parsed = new
                    {
                        artworks = parsingResult.Artworks.Count,
                        artists = parsingResult.Artists.Count,
                        domains = parsingResult.Domains.Count,
                        techniques = parsingResult.Techniques.Count,
                        periods = parsingResult.Periods.Count,
                        museums = parsingResult.Museums.Count
                    },
                    imported = importStats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'importation du JSON avec le parser optimisé: {Message}", ex.Message);
                return StatusCode(500, new { error = "Une erreur est survenue lors de l'importation des données.", details = ex.Message });
            }
        }
    }
}