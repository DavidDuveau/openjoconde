using Microsoft.Extensions.Logging;
using OpenJoconde.Core.Interfaces;
using OpenJoconde.Core.Models;
using OpenJoconde.Core.Parsers;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Service pour télécharger, analyser et importer les données Joconde
    /// </summary>
    public class JocondeDownloader : IJocondeDownloader
    {
        private readonly ILogger<JocondeDownloader> _logger;
        private readonly HttpClient _httpClient;

        public JocondeDownloader(
            ILogger<JocondeDownloader> logger,
            HttpClient httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Télécharge les données Joconde depuis l'URL spécifiée
        /// Version améliorée avec téléchargement par flux pour les fichiers volumineux
        /// </summary>
        public async Task<string> DownloadJocondeDataAsync(string url, string destinationPath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Téléchargement des données Joconde depuis {Url}", url);

            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("L'URL ne peut pas être vide", nameof(url));

            if (string.IsNullOrEmpty(destinationPath))
                throw new ArgumentException("Le chemin de destination ne peut pas être vide", nameof(destinationPath));

            // Créer le répertoire de destination s'il n'existe pas
            var directory = Path.GetDirectoryName(destinationPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            try
            {
                // Utiliser un téléchargement par flux pour éviter de charger tout le contenu en mémoire
                // Cela permet de gérer des fichiers volumineux sans problème de mémoire
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                
                // Commencer le téléchargement avec GetStreamAsync plutôt que GetAsync pour le streaming
                _logger.LogInformation("Début du téléchargement par flux depuis {Url}", url);
                
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                // Obtenir la taille du fichier si disponible
                long? totalBytes = response.Content.Headers.ContentLength;
                if (totalBytes.HasValue)
                {
                    _logger.LogInformation("Taille du fichier à télécharger: {Size} Mo", totalBytes.Value / (1024 * 1024));
                }
                
                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                
                // Copier le contenu par blocs avec un buffer de 8192 octets
                var buffer = new byte[8192];
                int bytesRead;
                long totalBytesRead = 0;
                var lastLogTime = DateTime.Now;
                
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    
                    // Mise à jour des logs de progression toutes les 5 secondes pour éviter de surcharger les logs
                    totalBytesRead += bytesRead;
                    var now = DateTime.Now;
                    if ((now - lastLogTime).TotalSeconds >= 5)
                    {
                        if (totalBytes.HasValue)
                        {
                            var progressPercentage = (double)totalBytesRead / totalBytes.Value * 100;
                            _logger.LogInformation("Progression du téléchargement: {Progress:F2}% ({Downloaded:F2} Mo / {Total:F2} Mo)",
                                progressPercentage,
                                totalBytesRead / (1024.0 * 1024.0),
                                totalBytes.Value / (1024.0 * 1024.0));
                        }
                        else
                        {
                            _logger.LogInformation("Téléchargés: {Downloaded:F2} Mo", totalBytesRead / (1024.0 * 1024.0));
                        }
                        lastLogTime = now;
                    }
                }
                
                _logger.LogInformation("Téléchargement terminé. Total téléchargé: {Downloaded:F2} Mo", totalBytesRead / (1024.0 * 1024.0));
                _logger.LogInformation("Données Joconde téléchargées avec succès dans {FilePath}", destinationPath);
                return destinationPath;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Le téléchargement a été annulé en raison d'un délai d'expiration. Envisagez d'augmenter le timeout du HttpClient.");
                throw new TimeoutException("Le téléchargement a dépassé le délai configuré. Le fichier est peut-être trop volumineux.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement des données Joconde: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Version courante de la source (ETag), lue par une requête HEAD, sans télécharger le fichier
        /// </summary>
        public async Task<string?> GetSourceVersionAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                return response.IsSuccessStatusCode ? response.Headers.ETag?.Tag : null;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Version de la source illisible : la synchronisation télécharge le fichier");
                return null;
            }
        }
    }
}
