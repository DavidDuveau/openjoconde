using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>Dépôt d'œuvres dont l'enregistrement groupé échoue toujours (base indisponible, valeur refusée…).</summary>
internal sealed class FailingArtworkRepository : IArtworkRepository
{
    public Task<int> BulkUpsertAsync(IEnumerable<Artwork> artworks) =>
        throw new InvalidOperationException("écriture des œuvres refusée");

    public Task<IEnumerable<Artwork>> GetAllAsync(int page = 1, int pageSize = 10) => throw new NotSupportedException();
    public Task<Artwork> GetByIdAsync(Guid id) => throw new NotSupportedException();
    public Task<Artwork> GetByReferenceAsync(string reference) => throw new NotSupportedException();
    public Task<IEnumerable<Artwork>> SearchAsync(string searchText = null, Guid? artistId = null, Guid? domainId = null, Guid? techniqueId = null, Guid? periodId = null, Guid? museumId = null, int page = 1, int pageSize = 10) => throw new NotSupportedException();
    public Task<Artwork> AddAsync(Artwork artwork) => throw new NotSupportedException();
    public Task<bool> UpdateAsync(Artwork artwork) => throw new NotSupportedException();
    public Task<bool> DeleteAsync(Guid id) => throw new NotSupportedException();
    public Task<int> GetCountAsync() => throw new NotSupportedException();
}

/// <summary>Dépôt de relations dont chaque écriture échoue.</summary>
internal sealed class FailingRelationsRepository : IArtworkRelationsRepository
{
    public Task<int> SaveArtworkArtistRelationsAsync(Guid artworkId, IEnumerable<ArtworkArtist> relations) => Fail();
    public Task<int> SaveArtworkDomainRelationsAsync(Guid artworkId, IEnumerable<Guid> domainIds) => Fail();
    public Task<int> SaveArtworkTechniqueRelationsAsync(Guid artworkId, IEnumerable<Guid> techniqueIds) => Fail();
    public Task<int> SaveArtworkPeriodRelationsAsync(Guid artworkId, IEnumerable<Guid> periodIds) => Fail();
    public Task<bool> DeleteArtworkRelationsAsync(Guid artworkId) => Task.FromResult(true);

    private static Task<int> Fail() => throw new InvalidOperationException("écriture des relations refusée");
}
