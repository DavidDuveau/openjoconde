using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JocondeDataController : ControllerBase
    {
        private readonly IJocondeDataService _jocondeDataService;
        private readonly IDataImportService _dataImportService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JocondeDataController> _logger;

        public JocondeDataController(
            IJocondeDataService jocondeDataService,
            IDataImportService dataImportService,
            IConfiguration configuration,
            ILogger<JocondeDataController> logger)
        {
            _jocondeDataService = jocondeDataService ?? throw new ArgumentNullException(nameof(jocondeDataService));
            _dataImportService = dataImportService ?? throw new ArgumentNullException(nameof(dataImportService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Déclenche manuellement la mise à jour des données Joconde
        /// </summary>
        /// <returns>Rapport d'importation</returns>
        [HttpPost("import")]
        public async Task<IActionResult> ImportJocondeData(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Démarrage manuel de l'importation des données Joconde");

                // Récupérer l'URL du fichier XML Joconde depuis la configuration
                var jocondeUrl = _configuration["JocondeData:XmlUrl"];
                if (string.IsNullOrEmpty(jocondeUrl))
                {
                    jocondeUrl = "https://data.culture.gouv.fr/explore/dataset/joconde-musees-de-france-base-joconde/download/?format=xml";
                    _logger.LogWarning("URL du fichier XML Joconde non configurée, utilisation de l'URL par défaut: {Url}", jocondeUrl);
                }

                // Créer un répertoire temporaire pour le téléchargement
                var tempDirectory = Path.Combine(Path.GetTempPath(), "OpenJoconde");
                
                // Lancer la mise à jour des données
                var report = await _jocondeDataService.UpdateJocondeDataAsync(jocondeUrl, tempDirectory, cancellationToken);

                _logger.LogInformation("Importation des données Joconde terminée");
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'importation des données Joconde: {Message}", ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de l'importation des données Joconde.");
            }
        }

        /// <summary>
        /// Déclenche l'importation avancée des données Joconde avec analyse complète du XML
        /// </summary>
        /// <returns>Rapport d'importation détaillé</returns>
        [HttpPost("import/advanced")]
        public async Task<IActionResult> ImportJocondeDataAdvanced(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Démarrage manuel de l'importation avancée des données Joconde");

                // Récupérer l'URL du fichier XML Joconde depuis la configuration
                var jocondeUrl = _configuration["JocondeData:XmlUrl"];
                if (string.IsNullOrEmpty(jocondeUrl))
                {
                    jocondeUrl = "https://data.culture.gouv.fr/explore/dataset/joconde-musees-de-france-base-joconde/download/?format=xml";
                    _logger.LogWarning("URL du fichier XML Joconde non configurée, utilisation de l'URL par défaut: {Url}", jocondeUrl);
                }

                // Créer un répertoire temporaire pour le téléchargement
                var tempDirectory = Path.Combine(Path.GetTempPath(), "OpenJoconde");
                Directory.CreateDirectory(tempDirectory);
                
                // Télécharger le fichier XML
                var tempFilePath = await _dataImportService.DownloadJocondeDataAsync(jocondeUrl, Path.Combine(tempDirectory, $"joconde_{DateTime.Now:yyyyMMdd_HHmmss}.xml"), cancellationToken);
                
                // Importer les données avec le service avancé
                var report = await _dataImportService.ImportFromXmlFileAsync(
                    tempFilePath,
                    (stage, current, total) => _logger.LogInformation("{Stage}: {Current}/{Total}", stage, current, total),
                    cancellationToken);

                // Nettoyer les fichiers temporaires
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }

                _logger.LogInformation("Importation avancée des données Joconde terminée");
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'importation avancée des données Joconde: {Message}", ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de l'importation avancée des données Joconde.");
            }
        }

        /// <summary>
        /// Récupère l'état de la dernière importation de données
        /// </summary>
        /// <returns>État de l'importation</returns>
        [HttpGet("status")]
        public IActionResult GetImportStatus()
        {
            // Cette méthode serait implémentée pour suivre l'état de l'importation
            // Dans une implémentation complète, on stockerait l'état dans une table dédiée ou un cache

            return Ok(new { Status = "Not implemented yet" });
        }
    }
}
