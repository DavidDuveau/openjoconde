using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Dtos;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Repository implementation for artwork entities
    /// </summary>
    public class ArtworkRepository : IArtworkRepository
    {
        private const int ArtworkHashLength = 16;

        private readonly OpenJocondeDbContext _context;
        private readonly ILogger<ArtworkRepository> _logger;

        public ArtworkRepository(
            OpenJocondeDbContext context,
            ILogger<ArtworkRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Page d'œuvres correspondant à la recherche. Les identifiants de la page sont lus d'abord, sur l'index seul :
        /// sauter 500 000 lignes en les lisant entières coûte 16 s, en ne lisant que l'index 0,3 s. Les œuvres ne sont
        /// chargées qu'ensuite.
        /// </summary>
        public async Task<PaginatedResult<ArtworkListItemDto>> SearchAsync(ArtworkSearchCriteria criteria)
        {
            var sortBy = criteria.SortBy;
            if (sortBy != null && !ArtworkSearchCriteria.SortKeys.Contains(sortBy))
                throw new InvalidArtworkSearchException($"Tri inconnu : utiliser {string.Join(", ", ArtworkSearchCriteria.SortKeys)}.");

            var page = Math.Max(criteria.Page, 1);
            var pageSize = criteria.PageSize < 1 ? 10 : Math.Min(criteria.PageSize, 100);

            if (criteria.YearFrom > criteria.YearTo)
                throw new InvalidArtworkSearchException("L'année de début doit précéder l'année de fin.");

            IQueryable<Artwork> query = _context.Artworks;
            query = ArtworkFilter.Apply(query, criteria.MuseumId, criteria.ArtistId, criteria.DomainId, criteria.TechniqueId, criteria.PeriodId,
                criteria.Region, criteria.Department, criteria.YearFrom, criteria.YearTo);
            var filtered = criteria.YearFrom.HasValue || criteria.YearTo.HasValue || criteria.MuseumId.HasValue || criteria.ArtistId.HasValue || criteria.DomainId.HasValue
                || criteria.TechniqueId.HasValue || criteria.PeriodId.HasValue
                || !string.IsNullOrWhiteSpace(criteria.Region) || !string.IsNullOrWhiteSpace(criteria.Department);

            var searching = !string.IsNullOrWhiteSpace(criteria.Search);
            if (searching)
                query = ArtworkSearch.Apply(query, criteria.Search!);

            var condition = searching ? ArtworkSearch.BuildCondition(criteria.Search!.Trim()) : null;
            if (sortBy == "relevance" && condition == null)
                throw new InvalidArtworkSearchException("Le tri par pertinence demande une recherche par mots.");
            if (sortBy == "artist" && !(searching || filtered))
                throw new InvalidArtworkSearchException("Le tri par artiste demande une recherche ou un filtre.");

            if (sortBy == "distance" && !(criteria.Latitude.HasValue && criteria.Longitude.HasValue))
                throw new InvalidArtworkSearchException("Le tri par distance demande la latitude et la longitude.");

            IQueryable<Artwork> ordered = sortBy switch
            {
                "relevance" => ArtworkOrdering.ByRelevance(query, condition!),
                "date" => ArtworkOrdering.ByDate(query),
                "date_desc" => ArtworkOrdering.ByDate(query, descending: true),
                "artist" => ArtworkOrdering.ByArtist(query),
                "distance" => ArtworkOrdering.ByDistance(query, criteria.Latitude!.Value, criteria.Longitude!.Value),
                _ => ArtworkOrdering.ByTitle(query, searching || filtered, sortBy == "title_desc")
            };

            var totalItems = await ordered.CountAsync();

            var ids = await ordered
                .Select(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var loaded = await _context.Artworks
                .AsNoTracking()
                .Where(a => ids.Contains(a.Id))
                .Select(a => new ArtworkListItemDto
                {
                    Id = a.Id,
                    Reference = a.Reference,
                    InventoryNumber = a.InventoryNumber,
                    Denomination = a.Denomination,
                    Title = a.Title,
                    Description = a.Description,
                    Dimensions = a.Dimensions,
                    CreationDate = a.CreationDate,
                    CreationPlace = a.CreationPlace,
                    ConservationPlace = a.ConservationPlace,
                    Copyright = a.Copyright,
                    ImageUrl = a.ImageUrl,
                    AssociatedSiteUrl = a.AssociatedSiteUrl,
                    ArtistUnderCopyright = a.ArtistUnderCopyright,
                    PublicDomainYear = a.PublicDomainYear,
                    HasImage = a.HasImage,
                    MuseumId = a.MuseumId,
                    UpdatedAt = a.UpdatedAt
                })
                .ToDictionaryAsync(a => a.Id);

            return new PaginatedResult<ArtworkListItemDto>
            {
                Items = ids.Where(loaded.ContainsKey).Select(id => loaded[id]).ToList(),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Fiche complète d'une œuvre, ses relations incluses
        /// </summary>
        public Task<ArtworkDetailDto?> GetDetailAsync(Guid id) =>
            _context.Artworks
                .Where(a => a.Id == id)
                .AsNoTracking()
                .Select(a => new ArtworkDetailDto
                {
                    Id = a.Id,
                    Reference = a.Reference,
                    InventoryNumber = a.InventoryNumber,
                    Denomination = a.Denomination,
                    Title = a.Title,
                    Description = a.Description,
                    Dimensions = a.Dimensions,
                    CreationDate = a.CreationDate,
                    CreationPlace = a.CreationPlace,
                    ConservationPlace = a.ConservationPlace,
                    Copyright = a.Copyright,
                    ImageUrl = a.ImageUrl,
                    AssociatedSiteUrl = a.AssociatedSiteUrl,
                    ArtistUnderCopyright = a.ArtistUnderCopyright,
                    PublicDomainYear = a.PublicDomainYear,
                    HasImage = a.HasImage,
                    Museum = a.Museum == null ? null : new NamedItemDto { Id = a.Museum.Id, Name = a.Museum.Name },
                    Artists = a.Artists
                        .Select(aa => new ArtworkArtistDto
                        {
                            Id = aa.Artist.Id,
                            FirstName = aa.Artist.FirstName,
                            LastName = aa.Artist.LastName,
                            Role = aa.Role
                        })
                        .ToList(),
                    Domains = a.Domains.Select(d => new NamedItemDto { Id = d.Id, Name = d.Name }).ToList(),
                    Techniques = a.Techniques.Select(t => new NamedItemDto { Id = t.Id, Name = t.Name }).ToList(),
                    Periods = a.Periods.Select(p => new NamedItemDto { Id = p.Id, Name = p.Name }).ToList()
                })
                .FirstOrDefaultAsync();

        /// <summary>
        /// Insère ou met à jour des œuvres, identifiées par leur référence Joconde. Les œuvres existantes du lot
        /// sont lues en une requête et tous leurs champs sont mis à jour. Le suivi du contexte est vidé ensuite :
        /// il ne doit pas grossir au fil des lots.
        /// </summary>
        /// <param name="artworks">Œuvres à enregistrer ; leur Id est remplacé par celui de la base</param>
        /// <returns>Nombre d'œuvres insérées ou mises à jour</returns>
        public async Task<int> BulkUpsertAsync(IEnumerable<Artwork> artworks)
        {
            var artworkList = artworks.ToList();
            if (artworkList.Count == 0)
                return 0;

            try
            {
                var references = artworkList.Select(a => a.Reference).Distinct().ToList();
                var tracked = await _context.Artworks
                    .Where(a => references.Contains(a.Reference))
                    .ToDictionaryAsync(a => a.Reference, StringComparer.OrdinalIgnoreCase);

                foreach (var artwork in artworkList)
                {
                    if (tracked.TryGetValue(artwork.Reference, out var existing))
                    {
                        // L'appelant s'appuie sur l'Id réel pour écrire les relations
                        artwork.Id = existing.Id;
                        existing.InventoryNumber = artwork.InventoryNumber;
                        existing.Denomination = artwork.Denomination;
                        existing.Title = artwork.Title;
                        existing.Description = artwork.Description;
                        existing.Dimensions = artwork.Dimensions;
                        existing.CreationDate = artwork.CreationDate;
                        existing.CreationPlace = artwork.CreationPlace;
                        existing.ConservationPlace = artwork.ConservationPlace;
                        existing.Copyright = artwork.Copyright;
                        existing.ImageUrl = artwork.ImageUrl;
                        existing.AssociatedSiteUrl = artwork.AssociatedSiteUrl;
                        existing.ArtistUnderCopyright = artwork.ArtistUnderCopyright;
                        existing.PublicDomainYear = artwork.PublicDomainYear;
                        existing.HasImage = artwork.HasImage;
                        existing.MuseumId = artwork.MuseumId;
                        existing.ContentHash = artwork.ContentHash;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        artwork.Id = Guid.NewGuid();
                        artwork.UpdatedAt = DateTime.UtcNow;
                        _context.Artworks.Add(artwork);
                        // Une référence répétée dans le lot met à jour l'œuvre déjà ajoutée
                        tracked[artwork.Reference] = artwork;
                    }
                }

                await _context.SaveChangesAsync();
                return artworkList.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk upserting artworks");
                throw;
            }
            finally
            {
                _context.ChangeTracker.Clear();
            }
        }

        /// <summary>
        /// Référence et empreinte de contenu de chaque œuvre en base, lues en flux
        /// </summary>
        public async Task<Dictionary<string, Guid?>> GetContentHashesAsync()
        {
            var hashes = new Dictionary<string, Guid?>(StringComparer.OrdinalIgnoreCase);
            await foreach (var row in _context.Artworks.AsNoTracking()
                .Select(a => new { a.Reference, a.ContentHash })
                .AsAsyncEnumerable())
            {
                hashes[row.Reference] = row.ContentHash is { Length: ArtworkHashLength } bytes ? new Guid(bytes) : null;
            }

            return hashes;
        }

        /// <summary>
        /// Supprime les œuvres de ces références, par groupes ; leurs liens partent avec elles (suppression en cascade)
        /// </summary>
        public async Task<int> DeleteByReferencesAsync(IReadOnlyCollection<string> references)
        {
            var deleted = 0;
            foreach (var group in references.Chunk(1000))
            {
                deleted += await _context.Artworks.Where(a => group.Contains(a.Reference)).ExecuteDeleteAsync();
            }

            return deleted;
        }
    }
}
