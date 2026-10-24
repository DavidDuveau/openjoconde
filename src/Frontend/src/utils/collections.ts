import { reactive, readonly } from 'vue';
import { estIdentifiant } from '@/utils/favoris';

/** Clé de stockage et version du format : la sauvegarde exportée reprend ce format */
export const CLE_COLLECTIONS = 'openjoconde:collections';
export const VERSION_COLLECTIONS = 1;
export const LIMITE_COLLECTIONS = 50;
export const LIMITE_OEUVRES = 500;
export const LONGUEUR_NOM = 80;
export const LONGUEUR_DESCRIPTION = 500;

export interface Collection {
  /** Identifiant local de la collection, propre à ce navigateur */
  id: string;
  nom: string;
  description: string;
  /** Identifiants des œuvres, dans l'ordre choisi */
  oeuvres: string[];
  creeLe: string;
}

export type ProblemeCollections = 'stockage' | null;

/** Pourquoi une action a été refusée */
export type RefusCollection = 'nom-vide' | 'nom-long' | 'description-longue' | 'limite-collections' | 'limite-oeuvres' | 'stockage' | 'inconnue';

export type Resultat<T = void> = { ok: true; valeur: T } | { ok: false; refus: RefusCollection };

export const MESSAGES_COLLECTIONS: Record<RefusCollection, string> = {
  'nom-vide': 'Donnez un nom à la collection.',
  'nom-long': `Le nom d'une collection ne peut pas dépasser ${LONGUEUR_NOM} caractères.`,
  'description-longue': `La description ne peut pas dépasser ${LONGUEUR_DESCRIPTION} caractères.`,
  'limite-collections': `Vous avez atteint la limite de ${LIMITE_COLLECTIONS} collections : supprimez-en une ou exportez vos collections.`,
  'limite-oeuvres': `Une collection ne peut pas contenir plus de ${LIMITE_OEUVRES} œuvres.`,
  stockage: "Les collections ne peuvent pas être gardées sur cet appareil : le stockage du navigateur est indisponible ou plein (navigation privée ?). Exportez vos collections pour ne rien perdre.",
  inconnue: "Cette collection n'existe pas ou a été supprimée."
};

const etat = reactive({ collections: [] as Collection[], probleme: null as ProblemeCollections });

export const estIdentifiantCollection = (valeur: unknown): valeur is string =>
  typeof valeur === 'string' && /^[A-Za-z0-9-]{1,64}$/.test(valeur);

/** Une collection relue ou importée, ou null si elle n'a pas la forme attendue ; rien n'est interprété comme du code */
export function collectionValide(brut: unknown): Collection | null {
  const c = brut as Partial<Collection> | null;
  if (!c || typeof c !== 'object' || !estIdentifiantCollection(c.id) || typeof c.nom !== 'string') return null;
  const nom = c.nom.trim();
  const description = typeof c.description === 'string' ? c.description : '';
  if (!nom || nom.length > LONGUEUR_NOM || description.length > LONGUEUR_DESCRIPTION || !Array.isArray(c.oeuvres)) return null;
  const vus = new Set<string>();
  for (const id of c.oeuvres) if (estIdentifiant(id)) vus.add(id);
  return {
    id: c.id,
    nom,
    description,
    oeuvres: [...vus].slice(0, LIMITE_OEUVRES),
    creeLe: typeof c.creeLe === 'string' ? c.creeLe : ''
  };
}

function lire(): Collection[] {
  try {
    const brut = window.localStorage.getItem(CLE_COLLECTIONS);
    if (!brut) return [];
    const donnees = JSON.parse(brut);
    if (donnees?.version !== VERSION_COLLECTIONS || !Array.isArray(donnees.collections)) return [];
    const ids = new Set<string>();
    const collections: Collection[] = [];
    for (const element of donnees.collections) {
      const c = collectionValide(element);
      if (!c || ids.has(c.id)) continue;
      ids.add(c.id);
      collections.push(c);
    }
    return collections.slice(0, LIMITE_COLLECTIONS);
  } catch {
    return [];
  }
}

function ecrire(collections: Collection[]): boolean {
  try {
    window.localStorage.setItem(CLE_COLLECTIONS, JSON.stringify({ version: VERSION_COLLECTIONS, collections }));
    return true;
  } catch {
    return false;
  }
}

export function chargerCollections(): void {
  etat.collections = lire();
}

chargerCollections();
if (typeof window !== 'undefined') {
  window.addEventListener('storage', evenement => {
    if (evenement.key === null || evenement.key === CLE_COLLECTIONS) chargerCollections();
  });
}

export const collections = readonly(etat);

export const trouverCollection = (id: string): Collection | undefined => etat.collections.find(c => c.id === id);

export function oublierProblemeCollections(): void {
  etat.probleme = null;
}

/** Enregistre la nouvelle liste puis l'adopte ; si le stockage refuse, rien ne change */
function appliquer(suivantes: Collection[]): boolean {
  etat.probleme = null;
  if (!ecrire(suivantes)) {
    etat.probleme = 'stockage';
    return false;
  }
  etat.collections = suivantes;
  return true;
}

const echec = (refus: RefusCollection): Resultat<never> => ({ ok: false, refus });

function verifierNom(nom: string, description: string): RefusCollection | null {
  const propre = nom.trim();
  if (!propre) return 'nom-vide';
  if (propre.length > LONGUEUR_NOM) return 'nom-long';
  if (description.length > LONGUEUR_DESCRIPTION) return 'description-longue';
  return null;
}

export function nouvelIdentifiant(): string {
  const aleatoire = globalThis.crypto?.randomUUID?.();
  return aleatoire ?? `c${Date.now().toString(36)}${Math.random().toString(36).slice(2, 10)}`;
}

export function creerCollection(nom: string, description = '', oeuvres: string[] = []): Resultat<Collection> {
  const refus = verifierNom(nom, description);
  if (refus) return echec(refus);
  if (etat.collections.length >= LIMITE_COLLECTIONS) return echec('limite-collections');
  const collection: Collection = {
    id: nouvelIdentifiant(),
    nom: nom.trim(),
    description,
    oeuvres: [...new Set(oeuvres)].slice(0, LIMITE_OEUVRES),
    creeLe: new Date().toISOString()
  };
  return appliquer([...etat.collections, collection]) ? { ok: true, valeur: collection } : echec('stockage');
}

function modifier(id: string, changer: (c: Collection) => Collection | RefusCollection): Resultat {
  const courante = trouverCollection(id);
  if (!courante) return echec('inconnue');
  const nouvelle = changer(courante);
  if (typeof nouvelle === 'string') return echec(nouvelle);
  return appliquer(etat.collections.map(c => (c.id === id ? nouvelle : c))) ? { ok: true, valeur: undefined } : echec('stockage');
}

export function renommerCollection(id: string, nom: string, description: string): Resultat {
  const refus = verifierNom(nom, description);
  if (refus) return echec(refus);
  return modifier(id, c => ({ ...c, nom: nom.trim(), description }));
}

export function supprimerCollection(id: string): Resultat {
  if (!trouverCollection(id)) return echec('inconnue');
  return appliquer(etat.collections.filter(c => c.id !== id)) ? { ok: true, valeur: undefined } : echec('stockage');
}

/** Ajoute une œuvre en fin de collection ; une œuvre déjà présente n'est pas ajoutée une seconde fois */
export function ajouterOeuvre(id: string, oeuvre: string): Resultat<boolean> {
  const courante = trouverCollection(id);
  if (!courante) return echec('inconnue');
  if (courante.oeuvres.includes(oeuvre)) return { ok: true, valeur: false };
  if (courante.oeuvres.length >= LIMITE_OEUVRES) return echec('limite-oeuvres');
  const resultat = modifier(id, c => ({ ...c, oeuvres: [...c.oeuvres, oeuvre] }));
  return resultat.ok ? { ok: true, valeur: true } : resultat;
}

export function retirerOeuvre(id: string, oeuvre: string): Resultat {
  return modifier(id, c => ({ ...c, oeuvres: c.oeuvres.filter(o => o !== oeuvre) }));
}

/** Monte (-1) ou descend (+1) une œuvre d'un cran ; sans effet aux extrémités */
export function deplacerOeuvre(id: string, oeuvre: string, sens: -1 | 1): Resultat {
  return modifier(id, c => {
    const de = c.oeuvres.indexOf(oeuvre);
    const vers = de + sens;
    if (de < 0 || vers < 0 || vers >= c.oeuvres.length) return c;
    const oeuvres = [...c.oeuvres];
    [oeuvres[de], oeuvres[vers]] = [oeuvres[vers], oeuvres[de]];
    return { ...c, oeuvres };
  });
}

/** Remplace toutes les collections (import d'une sauvegarde) ; faux, sans rien changer, si le stockage refuse */
export function definirCollections(liste: Collection[]): boolean {
  return appliquer(liste.slice(0, LIMITE_COLLECTIONS));
}
