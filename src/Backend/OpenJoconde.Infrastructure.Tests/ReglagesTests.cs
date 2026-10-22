using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenJoconde.Infrastructure.Configuration;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Réglages des synchronisations : une seule section « Sync », contrôlée au démarrage (TECH-15).
/// </summary>
public class ReglagesTests
{
    private static SyncOptions Valide() => new() { SourceUrl = "https://exemple.test/joconde.csv" };

    private static ValidateOptionsResult Valider(SyncOptions options) => new SyncOptionsValidator().Validate(null, options);

    [Fact]
    public void DesReglagesCorrects_SontAcceptes()
    {
        var options = Valide();
        options.MuseofileSourceUrl = "http://exemple.test/museofile.csv";
        options.IntervalHours = 6;

        Assert.True(Valider(options).Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("pas une adresse")]
    [InlineData("ftp://exemple.test/joconde.csv")]
    public void UneSourceJocondeAbsenteOuNonHttp_EstRefusee(string url)
    {
        var options = Valide();
        options.SourceUrl = url;

        var resultat = Valider(options);

        Assert.True(resultat.Failed);
        Assert.Contains("Sync:SourceUrl", resultat.FailureMessage);
    }

    [Fact]
    public void UneSourceMuseofileVideEstPermise_MaisPasUneAdresseInvalide()
    {
        var options = Valide();
        options.MuseofileSourceUrl = "";
        Assert.True(Valider(options).Succeeded);

        options.MuseofileSourceUrl = "n'importe quoi";
        Assert.Contains("Sync:MuseofileSourceUrl", Valider(options).FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(double.NaN)]
    [InlineData(100000)]
    public void UnIntervalleAbsurde_EstRefuse(double heures)
    {
        var options = Valide();
        options.IntervalHours = heures;

        Assert.Contains("Sync:IntervalHours", Valider(options).FailureMessage);
    }

    [Fact]
    public void LesErreursSontToutesRapportees_PasSeulementLaPremiere()
    {
        var resultat = Valider(new SyncOptions { SourceUrl = "", MuseofileSourceUrl = "x", IntervalHours = 0 });

        Assert.Equal(3, resultat.Failures!.Count());
    }

    private static IHost Serveur(Dictionary<string, string?> reglages)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureHostConfiguration(c => c.Sources.Clear())
            .ConfigureAppConfiguration(c => { c.Sources.Clear(); c.AddInMemoryCollection(reglages); })
            .ConfigureServices((contexte, services) =>
            {
                services.AddOptions<SyncOptions>()
                    .Bind(contexte.Configuration.GetSection(SyncOptions.SectionName))
                    .ValidateOnStart();
                services.AddSingleton<IValidateOptions<SyncOptions>, SyncOptionsValidator>();
            })
            .Build();
    }

    [Fact]
    public async Task UneConfigurationInvalide_ArreteLeDemarrageDuServeur()
    {
        using var serveur = Serveur(new() { ["Sync:SourceUrl"] = "nimporte quoi" });

        var erreur = await Assert.ThrowsAsync<OptionsValidationException>(() => serveur.StartAsync());

        Assert.Contains("Sync:SourceUrl", erreur.Message);
    }

    [Fact]
    public async Task UneConfigurationValide_DemarreEtLieLesValeursDeLaSectionSync()
    {
        using var serveur = Serveur(new()
        {
            ["Sync:SourceUrl"] = "https://exemple.test/joconde.csv",
            ["Sync:CheckForUpdatesOnStartup"] = "true",
            ["Sync:IntervalHours"] = "12"
        });

        await serveur.StartAsync();

        var options = serveur.Services.GetRequiredService<IOptions<SyncOptions>>().Value;
        Assert.True(options.CheckForUpdatesOnStartup);
        Assert.Equal(12, options.IntervalHours);
        await serveur.StopAsync();
    }
}
