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
        /// Lit un fichier CSV Joconde en flux et rend ses notices par lots de <paramref name="batchSize"/> œuvres,
        /// chaque lot portant les entités liées vues pour la première fois (voir <see cref="ParsingResult"/>).
        /// Le fichier n'est jamais gardé entier en mémoire : <paramref name="onBatch"/> doit consommer le lot.
        /// </summary>
        /// <returns>Les totaux du fichier lu</returns>
        Task<ParsingSummary> ParseInBatchesAsync(
            string csvFilePath,
            int batchSize,
            Func<ParsingResult, Task> onBatch,
            Action<int, int>? progressCallback = null,
            CancellationToken cancellationToken = default);
    }
}
