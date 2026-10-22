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
        /// Lit un fichier JSON Joconde en flux et rend ses notices par lots de <paramref name="batchSize"/> œuvres,
        /// chaque lot portant les entités liées vues pour la première fois (voir <see cref="ParsingResult"/>).
        /// Le fichier n'est jamais gardé entier en mémoire : <paramref name="onBatch"/> doit consommer le lot.
        /// </summary>
        /// <param name="jsonFilePath">Chemin vers le fichier JSON</param>
        /// <param name="batchSize">Nombre d'œuvres par lot</param>
        /// <param name="onBatch">Appelée avec chaque lot, dans l'ordre du fichier</param>
        /// <param name="progressCallback">Callback pour suivre la progression</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Les totaux du fichier lu</returns>
        Task<ParsingSummary> ParseInBatchesAsync(
            string jsonFilePath,
            int batchSize,
            Func<ParsingResult, Task> onBatch,
            Action<int, int>? progressCallback = null,
            CancellationToken cancellationToken = default);
    }
}
