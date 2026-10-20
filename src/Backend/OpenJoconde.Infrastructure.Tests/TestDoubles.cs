using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>Dépôt d'œuvres dont l'enregistrement groupé échoue toujours (base indisponible, valeur refusée…).</summary>
internal sealed class FailingArtworkRepository : IArtworkRepository
{
    public Task<int> BulkUpsertAsync(IEnumerable<Artwork> artworks) =>
        throw new InvalidOperationException("écriture des œuvres refusée");
}

/// <summary>Dépôt de relations dont chaque écriture échoue.</summary>
internal sealed class FailingRelationsRepository : IArtworkRelationsRepository
{
    public Task<int> SaveBatchAsync(IReadOnlyCollection<ArtworkRelationSet> relations) =>
        throw new InvalidOperationException("écriture des relations refusée");

    public Task<bool> DeleteArtworkRelationsAsync(Guid artworkId) => Task.FromResult(true);
}
