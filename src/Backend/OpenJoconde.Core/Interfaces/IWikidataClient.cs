using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Interfaces
{
    /// <summary>
    /// Ce que Wikidata sait d'une entité, réduit à ce dont le rapprochement des artistes a besoin
    /// </summary>
    /// <param name="Id">Identifiant Wikidata (Q296)</param>
    /// <param name="Label">Nom en français</param>
    /// <param name="Aliases">Autres noms en français</param>
    /// <param name="IsHuman">L'entité est une personne</param>
    /// <param name="BirthYears">Années de naissance déclarées (plusieurs en cas de désaccord entre sources)</param>
    /// <param name="DeathYears">Années de décès déclarées</param>
    /// <param name="FrenchWikipediaTitle">Titre de l'article Wikipédia en français, s'il existe</param>
    /// <param name="CitizenshipIds">Identifiants des pays de nationalité</param>
    /// <param name="MovementIds">Identifiants des courants artistiques</param>
    public sealed record WikidataEntity(
        string Id,
        string? Label,
        IReadOnlyList<string> Aliases,
        bool IsHuman,
        IReadOnlyList<int> BirthYears,
        IReadOnlyList<int> DeathYears,
        string? FrenchWikipediaTitle,
        IReadOnlyList<string> CitizenshipIds,
        IReadOnlyList<string> MovementIds);

    /// <summary>
    /// Lecture de Wikidata (CC0) et de Wikipédia (CC BY-SA) : de quoi rapprocher un artiste de sa notice et en tirer
    /// un résumé biographique. Une panne se signale par une exception.
    /// </summary>
    public interface IWikidataClient
    {
        /// <summary>Identifiants des entités dont le nom ressemble au nom donné (les premiers résultats seulement)</summary>
        Task<IReadOnlyList<string>> SearchAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>Les entités demandées, par identifiant ; une entité inconnue est absente du résultat</summary>
        Task<IReadOnlyDictionary<string, WikidataEntity>> GetEntitiesAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default);

        /// <summary>Résumé de l'article Wikipédia en français ; null si l'article n'a pas de résumé</summary>
        Task<string?> GetSummaryAsync(string title, CancellationToken cancellationToken = default);
    }
}
