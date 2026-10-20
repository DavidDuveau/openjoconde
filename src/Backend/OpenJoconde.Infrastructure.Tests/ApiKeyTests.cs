using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenJoconde.API.Security;

namespace OpenJoconde.Infrastructure.Tests;

public class ApiKeyTests
{
    private static async Task<IActionResult?> AutoriserAsync(string? configuree, string? fournie)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [RequireApiKeyAttribute.ConfigurationKey] = configuree })
            .Build();
        var http = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton<IConfiguration>(configuration).BuildServiceProvider() };
        if (fournie != null)
            http.Request.Headers[RequireApiKeyAttribute.HeaderName] = fournie;

        var context = new AuthorizationFilterContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>());
        await new RequireApiKeyAttribute().OnAuthorizationAsync(context);
        return context.Result;
    }

    [Fact]
    public async Task CleCorrecte_Autorise() => Assert.Null(await AutoriserAsync("secret", "secret"));

    [Theory]
    [InlineData("mauvaise")]
    [InlineData("")]
    [InlineData(null)]
    public async Task CleAbsenteOuFausse_Refuse(string? fournie) =>
        Assert.IsType<UnauthorizedResult>(await AutoriserAsync("secret", fournie));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task CleNonConfiguree_RefuseMemeSiL_appelantEnEnvoieUne(string? configuree) =>
        Assert.IsType<UnauthorizedResult>(await AutoriserAsync(configuree, configuree ?? ""));

    [Fact]
    public void LesRoutesD_ecritureSontProtegees()
    {
        var routes = typeof(RequireApiKeyAttribute).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t))
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes(typeof(HttpPostAttribute), true).Any()
                     || m.GetCustomAttributes(typeof(HttpPutAttribute), true).Any()
                     || m.GetCustomAttributes(typeof(HttpDeleteAttribute), true).Any());

        Assert.All(routes, m => Assert.True(
            m.GetCustomAttributes(typeof(RequireApiKeyAttribute), true).Any() || m.DeclaringType!.GetCustomAttributes(typeof(RequireApiKeyAttribute), true).Any(),
            $"{m.DeclaringType!.Name}.{m.Name} n'exige pas la clé d'API"));
    }
}
