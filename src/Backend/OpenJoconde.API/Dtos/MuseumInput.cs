using OpenJoconde.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace OpenJoconde.API.Dtos
{
    /// <summary>
    /// Ce qu'un appelant peut fournir pour créer ou modifier un musée : ni identifiant ni dates, que le serveur maîtrise.
    /// Les longueurs sont celles des colonnes.
    /// </summary>
    public class MuseumInput
    {
        /// <summary>Nom du musée (obligatoire)</summary>
        [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
        /// <summary>Ville</summary>
        [MaxLength(200)] public string? City { get; set; }
        /// <summary>Département</summary>
        [MaxLength(200)] public string? Department { get; set; }
        /// <summary>Région</summary>
        [MaxLength(200)] public string? Region { get; set; }
        /// <summary>Adresse postale</summary>
        [MaxLength(10000)] public string? Address { get; set; }
        /// <summary>Code postal</summary>
        [MaxLength(20)] public string? ZipCode { get; set; }
        /// <summary>Téléphone</summary>
        [MaxLength(50)] public string? Phone { get; set; }
        /// <summary>Courriel</summary>
        [MaxLength(200)] public string? Email { get; set; }
        /// <summary>Adresse du site officiel</summary>
        [MaxLength(500)] public string? Website { get; set; }
        /// <summary>Histoire et description du musée</summary>
        [MaxLength(100000)] public string? Description { get; set; }
        /// <summary>Code Muséofile</summary>
        [MaxLength(50)] public string? Code { get; set; }
        /// <summary>Longitude, en degrés (-180 à 180)</summary>
        [Range(-180, 180)] public double? Longitude { get; set; }
        /// <summary>Latitude, en degrés (-90 à 90)</summary>
        [Range(-90, 90)] public double? Latitude { get; set; }

        /// <summary>Reporte les champs fournis sur le musée ; l'identifiant et les dates ne bougent pas</summary>
        public void ApplyTo(Museum museum)
        {
            museum.Name = Name.Trim();
            museum.City = City;
            museum.Department = Department;
            museum.Region = Region;
            museum.Address = Address;
            museum.ZipCode = ZipCode;
            museum.Phone = Phone;
            museum.Email = Email;
            museum.Website = Website;
            museum.Description = Description;
            museum.Code = Code;
            museum.Longitude = Longitude;
            museum.Latitude = Latitude;
        }
    }
}
