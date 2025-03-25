using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Infrastructure.Services;
using System;
using System.Net.Http;

namespace OpenJoconde.API.Extensions
{
    /// <summary>
    /// Extensions pour la configuration des HttpClient
    /// </summary>
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Configure un HttpClient avec un timeout étendu pour les téléchargements volumineux
        /// </summary>
        public static IHttpClientBuilder AddJocondeHttpClient(this IServiceCollection services)
        {
            return services.AddHttpClient<IJocondeDataService, JocondeDataService>(client =>
            {
                // Timeout étendu à 10 minutes pour les téléchargements volumineux
                client.Timeout = TimeSpan.FromMinutes(10);
            });
        }
    }
}
