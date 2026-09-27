using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Interface pour le service de données Joconde (téléchargement uniquement)
    /// </summary>
    public interface IJocondeDataService
    {
        /// <summary>
        /// Télécharge les données Joconde depuis l'URL spécifiée
        /// </summary>
        Task<string> DownloadJocondeDataAsync(string url, string destinationPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Télécharge le dernier fichier Joconde disponible
        /// </summary>
        Task<string> DownloadLatestFileAsync(string destinationDirectory, CancellationToken cancellationToken = default);
    }
}
