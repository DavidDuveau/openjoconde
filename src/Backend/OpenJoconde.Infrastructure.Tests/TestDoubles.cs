using OpenJoconde.Core.Dtos;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>Dépôt d'œuvres dont l'enregistrement groupé échoue toujours (base indisponible, valeur refusée…).</summary>
internal sealed class FailingArtworkRepository : IArtworkRepository
{
    public Task<int> BulkUpsertAsync(IEnumerable<Artwork> artworks) =>
        throw new InvalidOperationException("écriture des œuvres refusée");

    public Task<PaginatedResult<ArtworkListItemDto>> SearchAsync(ArtworkSearchCriteria criteria) => throw new NotSupportedException();
    public Task<ArtworkDetailDto?> GetDetailAsync(Guid id) => throw new NotSupportedException();
    public Task<Dictionary<string, Guid?>> GetContentHashesAsync() => Task.FromResult(new Dictionary<string, Guid?>());
    public Task<int> DeleteByReferencesAsync(IReadOnlyCollection<string> references) => Task.FromResult(0);
}

/// <summary>Dépôt de relations dont chaque écriture échoue.</summary>
internal sealed class FailingRelationsRepository : IArtworkRelationsRepository
{
    public Task<int> SaveBatchAsync(IReadOnlyCollection<ArtworkRelationSet> relations) =>
        throw new InvalidOperationException("écriture des relations refusée");

    public Task<bool> DeleteArtworkRelationsAsync(Guid artworkId) => Task.FromResult(true);
}
