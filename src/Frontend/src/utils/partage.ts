import { estIdentifiant } from '@/utils/favoris';
import { LIMITE_OEUVRES, LONGUEUR_DESCRIPTION, LONGUEUR_NOM } from '@/utils/collections';

/** Au-delà, une collection est trop grande pour un lien : on propose l'export */
export const LIMITE_PARTAGE = 100;
/** Longueur maximale du fragment accepté à la lecture d'un lien */
export const LONGUEUR_FRAGMENT = 8000;
const VERSION_PARTAGE = 1;

export class ErreurPartage extends Error {}

export interface CollectionPartagee {
  nom: string;
  description: string;
  oeuvres: string[];
}

const base64Url = (octets: Uint8Array): string => {
  let binaire = '';
  octets.forEach(o => { binaire += String.fromCharCode(o); });
  return btoa(binaire).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
};

const depuisBase64Url = (texte: string): Uint8Array => {
  const binaire = atob(texte.replace(/-/g, '+').replace(/_/g, '/'));
  return Uint8Array.from(binaire, c => c.charCodeAt(0));
};

/** Le fragment (après le « # ») qui porte la collection : nom, description et identifiants des œuvres, sans les tirets */
export function encoderPartage(collection: CollectionPartagee): string | null {
  if (collection.oeuvres.length > LIMITE_PARTAGE) return null;
  const json = JSON.stringify({
    v: VERSION_PARTAGE,
    n: collection.nom,
    d: collection.description,
    o: collection.oeuvres.map(id => id.replace(/-/g, ''))
  });
  return base64Url(new TextEncoder().encode(json));
}

const reformer = (hex: string): string =>
  `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;

/** Lit un fragment de lien ; tout ce qui n'a pas la forme attendue est refusé, rien n'est interprété */
export function decoderPartage(fragment: string): CollectionPartagee {
  const texte = fragment.replace(/^#/, '');
  if (!texte) throw new ErreurPartage("Ce lien ne contient aucune collection.");
  if (texte.length > LONGUEUR_FRAGMENT) throw new ErreurPartage('Ce lien est trop long pour être une collection partagée.');
  if (!/^[A-Za-z0-9_-]+$/.test(texte)) throw new ErreurPartage("Ce lien n'est pas un lien de collection valide.");

  let donnees: { v?: unknown; n?: unknown; d?: unknown; o?: unknown };
  try {
    donnees = JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(depuisBase64Url(texte)));
  } catch {
    throw new ErreurPartage("Ce lien n'est pas un lien de collection valide.");
  }
  if (!donnees || typeof donnees !== 'object' || donnees.v !== VERSION_PARTAGE) {
    throw new ErreurPartage("La version de ce lien n'est pas prise en charge.");
  }
  const { n, d, o } = donnees;
  if (typeof n !== 'string' || !n.trim() || n.length > LONGUEUR_NOM || typeof d !== 'string' || d.length > LONGUEUR_DESCRIPTION
    || !Array.isArray(o) || o.length > Math.min(LIMITE_PARTAGE, LIMITE_OEUVRES)) {
    throw new ErreurPartage("Ce lien n'est pas un lien de collection valide.");
  }
  const oeuvres: string[] = [];
  for (const hex of o) {
    const id = typeof hex === 'string' && /^[0-9a-f]{32}$/i.test(hex) ? reformer(hex) : '';
    if (!estIdentifiant(id)) throw new ErreurPartage("Ce lien n'est pas un lien de collection valide.");
    if (!oeuvres.includes(id)) oeuvres.push(id);
  }
  return { nom: n.trim(), description: d, oeuvres };
}

/** Adresse de la page de lecture d'une collection partagée, la collection étant dans le fragment */
export function adressePartage(collection: CollectionPartagee, origine: string, base: string): string | null {
  const fragment = encoderPartage(collection);
  return fragment === null ? null : `${origine}${base.replace(/\/$/, '')}/partage#${fragment}`;
}
