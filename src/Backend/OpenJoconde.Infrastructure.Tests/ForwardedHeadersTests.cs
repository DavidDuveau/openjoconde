using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>En-têtes <c>X-Forwarded-*</c> derrière un reverse proxy, et limitation de débit par visiteur (TECH-51)</summary>
public class ForwardedHeadersTests
{
    private const int Limit = 20;

    /// <summary>Le serveur de test n'a pas d'adresse distante : on lui donne celle du pair TCP</summary>
    private sealed class RemoteAddressFilter(string address) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(address);
                return nextMiddleware();
            });
            next(app);
        };
    }

    private static ApiFactory FactoryBehind(string peerAddress, Action<Dictionary<string, string?>>? settings = null)
    {
        var factory = new ApiFactory
        {
            Overrides = services => services.AddSingleton<IStartupFilter>(new RemoteAddressFilter(peerAddress))
        };
        settings?.Invoke(factory.Settings);
        return factory;
    }

    /// <summary>Une écriture refusée (400) : la limitation de débit s'applique avant le contrôleur</summary>
    private static async Task<HttpStatusCode> WriteAsVisitor(HttpClient client, string visitor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/museums") { Content = JsonContent.Create(new { }) };
        request.Headers.Add("X-Forwarded-For", visitor);
        return (await client.SendAsync(request)).StatusCode;
    }

    private static async Task ExhaustLimitAsync(HttpClient client, Func<int, string> visitor)
    {
        for (var i = 0; i < Limit; i++)
            Assert.NotEqual(HttpStatusCode.TooManyRequests, await WriteAsVisitor(client, visitor(i)));
    }

    [Fact]
    public async Task ProxyDeConfiance_LaLimiteSApplique_ParAdresseDuVisiteur()
    {
        await using var factory = FactoryBehind("10.0.0.1", s => s["ForwardedHeaders:KnownProxies:0"] = "10.0.0.1");
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        await ExhaustLimitAsync(client, _ => "203.0.113.5");

        Assert.Equal(HttpStatusCode.TooManyRequests, await WriteAsVisitor(client, "203.0.113.5"));
        // Un autre visiteur derrière le même proxy garde son quota
        Assert.NotEqual(HttpStatusCode.TooManyRequests, await WriteAsVisitor(client, "203.0.113.6"));
    }

    [Fact]
    public async Task PlageDeConfiance_LEnTeteEstLu()
    {
        await using var factory = FactoryBehind("10.1.2.3", s => s["ForwardedHeaders:KnownNetworks:0"] = "10.1.0.0/16");
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        await ExhaustLimitAsync(client, _ => "203.0.113.5");

        Assert.Equal(HttpStatusCode.TooManyRequests, await WriteAsVisitor(client, "203.0.113.5"));
        Assert.NotEqual(HttpStatusCode.TooManyRequests, await WriteAsVisitor(client, "203.0.113.6"));
    }

    [Fact]
    public async Task ProxyInconnu_LEnTeteEstIgnore_LaLimiteResteSurLePair()
    {
        await using var factory = FactoryBehind("10.0.0.9", s => s["ForwardedHeaders:KnownProxies:0"] = "10.0.0.1");
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        // Une adresse différente à chaque requête ne contourne pas la limite
        await ExhaustLimitAsync(client, i => $"203.0.113.{i + 10}");

        Assert.Equal(HttpStatusCode.TooManyRequests, await WriteAsVisitor(client, "203.0.113.99"));
    }

    [Fact]
    public async Task SansProxyDeclare_LEnTeteEstIgnore()
    {
        await using var factory = FactoryBehind("10.0.0.1");
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        await ExhaustLimitAsync(client, i => $"203.0.113.{i + 10}");

        Assert.Equal(HttpStatusCode.TooManyRequests, await WriteAsVisitor(client, "203.0.113.99"));
    }
}
