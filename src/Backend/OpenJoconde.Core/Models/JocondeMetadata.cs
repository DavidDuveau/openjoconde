using System;

namespace OpenJoconde.Core.Models
{
    /// <summary>
    /// Métadonnées sur la source des données Joconde
    /// </summary>
    public class JocondeMetadata
    {
        /// <summary>
        /// Identifiant unique
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Version de la source de données
        /// </summary>
        public string SourceVersion { get; set; }
        
        /// <summary>
        /// Date de la dernière mise à jour
        /// </summary>
        public DateTime? LastUpdateDate { get; set; }
        
        /// <summary>
        /// Nombre total d'enregistrements
        /// </summary>
        public int? TotalRecords { get; set; }
        
        /// <summary>
        /// Version du schéma
        /// </summary>
        public string SchemaVersion { get; set; }
        
        /// <summary>
        /// Notes supplémentaires
        /// </summary>
        public string Notes { get; set; }
        
        /// <summary>
        /// Date de création de l'enregistrement
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Date de mise à jour de l'enregistrement
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
