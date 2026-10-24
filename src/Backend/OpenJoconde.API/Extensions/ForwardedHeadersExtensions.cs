using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;

namespace OpenJoconde.API.Extensions
{
    /// <summary>
    /// Prise en compte des en-têtes <c>X-Forwarded-For</c> et <c>X-Forwarded-Proto</c> d'un reverse proxy (section « ForwardedHeaders »).
    /// Sans proxy déclaré, les en-têtes sont ignorés : n'importe quel visiteur pourrait sinon forger son adresse
    /// et contourner la limitation de débit.
    /// </summary>
    public static class ForwardedHeadersExtensions
    {
        /// <summary>Nom de la section de configuration</summary>
        public const string SectionName = "ForwardedHeaders";

        /// <summary>
        /// Active la lecture des en-têtes transmis par les proxys de confiance (<c>KnownProxies</c> : adresses, <c>KnownNetworks</c> : plages CIDR).
        /// À placer avant la limitation de débit et la redirection HTTPS ; sans effet si aucun proxy n'est déclaré.
        /// </summary>
        public static WebApplication UseTrustedForwardedHeaders(this WebApplication app)
        {
            var section = app.Configuration.GetSection(SectionName);
            var proxies = section.GetSection("KnownProxies").Get<string[]>() ?? Array.Empty<string>();
            var networks = section.GetSection("KnownNetworks").Get<string[]>() ?? Array.Empty<string>();
            if (proxies.Length == 0 && networks.Length == 0)
                return app;

            var options = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            // Par défaut seul le bouclage est de confiance : on le remplace par la liste déclarée
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            foreach (var proxy in proxies)
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            foreach (var network in networks)
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));

            app.UseForwardedHeaders(options);
            return app;
        }
    }
}
