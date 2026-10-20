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
        public string SyncType { get; set; } = string.Empty;

        /// <summary>
        /// Origine de la synchronisation ('Manual' ou 'Automatic')
        /// </summary>
        public string TriggeredBy { get; set; } = "Manual";
        
        /// <summary>
        /// Date et heure de début de la synchronisation
        /// </summary>
        public DateTime StartedAt { get; set; }
        
        /// <summary>
        /// Date et heure de création du log
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Date et heure de fin de la synchronisation
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        
        /// <summary>
        /// Nombre d'œuvres traitées pendant la synchronisation
        /// </summary>
        public int ArtworksProcessed { get; set; }
        
        /// <summary>
        /// Nombre d'artistes traités pendant la synchronisation
        /// </summary>
        public int ArtistsProcessed { get; set; }
        
        /// <summary>
        /// Nombre total d'items traités pendant la synchronisation
        /// </summary>
        public int ItemsProcessed { get => ArtworksProcessed + ArtistsProcessed; }
        
        /// <summary>
        /// Indique si la synchronisation s'est terminée avec succès
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Nombre d'éléments perdus pendant l'import (voir ImportReport.Errors)
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Message d'erreur en cas d'échec
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Statut de la synchronisation : 'Running', 'Completed', 'CompletedWithErrors', 'Failed', 'Canceled'
        /// ou 'Interrupted' (le serveur s'est arrêté en cours de route)
        /// </summary>
        public string Status { get; set; } = "Pending";
    }
}