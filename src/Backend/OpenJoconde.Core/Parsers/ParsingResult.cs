using OpenJoconde.Core.Models;
using System.Collections.Generic;

namespace OpenJoconde.Core.Parsers
{
    /// <summary>
    /// Résultat de l'analyse d'un fichier contenant toutes les entités extraites
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
}
