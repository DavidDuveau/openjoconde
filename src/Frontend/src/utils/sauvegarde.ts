import {
  Collection, collectionValide, collections, definirCollections, LIMITE_COLLECTIONS
} from '@/utils/collections';
import { definirFavoris, estIdentifiant, Favori, favoris, LIMITE_FAVORIS } from '@/utils/favoris';

export const VERSION_SAUVEGARDE = 1;
/** Taille maximale d'un fichier de sauvegarde accepté, en octets */
export const TAILLE_MAXIMALE = 1_000_000;

export class ErreurSauvegarde extends Error {}

export interface CompteRendu {
  favorisAjoutes: number;
  favorisIgnores: number;
  collectionsAjoutees: number;
  collectionsIgnorees: number;
}

export type ModeImport = 'fusionner' | 'remplacer';

/** Le contenu du fichier d'export : favoris et collections, avec la version du format et la date d'export */
export function construireSauvegarde(maintenant: Date = new Date()) {
  return {
    format: 'openjoconde-sauvegarde',
    version: VERSION_SAUVEGARDE,
    exporteLe: maintenant.toISOString(),
    favoris: favoris.favoris.map(f => ({ id: f.id, ajouteLe: f.ajouteLe })),
    collections: collections.collections.map(c => ({ ...c, oeuvres: [...c.oeuvres] }))
  };
}

export const sauvegardeJson = (maintenant?: Date): string => JSON.stringify(construireSauvegarde(maintenant), null, 2);

export const nomFichierSauvegarde = (maintenant: Date = new Date()): string =>
  `openjoconde-sauvegarde-${maintenant.toISOString().slice(0, 10)}.json`;

interface Lue {
  favoris: Favori[];
  collections: Collection[];
  favorisIgnores: number;
  collectionsIgnorees: number;
}

/**
 * Valide un fichier avant tout changement : une erreur de forme générale (taille, JSON, version, listes) refuse tout le
 * fichier ; un élément mal formé est écarté et compté, sans empêcher les autres.
 */
export function lireSauvegarde(texte: string): Lue {
  if (texte.length > TAILLE_MAXIMALE) {
    throw new ErreurSauvegarde('Ce fichier est trop gros pour être une sauvegarde OpenJoconde.');
  }
  let donnees: unknown;
  try {
    donnees = JSON.parse(texte);
  } catch {
    throw new ErreurSauvegarde("Ce fichier n'est pas un fichier de sauvegarde valide (JSON illisible).");
  }
  const d = donnees as { format?: unknown; version?: unknown; favoris?: unknown; collections?: unknown } | null;
  if (!d || typeof d !== 'object' || Array.isArray(d) || d.format !== 'openjoconde-sauvegarde') {
    throw new ErreurSauvegarde("Ce fichier n'est pas une sauvegarde OpenJoconde.");
  }
  if (d.version !== VERSION_SAUVEGARDE) {
    throw new ErreurSauvegarde(`La version ${String(d.version)} de ce fichier n'est pas prise en charge (version attendue : ${VERSION_SAUVEGARDE}).`);
  }
  if (!Array.isArray(d.favoris) || !Array.isArray(d.collections)) {
    throw new ErreurSauvegarde('Ce fichier de sauvegarde est incomplet : il manque la liste des favoris ou des collections.');
  }

  const vus = new Set<string>();
  const lesFavoris: Favori[] = [];
  let favorisIgnores = 0;
  for (const element of d.favoris) {
    const f = element as { id?: unknown; ajouteLe?: unknown } | null;
    if (!f || !estIdentifiant(f.id) || vus.has(f.id)) {
      favorisIgnores++;
      continue;
    }
    vus.add(f.id);
    lesFavoris.push({ id: f.id, ajouteLe: typeof f.ajouteLe === 'string' ? f.ajouteLe : '' });
  }

  const ids = new Set<string>();
  const lesCollections: Collection[] = [];
  let collectionsIgnorees = 0;
  for (const element of d.collections) {
    const c = collectionValide(element);
    if (!c || ids.has(c.id)) {
      collectionsIgnorees++;
      continue;
    }
    ids.add(c.id);
    lesCollections.push(c);
  }
  return { favoris: lesFavoris, collections: lesCollections, favorisIgnores, collectionsIgnorees };
}

/**
 * Importe une sauvegarde. Fusionner garde les données actuelles et ajoute le reste (une collection de même identifiant
 * est ignorée, les favoris en trop au-delà de la limite aussi) ; remplacer substitue le fichier aux données actuelles.
 * Rien n'est modifié si le stockage refuse.
 */
export function importerSauvegarde(texte: string, mode: ModeImport): CompteRendu {
  const lue = lireSauvegarde(texte);
  let suiteFavoris: Favori[];
  let suiteCollections: Collection[];
  const compte: CompteRendu = { favorisAjoutes: 0, favorisIgnores: lue.favorisIgnores, collectionsAjoutees: 0, collectionsIgnorees: lue.collectionsIgnorees };

  if (mode === 'remplacer') {
    suiteFavoris = lue.favoris.slice(0, LIMITE_FAVORIS);
    suiteCollections = lue.collections.slice(0, LIMITE_COLLECTIONS);
    compte.favorisIgnores += lue.favoris.length - suiteFavoris.length;
    compte.collectionsIgnorees += lue.collections.length - suiteCollections.length;
  } else {
    suiteFavoris = favoris.favoris.map(f => ({ ...f }));
    const connus = new Set(suiteFavoris.map(f => f.id));
    for (const f of lue.favoris) {
      if (connus.has(f.id) || suiteFavoris.length >= LIMITE_FAVORIS) {
        compte.favorisIgnores++;
        continue;
      }
      connus.add(f.id);
      suiteFavoris.push(f);
    }
    suiteCollections = collections.collections.map(c => ({ ...c, oeuvres: [...c.oeuvres] }));
    const idsConnus = new Set(suiteCollections.map(c => c.id));
    for (const c of lue.collections) {
      if (idsConnus.has(c.id) || suiteCollections.length >= LIMITE_COLLECTIONS) {
        compte.collectionsIgnorees++;
        continue;
      }
      idsConnus.add(c.id);
      suiteCollections.push(c);
    }
  }
  compte.favorisAjoutes = mode === 'remplacer' ? suiteFavoris.length : suiteFavoris.length - favoris.favoris.length;
  compte.collectionsAjoutees = mode === 'remplacer' ? suiteCollections.length : suiteCollections.length - collections.collections.length;

  const anciensFavoris = favoris.favoris.map(f => ({ ...f }));
  if (!definirFavoris(suiteFavoris)) {
    throw new ErreurSauvegarde("Le stockage du navigateur est indisponible ou plein : rien n'a été importé.");
  }
  if (!definirCollections(suiteCollections)) {
    definirFavoris(anciensFavoris);
    throw new ErreurSauvegarde("Le stockage du navigateur est indisponible ou plein : rien n'a été importé.");
  }
  return compte;
}
