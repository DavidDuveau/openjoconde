using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>Santé, référentiels et démarrage de l'API réelle (TECH-43)</summary>
public class ApiGeneralTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiGeneralTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Ping_RepondPong()
    {
        var response = await _factory.CreateClient().GetAsync("/api/health/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("pong", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Sante_BaseJoignable_RendLesStatistiques()
    {
        var response = await _factory.CreateClient().GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
        Assert.Equal(0, body.GetProperty("statistics").GetProperty("artworks").GetInt32());
    }

    [Fact]
    public async Task Referentiels_ListentEtFiltrentParNom()
    {
        await using (var context = _factory.Database.CreateContext())
        {
            context.Domains.AddRange(new Domain { Id = Guid.NewGuid(), Name = "peinture" }, new Domain { Id = Guid.NewGuid(), Name = "sculpture" });
            context.Techniques.Add(new Technique { Id = Guid.NewGuid(), Name = "huile sur toile" });
            context.Periods.Add(new Period { Id = Guid.NewGuid(), Name = "XIXe siècle" });
            await context.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var domains = await client.GetFromJsonAsync<JsonElement>("/api/domains?pageSize=500&page=0");
        Assert.Equal(2, domains.GetProperty("totalItems").GetInt32());
        Assert.Equal(100, domains.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, domains.GetProperty("page").GetInt32());

        var filtered = await client.GetFromJsonAsync<JsonElement>("/api/domains?name=PEINT&pageSize=0");
        Assert.Equal(1, filtered.GetProperty("totalItems").GetInt32());
        Assert.Equal(10, filtered.GetProperty("pageSize").GetInt32());

        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>("/api/techniques")).GetProperty("totalItems").GetInt32());
        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>("/api/periods?name=xix")).GetProperty("totalItems").GetInt32());
    }

    [Fact]
    public async Task Swagger_EstAbsentHorsDeveloppement_EtLaRouteInconnueRepond404()
    {
        var response = await _factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class ApiDeveloppementTests
{
    [Fact]
    public async Task EnDeveloppement_LeSwaggerEstPublie_EtLaSynchroAutomatiqueSeDeclare()
    {
        await using var factory = new ApiFactory { Environment = "Development" };
        factory.Settings["Sync:CheckForUpdatesOnStartup"] = "true";
        factory.Settings["Cors:Origins:0"] = "http://localhost:9999";
        await factory.InitializeAsync();

        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("OpenJoconde API", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BaseInjoignable_LeDemarrageContinue_EtLaSanteRepond503()
    {
        await using var factory = new ApiFactory();
        // Pas de base créée : le serveur répond, la connexion échoue
        var response = await factory.CreateClient().GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Sante_UneTableManquante_Repond503SansDetail()
    {
        await using var factory = new ApiFactory();
        await factory.InitializeAsync();
        await using (var connection = new NpgsqlConnection(factory.Database.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("DROP TABLE artwork CASCADE", connection);
            await command.ExecuteNonQueryAsync();
        }

        var response = await factory.CreateClient().GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Internal error during health check", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
    }
}

/// <summary>Les musées par l'API : droits, validation, CRUD sur la base réelle</summary>
public class MuseumsApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public MuseumsApiTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task SansCle_EcritureRefusee_LectureLibre()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/museums", new { name = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/museums")).StatusCode);
    }

    [Fact]
    public async Task Cycle_CreerLireModifierRechercherSupprimer()
    {
        var client = _factory.CreateAuthenticatedClient();

        var created = await client.PostAsJsonAsync("/api/museums", new { name = "  Musée du Cycle  ", city = "Lyon", latitude = 45.7, longitude = 4.8 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var museum = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = museum.GetProperty("id").GetGuid();
        Assert.Equal("Musée du Cycle", museum.GetProperty("name").GetString());

        Assert.Equal("Lyon", (await client.GetFromJsonAsync<JsonElement>($"/api/museums/{id}")).GetProperty("city").GetString());
        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>("/api/museums?name=cycle")).GetProperty("totalItems").GetInt32());

        var updated = await client.PutAsJsonAsync($"/api/museums/{id}", new { name = "Musée du Cycle", city = "Villeurbanne" });
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        Assert.Equal("Villeurbanne", (await client.GetFromJsonAsync<JsonElement>($"/api/museums/{id}")).GetProperty("city").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/museums/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/museums/{id}")).StatusCode);
    }

    [Fact]
    public async Task Introuvable_Repond404_EtDonneesInvalides_400()
    {
        var client = _factory.CreateAuthenticatedClient();
        var unknown = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/museums/{unknown}", new { name = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/museums/{unknown}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/museums", new { name = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/museums", new { name = "X", latitude = 123 })).StatusCode);
    }

    [Fact]
    public async Task Doublon_FaitEchouerLaCreation_En500SansDetail()
    {
        var client = _factory.CreateAuthenticatedClient();
        await using (var context = _factory.Database.CreateContext())
        {
            // Rendre l'écriture impossible : le nom est unique via un index ajouté ici
            await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX ux_museum_test ON museum (name, city)");
        }

        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/museums", new { name = "Doublon", city = "Nice" })).StatusCode);
        var second = await client.PostAsJsonAsync("/api/museums", new { name = "Doublon", city = "Nice" });

        Assert.Equal(HttpStatusCode.InternalServerError, second.StatusCode);
        Assert.DoesNotContain("ux_museum_test", await second.Content.ReadAsStringAsync());
    }
}

/// <summary>Chaque route de musée répond 500 sans détail quand le dépôt échoue ou refuse d'écrire</summary>
public class MuseumsApiFailureTests
{
    private static Museum Existing => new() { Id = Guid.NewGuid(), Name = "Existant" };

    private static async Task<HttpResponseMessage> Send(IMuseumRepository repository, HttpMethod method, string url, object? body = null)
    {
        await using var factory = new ApiFactory { Overrides = ApiFactory.Replacing(repository) };
        var client = factory.CreateAuthenticatedClient();
        var request = new HttpRequestMessage(method, url);
        if (body != null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task DepotEnPanne_Toutes_Repondent500()
    {
        var broken = Stub.Create<IMuseumRepository>();
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.InternalServerError, (await Send(broken, HttpMethod.Get, "/api/museums")).StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, (await Send(broken, HttpMethod.Get, $"/api/museums/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, (await Send(broken, HttpMethod.Post, "/api/museums", new { name = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, (await Send(broken, HttpMethod.Put, $"/api/museums/{id}", new { name = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, (await Send(broken, HttpMethod.Delete, $"/api/museums/{id}")).StatusCode);
    }

    [Fact]
    public async Task EcritureRefusee_Repond500()
    {
        var refusing = Stub.Create<IMuseumRepository>(
            Stub.Returns("GetByIdAsync", Task.FromResult<Museum?>(Existing)),
            Stub.Returns("UpdateAsync", Task.FromResult(false)),
            Stub.Returns("DeleteAsync", Task.FromResult(false)));
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.InternalServerError, (await Send(refusing, HttpMethod.Put, $"/api/museums/{id}", new { name = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, (await Send(refusing, HttpMethod.Delete, $"/api/museums/{id}")).StatusCode);
    }
}

/// <summary>Les artistes par l'API</summary>
public class ArtistsApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ArtistsApiTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Cycle_CreerLireModifierRechercherSupprimer()
    {
        var client = _factory.CreateAuthenticatedClient();

        var created = await client.PostAsJsonAsync("/api/artists", new { lastName = "MONET", firstName = "Claude", nationality = "française" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/artists/{id}");
        Assert.Equal("MONET", detail.GetProperty("lastName").GetString());
        Assert.Equal(0, detail.GetProperty("artworksCount").GetInt32());
        var search = await client.GetFromJsonAsync<JsonElement>("/api/artists?name=monet");
        Assert.Equal(1, search.GetProperty("totalItems").GetInt32());

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/artists/{id}", new { lastName = "MONET", firstName = "Oscar-Claude" })).StatusCode);
        Assert.Equal("Oscar-Claude", (await client.GetFromJsonAsync<JsonElement>($"/api/artists/{id}")).GetProperty("firstName").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/artists/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/artists/{id}")).StatusCode);
    }

    [Fact]
    public async Task Introuvable_404_Invalide_400_SansCle_401()
    {
        var client = _factory.CreateAuthenticatedClient();
        var unknown = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/artists/{unknown}", new { lastName = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/artists/{unknown}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/artists", new { lastName = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().DeleteAsync($"/api/artists/{unknown}")).StatusCode);
    }
}

/// <summary>Chaque route d'artiste répond 500 sans détail quand le dépôt échoue ou refuse d'écrire</summary>
public class ArtistsApiFailureTests
{
    private static async Task<HttpResponseMessage> Send(IArtistRepository repository, HttpMethod method, string url, object? body = null)
    {
        await using var factory = new ApiFactory { Overrides = ApiFactory.Replacing(repository) };
        var client = factory.CreateAuthenticatedClient();
        var request = new HttpRequestMessage(method, url);
        if (body != null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task DepotEnPanne_Toutes_Repondent500()
    {
        var broken = Stub.Create<IArtistRepository>();
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.InternalServerError, (await Send(broken, HttpMethod.Get, "/api/artists")).StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, (await Send(broken, HttpMethod.Get, $"/api/artists/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, (await Send(broken, HttpMethod.Post, "/api/artists", new { lastName = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, (await Send(broken, HttpMethod.Put, $"/api/artists/{id}", new { lastName = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, (await Send(broken, HttpMethod.Delete, $"/api/artists/{id}")).StatusCode);
    }

    [Fact]
    public async Task EcritureRefusee_Repond500()
    {
        var refusing = Stub.Create<IArtistRepository>(
            Stub.Returns("GetByIdAsync", Task.FromResult<Artist?>(new Artist { Id = Guid.NewGuid(), LastName = "Existant" })),
            Stub.Returns("UpdateAsync", Task.FromResult(false)),
            Stub.Returns("DeleteAsync", Task.FromResult(false)));
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.InternalServerError, (await Send(refusing, HttpMethod.Put, $"/api/artists/{id}", new { lastName = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, (await Send(refusing, HttpMethod.Delete, $"/api/artists/{id}")).StatusCode);
    }
}

/// <summary>Synchronisation : statut, historique, démarrage et annulation</summary>
public class SyncApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SyncApiTests(ApiFactory factory) => _factory = factory;

    private async Task<Guid> AddLog(string status, DateTime startedAt, string? errorMessage = null)
    {
        var id = Guid.NewGuid();
        await using var context = _factory.Database.CreateContext();
        context.DataSyncLogs.Add(new DataSyncLog
        {
            Id = id, SyncType = "Joconde", TriggeredBy = "Manual", Status = status, StartedAt = startedAt, CreatedAt = startedAt,
            ErrorMessage = errorMessage, ArtworksProcessed = 3, ArtistsProcessed = 2
        });
        await context.SaveChangesAsync();
        return id;
    }

    [Fact]
    public async Task Statut_PuisHistorique_PuisAnnulation()
    {
        var client = _factory.CreateAuthenticatedClient();

        var never = await client.GetFromJsonAsync<JsonElement>("/api/sync/status");
        Assert.Equal("Never Synced", never.GetProperty("status").GetString());

        var finished = await AddLog("Completed", DateTime.UtcNow.AddHours(-2), "détail d'erreur");
        var running = await AddLog("Running", DateTime.UtcNow.AddMinutes(-5));

        var status = await client.GetFromJsonAsync<JsonElement>("/api/sync/status");
        Assert.Equal("Running", status.GetProperty("status").GetString());
        Assert.Equal(5, status.GetProperty("itemsProcessed").GetInt32());
        Assert.False(status.TryGetProperty("errorMessage", out _));

        var history = await client.GetFromJsonAsync<JsonElement>("/api/sync/history?count=500");
        Assert.Equal(2, history.GetArrayLength());
        Assert.Equal("détail d'erreur", history[1].GetProperty("errorMessage").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync("/api/sync/history")).StatusCode);

        // Aucun coordinateur ne la porte : une synchro « Running » d'un autre serveur ne s'annule pas d'ici
        var cancelRunning = await client.PostAsync($"/api/sync/cancel/{running}", null);
        Assert.Equal(HttpStatusCode.Conflict, cancelRunning.StatusCode);
        Assert.Contains("ne tourne pas dans ce serveur", await cancelRunning.Content.ReadAsStringAsync());

        var cancelFinished = await client.PostAsync($"/api/sync/cancel/{finished}", null);
        Assert.Equal(HttpStatusCode.Conflict, cancelFinished.StatusCode);
        Assert.Contains("n'est pas en cours", await cancelFinished.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/sync/cancel/{Guid.NewGuid()}", null)).StatusCode);
    }
}

/// <summary>Démarrage et annulation d'une synchronisation, avec un coordinateur simulé</summary>
public class SyncCoordinatorApiTests
{
    private sealed class FakeCoordinator(Func<bool, Task<SyncStartResult>> start, Func<Guid, bool> cancel) : ISyncCoordinator
    {
        public bool? LastForce { get; private set; }

        public Task<SyncStartResult> TryStartAsync(string triggeredBy, CancellationToken cancellationToken = default, bool force = false)
        {
            LastForce = force;
            return start(force);
        }

        public bool RequestCancel(Guid syncId) => cancel(syncId);
    }

    private static async Task<(HttpResponseMessage Response, FakeCoordinator Coordinator)> Post(Func<bool, Task<SyncStartResult>> start, Func<Guid, bool> cancel, string url)
    {
        var coordinator = new FakeCoordinator(start, cancel);
        await using var factory = new ApiFactory { Overrides = ApiFactory.WithCoordinator(coordinator) };
        var response = await factory.CreateAuthenticatedClient().PostAsync(url, null);
        return (response, coordinator);
    }

    [Fact]
    public async Task Demarrage_Accepte_RendLIdentifiant_EtTransmetForce()
    {
        var id = Guid.NewGuid();
        var (response, coordinator) = await Post(_ => Task.FromResult(SyncStartResult.Running(id, Task.CompletedTask)), _ => false, "/api/sync/start?force=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(id, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("syncId").GetGuid());
        Assert.True(coordinator.LastForce);
    }

    [Fact]
    public async Task Demarrage_DejaEnCours_Repond409()
    {
        var (response, _) = await Post(_ => Task.FromResult(SyncStartResult.Busy(DateTime.UtcNow)), _ => false, "/api/sync/start");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Demarrage_EnPanne_Repond500SansDetail()
    {
        var (response, _) = await Post(_ => throw new InvalidOperationException("secret interne"), _ => false, "/api/sync/start");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("secret", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Annulation_Acceptee_Repond202()
    {
        var (response, _) = await Post(_ => throw new NotSupportedException(), _ => true, $"/api/sync/cancel/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }
}

/// <summary>Import d'un fichier fourni, avec un service d'import simulé pour les cas d'erreur</summary>
public class ImportApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ImportApiTests(ApiFactory factory) => _factory = factory;

    private static MultipartFormDataContent File(string name, byte[] content)
    {
        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(content), "file", name);
        return form;
    }

    [Fact]
    public async Task ImportDUnFichierJson_EcritLesOeuvres()
    {
        var client = _factory.CreateAuthenticatedClient();
        var json = await System.IO.File.ReadAllBytesAsync(ImportDatabase.FixturePath("joconde_une_oeuvre.json"));

        var response = await client.PostAsync("/api/import/from-file", File("une.json", json));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Importation réussie", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
        await using var context = _factory.Database.CreateContext();
        Assert.True(await context.Artworks.AnyAsync());
    }

    [Fact]
    public async Task Refus_FichierVide_ExtensionInconnue_SansCle()
    {
        var client = _factory.CreateAuthenticatedClient();

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/import/from-file", File("vide.csv", []))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/import/from-file", File("x.txt", [1, 2, 3]))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().PostAsync("/api/import/from-file", File("x.csv", [1]))).StatusCode);
    }

    [Fact]
    public async Task ImportEnPanne_Repond500_EtLeFichierTemporaireEstSupprime()
    {
        string? seenPath = null;
        var service = Stub.Create<IDataImportService>(
            ("ImportFromFileAsync", args => { seenPath = (string)args[0]!; throw new InvalidOperationException("détail interne"); }));
        await using var factory = new ApiFactory { Overrides = ApiFactory.Replacing(service) };

        var response = await factory.CreateAuthenticatedClient().PostAsync("/api/import/from-file", File("x.csv", [1, 2, 3]));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("interne", await response.Content.ReadAsStringAsync());
        Assert.NotNull(seenPath);
        Assert.False(System.IO.File.Exists(seenPath));
    }

    [Fact]
    public async Task ImportAvecErreurs_LeDitDansLeMessage_EtRelaieLaProgression()
    {
        var service = Stub.Create<IDataImportService>(
            ("ImportFromFileAsync", args =>
            {
                ((Action<string, int, int>)args[1]!)("Œuvres", 1, 2);
                return Task.FromResult(new ImportReport { Success = false });
            }));
        await using var factory = new ApiFactory { Overrides = ApiFactory.Replacing(service) };

        var response = await factory.CreateAuthenticatedClient().PostAsync("/api/import/from-file", File("x.csv", [1]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Importation terminée avec des erreurs", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
    }
}
