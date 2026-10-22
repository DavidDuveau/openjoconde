using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenJoconde.Infrastructure.Configuration;
using Microsoft.OpenApi;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Infrastructure.Data;
using OpenJoconde.Infrastructure.Services;
using OpenJoconde.API.Extensions;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading.RateLimiting;
using System.IO;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "OpenJoconde API", 
        Version = "v1",
        Description = "API pour l'exploitation des données de la base Joconde",
        Contact = new OpenApiContact
        {
            Name = "Équipe OpenJoconde",
            Email = "contact@openjoconde.org"
        }
    });
    
    // Activation des commentaires XML pour la documentation Swagger
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Database Configuration
builder.Services.AddDbContext<OpenJocondeDbContext>(options =>
{
    options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null))
        .UseSnakeCaseNamingConvention();
});

// Register HTTP client with extended timeout for large downloads
builder.Services.AddJocondeHttpClient();
builder.Services.AddHttpClient<AutoSyncService>(client =>
{
    // Timeout étendu à 10 minutes pour les téléchargements volumineux
    client.Timeout = TimeSpan.FromMinutes(10);
});

// Réglages des synchronisations (section « Sync »), contrôlés au démarrage
builder.Services.AddOptions<SyncOptions>()
    .Bind(builder.Configuration.GetSection(SyncOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<SyncOptions>, SyncOptionsValidator>();

// Register infrastructure services
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register auto sync service if enabled
if (builder.Configuration.GetValue<bool>($"{SyncOptions.SectionName}:{nameof(SyncOptions.CheckForUpdatesOnStartup)}"))
{
    builder.Services.AddHostedService<AutoSyncService>();
}

// CORS Policy
builder.Services.AddCors(options =>
{
    // Origines du front en plus de celles de développement (tests de bout en bout, déploiement) : Cors:Origins
    var extraOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
    options.AddPolicy("AllowVueApp", builder =>
    {
        builder.WithOrigins(new[]
            {
                "http://localhost:8080",  // Vue CLI serve
                "http://localhost:5173",  // Vite
                "http://localhost:3000",  // Additional common port
                "http://localhost:4200"   // Angular CLI (if needed)
            }.Concat(extraOrigins).ToArray())
                .AllowAnyHeader()
                .AllowAnyMethod();
    });
});

// Limitation de débit des routes d'écriture et d'import, par adresse IP
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("ecriture", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "inconnue",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

// Ensure the temp directory exists
var tempDir = app.Services.GetRequiredService<IOptions<SyncOptions>>().Value.TempDirectory;
if (!string.IsNullOrEmpty(tempDir) && !Directory.Exists(tempDir))
{
    Directory.CreateDirectory(tempDir);
}

// Vérifier si la base de données existe et est correctement initialisée
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OpenJocondeDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        if (dbContext.Database.CanConnect())
            logger.LogInformation("Connexion à la base de données PostgreSQL établie avec succès.");
        else
            logger.LogError("Impossible de se connecter à la base de données PostgreSQL : vérifier la chaîne de connexion et que la base existe.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erreur lors de la vérification de la connexion à la base de données.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "OpenJoconde API v1"));
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("AllowVueApp");
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();
