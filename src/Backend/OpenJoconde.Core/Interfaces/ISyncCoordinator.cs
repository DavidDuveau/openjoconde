using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Résultat d'une demande de démarrage de synchronisation
    /// </summary>
    public sealed class SyncStartResult
    {
        private SyncStartResult() { }

        /// <summary>
        /// La synchronisation a démarré
        /// </summary>
        public bool Started { get; private init; }

        /// <summary>
        /// Identifiant du journal de la synchronisation démarrée
        /// </summary>
        public Guid? SyncId { get; private init; }

        /// <summary>
        /// Se termine avec la synchronisation, quelle que soit son issue
        /// </summary>
        public Task Completion { get; private init; } = Task.CompletedTask;

        /// <summary>
        /// Début de la synchronisation déjà en cours, quand le démarrage est refusé
        /// </summary>
        public DateTime? RunningSince { get; private init; }

        public static SyncStartResult Running(Guid id, Task completion) =>
            new() { Started = true, SyncId = id, Completion = completion };

        public static SyncStartResult Busy(DateTime? runningSince) =>
            new() { Started = false, RunningSince = runningSince };
    }

    /// <summary>
    /// Lance, suit et annule les synchronisations avec la source Joconde. Une seule synchronisation à la fois,
    /// tous processus confondus ; chacune laisse une ligne dans le journal DataSyncLog.
    /// </summary>
    public interface ISyncCoordinator
    {
        /// <summary>
        /// Démarre une synchronisation en arrière-plan, sauf si une autre est déjà en cours
        /// </summary>
        /// <param name="triggeredBy">« Manual » ou « Automatic »</param>
        Task<SyncStartResult> TryStartAsync(string triggeredBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Demande l'annulation d'une synchronisation en cours dans ce processus
        /// </summary>
        /// <returns>false si aucune synchronisation de ce processus n'a cet identifiant</returns>
        bool RequestCancel(Guid syncId);
    }
}
