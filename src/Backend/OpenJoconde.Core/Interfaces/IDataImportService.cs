using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Service for importing data from Joconde XML files
    /// </summary>
    public interface IDataImportService
    {
        /// <summary>
        /// Import data from a Joconde XML file
        /// </summary>
        /// <param name="xmlFilePath">Path to the XML file</param>
        /// <returns>Number of records imported</returns>
        Task<int> ImportFromXmlFileAsync(string xmlFilePath);

        /// <summary>
        /// Download the latest Joconde XML file from data.gouv.fr
        /// </summary>
        /// <param name="destinationPath">Path where to save the file</param>
        /// <returns>Path of the downloaded file</returns>
        Task<string> DownloadLatestXmlFileAsync(string destinationPath);

        /// <summary>
        /// Run the complete import process: download the latest file and import it
        /// </summary>
        /// <param name="downloadPath">Path where to save the downloaded file</param>
        /// <returns>Number of records imported</returns>
        Task<int> RunImportProcessAsync(string downloadPath);
    }
}
