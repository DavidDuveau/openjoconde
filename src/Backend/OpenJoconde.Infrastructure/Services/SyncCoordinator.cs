using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Lance, suit et annule les synchronisations. L'exclusion mutuelle repose sur un verrou applicatif SQL Server
    /// tenu par une connexion pendant toute la synchronisation : il vaut pour tous les processus qui partagent la base,
    /// et la base le libère d'elle-même si le processus meurt. Une ligne « Running » laissée par un processus mort
    /// est donc reconnaissable dès qu'on obtient le verrou.
    /// </summary>
    public sealed class SyncCoordinator : ISyncCoordinator
    {
        private const string LockResource = "OpenJoconde.Sync";
        private const string FailureMessage = "La synchronisation a échoué, voir les journaux du serveur";
        private const int MaxStoredMessageLength = 2000;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SyncCoordinator> _logger;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _running = new();

        public SyncCoordinator(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<SyncCoordinator> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SyncStartResult> TryStartAsync(string triggeredBy, CancellationToken cancellationToken = default)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("La chaîne de connexion 'DefaultConnection' n'est pas configurée");

            var lockConnection = new SqlConnection(connectionString);
            try
            {
                await lockConnection.OpenAsync(cancellationToken);
                if (!await TryAcquireLockAsync(lockConnection, cancellationToken))
                {
                    await lockConnection.DisposeAsync();
                    return SyncStartResult.Busy(await RunningSinceAsync(cancellationToken));
                }

                var syncId = await OpenLogAsync(triggeredBy, cancellationToken);
                var cancellation = new CancellationTokenSource();
                _running[syncId] = cancellation;

                // RunAsync prend possession de la connexion : le verrou tient jusqu'à la fin de la synchronisation
                var completion = Task.Run(() => RunAsync(syncId, lockConnection, cancellation));
                return SyncStartResult.Running(syncId, completion);
            }
            catch
            {
                await lockConnection.DisposeAsync();
                throw;
            }
        }

        public bool RequestCancel(Guid syncId)
        {
            if (!_running.TryGetValue(syncId, out var cancellation))
                return false;

            _logger.LogInformation("Annulation demandée pour la synchronisation {SyncId}", syncId);
            cancellation.Cancel();
            return true;
        }

        private static async Task<bool> TryAcquireLockAsync(SqlConnection connection, CancellationToken cancellationToken)
        {
            await using var command = new SqlCommand(
                "DECLARE @result int; EXEC @result = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0; SELECT @result;",
                connection);
            command.Parameters.AddWithValue("@resource", LockResource);

            // 0 et 1 : verrou obtenu (tout de suite, ou après attente) ; négatif : refusé
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) >= 0;
        }

        private async Task<DateTime?> RunningSinceAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<OpenJocondeDbContext>();
            return await context.DataSyncLogs
                .Where(log => log.Status == "Running")
                .OrderByDescending(log => log.StartedAt)
                .Select(log => (DateTime?)log.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Écrit la ligne de journal de la nouvelle synchronisation. Le verrou est à nous : toute ligne « Running »
        /// restante appartient à un processus qui s'est arrêté sans conclure.
        /// </summary>
        private async Task<Guid> OpenLogAsync(string triggeredBy, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<OpenJocondeDbContext>();

            var now = DateTime.UtcNow;
            var interrupted = await context.DataSyncLogs
                .Where(log => log.Status == "Running")
                .ExecuteUpdateAsync(set => set
                    .SetProperty(log => log.Status, "Interrupted")
                    .SetProperty(log => log.CompletedAt, now)
                    .SetProperty(log => log.Success, false)
                    .SetProperty(log => log.ErrorMessage, "Interrompue : le serveur s'est arrêté avant la fin"),
                    cancellationToken);
            if (interrupted > 0)
            {
                _logger.LogWarning("{Count} synchronisation(s) laissée(s) « Running » par un arrêt du serveur marquée(s) « Interrupted »", interrupted);
            }

            var log = new DataSyncLog
            {
                Id = Guid.NewGuid(),
                SyncType = "Full",
                TriggeredBy = triggeredBy,
                StartedAt = now,
                CreatedAt = now,
                Status = "Running"
            };
            context.DataSyncLogs.Add(log);
            await context.SaveChangesAsync(cancellationToken);
            return log.Id;
        }

        private async Task RunAsync(Guid syncId, SqlConnection lockConnection, CancellationTokenSource cancellation)
        {
            var outcome = new SyncOutcome { Status = "Failed", ErrorMessage = FailureMessage };

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var downloader = scope.ServiceProvider.GetRequiredService<IJocondeDataService>();
                var importer = scope.ServiceProvider.GetRequiredService<IDataImportService>();

                var sourceUrl = _configuration["JocondeData:SourceUrl"]
                    ?? throw new InvalidOperationException("L'URL source des données Joconde n'est pas configurée");
                var tempDirectory = _configuration["JocondeData:TempDirectory"] ?? Path.Combine(Path.GetTempPath(), "openjoconde");
                Directory.CreateDirectory(tempDirectory);
                var filePath = Path.Combine(tempDirectory, $"joconde_{DateTime.Now:yyyyMMdd_HHmmss}{JocondeSourceFile.ExtensionFor(sourceUrl)}");

                try
                {
                    await downloader.DownloadJocondeDataAsync(sourceUrl, filePath, cancellation.Token);
                    var report = await importer.ImportFromFileAsync(filePath, LogProgress, cancellation.Token);

                    outcome = new SyncOutcome
                    {
                        Status = report.Success ? "Completed" : "CompletedWithErrors",
                        Success = report.Success,
                        ArtworksProcessed = report.ImportedArtworks,
                        ArtistsProcessed = report.ImportedArtists,
                        ErrorCount = report.Errors,
                        ErrorMessage = Truncate(report.ErrorMessage)
                    };
                }
                finally
                {
                    // Le fichier fait plus d'un Go : il ne reste pas, quelle que soit l'issue
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Synchronisation {SyncId} annulée", syncId);
                outcome = new SyncOutcome { Status = "Canceled", ErrorMessage = "Synchronisation annulée" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la synchronisation {SyncId}", syncId);
            }
            finally
            {
                // La conclusion est écrite avant de lâcher le verrou : personne ne voit « Running » sans synchronisation vivante
                await CloseLogAsync(syncId, outcome);
                _running.TryRemove(syncId, out _);
                cancellation.Dispose();
                await lockConnection.DisposeAsync();
            }
        }

        private async Task CloseLogAsync(Guid syncId, SyncOutcome outcome)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<OpenJocondeDbContext>();
                var log = await context.DataSyncLogs.FindAsync(syncId);
                if (log == null)
                    return;

                log.CompletedAt = DateTime.UtcNow;
                log.Status = outcome.Status;
                log.Success = outcome.Success;
                log.ArtworksProcessed = outcome.ArtworksProcessed;
                log.ArtistsProcessed = outcome.ArtistsProcessed;
                log.ErrorCount = outcome.ErrorCount;
                log.ErrorMessage = outcome.ErrorMessage;
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Impossible d'écrire la conclusion de la synchronisation {SyncId} ; elle sera marquée « Interrupted » à la prochaine", syncId);
            }
        }

        private void LogProgress(string stage, int current, int total)
        {
            if (total > 0 && (current % 10000 == 0 || current == total))
            {
                _logger.LogInformation("{Stage}: {Current}/{Total} ({Percent}%)", stage, current, total, Math.Round((double)current / total * 100));
            }
        }

        private static string? Truncate(string? message) =>
            message is { Length: > MaxStoredMessageLength } ? message[..MaxStoredMessageLength] : message;

        private sealed class SyncOutcome
        {
            public string Status { get; init; } = "Failed";
            public bool Success { get; init; }
            public int ArtworksProcessed { get; init; }
            public int ArtistsProcessed { get; init; }
            public int ErrorCount { get; init; }
            public string? ErrorMessage { get; init; }
        }
    }
}
