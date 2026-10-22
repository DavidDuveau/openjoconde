// Sources des données et leur licence : affichées dans le pied de page, sur les fiches et sur la page « À propos »
export const LICENCE_URL = 'https://github.com/etalab/licence-ouverte/blob/master/LO.md';

export interface Source {
  nom: string;
  description: string;
  url: string;
}

export const SOURCES: Source[] = [
  {
    nom: 'Joconde',
    description: 'Collections des musées de France (œuvres, artistes, musées déposants)',
    url: 'https://www.data.gouv.fr/datasets/collections-des-musees-de-france-base-joconde'
  },
  {
    nom: 'Muséofile',
    description: 'Répertoire des Musées de France (adresse, téléphone, site, histoire, coordonnées)',
    url: 'https://www.data.gouv.fr/datasets/musees-de-france-base-museofile'
  }
];
