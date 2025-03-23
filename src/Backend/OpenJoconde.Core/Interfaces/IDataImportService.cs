using OpenJoconde.Core.Models;
using OpenJoconde.Core.Parsers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Service responsable de l'importation des données depuis le parseur vers la base de données
    /// </summary>
    public interface IDataImportService
    {
        /// <summary>
        /// Importe les données du résultat de parsing dans la base de données
        /// </summary>
        /// <param name="parsingResult">Résultat du parsing contenant les entités à importer</param>
        /// <param name="progressCallback">Callback pour suivre la progression</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Statistiques sur l'importation</returns>
        Task<ImportStatistics> ImportDataAsync(
            ParsingResult parsingResult, 
            Action<string, int, int> progressCallback = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Télécharge les données Joconde depuis une URL
        /// </summary>
        /// <param name="url">URL du fichier à télécharger</param>
        /// <param name="destinationPath">Chemin de destination pour le fichier</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Chemin du fichier téléchargé</returns>
        Task<string> DownloadJocondeDataAsync(string url, string destinationPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Importe les données depuis un fichier XML
        /// </summary>
        /// <param name="xmlFilePath">Chemin du fichier XML à importer</param>
        /// <param name="progressCallback">Callback pour suivre la progression</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Rapport d'importation</returns>
        Task<ImportReport> ImportFromXmlFileAsync(string xmlFilePath, Action<string, int, int> progressCallback = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Statistiques sur l'importation des données
    /// </summary>
    public class ImportStatistics
    {
        /// <summary>
        /// Nombre d'œuvres importées
        /// </summary>
        public int ArtworksImported { get; set; }

        /// <summary>
        /// Nombre d'artistes importés
        /// </summary>
        public int ArtistsImported { get; set; }

        /// <summary>
        /// Nombre de musées importés
        /// </summary>
        public int MuseumsImported { get; set; }

        /// <summary>
        /// Nombre de domaines importés
        /// </summary>
        public int DomainsImported { get; set; }

        /// <summary>
        /// Nombre de techniques importées
        /// </summary>
        public int TechniquesImported { get; set; }

        /// <summary>
        /// Nombre de périodes importées
        /// </summary>
        public int PeriodsImported { get; set; }

        /// <summary>
        /// Durée totale de l'importation
        /// </summary>
        public TimeSpan Duration { get; set; }
    }
}
