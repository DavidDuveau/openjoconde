using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenJoconde.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace OpenJoconde.API.Controllers
{
    /// <summary>
    /// Contrôleur pour vérifier la santé de l'application
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly OpenJocondeDbContext _context;
        private readonly ILogger<HealthController> _logger;

        public HealthController(OpenJocondeDbContext context, ILogger<HealthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Vérifie la santé de l'application et de ses dépendances
        /// </summary>
        /// <returns>État de santé de l'application</returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                // Vérifier la connexion à la base de données
                var canConnect = await _context.Database.CanConnectAsync();
                
                if (!canConnect)
                {
                    _logger.LogError("Impossible de se connecter à la base de données");
                    return StatusCode(503, new
                    {
                        status = "Unhealthy",
                        timestamp = DateTime.UtcNow,
                        database = "Disconnected",
                        message = "Cannot connect to database"
                    });
                }

                // Vérifier le nombre d'œuvres dans la base
                var artworkCount = await _context.Artworks.CountAsync();
                
                return Ok(new
                {
                    status = "Healthy",
                    timestamp = DateTime.UtcNow,
                    database = "Connected",
                    statistics = new
                    {
                        artworks = artworkCount,
                        artists = await _context.Artists.CountAsync(),
                        museums = await _context.Museums.CountAsync()
                    },
                    version = "1.2.0",
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de santé");
                return StatusCode(503, new
                {
                    status = "Unhealthy",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message,
                    message = "Internal error during health check"
                });
            }
        }

        /// <summary>
        /// Vérification simple pour les load balancers
        /// </summary>
        /// <returns>200 OK si l'application répond</returns>
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { status = "pong", timestamp = DateTime.UtcNow });
        }
    }
}
