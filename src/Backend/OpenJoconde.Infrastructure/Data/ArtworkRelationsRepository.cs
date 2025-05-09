using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Data
{
    /// <summary>
    /// Implémentation du repository pour la gestion des relations entre les œuvres et les autres entités
    /// </summary>
    public class ArtworkRelationsRepository : IArtworkRelationsRepository
    {
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
        /// Crée ou met à jour les relations entre une œuvre et ses artistes
        /// </summary>
        public async Task<int> SaveArtworkArtistRelationsAsync(Guid artworkId, IEnumerable<ArtworkArtist> relations)
        {
            try
            {
                // D'abord, supprimer les relations existantes
                await DeleteRelationsAsync("ArtworkArtist", artworkId);

                // Ensuite, insérer les nouvelles relations
                var count = 0;
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    foreach (var relation in relations)
                    {
                        using (var command = new SqlCommand(
                            "INSERT INTO ArtworkArtist (ArtworkId, ArtistId, Role) VALUES (@ArtworkId, @ArtistId, @Role)",
                            connection))
                        {
                            command.Parameters.AddWithValue("@ArtworkId", artworkId);
                            command.Parameters.AddWithValue("@ArtistId", relation.Artist.Id);
                            command.Parameters.AddWithValue("@Role", relation.Role ?? (object)DBNull.Value);
                            
                            await command.ExecuteNonQueryAsync();
                            count++;
                        }
                    }
                }

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la sauvegarde des relations œuvre-artiste pour l'œuvre {ArtworkId}", artworkId);
                throw;
            }
        }

        /// <summary>
        /// Crée ou met à jour les relations entre une œuvre et ses domaines
        /// </summary>
        public async Task<int> SaveArtworkDomainRelationsAsync(Guid artworkId, IEnumerable<Guid> domainIds)
        {
            try
            {
                return await SaveSimpleRelationsAsync("ArtworkDomain", artworkId, "DomainId", domainIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la sauvegarde des relations œuvre-domaine pour l'œuvre {ArtworkId}", artworkId);
                throw;
            }
        }

        /// <summary>
        /// Crée ou met à jour les relations entre une œuvre et ses techniques
        /// </summary>
        public async Task<int> SaveArtworkTechniqueRelationsAsync(Guid artworkId, IEnumerable<Guid> techniqueIds)
        {
            try
            {
                return await SaveSimpleRelationsAsync("ArtworkTechnique", artworkId, "TechniqueId", techniqueIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la sauvegarde des relations œuvre-technique pour l'œuvre {ArtworkId}", artworkId);
                throw;
            }
        }

        /// <summary>
        /// Crée ou met à jour les relations entre une œuvre et ses périodes
        /// </summary>
        public async Task<int> SaveArtworkPeriodRelationsAsync(Guid artworkId, IEnumerable<Guid> periodIds)
        {
            try
            {
                return await SaveSimpleRelationsAsync("ArtworkPeriod", artworkId, "PeriodId", periodIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la sauvegarde des relations œuvre-période pour l'œuvre {ArtworkId}", artworkId);
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
                await DeleteRelationsAsync("ArtworkArtist", artworkId);
                await DeleteRelationsAsync("ArtworkDomain", artworkId);
                await DeleteRelationsAsync("ArtworkTechnique", artworkId);
                await DeleteRelationsAsync("ArtworkPeriod", artworkId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression des relations pour l'œuvre {ArtworkId}", artworkId);
                throw;
            }
        }

        /// <summary>
        /// Méthode utilitaire pour sauvegarder les relations simples (sans attributs supplémentaires)
        /// </summary>
        private async Task<int> SaveSimpleRelationsAsync(string tableName, Guid artworkId, string foreignKeyName, IEnumerable<Guid> foreignKeys)
        {
            // D'abord, supprimer les relations existantes
            await DeleteRelationsAsync(tableName, artworkId);

            // Ensuite, insérer les nouvelles relations
            var count = 0;
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                foreach (var foreignKey in foreignKeys)
                {
                    using (var command = new SqlCommand(
                        $"INSERT INTO {tableName} (ArtworkId, {foreignKeyName}) VALUES (@ArtworkId, @ForeignKey)",
                        connection))
                    {
                        command.Parameters.AddWithValue("@ArtworkId", artworkId);
                        command.Parameters.AddWithValue("@ForeignKey", foreignKey);
                        
                        await command.ExecuteNonQueryAsync();
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Méthode utilitaire pour supprimer les relations existantes
        /// </summary>
        private async Task DeleteRelationsAsync(string tableName, Guid artworkId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                using (var command = new SqlCommand($"DELETE FROM {tableName} WHERE ArtworkId = @ArtworkId", connection))
                {
                    command.Parameters.AddWithValue("@ArtworkId", artworkId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
