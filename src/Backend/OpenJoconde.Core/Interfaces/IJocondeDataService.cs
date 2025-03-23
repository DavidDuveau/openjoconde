using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Interface pour le service de gestion des données Joconde
    /// </summary>
    public interface IJocondeDataService
    {
        /// <summary>
        /// Télécharge les données Joconde depuis l'URL spécifiée
        /// </summary>
        /// <param name="url">URL du fichier XML Joconde</param>
        /// <param name="destinationPath">Chemin de destination pour le fichier téléchargé</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Chemin du fichier téléchargé</returns>
        Task<string> DownloadJocondeDataAsync(string url, string destinationPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyse un fichier XML Joconde et extrait les oeuvres d'art
        /// </summary>
        /// <param name="xmlFilePath">Chemin du fichier XML</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Liste des oeuvres d'art extraites</returns>
        Task<IEnumerable<Artwork>> ParseJocondeXmlAsync(string xmlFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Importe les oeuvres d'art dans la base de données
        /// </summary>
        /// <param name="artworks">Liste des oeuvres d'art à importer</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Nombre d'oeuvres importées</returns>
        Task<int> ImportArtworksAsync(IEnumerable<Artwork> artworks, CancellationToken cancellationToken = default);

        /// <summary>
        /// Exécute le processus complet de mise à jour des données Joconde
        /// </summary>
        /// <param name="xmlUrl">URL du fichier XML Joconde</param>
        /// <param name="tempDirectory">Répertoire temporaire pour stocker le fichier téléchargé</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Rapport d'importation avec les statistiques</returns>
        Task<ImportReport> UpdateJocondeDataAsync(string xmlUrl, string tempDirectory, CancellationToken cancellationToken = default);
    }

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
    }
}
