using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Enrichit les musées avec la base Muséofile (répertoire des Musées de France, data.gouv.fr) : adresse, code postal,
    /// téléphone, site Internet, histoire. Le rapprochement se fait par le code Muséofile du musée (M0569).
    /// </summary>
    public interface IMuseofileService
    {
        /// <summary>
        /// Télécharge le fichier Muséofile et l'applique
        /// </summary>
        Task<MuseofileReport> EnrichFromUrlAsync(string url, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applique un fichier Muséofile local (CSV, séparateur « | »)
        /// </summary>
        Task<MuseofileReport> EnrichFromFileAsync(string csvFilePath, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Bilan d'un enrichissement
    /// </summary>
    /// <param name="Entries">Musées lus dans le fichier Muséofile</param>
    /// <param name="Matched">Musées de la base retrouvés dans le fichier par leur code</param>
    /// <param name="Updated">Musées dont au moins un champ a changé</param>
    /// <param name="Unmatched">Musées de la base sans correspondance dans le fichier</param>
    public sealed record MuseofileReport(int Entries, int Matched, int Updated, int Unmatched);
}
