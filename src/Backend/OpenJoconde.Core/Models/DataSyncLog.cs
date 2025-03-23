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
        /// Date et heure de fin de la synchronisation
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        
        /// <summary>
        /// Statut de la synchronisation ('Running', 'Completed', 'Failed')
        /// </summary>
        public string Status { get; set; }
        
        /// <summary>
        /// Nombre d'éléments traités
        /// </summary>
        public int ItemsProcessed { get; set; }
        
        /// <summary>
        /// Message d'erreur en cas d'échec
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Date de création de l'enregistrement
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
