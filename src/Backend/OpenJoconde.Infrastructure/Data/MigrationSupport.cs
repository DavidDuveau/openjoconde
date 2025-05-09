using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace OpenJoconde.Infrastructure.Data
{
    /// <summary>
    /// Classe utilisée pour générer les migrations SQL Server.
    /// Note: Cette classe n'est pas utilisée en production, seulement pour la génération des migrations.
    /// </summary>
    public class OpenJocondeDbContextFactory : IDesignTimeDbContextFactory<OpenJocondeDbContext>
    {
        public OpenJocondeDbContext CreateDbContext(string[] args)
        {
            // Créer un chemin absolu vers le fichier appsettings.json
            var baseDirectory = Directory.GetCurrentDirectory();
            var configPath = Path.Combine(baseDirectory, "appsettings.json");
            
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(configPath, optional: false)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<OpenJocondeDbContext>();
            optionsBuilder.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()
            );

            return new OpenJocondeDbContext(optionsBuilder.Options);
        }
    }
}
