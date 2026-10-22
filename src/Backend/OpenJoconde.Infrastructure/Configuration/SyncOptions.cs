using Microsoft.Extensions.Options;

namespace OpenJoconde.Infrastructure.Configuration
{
    /// <summary>
    /// Réglages des synchronisations (section « Sync » de la configuration) : sources de données, dossier temporaire,
    /// synchronisation automatique. Validés au démarrage : une valeur absurde arrête le serveur avec un message clair
    /// au lieu d'échouer à la première synchronisation.
    /// </summary>
    public class SyncOptions
    {
        public const string SectionName = "Sync";

        /// <summary>Adresse du fichier Joconde (CSV)</summary>
        public string SourceUrl { get; set; } = string.Empty;

        /// <summary>Adresse du fichier Muséofile (CSV) ; vide : les musées ne sont pas complétés</summary>
        public string? MuseofileSourceUrl { get; set; }

        /// <summary>Dossier des fichiers téléchargés ; vide : dossier temporaire du système</summary>
        public string? TempDirectory { get; set; }

        /// <summary>Lance une synchronisation au démarrage puis toutes les <see cref="IntervalHours"/> heures</summary>
        public bool CheckForUpdatesOnStartup { get; set; }

        /// <summary>Heures entre deux synchronisations automatiques</summary>
        public double IntervalHours { get; set; } = 24;
    }

    public sealed class SyncOptionsValidator : IValidateOptions<SyncOptions>
    {
        public ValidateOptionsResult Validate(string? name, SyncOptions options)
        {
            var errors = new List<string>();

            if (!IsHttpUrl(options.SourceUrl))
                errors.Add($"{SyncOptions.SectionName}:SourceUrl doit être une adresse http ou https.");
            if (!string.IsNullOrWhiteSpace(options.MuseofileSourceUrl) && !IsHttpUrl(options.MuseofileSourceUrl))
                errors.Add($"{SyncOptions.SectionName}:MuseofileSourceUrl doit être vide ou une adresse http ou https.");
            if (!(options.IntervalHours >= 0.01 && options.IntervalHours <= 24 * 366))
                errors.Add($"{SyncOptions.SectionName}:IntervalHours doit être compris entre 0,01 et {24 * 366} heures.");

            return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
        }

        private static bool IsHttpUrl(string? value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
