using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Infrastructure.Data;
using OpenJoconde.Infrastructure.Services;

namespace OpenJoconde.API.Extensions
{
    /// <summary>
    /// Extensions pour la configuration des services dans le conteneur d'injection de dépendances
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configure les services d'infrastructure pour l'application
        /// </summary>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Repositories
            services.AddScoped<IArtworkRepository, ArtworkRepository>();
            services.AddScoped<IArtistRepository, ArtistRepository>();
            services.AddScoped<IMuseumRepository, MuseumRepository>();
            services.AddScoped<IDomainRepository, DomainRepository>();
            services.AddScoped<ITechniqueRepository, TechniqueRepository>();
            services.AddScoped<IPeriodRepository, PeriodRepository>();
            
            // Configuration de la chaîne de connexion pour le repository des relations
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new System.InvalidOperationException("La chaîne de connexion 'DefaultConnection' n'est pas configurée");
            services.AddScoped<IArtworkRelationsRepository>(provider => 
                new ArtworkRelationsRepository(
                    connectionString, 
                    provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ArtworkRelationsRepository>>()));

            // Services
            services.AddScoped<IJocondeDataService, JocondeDataService>();
            services.AddScoped<IJocondeXmlParser, JocondeXmlParserService>();
            // Utiliser l'implémentation avec streaming pour éviter les OutOfMemoryException
            services.AddScoped<IJocondeJsonParser, StreamingJocondeJsonParserService>();
            services.AddScoped<IDataImportService, AdvancedDataImportService>();
            services.AddScoped<BatchProcessingService>();

            return services;
        }
    }
}
