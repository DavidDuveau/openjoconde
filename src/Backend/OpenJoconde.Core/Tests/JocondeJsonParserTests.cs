using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenJoconde.Core.Models;
using OpenJoconde.Core.Parsers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenJoconde.Core.Tests
{
    [TestClass]
    public class JocondeJsonParserTests
    {
        private string CreateTestJsonFile()
        {
            // Création d'un fichier JSON de test
            string json = @"{
                ""total_count"": 713915,
                ""results"": [
                    {
                        ""reference"": ""50510016755"",
                        ""ancien_depot"": null,
                        ""appellation"": null,
                        ""ancienne_appartenance"": ""Polakovits Mathias"",
                        ""ancienne_attribution"": null,
                        ""auteur"": ""DUBREUIL Toussaint"",
                        ""bibliographie"": null,
                        ""commentaires"": null,
                        ""presence_image"": ""non"",
                        ""date_d_acquisition"": ""1987 entrée matérielle"",
                        ""date_de_depot"": null,
                        ""decouverte_collecte"": null,
                        ""denomination"": ""recto verso"",
                        ""lieu_de_depot"": null,
                        ""description"": ""plume et encre, lavis brun, tracé préparatoire à la sanguine"",
                        ""mesures"": ""H. 28.9 ; L. 41.5"",
                        ""date_de_mise_a_jour"": ""2022-07-22"",
                        ""date_creation"": ""2002-05-21"",
                        ""domaine"": [
                            ""dessin""
                        ],
                        ""region"": ""Ile-de-France"",
                        ""departement"": ""Paris"",
                        ""date_sujet_represente"": null,
                        ""ecole_pays"": ""France"",
                        ""epoque"": null,
                        ""exposition"": ""Maîtres français 1550-1800, dessins de la donation Mathias Polakovits à l'Ecole des Beaux-Arts."",
                        ""genese"": null,
                        ""geographie_historique"": null,
                        ""inscription"": null,
                        ""numero_inventaire"": ""PM 454"",
                        ""appellation_musee_de_france"": null,
                        ""lien_base_arcade"": null,
                        ""lieu_de_creation_utilisation"": null,
                        ""localisation"": ""Paris ; musée de l'Ecole nationale supérieure des beaux-arts"",
                        ""ville"": ""Paris"",
                        ""lien_video"": null,
                        ""manquant"": null,
                        ""manquant_com"": null,
                        ""millesime_de_creation"": null,
                        ""millesime_d_utilisation"": null,
                        ""code_museofile"": ""M5051"",
                        ""nom_officiel_musee"": ""musée de l'Ecole nationale supérieure des beaux-arts"",
                        ""genre"": null,
                        ""onomastique"": null,
                        ""precisions_sur_l_auteur"": ""Paris, 1561 ; Paris, 1602"",
                        ""precisions_decouverte_collecte"": null,
                        ""periode_de_l_original_copie"": null,
                        ""periode_de_creation"": ""2e moitié 16e siècle"",
                        ""periode_d_utilisation"": null,
                        ""precisions_inscriptions"": null,
                        ""precisions_lieux_creations"": null,
                        ""precisions_sujets_representes"": null,
                        ""precisions_utilisation"": null,
                        ""references_memoires"": null,
                        ""references_merimee"": null,
                        ""reference_maj"": null,
                        ""references_palissy"": null,
                        ""sujet_represente"": ""figure (grotesque)"",
                        ""lien_inha"": null,
                        ""source_de_la_representation"": null,
                        ""statut_juridique"": ""propriété de l'Etat;don manuel;musée de l'Ecole nationale supérieure des beaux-arts"",
                        ""materiaux_techniques"": [
                            ""plume, encre, lavis brun, sanguine""
                        ],
                        ""titre"": ""Grotesques ; Grotesques (verso)"",
                        ""utilisation"": null,
                        ""lien_site_associe"": null,
                        ""coordonnees"": {
                            ""lon"": 2.33167,
                            ""lat"": 48.845748
                        },
                        ""artiste_sous_droits"": null,
                        ""date_entree_dans_le_domaine_public"": null
                    }
                ]
            }";

            string tempPath = Path.GetTempFileName();
            File.WriteAllText(tempPath, json);
            return tempPath;
        }

        [TestMethod]
        public async Task ParseAsync_ValidFile_ReturnsCorrectData()
        {
            // Arrange
            string testFile = CreateTestJsonFile();
            try
            {
                var parser = new JocondeJsonParser();
                int progressCalls = 0;
                Action<int, int> progressCallback = (current, total) => { progressCalls++; };

                // Act
                var result = await parser.ParseAsync(testFile, progressCallback);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(1, result.Artworks.Count);
                
                // Vérification de l'œuvre
                var artwork = result.Artworks[0];
                Assert.AreEqual("50510016755", artwork.Reference);
                Assert.AreEqual("PM 454", artwork.InventoryNumber);
                Assert.AreEqual("recto verso", artwork.Denomination);
                Assert.AreEqual("Grotesques ; Grotesques (verso)", artwork.Title);
                Assert.AreEqual("plume et encre, lavis brun, tracé préparatoire à la sanguine", artwork.Description);
                Assert.AreEqual("H. 28.9 ; L. 41.5", artwork.Dimensions);
                Assert.AreEqual("2e moitié 16e siècle", artwork.CreationDate);
                Assert.AreEqual("Paris ; musée de l'Ecole nationale supérieure des beaux-arts", artwork.ConservationPlace);
                
                // Vérification des artistes
                Assert.AreEqual(1, result.Artists.Count);
                var artist = result.Artists[0];
                Assert.AreEqual("DUBREUIL Toussaint", artist.LastName);
                
                // Vérification des domaines
                Assert.AreEqual(1, result.Domains.Count);
                var domain = result.Domains[0];
                Assert.AreEqual("dessin", domain.Name);
                
                // Vérification des techniques
                Assert.IsTrue(result.Techniques.Count > 0);
                
                // Vérification des musées
                Assert.AreEqual(1, result.Museums.Count);
                var museum = result.Museums[0];
                Assert.AreEqual("musée de l'Ecole nationale supérieure des beaux-arts", museum.Name);
                Assert.AreEqual("Paris", museum.City);
                Assert.AreEqual("M5051", museum.Code);
                Assert.AreEqual(2.33167, museum.Longitude);
                Assert.AreEqual(48.845748, museum.Latitude);
                
                // Vérification du callback de progression
                Assert.AreEqual(1, progressCalls);
            }
            finally
            {
                // Nettoyage
                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                }
            }
        }
    }
}
