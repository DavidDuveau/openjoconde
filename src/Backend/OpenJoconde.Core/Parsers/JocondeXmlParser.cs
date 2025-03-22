using OpenJoconde.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace OpenJoconde.Core.Parsers
{
    /// <summary>
    /// Parser spécialisé pour les fichiers XML de la base Joconde
    /// Optimisé pour le traitement de grands fichiers et l'extraction des entités liées
    /// </summary>
    public class JocondeXmlParser
    {
        private readonly Dictionary<string, Domain> _domains = new Dictionary<string, Domain>();
        private readonly Dictionary<string, Technique> _techniques = new Dictionary<string, Technique>();
        private readonly Dictionary<string, Period> _periods = new Dictionary<string, Period>();
        private readonly Dictionary<string, Artist> _artists = new Dictionary<string, Artist>();
        private readonly Dictionary<string, Museum> _museums = new Dictionary<string, Museum>();

        /// <summary>
        /// Parse un fichier XML Joconde et extrait toutes les entités (œuvres, artistes, domaines, etc.)
        /// </summary>
        /// <param name="xmlFilePath">Chemin vers le fichier XML</param>
        /// <param name="progressCallback">Callback pour suivre la progression</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Collection d'œuvres et entités liées</returns>
        public async Task<ParsingResult> ParseAsync(
            string xmlFilePath, 
            Action<int, int> progressCallback = null, 
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(xmlFilePath))
            {
                throw new FileNotFoundException("Le fichier XML Joconde n'existe pas", xmlFilePath);
            }

            var result = new ParsingResult();
            var artworks = new List<Artwork>();

            // Approche de streaming pour les grands fichiers
            using (var reader = XmlReader.Create(xmlFilePath, new XmlReaderSettings { Async = true }))
            {
                XDocument doc = null;
                
                // Lire l'en-tête pour déterminer le nombre total d'enregistrements
                await reader.MoveToContentAsync();
                doc = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
                
                XNamespace ns = doc.Root.GetDefaultNamespace();
                var totalRecords = doc.Root.Elements(ns + "record").Count();
                int processedRecords = 0;

                // Réinitialiser le reader pour commencer le traitement
                reader.Close();
                using (var newReader = XmlReader.Create(xmlFilePath, new XmlReaderSettings { Async = true }))
                {
                    // Utilisation d'un reader pour parcourir les éléments un par un
                    while (await newReader.ReadAsync())
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        if (newReader.NodeType == XmlNodeType.Element && newReader.Name == "record" || newReader.Name.EndsWith(":record"))
                        {
                            // Lire l'élément record complet
                            var recordElement = (XElement)XNode.ReadFrom(newReader);
                            
                            // Traiter l'enregistrement
                            var artwork = ProcessRecordElement(recordElement, ns);
                            if (artwork != null)
                            {
                                artworks.Add(artwork);
                            }

                            // Mise à jour de la progression
                            processedRecords++;
                            progressCallback?.Invoke(processedRecords, totalRecords);

                            // Libérer la mémoire régulièrement
                            if (processedRecords % 1000 == 0)
                            {
                                GC.Collect();
                            }
                        }
                    }
                }
            }

            // Construire le résultat final
            result.Artworks = artworks;
            result.Domains = new List<Domain>(_domains.Values);
            result.Techniques = new List<Technique>(_techniques.Values);
            result.Periods = new List<Period>(_periods.Values);
            result.Artists = new List<Artist>(_artists.Values);
            result.Museums = new List<Museum>(_museums.Values);

            return result;
        }

        /// <summary>
        /// Traite un élément record XML et extrait une œuvre et ses entités liées
        /// </summary>
        private Artwork ProcessRecordElement(XElement record, XNamespace ns)
        {
            try
            {
                // Extraction de l'œuvre
                var artwork = new Artwork
                {
                    Id = Guid.NewGuid(),
                    Reference = GetElementValue(record, "REF", ns),
                    InventoryNumber = GetElementValue(record, "INV", ns),
                    Title = GetElementValue(record, "TITR", ns),
                    Description = GetElementValue(record, "DESC", ns),
                    Dimensions = GetElementValue(record, "DIMS", ns),
                    CreationDate = GetElementValue(record, "DAPT", ns),
                    CreationPlace = GetElementValue(record, "LOCA", ns),
                    ConservationPlace = GetElementValue(record, "LOCA2", ns),
                    Copyright = GetElementValue(record, "COPY", ns),
                    ImageUrl = GetElementValue(record, "IMG", ns),
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                // Vérifier si l'œuvre a un minimum de données requises
                if (string.IsNullOrWhiteSpace(artwork.Reference) || 
                    (string.IsNullOrWhiteSpace(artwork.Title) && string.IsNullOrWhiteSpace(artwork.Description)))
                {
                    return null; // Ignorer les enregistrements incomplets
                }

                // Extraction du musée
                var museumName = GetElementValue(record, "LOCA2", ns);
                if (!string.IsNullOrWhiteSpace(museumName))
                {
                    if (!_museums.TryGetValue(museumName, out var museum))
                    {
                        museum = new Museum
                        {
                            Id = Guid.NewGuid(),
                            Name = museumName,
                            City = GetElementValue(record, "VILLE", ns),
                            Department = GetElementValue(record, "DEPT", ns)
                        };
                        _museums[museumName] = museum;
                    }
                }

                // Extraction des domaines
                var domainsStr = GetElementValue(record, "DOMN", ns);
                if (!string.IsNullOrWhiteSpace(domainsStr))
                {
                    foreach (var domainName in domainsStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmedName = domainName.Trim();
                        if (!_domains.TryGetValue(trimmedName, out var domain))
                        {
                            domain = new Domain
                            {
                                Id = Guid.NewGuid(),
                                Name = trimmedName
                            };
                            _domains[trimmedName] = domain;
                        }
                        artwork.Domains.Add(domain);
                    }
                }

                // Extraction des techniques
                var techniquesStr = GetElementValue(record, "TECH", ns);
                if (!string.IsNullOrWhiteSpace(techniquesStr))
                {
                    foreach (var techName in techniquesStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmedName = techName.Trim();
                        if (!_techniques.TryGetValue(trimmedName, out var technique))
                        {
                            technique = new Technique
                            {
                                Id = Guid.NewGuid(),
                                Name = trimmedName
                            };
                            _techniques[trimmedName] = technique;
                        }
                        artwork.Techniques.Add(technique);
                    }
                }

                // Extraction des périodes
                var periodsStr = GetElementValue(record, "PERI", ns);
                if (!string.IsNullOrWhiteSpace(periodsStr))
                {
                    foreach (var periodName in periodsStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmedName = periodName.Trim();
                        if (!_periods.TryGetValue(trimmedName, out var period))
                        {
                            period = new Period
                            {
                                Id = Guid.NewGuid(),
                                Name = trimmedName
                            };
                            _periods[trimmedName] = period;
                        }
                        artwork.Periods.Add(period);
                    }
                }

                // Extraction des artistes
                var artistName = GetElementValue(record, "AUTR", ns);
                if (!string.IsNullOrWhiteSpace(artistName))
                {
                    // Certains artistes sont séparés par des points-virgules
                    foreach (var name in artistName.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmedName = name.Trim();
                        
                        // Ignorer les entrées trop courtes ou non significatives
                        if (trimmedName.Length < 2 || trimmedName.ToLower() == "anonyme")
                            continue;
                        
                        // Tenter d'extraire le prénom et le nom
                        string firstName = string.Empty;
                        string lastName = trimmedName;
                        
                        // Si présence de virgule, format probable : "NOM, Prénom"
                        if (trimmedName.Contains(','))
                        {
                            var parts = trimmedName.Split(',', 2);
                            lastName = parts[0].Trim();
                            firstName = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                        }
                        else if (trimmedName.Contains(' '))
                        {
                            // Format probable : "Prénom NOM"
                            var parts = trimmedName.Split(' ', 2);
                            firstName = parts[0].Trim();
                            lastName = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                        }
                        
                        // Clé unique pour l'artiste
                        var artistKey = $"{lastName}|{firstName}".ToLower();
                        
                        if (!_artists.TryGetValue(artistKey, out var artist))
                        {
                            artist = new Artist
                            {
                                Id = Guid.NewGuid(),
                                LastName = lastName,
                                FirstName = firstName,
                                // Autres données d'artiste à extraire si disponibles
                                BirthDate = GetElementValue(record, "BORN", ns),
                                DeathDate = GetElementValue(record, "DIED", ns),
                                Nationality = GetElementValue(record, "NAT", ns)
                            };
                            _artists[artistKey] = artist;
                        }
                        
                        // Créer la relation œuvre-artiste
                        var artworkArtist = new ArtworkArtist
                        {
                            Artist = artist,
                            Role = GetElementValue(record, "ROLE", ns)
                        };
                        
                        artwork.Artists.Add(artworkArtist);
                    }
                }

                return artwork;
            }
            catch
            {
                // Log de l'erreur dans une implémentation réelle
                return null;
            }
        }

        /// <summary>
        /// Extrait la valeur d'un élément XML
        /// </summary>
        private string GetElementValue(XElement element, string name, XNamespace ns)
        {
            return element.Element(ns + name)?.Value?.Trim() ?? string.Empty;
        }
    }

    /// <summary>
    /// Résultat de l'analyse XML contenant toutes les entités extraites
    /// </summary>
    public class ParsingResult
    {
        public List<Artwork> Artworks { get; set; } = new List<Artwork>();
        public List<Artist> Artists { get; set; } = new List<Artist>();
        public List<Domain> Domains { get; set; } = new List<Domain>();
        public List<Technique> Techniques { get; set; } = new List<Technique>();
        public List<Period> Periods { get; set; } = new List<Period>();
        public List<Museum> Museums { get; set; } = new List<Museum>();
    }
}
