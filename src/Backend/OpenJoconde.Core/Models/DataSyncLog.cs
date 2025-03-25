using System;

namespace OpenJoconde.Core.Models
{
    /// <summary>
    /// Journal des synchronisations de données avec la source
    /// </summary>
    public class DataSyncLog
    {
        /// <summary>
        /// Identifiant unique
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Type de synchronisation ('Full' ou 'Incremental')
        /// </summary>
        public string SyncType { get; set; }
        
        /// <summary>
        /// Date et heure de début de la synchronisation
        /// </summary>
        public DateTime StartedAt { get; set; }
        
        /// <summary>
        /// Date et heure de début (alias pour compatibilité avec AutoSyncService)
        /// </summary>
        public DateTime StartTime { get => StartedAt; set => StartedAt = value; }
        
        /// <summary>
        /// Date et heure de fin de la synchronisation
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        
        /// <summary>
        /// Date et heure de fin (alias pour compatibilité avec AutoSyncService)
        /// </summary>
        public DateTime? EndTime { get => CompletedAt; set => CompletedAt = value; }
    }
}