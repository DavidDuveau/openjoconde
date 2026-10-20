using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
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
