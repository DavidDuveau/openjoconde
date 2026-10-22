using OpenJoconde.Core.Models;
using OpenJoconde.Core.Parsers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Une notice Joconde, quel que soit le format source (JSON ou CSV). Les noms de champs sont ceux de l'export,
    /// en minuscules (reference, titre, materiaux_techniques, coordonnees…).
    /// </summary>
    public interface IJocondeRecord
    {
        /// <summary>Valeur texte d'un champ, ou null s'il est absent ou vide.</summary>
        string? GetString(string field);

        /// <summary>Valeurs d'un champ multivalué (tableau JSON, ou texte séparé par « ; »).</summary>
        List<string> GetValues(string field);

        /// <summary>Coordonnées du musée : objet { lon, lat } (JSON) ou texte « lat, lon » (CSV).</summary>
        bool TryGetCoordinates(out double latitude, out double longitude);
    }

    /// <summary>
    /// Transforme des notices en œuvres et entités liées dédoublonnées. Une instance par fichier parsé.
    /// </summary>
    public class JocondeRecordMapper
    {
        // Largeurs des colonnes du modèle (OpenJocondeDbContext). Dans l'export réel, des titres, des numéros
        // d'inventaire, des noms de technique et des auteurs les dépassent : une seule valeur trop longue ferait
        // échouer l'enregistrement de tout son lot.
        private const int ReferenceMax = 100;
        private const int LongNameMax = 500;
        private const int ShortNameMax = 200;
        private const int DateMax = 100;
        private const int UrlMax = 2000;

        private static readonly Regex ArtistDatesInParentheses = new(@"\(([^)]+)\)", RegexOptions.Compiled);

        private readonly Dictionary<string, Artist> _artists = new();
        private readonly Dictionary<string, Domain> _domains = new();
        private readonly Dictionary<string, Technique> _techniques = new();
        private readonly Dictionary<string, Period> _periods = new();
        private readonly Dictionary<string, Museum> _museums = new();
        private readonly List<Artwork> _artworks = new();

        /// <summary>Ajoute une notice ; lève une exception si elle est inexploitable.</summary>
        public void Add(IJocondeRecord record)
        {
            var artwork = ExtractArtwork(record);
            var author = ExtractRelatedEntities(record, artwork);
            artwork.ContentHash = ArtworkFingerprint.Compute(artwork, author);
            _artworks.Add(artwork);
        }

        public ParsingResult ToResult() => new()
        {
            Artworks = _artworks,
            Artists = _artists.Values.ToList(),
            Domains = _domains.Values.ToList(),
            Techniques = _techniques.Values.ToList(),
            Periods = _periods.Values.ToList(),
            Museums = _museums.Values.ToList()
        };

        private static Artwork ExtractArtwork(IJocondeRecord record)
        {
            var reference = record.GetString("reference");
            if (reference == null)
                throw new FormatException("Notice sans référence");

            var periods = record.GetValues("periode_de_creation");
            var creationDate = periods.Count > 0
                ? string.Join(" ; ", periods)
                : record.GetString("millesime_de_creation") ?? record.GetString("epoque");

            return new Artwork
            {
                Id = Guid.NewGuid(),
                Reference = Fit(reference, ReferenceMax)!,
                InventoryNumber = Fit(record.GetString("numero_inventaire"), LongNameMax),
                Denomination = record.GetString("denomination") ?? "Œuvre sans dénomination",
                Title = Fit(record.GetString("titre"), LongNameMax),
                Description = record.GetString("description"),
                Dimensions = record.GetString("mesures"),
                CreationDate = creationDate,
                CreationPlace = record.GetString("lieu_de_creation_utilisation") ?? record.GetString("ecole_pays"),
                ConservationPlace = record.GetString("localisation") ?? record.GetString("lieu_de_depot"),
                AssociatedSiteUrl = HttpUrl(record.GetString("lien_site_associe")),
                ArtistUnderCopyright = UnderCopyright(record.GetValues("artiste_sous_droits")),
                PublicDomainYear = LatestYear(record.GetString("date_entree_dans_le_domaine_public")),
                HasImage = YesNo(record.GetString("presence_image")),
                // L'export ne fournit ni URL d'image (seulement presence_image) ni texte de droits.
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <returns>L'artiste de la notice, dont l'empreinte tient compte</returns>
        private Artist? ExtractRelatedEntities(IJocondeRecord record, Artwork artwork)
        {
            Artist? author = null;
            var artistName = record.GetString("auteur");
            if (artistName != null)
            {
                var key = artistName.ToLowerInvariant();
                if (!_artists.TryGetValue(key, out var artist))
                {
                    artist = ParseArtistName(artistName);
                    _artists[key] = artist;
                }

                author = artist;
                artwork.Artists.Add(new ArtworkArtist
                {
                    ArtistId = artist.Id,
                    ArtworkId = artwork.Id,
                    Role = "Créateur"
                });
            }

            foreach (var name in FitAll(record.GetValues("domaine"), ShortNameMax))
            {
                if (!_domains.TryGetValue(name.ToLowerInvariant(), out var domain))
                {
                    domain = new Domain { Id = Guid.NewGuid(), Name = name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    _domains[name.ToLowerInvariant()] = domain;
                }
                artwork.Domains.Add(domain);
            }

            foreach (var name in FitAll(record.GetValues("materiaux_techniques"), ShortNameMax))
            {
                if (!_techniques.TryGetValue(name.ToLowerInvariant(), out var technique))
                {
                    technique = new Technique { Id = Guid.NewGuid(), Name = name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    _techniques[name.ToLowerInvariant()] = technique;
                }
                artwork.Techniques.Add(technique);
            }

            var periodNames = record.GetValues("periode_de_creation");
            if (periodNames.Count == 0)
                periodNames = record.GetValues("epoque");

            foreach (var name in FitAll(periodNames, ShortNameMax))
            {
                if (!_periods.TryGetValue(name.ToLowerInvariant(), out var period))
                {
                    period = new Period { Id = Guid.NewGuid(), Name = name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    _periods[name.ToLowerInvariant()] = period;
                }
                artwork.Periods.Add(period);
            }

            var museumName = Fit(record.GetString("nom_officiel_musee") ?? record.GetString("localisation"), LongNameMax);
            if (museumName == null)
                return author;

            if (_museums.TryGetValue(museumName.ToLowerInvariant(), out var knownMuseum))
            {
                artwork.Museum = knownMuseum;
            }
            else
            {
                var museum = new Museum
                {
                    Id = Guid.NewGuid(),
                    Name = museumName,
                    Code = record.GetString("code_museofile"),
                    City = Fit(record.GetString("ville"), ShortNameMax),
                    Department = Fit(record.GetString("departement"), ShortNameMax),
                    Region = record.GetString("region"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (record.TryGetCoordinates(out var latitude, out var longitude))
                {
                    museum.Latitude = latitude;
                    museum.Longitude = longitude;
                }

                _museums[museumName.ToLowerInvariant()] = museum;
                artwork.Museum = museum;
            }

            return author;
        }

        /// <summary>
        /// Découpe un nom d'auteur Joconde (ex. « DUBREUIL Toussaint », « Charnay Armand (1844-1915) »)
        /// </summary>
        private static Artist ParseArtistName(string fullName)
        {
            var artist = new Artist
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var dateMatch = ArtistDatesInParentheses.Match(fullName);
            if (dateMatch.Success)
            {
                var dateParts = dateMatch.Groups[1].Value.Split('-');
                if (dateParts.Length == 2)
                {
                    artist.BirthDate = Fit(dateParts[0].Trim(), DateMax);
                    artist.DeathDate = Fit(dateParts[1].Trim(), DateMax);
                }

                fullName = fullName.Replace(dateMatch.Value, "").Trim();
            }

            var nameParts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length >= 2)
            {
                artist.LastName = Fit(nameParts[0], LongNameMax)!;
                artist.FirstName = Fit(string.Join(" ", nameParts.Skip(1)), LongNameMax);
            }
            else
            {
                artist.LastName = Fit(fullName.Trim(), LongNameMax)!;
            }

            return artist;
        }

        /// <summary>Tronque une valeur à la largeur de sa colonne.</summary>
        private static string? Fit(string? value, int maxLength) =>
            value is { } text && text.Length > maxLength ? text[..maxLength].TrimEnd() : value;

        /// <summary>Tronque des noms avant dédoublonnage : deux noms qui ne diffèrent qu'après la limite n'en font qu'un.</summary>
        private static IEnumerable<string> FitAll(IEnumerable<string> names, int maxLength) =>
            names.Select(name => Fit(name, maxLength)!).Distinct();

        /// <summary>Découpe « lat, lon » ; faux si le texte n'a pas ce format.</summary>
        internal static bool TryParseLatLon(string? text, out double latitude, out double longitude)
        {
            latitude = longitude = 0;
            var parts = text?.Split(',', StringSplitOptions.TrimEntries);
            return parts is { Length: 2 }
                && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out latitude)
                && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out longitude);
        }

        /// <summary>Découpe un champ multivalué texte (« a;b;c »).</summary>
        /// <summary>
        /// Une adresse web que la page peut proposer en lien. Tout autre schéma (javascript:, data:…) est écarté,
        /// comme une adresse démesurée.
        /// </summary>
        internal static string? HttpUrl(string? text) =>
            text != null && text.Length <= UrlMax
                && Uri.TryCreate(text, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? text
                : null;

        /// <summary>« oui » pour un des auteurs suffit ; « non » seulement quand aucun n'est sous droits ; sinon inconnu.</summary>
        /// <summary>
        /// « oui » ou « non » de l'export ; null pour toute autre valeur ou une valeur absente
        /// </summary>
        internal static bool? YesNo(string? value)
        {
            var text = value?.Trim();
            if (string.Equals(text, "oui", StringComparison.OrdinalIgnoreCase))
                return true;
            return string.Equals(text, "non", StringComparison.OrdinalIgnoreCase) ? false : null;
        }

        internal static bool? UnderCopyright(List<string> values)
        {
            if (values.Any(v => v.Equals("oui", StringComparison.OrdinalIgnoreCase)))
                return true;
            return values.Any(v => v.Equals("non", StringComparison.OrdinalIgnoreCase)) ? false : null;
        }

        /// <summary>
        /// L'œuvre est libre quand tous ses auteurs le sont : on retient l'année la plus tardive de la notice
        /// </summary>
        internal static int? LatestYear(string? text)
        {
            if (text == null)
                return null;

            var years = Regex.Matches(text, @"\b\d{4}\b").Select(m => int.Parse(m.Value))
                .Where(y => y is >= 1000 and <= 2300).ToList();
            return years.Count == 0 ? null : years.Max();
        }

        internal static List<string> SplitValues(string? text) =>
            text == null
                ? new List<string>()
                : text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToList();
    }
}
