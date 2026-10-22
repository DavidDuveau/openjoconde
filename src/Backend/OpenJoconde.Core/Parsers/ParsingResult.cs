using OpenJoconde.Core.Models;
using System.Collections.Generic;

namespace OpenJoconde.Core.Parsers
{
    /// <summary>
    /// Un lot de notices lues dans un fichier : ses œuvres et les entités liées rencontrées pour la première fois
    /// dans ce lot (celles déjà rendues par un lot précédent n'y figurent plus)
    /// </summary>
    public class ParsingResult
    {
        /// <summary>Œuvres du lot</summary>
        public List<Artwork> Artworks { get; set; } = new List<Artwork>();
        /// <summary>Artistes vus pour la première fois dans le lot</summary>
        public List<Artist> Artists { get; set; } = new List<Artist>();
        /// <summary>Domaines vus pour la première fois dans le lot</summary>
        public List<Domain> Domains { get; set; } = new List<Domain>();
        /// <summary>Techniques vues pour la première fois dans le lot</summary>
        public List<Technique> Techniques { get; set; } = new List<Technique>();
        /// <summary>Périodes vues pour la première fois dans le lot</summary>
        public List<Period> Periods { get; set; } = new List<Period>();
        /// <summary>Musées vus pour la première fois dans le lot</summary>
        public List<Museum> Museums { get; set; } = new List<Museum>();

        /// <summary>
        /// Nombre de notices du fichier qui n'ont pas pu être lues (absentes des listes ci-dessus)
        /// </summary>
        public int RejectedRecords { get; set; }
    }

    /// <summary>
    /// Totaux d'un fichier lu en entier
    /// </summary>
    public class ParsingSummary
    {
        /// <summary>Nombre d'œuvres lues</summary>
        public int Artworks { get; init; }
        /// <summary>Nombre d'artistes distincts lus</summary>
        public int Artists { get; init; }
        /// <summary>Nombre de domaines distincts lus</summary>
        public int Domains { get; init; }
        /// <summary>Nombre de techniques distinctes lues</summary>
        public int Techniques { get; init; }
        /// <summary>Nombre de périodes distinctes lues</summary>
        public int Periods { get; init; }
        /// <summary>Nombre de musées distincts lus</summary>
        public int Museums { get; init; }

        /// <summary>
        /// Nombre de notices du fichier qui n'ont pas pu être lues
        /// </summary>
        public int RejectedRecords { get; init; }
    }
}
