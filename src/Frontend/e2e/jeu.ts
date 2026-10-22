/**
 * Jeu de notices de test, connu à l'avance : les scénarios en tirent leurs valeurs attendues, sans les demander à
 * l'API qu'ils vérifient. Les titres mêlent accents, majuscules accentuées et « œ » : la base trie en ICU fr-FR, un
 * tri par code de caractère les rangerait autrement.
 */
export const TITRES = [
  'Les Baigneuses', 'Portrait de femme', 'Nature morte aux pommes', 'Paysage de neige', 'La Liseuse', 'Vase décoré',
  'Étude de nu', 'Eventail japonais', 'Œuvre de jeunesse', 'Orphée', 'Zèbre au pré'
];

export const NOMBRE = 24;

export interface Notice {
  numero: number;
  reference: string;
  /** Vide pour une notice sans titre (une sur six) */
  titre: string;
  /** Nom de famille de l'artiste (cinq artistes, un prénom commun « Prénom ») */
  auteur: string;
  domaine: string;
  technique: string;
  periode: string;
  musee: string;
}

export const NOTICES: Notice[] = Array.from({ length: NOMBRE }, (_, i) => {
  const numero = i + 1;
  return {
    numero,
    reference: `E2E${String(numero).padStart(5, '0')}`,
    titre: numero % 6 === 0 ? '' : `${TITRES[numero % TITRES.length]} ${numero}`,
    auteur: `AUTEUR${numero % 5}`,
    domaine: `domaine ${numero % 3}`,
    technique: `technique ${numero % 4}`,
    periode: `période ${numero % 3}`,
    musee: `musée ${numero % 3}`
  };
});

/** Ordre alphabétique français (ICU), comme la base ; les notices sans titre ferment toujours la liste */
export function titresTries(sens: 'asc' | 'desc', parmi: Notice[] = NOTICES): string[] {
  const collateur = new Intl.Collator('fr');
  const titres = parmi.filter(n => n.titre).map(n => n.titre).sort((a, b) => collateur.compare(a, b));
  const sans = parmi.filter(n => !n.titre).map(() => '');
  return [...(sens === 'asc' ? titres : titres.reverse()), ...sans];
}

/** Notices dont le titre porte un mot qui commence par le terme (sans tenir compte de la casse ni des accents) */
export function parMotDuTitre(terme: string): Notice[] {
  const normaliser = (texte: string) => texte.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase();
  const debut = normaliser(terme);
  return NOTICES.filter(n => normaliser(n.titre).split(/[^\p{L}\p{N}]+/u).some(mot => mot.startsWith(debut)));
}
