using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Bilan d'un lot d'enrichissement
    /// </summary>
    /// <param name="Examined">Artistes traités (rapprochés ou non)</param>
    /// <param name="Enriched">Artistes dont la biographie a été trouvée</param>
    /// <param name="Failed">Artistes laissés pour une prochaine fois : Wikidata ou Wikipédia n'a pas répondu</param>
    /// <param name="Aborted">Le lot a été interrompu par des pannes répétées</param>
    public sealed record ArtistEnrichmentReport(int Examined, int Enriched, int Failed, bool Aborted);

    /// <summary>
    /// Complète les artistes avec Wikidata et Wikipédia (biographie, nationalité, courant artistique). Le rapprochement est
    /// strict : au moindre doute, l'artiste reste sans biographie plutôt que d'en recevoir celle d'un homonyme.
    /// </summary>
    public interface IArtistEnrichmentService
    {
        /// <summary>
        /// Traite un lot d'artistes jamais examinés, les plus riches en œuvres d'abord. Un artiste examiné sans
        /// correspondance n'est pas réexaminé.
        /// </summary>
        Task<ArtistEnrichmentReport> EnrichBatchAsync(CancellationToken cancellationToken = default);
    }
}
