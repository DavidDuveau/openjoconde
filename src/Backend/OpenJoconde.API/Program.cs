using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Infrastructure.Data;
using OpenJoconde.Infrastructure.Services;
using OpenJoconde.API.Extensions;
using System;
using System.IO;

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
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null)
    );
});

// Register HTTP client
builder.Services.AddHttpClient<IJocondeDataService, JocondeDataService>();
builder.Services.AddHttpClient<AutoSyncService>();

// Register infrastructure services
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register auto sync service if enabled - temporarily disabled until fixed
// if (builder.Configuration.GetValue<bool>("JocondeData:CheckForUpdatesOnStartup", false))
// {
//     builder.Services.AddHostedService<AutoSyncService>();
// }

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
