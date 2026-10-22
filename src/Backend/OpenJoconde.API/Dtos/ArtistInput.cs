using OpenJoconde.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace OpenJoconde.API.Dtos
{
    /// <summary>
    /// Ce qu'un appelant peut fournir pour créer ou modifier un artiste : ni identifiant, ni dates, ni œuvres liées.
    /// </summary>
    public class ArtistInput
    {
        /// <summary>Nom de famille (obligatoire)</summary>
        [Required, MaxLength(200)] public string LastName { get; set; } = string.Empty;
        /// <summary>Prénom</summary>
        [MaxLength(200)] public string? FirstName { get; set; }
        /// <summary>Date de naissance, en texte libre</summary>
        [MaxLength(100)] public string? BirthDate { get; set; }
        /// <summary>Date de décès, en texte libre</summary>
        [MaxLength(100)] public string? DeathDate { get; set; }
        /// <summary>Nationalité</summary>
        [MaxLength(200)] public string? Nationality { get; set; }
        /// <summary>Biographie</summary>
        [MaxLength(100000)] public string? Biography { get; set; }

        /// <summary>Reporte les champs fournis sur l'artiste ; l'identifiant, les dates et les œuvres ne bougent pas</summary>
        public void ApplyTo(Artist artist)
        {
            artist.LastName = LastName.Trim();
            artist.FirstName = FirstName;
            artist.BirthDate = BirthDate;
            artist.DeathDate = DeathDate;
            artist.Nationality = Nationality;
            artist.Biography = Biography;
        }
    }
}
