using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Implémentation du repository pour la gestion des relations entre les œuvres et les autres entités.
    /// Les relations d'un lot d'œuvres sont écrites par SqlBulkCopy dans une seule transaction.
    /// </summary>
    public class ArtworkRelationsRepository : IArtworkRelationsRepository
    {
        // ArtworkArtist est une entité explicite ; les autres tables de jonction sont générées par EF
        // (relations many-to-many) et suivent sa convention de nommage : ArtworksId, DomainsId, etc.
        private const string ArtworkIdColumn = "ArtworkId";
        private const string JoinArtworkIdColumn = "ArtworksId";

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
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

                var artworkIds = string.Join(',', relations.Select(r => r.ArtworkId));
                await DeleteRelationsAsync(connection, transaction, "ArtworkArtist", ArtworkIdColumn, artworkIds);
                await DeleteRelationsAsync(connection, transaction, "ArtworkDomain", JoinArtworkIdColumn, artworkIds);
                await DeleteRelationsAsync(connection, transaction, "ArtworkTechnique", JoinArtworkIdColumn, artworkIds);
                await DeleteRelationsAsync(connection, transaction, "ArtworkPeriod", JoinArtworkIdColumn, artworkIds);

                var written = 0;

                var artists = NewTable(ArtworkIdColumn, "ArtistId", "Role");
                foreach (var set in relations)
                {
                    foreach (var link in set.Artists.GroupBy(a => a.ArtistId).Select(g => g.First()))
                    {
                        artists.Rows.Add(set.ArtworkId, link.ArtistId, (object?)link.Role ?? DBNull.Value);
                    }
                }
                written += await BulkInsertAsync(connection, transaction, "ArtworkArtist", artists);

                written += await BulkInsertJoinAsync(connection, transaction, "ArtworkDomain", "DomainsId", relations, r => r.DomainIds);
                written += await BulkInsertJoinAsync(connection, transaction, "ArtworkTechnique", "TechniquesId", relations, r => r.TechniqueIds);
                written += await BulkInsertJoinAsync(connection, transaction, "ArtworkPeriod", "PeriodsId", relations, r => r.PeriodIds);

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
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

                var id = artworkId.ToString();
                await DeleteRelationsAsync(connection, transaction, "ArtworkArtist", ArtworkIdColumn, id);
                await DeleteRelationsAsync(connection, transaction, "ArtworkDomain", JoinArtworkIdColumn, id);
                await DeleteRelationsAsync(connection, transaction, "ArtworkTechnique", JoinArtworkIdColumn, id);
                await DeleteRelationsAsync(connection, transaction, "ArtworkPeriod", JoinArtworkIdColumn, id);

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression des relations pour l'œuvre {ArtworkId}", artworkId);
                throw;
            }
        }

        private static DataTable NewTable(params string[] columns)
        {
            var table = new DataTable();
            foreach (var column in columns)
            {
                table.Columns.Add(column, column == "Role" ? typeof(string) : typeof(Guid));
            }
            return table;
        }

        private static Task<int> BulkInsertJoinAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            string tableName,
            string foreignKeyName,
            IReadOnlyCollection<ArtworkRelationSet> relations,
            Func<ArtworkRelationSet, IEnumerable<Guid>> selectIds)
        {
            var table = NewTable(JoinArtworkIdColumn, foreignKeyName);
            foreach (var set in relations)
            {
                foreach (var id in selectIds(set).Distinct())
                {
                    table.Rows.Add(set.ArtworkId, id);
                }
            }

            return BulkInsertAsync(connection, transaction, tableName, table);
        }

        private static async Task<int> BulkInsertAsync(SqlConnection connection, SqlTransaction transaction, string tableName, DataTable table)
        {
            if (table.Rows.Count == 0)
                return 0;

            using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
            {
                DestinationTableName = tableName
            };
            foreach (DataColumn column in table.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }

            await bulkCopy.WriteToServerAsync(table);
            return table.Rows.Count;
        }

        /// <summary>
        /// Supprime les relations des œuvres listées (identifiants séparés par des virgules)
        /// </summary>
        private static async Task DeleteRelationsAsync(SqlConnection connection, SqlTransaction transaction, string tableName, string artworkIdColumn, string artworkIds)
        {
            await using var command = new SqlCommand(
                $"DELETE FROM {tableName} WHERE {artworkIdColumn} IN (SELECT CAST(value AS uniqueidentifier) FROM STRING_SPLIT(@ArtworkIds, ','))",
                connection,
                transaction);
            command.Parameters.AddWithValue("@ArtworkIds", artworkIds);
            await command.ExecuteNonQueryAsync();
        }
    }
}
