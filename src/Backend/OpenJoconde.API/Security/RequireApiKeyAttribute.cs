using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OpenJoconde.API.Security
{
    /// <summary>
    /// Réserve une route d'écriture ou d'administration aux appelants qui présentent la clé d'API
    /// configurée (<c>Security:ApiKey</c>) dans l'en-tête <c>X-Api-Key</c>.
    /// Sans clé configurée, la route est refusée : on ne laisse jamais une route d'écriture ouverte par défaut.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireApiKeyAttribute : Attribute, IAsyncAuthorizationFilter
    {
        /// <summary>En-tête HTTP qui porte la clé d'API</summary>
        public const string HeaderName = "X-Api-Key";
        /// <summary>Clé de configuration qui porte la clé d'API attendue</summary>
        public const string ConfigurationKey = "Security:ApiKey";

        /// <summary>Refuse la requête (401) si la clé d'API est absente, fausse ou non configurée</summary>
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var services = context.HttpContext.RequestServices;
            var expected = (services.GetService(typeof(IConfiguration)) as IConfiguration)?[ConfigurationKey];

            if (string.IsNullOrWhiteSpace(expected))
            {
                (services.GetService(typeof(ILogger<RequireApiKeyAttribute>)) as ILogger)?
                    .LogWarning("Route protégée appelée alors que {Key} n'est pas configurée : refusée", ConfigurationKey);
                context.Result = new UnauthorizedResult();
                return Task.CompletedTask;
            }

            var provided = context.HttpContext.Request.Headers[HeaderName].ToString();
            if (!SameKey(provided, expected))
            {
                context.Result = new UnauthorizedResult();
            }

            return Task.CompletedTask;
        }

        // Comparaison en temps constant, sur des empreintes de longueur fixe
        private static bool SameKey(string provided, string expected) =>
            CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(provided)),
                SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
    }
}
