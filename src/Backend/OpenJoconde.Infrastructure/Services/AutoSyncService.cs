using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Déclenche périodiquement la synchronisation des données Joconde en arrière-plan.
    /// Le téléchargement, l'import et le journal sont ceux de <see cref="ISyncCoordinator"/>, comme pour la synchronisation manuelle.
    /// </summary>
    public class AutoSyncService : BackgroundService
    {
        private readonly ILogger<AutoSyncService> _logger;
        private readonly ISyncCoordinator _coordinator;
        private readonly TimeSpan _syncInterval;

        public AutoSyncService(
            ILogger<AutoSyncService> logger,
            IConfiguration configuration,
            ISyncCoordinator coordinator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _syncInterval = TimeSpan.FromHours(configuration.GetValue<double>("JocondeSync:IntervalHours", 24));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service de synchronisation automatique démarré. Intervalle: {SyncInterval}", _syncInterval);

            // Première synchronisation après un délai pour laisser l'application démarrer
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _coordinator.TryStartAsync("Automatic", stoppingToken);
                    if (result.Started)
                    {
                        await result.Completion.WaitAsync(stoppingToken);
                    }
                    else
                    {
                        _logger.LogInformation("Synchronisation automatique ignorée : une autre est déjà en cours depuis {Since}", result.RunningSince);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors du déclenchement de la synchronisation automatique");
                }

                _logger.LogInformation("Prochaine synchronisation prévue dans {SyncInterval}", _syncInterval);
                await Task.Delay(_syncInterval, stoppingToken);
            }
        }
    }
}
