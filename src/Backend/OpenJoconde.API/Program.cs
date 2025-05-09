using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Infrastructure.Data;
using OpenJoconde.Infrastructure.Services;
using OpenJoconde.API.Extensions;
using System;
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
    // Utilisation de SQL Server au lieu de PostgreSQL
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)
    );
});

// Register HTTP client with extended timeout for large downloads
builder.Services.AddJocondeHttpClient();
builder.Services.AddHttpClient<AutoSyncService>(client =>
{
    // Timeout étendu à 10 minutes pour les téléchargements volumineux
    client.Timeout = TimeSpan.FromMinutes(10);
});

// Register infrastructure services
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register auto sync service if enabled
if (builder.Configuration.GetValue<bool>("JocondeData:CheckForUpdatesOnStartup", false))
{
    builder.Services.AddHostedService<AutoSyncService>();
}

// CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", builder =>
    {
        builder.WithOrigins(
                "http://localhost:8080",
                "http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
    });
});

var app = builder.Build();

// Ensure the temp directory exists
var tempDir = builder.Configuration["JocondeData:TempDirectory"];
if (!string.IsNullOrEmpty(tempDir) && !Directory.Exists(tempDir))
{
    Directory.CreateDirectory(tempDir);
}

// Vérifier si la base de données existe et est correctement initialisée
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OpenJocondeDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    
    // Afficher la chaîne de connexion utilisée (masquer le mot de passe si présent)
    var connectionString = config.GetConnectionString("DefaultConnection");
    logger.LogInformation("Chaîne de connexion: {ConnectionString}", connectionString);
    
    // Vérifier les options de connexion
    logger.LogInformation("Vérification de la connexion à la base de données SQL Server...");
    
    try
    {
        // Tentative de connexion à la base de données
        var canConnect = dbContext.Database.CanConnect();
        if (!canConnect)
        {
            logger.LogError("Impossible de se connecter à la base de données SQL Server.");
            
            // Essayons de vérifier si le serveur SQL est accessible
            logger.LogInformation("Vérification de l'accessibilité du serveur SQL...");
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(
                    connectionString.Replace("Database=OpenJoconde;", "")))
                {
                    connection.Open();
                    logger.LogInformation("Serveur SQL accessible, mais la base de données 'OpenJoconde' pourrait ne pas exister.");
                }
            }
            catch (Exception innerEx)
            {
                logger.LogError(innerEx, "Erreur lors de la vérification de l'accessibilité du serveur SQL.");
            }
        }
        else
        {
            logger.LogInformation("Connexion à la base de données SQL Server établie avec succès.");
            
            // Vérifier le schéma de la base de données
            try 
            {
                var tables = dbContext.Model.GetEntityTypes().Select(t => t.GetTableName()).ToList();
                logger.LogInformation("Tables dans la base de données: {Tables}", string.Join(", ", tables));
            }
            catch (Exception schemaEx)
            {
                logger.LogError(schemaEx, "Erreur lors de la vérification du schéma de la base de données.");
            }
        }
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
app.UseAuthorization();
app.MapControllers();

app.Run();
