using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Parsers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Implémentation du service de parsing XML Joconde
    /// </summary>
    public class JocondeXmlParserService : IJocondeXmlParser
    {
        private readonly ILogger<JocondeXmlParserService> _logger;
        private readonly JocondeXmlParser _parser;

        public JocondeXmlParserService(ILogger<JocondeXmlParserService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _parser = new JocondeXmlParser();
        }

        /// <inheritdoc />
        public async Task<ParsingResult> ParseAsync(
            string xmlFilePath, 
            Action<int, int> progressCallback = null, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Démarrage du parsing du fichier XML Joconde: {FilePath}", xmlFilePath);

            try
            {
                // Enregistrer la progression avec logging
                Action<int, int> loggingProgress = null;
                if (progressCallback != null)
                {
                    loggingProgress = (current, total) =>
                    {
                        progressCallback(current, total);
                        if (current % 1000 == 0 || current == total)
                        {
                            _logger.LogInformation("Progression du parsing: {Current}/{Total} records ({Percent}%)",
                                current, total, Math.Round((double)current / total * 100));
                        }
                    };
                }

                // Déléguer au parser
                var result = await _parser.ParseAsync(xmlFilePath, loggingProgress, cancellationToken);

                _logger.LogInformation("Parsing terminé avec succès. {ArtworkCount} œuvres extraites", 
                    result.Artworks.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du parsing du fichier XML: {Message}", ex.Message);
                throw;
            }
        }
    }
}
