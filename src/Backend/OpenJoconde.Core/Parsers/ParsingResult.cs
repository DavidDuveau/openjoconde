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
        public List<Artwork> Artworks { get; set; } = new List<Artwork>();
        public List<Artist> Artists { get; set; } = new List<Artist>();
        public List<Domain> Domains { get; set; } = new List<Domain>();
        public List<Technique> Techniques { get; set; } = new List<Technique>();
        public List<Period> Periods { get; set; } = new List<Period>();
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
        public int Artworks { get; init; }
        public int Artists { get; init; }
        public int Domains { get; init; }
        public int Techniques { get; init; }
        public int Periods { get; init; }
        public int Museums { get; init; }

        /// <summary>
        /// Nombre de notices du fichier qui n'ont pas pu être lues
        /// </summary>
        public int RejectedRecords { get; init; }
    }
}
