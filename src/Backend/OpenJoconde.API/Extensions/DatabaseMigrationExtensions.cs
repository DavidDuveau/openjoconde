using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenJoconde.Infrastructure.Data;

namespace OpenJoconde.API.Extensions
{
    /// <summary>
    /// Migrations appliquées au démarrage (réglage <c>Database:MigrateOnStartup</c>, désactivé par défaut).
    /// Deux instances qui démarrent ensemble ne se gênent pas : <see cref="DatabaseMigrator"/> migre sous un verrou consultatif PostgreSQL.
    /// </summary>
    public static class DatabaseMigrationExtensions
    {
        /// <summary>Clé du réglage qui active les migrations au démarrage</summary>
        public const string SettingName = "Database:MigrateOnStartup";

        /// <summary>
        /// Applique les migrations en attente si le réglage est actif ; une panne arrête le démarrage plutôt que de servir un schéma incomplet.
        /// À appeler avant <c>app.Run()</c> : les services d'arrière-plan, qui lisent la base, démarrent après.
        /// </summary>
        public static async Task MigrateDatabaseIfConfiguredAsync(this WebApplication app)
        {
            if (!app.Configuration.GetValue<bool>(SettingName))
                return;

            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<OpenJocondeDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("OpenJoconde.Migrations");

            logger.LogInformation("Application des migrations au démarrage");
            await DatabaseMigrator.MigrateAsync(context);
            logger.LogInformation("Schéma à jour");
        }
    }
}
