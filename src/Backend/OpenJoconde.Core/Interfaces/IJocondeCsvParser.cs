using OpenJoconde.Core.Parsers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Interface pour le parseur du fichier CSV Joconde (séparateur « | », valeurs entre guillemets)
    /// </summary>
    public interface IJocondeCsvParser
    {
        /// <summary>
        /// Parse un fichier CSV Joconde et extrait toutes les entités (œuvres, artistes, domaines, etc.)
        /// </summary>
        Task<ParsingResult> ParseAsync(
            string csvFilePath,
            Action<int, int>? progressCallback = null,
            CancellationToken cancellationToken = default);
    }
}
