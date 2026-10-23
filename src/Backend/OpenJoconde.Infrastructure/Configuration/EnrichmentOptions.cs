using Microsoft.Extensions.Options;

namespace OpenJoconde.Infrastructure.Configuration
{
    /// <summary>
    /// Réglages de l'enrichissement des artistes par Wikidata et Wikipédia (section « Enrichment »). Désactivé par défaut :
    /// il appelle des services externes, on l'active explicitement là où c'est voulu.
    /// </summary>
    public class EnrichmentOptions
    {
        public const string SectionName = "Enrichment";

        /// <summary>Lance l'enrichissement en arrière-plan</summary>
        public bool Enabled { get; set; }

        /// <summary>Artistes traités par lot</summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>Nombre minimal d'œuvres pour qu'un artiste soit examiné (les moins fournis ont peu de chances d'avoir un article)</summary>
        public int MinimumArtworks { get; set; } = 3;

        /// <summary>Pause entre deux artistes, par politesse envers Wikidata</summary>
        public int PauseBetweenArtistsMilliseconds { get; set; } = 300;

        /// <summary>Pause entre deux lots tant qu'il reste des artistes à examiner</summary>
        public int PauseBetweenBatchesSeconds { get; set; } = 60;

        /// <summary>Attente avant de recommencer quand il n'y a plus rien à examiner (de nouveaux artistes arrivent avec les synchronisations)</summary>
        public int IdleHours { get; set; } = 12;

        /// <summary>Délai avant le premier lot, pour laisser l'application démarrer</summary>
        public int StartupDelaySeconds { get; set; } = 300;

        /// <summary>Contact joint à l'identifiant du client auprès de Wikimedia (politique d'usage des API) : adresse du projet ou courriel</summary>
        public string ContactUrl { get; set; } = "https://github.com/DavidDuveau/openjoconde";
    }

    public sealed class EnrichmentOptionsValidator : IValidateOptions<EnrichmentOptions>
    {
        public ValidateOptionsResult Validate(string? name, EnrichmentOptions options)
        {
            var errors = new List<string>();
            void Between(string setting, int value, int min, int max)
            {
                if (value < min || value > max)
                    errors.Add($"{EnrichmentOptions.SectionName}:{setting} doit être compris entre {min} et {max}.");
            }

            Between(nameof(options.BatchSize), options.BatchSize, 1, 1000);
            Between(nameof(options.MinimumArtworks), options.MinimumArtworks, 1, 1000);
            Between(nameof(options.PauseBetweenArtistsMilliseconds), options.PauseBetweenArtistsMilliseconds, 0, 60_000);
            Between(nameof(options.PauseBetweenBatchesSeconds), options.PauseBetweenBatchesSeconds, 0, 86_400);
            Between(nameof(options.IdleHours), options.IdleHours, 1, 24 * 30);
            Between(nameof(options.StartupDelaySeconds), options.StartupDelaySeconds, 0, 86_400);
            if (string.IsNullOrWhiteSpace(options.ContactUrl))
                errors.Add($"{EnrichmentOptions.SectionName}:ContactUrl est obligatoire : Wikimedia demande un contact dans l'identifiant du client.");

            return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
        }
    }
}
