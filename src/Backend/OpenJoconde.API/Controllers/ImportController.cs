using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Parsers;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImportController : ControllerBase
    {
        private readonly ILogger<ImportController> _logger;
        private readonly IJocondeXmlParser _xmlParser;
        private readonly IDataImportService _dataImportService;
        private readonly IJocondeDataService _jocondeDataService;

        public ImportController(
            ILogger<ImportController> logger,
            IJocondeXmlParser xmlParser,
            IDataImportService dataImportService,
            IJocondeDataService jocondeDataService)
        {
            _logger = logger;
            _xmlParser = xmlParser;
            _dataImportService = dataImportService;
            _jocondeDataService = jocondeDataService;
        }

        /// <summary>
        /// Importe les données à partir d'un fichier XML téléchargé
        /// </summary>
        [HttpPost("from-file")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ImportFromFile(IFormFile file, CancellationToken cancellationToken)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("Le fichier n'est pas valide.");

                // Récupération du chemin temporaire pour stocker le fichier
                var tempPath = Path.GetTempFileName();
                
                try
                {
                    // Sauvegarde du fichier reçu
                    using (var stream = new FileStream(tempPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream, cancellationToken);
                    }
                    
                    // Parsing du fichier XML
                    _logger.LogInformation("Début du parsing du fichier {FileName}", file.FileName);
                    var parsingResult = await _xmlParser.ParseAsync(tempPath, 
                        progressCallback: (current, total) =>
                        {
                            _logger.LogDebug("Progression du parsing: {Current}/{Total}", current, total);
                        },
                        cancellationToken: cancellationToken);
                    
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
                        message = "Importation réussie",
                        stats = importStats
                    });
                }
                finally
                {
                    // Nettoyage du fichier temporaire
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'importation des données");
                return StatusCode(500, "Une erreur est survenue lors de l'importation des données.");
            }
        }

        /// <summary>
        /// Télécharge le dernier fichier XML de data.gouv.fr et l'importe
        /// </summary>
        [HttpPost("from-datagouv")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ImportFromDataGouv(CancellationToken cancellationToken)
        {
            try
            {
                // Téléchargement du fichier
                _logger.LogInformation("Téléchargement du fichier depuis data.gouv.fr");
                var tempPath = Path.GetTempFileName();
                
                try
                {
                    // Téléchargement du fichier XML
                    await _jocondeDataService.DownloadLatestFileAsync(tempPath, cancellationToken);
                    
                    // Parsing du fichier XML
                    _logger.LogInformation("Début du parsing du fichier");
                    var parsingResult = await _xmlParser.ParseAsync(tempPath, 
                        progressCallback: (current, total) =>
                        {
                            _logger.LogDebug("Progression du parsing: {Current}/{Total}", current, total);
                        },
                        cancellationToken: cancellationToken);
                    
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
                        message = "Importation réussie",
                        stats = importStats
                    });
                }
                finally
                {
                    // Nettoyage du fichier temporaire
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'importation des données depuis data.gouv.fr");
                return StatusCode(500, "Une erreur est survenue lors de l'importation des données.");
            }
        }
    }
}
