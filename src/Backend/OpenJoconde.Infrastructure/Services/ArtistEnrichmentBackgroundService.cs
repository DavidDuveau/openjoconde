using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Infrastructure.Configuration;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Fait avancer l'enrichissement des artistes en arrière-plan, lot après lot : une pause entre deux lots tant qu'il en
    /// reste, une longue attente quand tout est examiné (les synchronisations Joconde amènent de nouveaux artistes).
    /// N'est enregistré que si « Enrichment:Enabled » est vrai.
    /// </summary>
    public sealed class ArtistEnrichmentBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly EnrichmentOptions _options;
        private readonly ILogger<ArtistEnrichmentBackgroundService> _logger;

        public ArtistEnrichmentBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<EnrichmentOptions> options,
            ILogger<ArtistEnrichmentBackgroundService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Enrichissement des artistes activé (lots de {BatchSize})", _options.BatchSize);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.StartupDelaySeconds), stoppingToken);
                while (!stoppingToken.IsCancellationRequested)
                {
                    var wait = await RunBatchAsync(stoppingToken);
                    await Task.Delay(wait, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Arrêt de l'application
            }
        }

        /// <summary>Traite un lot et dit combien de temps attendre avant le suivant</summary>
        private async Task<TimeSpan> RunBatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var report = await scope.ServiceProvider.GetRequiredService<IArtistEnrichmentService>().EnrichBatchAsync(cancellationToken);
                // Un lot vide : plus rien à examiner. Un lot interrompu : Wikimedia ne répond pas, on laisse du temps.
                if (report.Examined == 0 && !report.Aborted && report.Failed == 0)
                    return TimeSpan.FromHours(_options.IdleHours);
                return report.Aborted ? TimeSpan.FromHours(1) : TimeSpan.FromSeconds(_options.PauseBetweenBatchesSeconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors d'un lot d'enrichissement des artistes");
                return TimeSpan.FromHours(1);
            }
        }
    }
}
