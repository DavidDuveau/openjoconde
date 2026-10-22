using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Interface pour le service de données Joconde (téléchargement uniquement)
    /// </summary>
    public interface IJocondeDownloader
    {
        /// <summary>
        /// Télécharge les données Joconde depuis l'URL spécifiée
        /// </summary>
        Task<string> DownloadJocondeDataAsync(string url, string destinationPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Version courante de la source (ETag), lue sans télécharger le fichier ; null si la source ne l'indique pas
        /// </summary>
        Task<string?> GetSourceVersionAsync(string url, CancellationToken cancellationToken = default);
    }
}
