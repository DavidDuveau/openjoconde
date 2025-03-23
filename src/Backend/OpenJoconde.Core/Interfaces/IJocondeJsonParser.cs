using OpenJoconde.Core.Models;
using OpenJoconde.Core.Parsers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Interface pour le parseur de fichiers JSON Joconde
    /// </summary>
    public interface IJocondeJsonParser
    {
        /// <summary>
        /// Parse un fichier JSON Joconde et extrait toutes les entités (œuvres, artistes, domaines, etc.)
        /// </summary>
        /// <param name="jsonFilePath">Chemin vers le fichier JSON</param>
        /// <param name="progressCallback">Callback pour suivre la progression</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Collection d'œuvres et entités liées</returns>
        Task<ParsingResult> ParseAsync(
            string jsonFilePath, 
            Action<int, int> progressCallback = null, 
            CancellationToken cancellationToken = default);
    }
}
