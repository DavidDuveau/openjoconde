using OpenJoconde.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Interface pour le service de données Joconde
    /// </summary>
    public interface IJocondeDataService
    {
        /// <summary>
        /// Télécharge les données Joconde depuis l'URL spécifiée
        /// </summary>
        Task<string> DownloadJocondeDataAsync(string url, string destinationPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyse un fichier XML Joconde et extrait les oeuvres d'art (méthode maintenue pour compatibilité)
        /// </summary>
        Task<IEnumerable<Artwork>> ParseJocondeXmlAsync(string xmlFilePath, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Analyse un fichier JSON Joconde et extrait les oeuvres d'art
        /// </summary>
        Task<IEnumerable<Artwork>> ParseJocondeJsonAsync(string jsonFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Importe les oeuvres d'art dans la base de données
        /// </summary>
        Task<int> ImportArtworksAsync(IEnumerable<Artwork> artworks, CancellationToken cancellationToken = default);

        /// <summary>
        /// Exécute le processus complet de mise à jour des données Joconde
        /// </summary>
        Task<ImportReport> UpdateJocondeDataAsync(string dataUrl, string tempDirectory, CancellationToken cancellationToken = default);

        /// <summary>
        /// Télécharge le dernier fichier Joconde disponible
        /// </summary>
        Task<string> DownloadLatestFileAsync(string destinationDirectory, CancellationToken cancellationToken = default);

        /// <summary>
        /// Importe les données depuis un fichier XML
        /// </summary>
        Task<ImportReport> ImportFromXmlFileAsync(string xmlFilePath, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Importe les données depuis un fichier JSON
        /// </summary>
        Task<ImportReport> ImportFromJsonFileAsync(string jsonFilePath, CancellationToken cancellationToken = default);
    }
}
