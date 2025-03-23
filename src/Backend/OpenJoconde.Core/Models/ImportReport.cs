using System;

namespace OpenJoconde.Core.Models
{
    /// <summary>
    /// Rapport d'importation avec statistiques
    /// </summary>
    public class ImportReport
    {
        /// <summary>
        /// Date de l'importation
        /// </summary>
        public DateTime ImportDate { get; set; }
        
        /// <summary>
        /// Nombre total d'œuvres dans la source
        /// </summary>
        public int TotalArtworks { get; set; }
        
        /// <summary>
        /// Nombre d'œuvres importées avec succès
        /// </summary>
        public int ImportedArtworks { get; set; }
        
        /// <summary>
        /// Nombre d'œuvres mises à jour
        /// </summary>
        public int UpdatedArtworks { get; set; }
        
        /// <summary>
        /// Nombre d'œuvres ignorées
        /// </summary>
        public int SkippedArtworks { get; set; }
        
        /// <summary>
        /// Nombre d'erreurs rencontrées
        /// </summary>
        public int Errors { get; set; }
        
        /// <summary>
        /// Durée totale de l'importation
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Indique si l'opération a réussi
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message d'erreur en cas d'échec
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
