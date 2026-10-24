import { reactive, readonly } from 'vue';

/** Clé de stockage et version du format : l'export de US-COMPTE-03 reprendra ce format */
export const CLE_FAVORIS = 'openjoconde:favoris';
export const VERSION_FAVORIS = 1;
export const LIMITE_FAVORIS = 500;

export interface Favori {
  id: string;
  /** Date d'ajout, ISO 8601 */
  ajouteLe: string;
}

/** Pourquoi une écriture a échoué : stockage absent ou plein, ou limite de favoris atteinte */
export type ProblemeFavoris = 'stockage' | 'limite' | null;

const etat = reactive({ favoris: [] as Favori[], probleme: null as ProblemeFavoris });

const estIdentifiant = (valeur: unknown): valeur is string =>
  typeof valeur === 'string' && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(valeur);

/** Relit les favoris du navigateur ; tout ce qui n'a pas la forme attendue est écarté, jamais interprété */
function lire(): Favori[] {
  try {
    const brut = window.localStorage.getItem(CLE_FAVORIS);
    if (!brut) return [];
    const donnees = JSON.parse(brut);
    if (donnees?.version !== VERSION_FAVORIS || !Array.isArray(donnees.favoris)) return [];
    const vus = new Set<string>();
    const favoris: Favori[] = [];
    for (const element of donnees.favoris) {
      if (!estIdentifiant(element?.id) || vus.has(element.id)) continue;
      vus.add(element.id);
      favoris.push({ id: element.id, ajouteLe: typeof element.ajouteLe === 'string' ? element.ajouteLe : '' });
    }
    return favoris.slice(0, LIMITE_FAVORIS);
  } catch {
    return [];
  }
}

function ecrire(favoris: Favori[]): boolean {
  try {
    window.localStorage.setItem(CLE_FAVORIS, JSON.stringify({ version: VERSION_FAVORIS, favoris }));
    return true;
  } catch {
    return false;
  }
}

/** Recharge l'état depuis le navigateur (au démarrage, et quand un autre onglet modifie les favoris) */
export function chargerFavoris(): void {
  etat.favoris = lire();
}

chargerFavoris();
if (typeof window !== 'undefined') {
  window.addEventListener('storage', evenement => {
    if (evenement.key === null || evenement.key === CLE_FAVORIS) chargerFavoris();
  });
}

/** Favoris, du plus récemment ajouté au plus ancien */
export const favoris = readonly(etat);

export const estFavori = (id: string): boolean => etat.favoris.some(f => f.id === id);

export function oublierProbleme(): void {
  etat.probleme = null;
}

/** Ajoute un favori ; faux, avec le problème en état, si le stockage ou la limite l'interdit */
export function ajouterFavori(id: string): boolean {
  etat.probleme = null;
  if (estFavori(id)) return true;
  if (etat.favoris.length >= LIMITE_FAVORIS) {
    etat.probleme = 'limite';
    return false;
  }
  const suivants = [{ id, ajouteLe: new Date().toISOString() }, ...etat.favoris];
  if (!ecrire(suivants)) {
    etat.probleme = 'stockage';
    return false;
  }
  etat.favoris = suivants;
  return true;
}

/** Retire un favori ; si le stockage refuse, l'œuvre disparaît tout de même de la session */
export function retirerFavori(id: string): void {
  etat.probleme = null;
  const suivants = etat.favoris.filter(f => f.id !== id);
  if (!ecrire(suivants)) etat.probleme = 'stockage';
  etat.favoris = suivants;
}

export function basculerFavori(id: string): boolean {
  if (estFavori(id)) {
    retirerFavori(id);
    return true;
  }
  return ajouterFavori(id);
}

export const MESSAGES_FAVORIS: Record<Exclude<ProblemeFavoris, null>, string> = {
  stockage: "Les favoris ne peuvent pas être gardés sur cet appareil : le stockage du navigateur est indisponible ou plein (navigation privée ?).",
  limite: `Vous avez atteint la limite de ${LIMITE_FAVORIS} favoris : retirez-en pour en ajouter d'autres.`
};
