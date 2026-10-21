using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Implémentation du repository pour la gestion des relations entre les œuvres et les autres entités.
    /// Les relations d'un lot d'œuvres sont écrites par COPY binaire dans une seule transaction.
    /// </summary>
    public class ArtworkRelationsRepository : IArtworkRelationsRepository
    {
        // artwork_artist est une entité explicite ; les autres tables de jonction sont générées par EF
        // (relations many-to-many) et suivent sa convention de nommage : artworks_id, domains_id, etc.
        private const string ArtworkIdColumn = "artwork_id";
        private const string JoinArtworkIdColumn = "artworks_id";

        // Un lot de 500 œuvres peut dépasser les 30 secondes par défaut quand les tables comptent des millions de lignes
        private const int TimeoutSeconds = 600;

        private readonly string _connectionString;
        private readonly ILogger<ArtworkRelationsRepository> _logger;

        public ArtworkRelationsRepository(
            string connectionString,
            ILogger<ArtworkRelationsRepository> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        /// <summary>
        /// Remplace les relations d'un groupe d'œuvres, en une seule transaction
        /// </summary>
        public async Task<int> SaveBatchAsync(IReadOnlyCollection<ArtworkRelationSet> relations)
        {
            if (relations == null || relations.Count == 0)
                return 0;

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                await using var transaction = await connection.BeginTransactionAsync();

                await DeleteAllRelationsAsync(connection, relations.Select(r => r.ArtworkId));

                var written = 0;

                var artists = new List<object?[]>();
                foreach (var set in relations)
                {
                    foreach (var link in set.Artists.GroupBy(a => a.ArtistId).Select(g => g.First()))
                    {
                        artists.Add(new object?[] { set.ArtworkId, link.ArtistId, link.Role });
                    }
                }
                written += await CopyAsync(connection, "artwork_artist", new[] { ArtworkIdColumn, "artist_id", "role" }, artists);

                written += await CopyJoinAsync(connection, "artwork_domain", "domains_id", relations, r => r.DomainIds);
                written += await CopyJoinAsync(connection, "artwork_technique", "techniques_id", relations, r => r.TechniqueIds);
                written += await CopyJoinAsync(connection, "artwork_period", "periods_id", relations, r => r.PeriodIds);

                await transaction.CommitAsync();
                return written;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la sauvegarde des relations de {Count} œuvres", relations.Count);
                throw;
            }
        }

        /// <summary>
        /// Supprime toutes les relations pour une œuvre
        /// </summary>
        public async Task<bool> DeleteArtworkRelationsAsync(Guid artworkId)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                await using var transaction = await connection.BeginTransactionAsync();

                await DeleteAllRelationsAsync(connection, new[] { artworkId });

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression des relations pour l'œuvre {ArtworkId}", artworkId);
                throw;
            }
        }

        private static async Task DeleteAllRelationsAsync(NpgsqlConnection connection, IEnumerable<Guid> artworkIds)
        {
            await CreateArtworkIdTableAsync(connection, artworkIds);
            await DeleteRelationsAsync(connection, "artwork_artist", ArtworkIdColumn);
            await DeleteRelationsAsync(connection, "artwork_domain", JoinArtworkIdColumn);
            await DeleteRelationsAsync(connection, "artwork_technique", JoinArtworkIdColumn);
            await DeleteRelationsAsync(connection, "artwork_period", JoinArtworkIdColumn);
        }

        private static Task<int> CopyJoinAsync(
            NpgsqlConnection connection,
            string tableName,
            string foreignKeyName,
            IReadOnlyCollection<ArtworkRelationSet> relations,
            Func<ArtworkRelationSet, IEnumerable<Guid>> selectIds)
        {
            var rows = new List<object?[]>();
            foreach (var set in relations)
            {
                foreach (var id in selectIds(set).Distinct())
                {
                    rows.Add(new object?[] { set.ArtworkId, id });
                }
            }

            return CopyAsync(connection, tableName, new[] { JoinArtworkIdColumn, foreignKeyName }, rows);
        }

        /// <summary>
        /// Écrit les lignes par COPY binaire ; un Guid devient un uuid, une chaîne du texte, null une valeur absente
        /// </summary>
        private static async Task<int> CopyAsync(NpgsqlConnection connection, string tableName, string[] columns, List<object?[]> rows)
        {
            if (rows.Count == 0)
                return 0;

            await using var writer = await connection.BeginBinaryImportAsync(
                $"COPY {tableName} ({string.Join(", ", columns)}) FROM STDIN (FORMAT BINARY)");
            foreach (var row in rows)
            {
                await writer.StartRowAsync();
                foreach (var value in row)
                {
                    switch (value)
                    {
                        case null:
                            await writer.WriteNullAsync();
                            break;
                        case Guid guid:
                            await writer.WriteAsync(guid, NpgsqlDbType.Uuid);
                            break;
                        default:
                            await writer.WriteAsync((string)value, NpgsqlDbType.Text);
                            break;
                    }
                }
            }

            await writer.CompleteAsync();
            return rows.Count;
        }

        /// <summary>
        /// Crée la table temporaire artwork_ids avec les œuvres à traiter, pour que les suppressions passent par une
        /// jointure sur la clé des tables de relations plutôt que par un balayage. Elle disparaît avec la transaction.
        /// </summary>
        private static async Task CreateArtworkIdTableAsync(NpgsqlConnection connection, IEnumerable<Guid> artworkIds)
        {
            await using (var create = new NpgsqlCommand("CREATE TEMP TABLE artwork_ids (id uuid NOT NULL PRIMARY KEY) ON COMMIT DROP", connection))
            {
                create.CommandTimeout = TimeoutSeconds;
                await create.ExecuteNonQueryAsync();
            }

            await CopyAsync(connection, "artwork_ids", new[] { "id" }, artworkIds.Distinct().Select(id => new object?[] { id }).ToList());
        }

        /// <summary>
        /// Supprime les relations des œuvres de artwork_ids
        /// </summary>
        private static async Task DeleteRelationsAsync(NpgsqlConnection connection, string tableName, string artworkIdColumn)
        {
            await using var command = new NpgsqlCommand(
                $"DELETE FROM {tableName} AS relation USING artwork_ids AS selected WHERE selected.id = relation.{artworkIdColumn}",
                connection);
            command.CommandTimeout = TimeoutSeconds;
            await command.ExecuteNonQueryAsync();
        }
    }
}
