using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace OpenJoconde.Infrastructure.Data
{
    /// <summary>
    /// Applique les migrations sous un verrou consultatif PostgreSQL : deux instances qui démarrent ensemble
    /// ne migrent pas en même temps (la seconde attend, puis ne trouve plus rien à appliquer).
    /// </summary>
    public static class DatabaseMigrator
    {
        // Distinct de la clé de la synchronisation (SyncCoordinator)
        private const long LockKey = 0x4F4A4D47;

        /// <summary>Attend le verrou, applique les migrations en attente, libère le verrou</summary>
        public static async Task MigrateAsync(OpenJocondeDbContext context, CancellationToken cancellationToken = default)
        {
            // Le verrou est tenu par la session : la connexion reste ouverte pendant toute la migration
            await context.Database.OpenConnectionAsync(cancellationToken);
            try
            {
                await context.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_lock({LockKey})", cancellationToken);
                try
                {
                    await context.Database.MigrateAsync(cancellationToken);
                }
                finally
                {
                    await context.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_unlock({LockKey})", CancellationToken.None);
                }
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }
}
