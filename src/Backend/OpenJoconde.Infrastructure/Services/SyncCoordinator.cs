using Microsoft.Extensions.Options;
using OpenJoconde.Infrastructure.Configuration;
using Npgsql;
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
    /// Lance, suit et annule les synchronisations. L'exclusion mutuelle repose sur un verrou consultatif PostgreSQL
    /// tenu par une connexion pendant toute la synchronisation : il vaut pour tous les processus qui partagent la base,
    /// et la base le libère d'elle-même si le processus meurt. Une ligne « Running » laissée par un processus mort
    /// est donc reconnaissable dès qu'on obtient le verrou.
    /// </summary>
    public sealed class SyncCoordinator : ISyncCoordinator
    {
        // Identifiant du verrou consultatif PostgreSQL : « OJSY » en ASCII
        private const long LockKey = 0x4F4A5359;
        private const string FailureMessage = "La synchronisation a échoué, voir les journaux du serveur";
        private const int MaxStoredMessageLength = 2000;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly SyncOptions _options;
        private readonly ILogger<SyncCoordinator> _logger;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _running = new();

        public SyncCoordinator(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            IOptions<SyncOptions> options,
            ILogger<SyncCoordinator> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SyncStartResult> TryStartAsync(string triggeredBy, CancellationToken cancellationToken = default, bool force = false)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("La chaîne de connexion 'DefaultConnection' n'est pas configurée");

            var lockConnection = new NpgsqlConnection(connectionString);
            try
            {
                await lockConnection.OpenAsync(cancellationToken);
                if (!await TryAcquireLockAsync(lockConnection, cancellationToken))
                {
                    await lockConnection.DisposeAsync();
                    return SyncStartResult.Busy(await RunningSinceAsync(cancellationToken));
                }

                DeleteLeftoverDownloads();
                var syncId = await OpenLogAsync(triggeredBy, cancellationToken);
                var cancellation = new CancellationTokenSource();
                _running[syncId] = cancellation;

                // RunAsync prend possession de la connexion : le verrou tient jusqu'à la fin de la synchronisation
                var completion = Task.Run(() => RunAsync(syncId, lockConnection, cancellation, force));
                return SyncStartResult.Running(syncId, completion);
            }
            catch
            {
                await ReleaseLockAsync(lockConnection);
                throw;
            }
        }

        /// <summary>
        /// Libère le verrou puis rend la connexion. Fermer une connexion Npgsql la remet dans le pool sans terminer la
        /// session : le verrou resterait tenu par une connexion inactive, et la synchronisation suivante serait refusée
        /// </summary>
        private static async Task ReleaseLockAsync(NpgsqlConnection connection)
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    await using var command = new NpgsqlCommand("SELECT pg_advisory_unlock(@key)", connection);
                    command.Parameters.AddWithValue("key", LockKey);
                    await command.ExecuteScalarAsync();
                }
            }
            catch
            {
                // Connexion perdue : la base a déjà libéré le verrou avec la session
            }
            finally
            {
                await connection.DisposeAsync();
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

        private static async Task<bool> TryAcquireLockAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
        {
            // Verrou consultatif de session : tenu par la connexion, libéré par la base si elle se ferme ou meurt
            await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock(@key)", connection);
            command.Parameters.AddWithValue("key", LockKey);

            return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
        }

        /// <summary>
        /// Le verrou est à nous : un fichier téléchargé qui traîne vient d'un processus tué en pleine synchronisation
        /// </summary>
        private void DeleteLeftoverDownloads()
        {
            var tempDirectory = TempDirectory();
            if (!Directory.Exists(tempDirectory))
                return;

            foreach (var leftover in Directory.GetFiles(tempDirectory, "joconde_*"))
            {
                try
                {
                    File.Delete(leftover);
                    _logger.LogWarning("Fichier de synchronisation interrompue supprimé : {File}", leftover);
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "Impossible de supprimer {File}", leftover);
                }
            }
        }

        private string TempDirectory() =>
            (string.IsNullOrWhiteSpace(_options.TempDirectory) ? null : _options.TempDirectory) ?? Path.Combine(Path.GetTempPath(), "openjoconde");

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

        private async Task RunAsync(Guid syncId, NpgsqlConnection lockConnection, CancellationTokenSource cancellation, bool force)
        {
            var outcome = new SyncOutcome { Status = "Failed", ErrorMessage = FailureMessage };

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var downloader = scope.ServiceProvider.GetRequiredService<IJocondeDownloader>();
                var importer = scope.ServiceProvider.GetRequiredService<IDataImportService>();

                var sourceUrl = (string.IsNullOrWhiteSpace(_options.SourceUrl) ? null : _options.SourceUrl)
                    ?? throw new InvalidOperationException("L'URL source des données Joconde n'est pas configurée");
                // Source inchangée depuis la dernière synchronisation réussie : rien à télécharger
                var sourceVersion = await downloader.GetSourceVersionAsync(sourceUrl, cancellation.Token);
                if (!force && sourceVersion != null && sourceVersion == await LastSyncedVersionAsync(cancellation.Token))
                {
                    _logger.LogInformation("Source inchangée (version {Version}) : synchronisation {SyncId} sans téléchargement", sourceVersion, syncId);
                    outcome = new SyncOutcome
                    {
                        Status = "UpToDate",
                        Success = true,
                        SourceETag = sourceVersion,
                        ErrorMessage = "Source inchangée depuis la dernière synchronisation"
                    };
                    await EnrichMuseumsAsync(scope.ServiceProvider, cancellation.Token);
                    return;
                }

                var tempDirectory = TempDirectory();
                Directory.CreateDirectory(tempDirectory);
                var filePath = Path.Combine(tempDirectory, $"joconde_{DateTime.Now:yyyyMMdd_HHmmss}{JocondeSourceFile.ExtensionFor(sourceUrl)}");

                try
                {
                    await downloader.DownloadJocondeDataAsync(sourceUrl, filePath, cancellation.Token);
                    var report = await importer.ImportFromFileAsync(filePath, LogProgress, cancellation.Token, onlyChanges: true);

                    outcome = new SyncOutcome
                    {
                        Status = report.Success ? "Completed" : "CompletedWithErrors",
                        Success = report.Success,
                        ArtworksProcessed = report.ImportedArtworks,
                        ArtistsProcessed = report.ImportedArtists,
                        ArtworksUnchanged = report.SkippedArtworks,
                        ArtworksRemoved = report.RemovedArtworks,
                        // Une synchronisation avec pertes ne mémorise pas la version : la suivante recommence
                        SourceETag = report.Success ? sourceVersion : null,
                        ErrorCount = report.Errors,
                        ErrorMessage = Truncate(report.ErrorMessage)
                    };
                    await EnrichMuseumsAsync(scope.ServiceProvider, cancellation.Token);
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
                await ReleaseLockAsync(lockConnection);
            }
        }

        /// <summary>
        /// Complète les musées avec Muséofile (adresse, téléphone, site). Ce fichier est publié à part de Joconde : il est
        /// appliqué à chaque synchronisation, même quand Joconde n'a pas changé. Une panne de Muséofile ne fait pas
        /// échouer la synchronisation des œuvres.
        /// </summary>
        private async Task EnrichMuseumsAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            var url = _options.MuseofileSourceUrl;
            var museofile = services.GetService<IMuseofileService>();
            if (string.IsNullOrWhiteSpace(url) || museofile == null)
                return;

            try
            {
                await museofile.EnrichFromUrlAsync(url, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Les musées n'ont pas pu être complétés avec Muséofile");
            }
        }

        /// <summary>
        /// Version de la source à la dernière synchronisation réussie (importée ou constatée à jour)
        /// </summary>
        private async Task<string?> LastSyncedVersionAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<OpenJocondeDbContext>();
            return await context.DataSyncLogs
                .Where(log => (log.Status == "Completed" || log.Status == "UpToDate") && log.SourceETag != null)
                .OrderByDescending(log => log.StartedAt)
                .Select(log => log.SourceETag)
                .FirstOrDefaultAsync(cancellationToken);
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
                log.ArtworksUnchanged = outcome.ArtworksUnchanged;
                log.ArtworksRemoved = outcome.ArtworksRemoved;
                log.SourceETag = outcome.SourceETag;
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
            public int ArtworksUnchanged { get; init; }
            public int ArtworksRemoved { get; init; }
            public string? SourceETag { get; init; }
            public int ErrorCount { get; init; }
            public string? ErrorMessage { get; init; }
        }
    }
}
