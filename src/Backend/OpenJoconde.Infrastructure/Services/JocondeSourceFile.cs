using System;
using System.IO;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Nom des fichiers téléchargés depuis la source Joconde
    /// </summary>
    public static class JocondeSourceFile
    {
        private static readonly string[] SupportedExtensions = { ".csv", ".json" };

        /// <summary>
        /// Extension du fichier désigné par l'URL (.csv ou .json). L'import choisit son parseur d'après elle ;
        /// à défaut, on suppose le CSV publié sur data.gouv.fr.
        /// </summary>
        public static string ExtensionFor(string url)
        {
            var path = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : url;
            var extension = Path.GetExtension(path).ToLowerInvariant();
            return Array.IndexOf(SupportedExtensions, extension) >= 0 ? extension : ".csv";
        }
    }
}
