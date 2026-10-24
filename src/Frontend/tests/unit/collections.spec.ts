import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  ajouterOeuvre, chargerCollections, CLE_COLLECTIONS, collections, creerCollection, definirCollections, deplacerOeuvre,
  LIMITE_COLLECTIONS, LIMITE_OEUVRES, nouvelIdentifiant, oublierProblemeCollections, renommerCollection, retirerOeuvre,
  supprimerCollection, trouverCollection
} from '@/utils/collections';

const id = (n: number) => `00000000-0000-0000-0000-${String(n).padStart(12, '0')}`;

function creer(nom = 'Impressionnisme', oeuvres: string[] = []) {
  const r = creerCollection(nom, '', oeuvres);
  if (!r.ok) throw new Error(`échec : ${r.refus}`);
  return r.valeur;
}

describe('collections', () => {
  beforeEach(() => {
    window.localStorage.clear();
    chargerCollections();
    oublierProblemeCollections();
  });
  afterEach(() => vi.restoreAllMocks());

  it('crée une collection avec son nom et sa description, et la garde après rechargement', () => {
    const r = creerCollection('  Paysages  ', 'Vues de Normandie');

    expect(r.ok).toBe(true);
    chargerCollections();
    expect(collections.collections).toHaveLength(1);
    expect(collections.collections[0]).toMatchObject({ nom: 'Paysages', description: 'Vues de Normandie', oeuvres: [] });
    expect(JSON.parse(window.localStorage.getItem(CLE_COLLECTIONS)!).version).toBe(1);
  });

  it.each([
    ['un nom vide', '   ', '', 'nom-vide'],
    ['un nom de plus de 80 caractères', 'x'.repeat(81), '', 'nom-long'],
    ['une description de plus de 500 caractères', 'Nom', 'y'.repeat(501), 'description-longue']
  ])('refuse %s', (_cas, nom, description, refus) => {
    expect(creerCollection(nom, description)).toEqual({ ok: false, refus });
    expect(collections.collections).toHaveLength(0);
  });

  it('accepte un nom de 80 caractères', () => {
    expect(creerCollection('x'.repeat(80)).ok).toBe(true);
  });

  it('refuse au-delà de 50 collections', () => {
    for (let n = 0; n < LIMITE_COLLECTIONS; n++) creer(`C${n}`);

    expect(creerCollection('de trop')).toEqual({ ok: false, refus: 'limite-collections' });
  });

  it('renomme une collection, et refuse un nom invalide ou une collection inconnue', () => {
    const c = creer();

    expect(renommerCollection(c.id, 'Nouveau', 'Texte').ok).toBe(true);
    expect(trouverCollection(c.id)).toMatchObject({ nom: 'Nouveau', description: 'Texte' });
    expect(renommerCollection(c.id, '', '')).toEqual({ ok: false, refus: 'nom-vide' });
    expect(renommerCollection('inconnue', 'X', '')).toEqual({ ok: false, refus: 'inconnue' });
  });

  it('supprime une collection', () => {
    const c = creer();

    expect(supprimerCollection(c.id).ok).toBe(true);
    expect(collections.collections).toHaveLength(0);
    expect(supprimerCollection(c.id)).toEqual({ ok: false, refus: 'inconnue' });
  });

  it("ajoute une œuvre à plusieurs collections, mais pas deux fois à la même", () => {
    const a = creer('A');
    const b = creer('B');

    expect(ajouterOeuvre(a.id, id(1))).toEqual({ ok: true, valeur: true });
    expect(ajouterOeuvre(b.id, id(1))).toEqual({ ok: true, valeur: true });
    expect(ajouterOeuvre(a.id, id(1))).toEqual({ ok: true, valeur: false });
    expect(trouverCollection(a.id)!.oeuvres).toEqual([id(1)]);
    expect(ajouterOeuvre('inconnue', id(1))).toEqual({ ok: false, refus: 'inconnue' });
  });

  it('refuse au-delà de 500 œuvres par collection', () => {
    const c = creer('Pleine', Array.from({ length: LIMITE_OEUVRES }, (_, i) => id(i + 1)));

    expect(ajouterOeuvre(c.id, id(LIMITE_OEUVRES + 1))).toEqual({ ok: false, refus: 'limite-oeuvres' });
  });

  it('retire une œuvre et en déplace une d’un cran, sans effet aux extrémités', () => {
    const c = creer('Ordre', [id(1), id(2), id(3)]);

    deplacerOeuvre(c.id, id(3), -1);
    expect(trouverCollection(c.id)!.oeuvres).toEqual([id(1), id(3), id(2)]);
    deplacerOeuvre(c.id, id(1), -1);
    deplacerOeuvre(c.id, id(2), 1);
    deplacerOeuvre(c.id, id(99), 1);
    expect(trouverCollection(c.id)!.oeuvres).toEqual([id(1), id(3), id(2)]);
    deplacerOeuvre(c.id, id(1), 1);
    expect(trouverCollection(c.id)!.oeuvres).toEqual([id(3), id(1), id(2)]);

    retirerOeuvre(c.id, id(1));
    expect(trouverCollection(c.id)!.oeuvres).toEqual([id(3), id(2)]);
  });

  it("ne change rien et le dit quand le stockage refuse l'écriture", () => {
    const c = creer('Stable', [id(1)]);
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('plein'); });

    expect(creerCollection('Autre')).toEqual({ ok: false, refus: 'stockage' });
    expect(ajouterOeuvre(c.id, id(2))).toEqual({ ok: false, refus: 'stockage' });
    expect(collections.probleme).toBe('stockage');
    expect(collections.collections).toHaveLength(1);
    expect(trouverCollection(c.id)!.oeuvres).toEqual([id(1)]);
  });

  it('survit à un stockage illisible ou mal formé', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => { throw new Error('bloqué'); });
    chargerCollections();
    expect(collections.collections).toEqual([]);
    vi.restoreAllMocks();

    for (const contenu of ['pas du json', JSON.stringify({ version: 9, collections: [] }), JSON.stringify({ version: 1 })]) {
      window.localStorage.setItem(CLE_COLLECTIONS, contenu);
      chargerCollections();
      expect(collections.collections).toEqual([]);
    }
  });

  it('écarte les collections invalides, les doublons et les identifiants d’œuvre invalides', () => {
    window.localStorage.setItem(CLE_COLLECTIONS, JSON.stringify({
      version: 1,
      collections: [
        { id: 'c1', nom: 'Bonne', description: 'd', oeuvres: [id(1), id(1), '<script>', 5], creeLe: 'hier' },
        { id: 'c1', nom: 'Doublon', oeuvres: [] },
        { id: 'c2', nom: '', oeuvres: [] },
        { id: 'c3', nom: 'x'.repeat(81), oeuvres: [] },
        { id: 'c4 espace', nom: 'Mauvais id', oeuvres: [] },
        { id: 'c5', nom: 'Sans liste' },
        { id: 'c6', nom: 'Description longue', description: 'z'.repeat(501), oeuvres: [] },
        { id: 'c7', nom: 'Sans description', oeuvres: [id(2)] },
        null
      ]
    }));

    chargerCollections();

    expect(collections.collections.map(c => c.nom)).toEqual(['Bonne', 'Sans description']);
    expect(collections.collections[0].oeuvres).toEqual([id(1)]);
    expect(collections.collections[1]).toMatchObject({ description: '', creeLe: '' });
  });

  it('se met à jour quand un autre onglet modifie les collections, et ignore les autres clés', () => {
    window.localStorage.setItem(CLE_COLLECTIONS, JSON.stringify({ version: 1, collections: [{ id: 'c1', nom: 'Ailleurs', oeuvres: [] }] }));

    window.dispatchEvent(new StorageEvent('storage', { key: 'autre' }));
    expect(collections.collections).toHaveLength(0);
    window.dispatchEvent(new StorageEvent('storage', { key: CLE_COLLECTIONS }));
    expect(collections.collections).toHaveLength(1);
  });

  it("produit des identifiants distincts, même sans randomUUID", () => {
    expect(nouvelIdentifiant()).not.toBe(nouvelIdentifiant());
    vi.stubGlobal('crypto', undefined);
    expect(nouvelIdentifiant()).toMatch(/^c[a-z0-9]+$/);
    vi.unstubAllGlobals();
  });

  it('remplace toutes les collections en respectant la limite', () => {
    const liste = Array.from({ length: LIMITE_COLLECTIONS + 5 }, (_, i) => ({ id: `c${i}`, nom: `C${i}`, description: '', oeuvres: [], creeLe: '' }));

    expect(definirCollections(liste)).toBe(true);
    expect(collections.collections).toHaveLength(LIMITE_COLLECTIONS);
  });
});
