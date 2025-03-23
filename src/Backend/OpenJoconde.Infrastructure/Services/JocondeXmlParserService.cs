using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using OpenJoconde.Core.Parsers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Service d'implémentation du parser XML Joconde
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

        /// <summary>
        /// Analyse un fichier XML et en extrait les entités (œuvres, artistes, etc.)
        /// </summary>
        public async Task<ParsingResult> ParseAsync(
            string xmlFilePath, 
            Action<int, int> progressCallback = null, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Démarrage de l'analyse du fichier XML {FilePath}", xmlFilePath);

            try
            {
                // Utiliser le parser sous-jacent pour faire le travail
                var result = await _parser.ParseAsync(xmlFilePath, progressCallback, cancellationToken);
                
                _logger.LogInformation("Analyse terminée: {ArtworksCount} œuvres, {ArtistsCount} artistes, " +
                                     "{MuseumsCount} musées, {DomainsCount} domaines, " +
                                     "{TechniquesCount} techniques, {PeriodsCount} périodes",
                                     result.Artworks.Count,
                                     result.Artists.Count,
                                     result.Museums.Count,
                                     result.Domains.Count,
                                     result.Techniques.Count,
                                     result.Periods.Count);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'analyse du fichier XML: {Message}", ex.Message);
                throw;
            }
        }
    }
}
