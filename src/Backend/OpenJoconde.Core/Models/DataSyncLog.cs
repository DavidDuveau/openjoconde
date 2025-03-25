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
        
        /// <summary>
        /// Nombre d'œuvres traitées pendant la synchronisation
        /// </summary>
        public int ArtworksProcessed { get; set; }
        
        /// <summary>
        /// Nombre d'artistes traités pendant la synchronisation
        /// </summary>
        public int ArtistsProcessed { get; set; }
        
        /// <summary>
        /// Indique si la synchronisation s'est terminée avec succès
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Message d'erreur en cas d'échec
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Statut de la synchronisation (par exemple: 'Pending', 'Running', 'Completed', 'Failed')
        /// </summary>
        public string Status { get; set; } = "Pending";
    }
}