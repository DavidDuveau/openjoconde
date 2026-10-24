import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ajouterOeuvre, chargerCollections, collections, creerCollection, LIMITE_COLLECTIONS } from '@/utils/collections';
import { ajouterFavori, chargerFavoris, estFavori, favoris, LIMITE_FAVORIS } from '@/utils/favoris';
import {
  construireSauvegarde, ErreurSauvegarde, importerSauvegarde, lireSauvegarde, nomFichierSauvegarde, sauvegardeJson, TAILLE_MAXIMALE
} from '@/utils/sauvegarde';

const id = (n: number) => `00000000-0000-0000-0000-${String(n).padStart(12, '0')}`;
const fichier = (extra: Record<string, unknown> = {}) => JSON.stringify({
  format: 'openjoconde-sauvegarde', version: 1, exporteLe: '2026-10-24T00:00:00Z', favoris: [], collections: [], ...extra
});
const col = (n: number, extra: Record<string, unknown> = {}) => ({ id: `c${n}`, nom: `Collection ${n}`, description: '', oeuvres: [id(n)], creeLe: '', ...extra });

describe('sauvegarde', () => {
  beforeEach(() => {
    window.localStorage.clear();
    chargerFavoris();
    chargerCollections();
  });
  afterEach(() => vi.restoreAllMocks());

  it('exporte favoris et collections avec la version du format et la date', () => {
    ajouterFavori(id(1));
    creerCollection('A', 'desc', [id(2)]);

    const donnees = JSON.parse(sauvegardeJson(new Date('2026-10-24T10:00:00Z')));

    expect(donnees).toMatchObject({ format: 'openjoconde-sauvegarde', version: 1, exporteLe: '2026-10-24T10:00:00.000Z' });
    expect(donnees.favoris.map((f: { id: string }) => f.id)).toEqual([id(1)]);
    expect(donnees.collections[0]).toMatchObject({ nom: 'A', description: 'desc', oeuvres: [id(2)] });
    expect(construireSauvegarde().collections).toHaveLength(1);
    expect(nomFichierSauvegarde(new Date('2026-10-24T10:00:00Z'))).toBe('openjoconde-sauvegarde-2026-10-24.json');
  });

  it("réimporte son propre export à l'identique après remplacement", () => {
    ajouterFavori(id(1));
    creerCollection('A', '', [id(2), id(3)]);
    const export1 = sauvegardeJson();
    window.localStorage.clear();
    chargerFavoris();
    chargerCollections();

    const compte = importerSauvegarde(export1, 'remplacer');

    expect(compte).toMatchObject({ favorisAjoutes: 1, collectionsAjoutees: 1, favorisIgnores: 0, collectionsIgnorees: 0 });
    expect(collections.collections[0].oeuvres).toEqual([id(2), id(3)]);
    expect(estFavori(id(1))).toBe(true);
  });

  it.each([
    ['trop gros', 'x'.repeat(TAILLE_MAXIMALE + 1), /trop gros/],
    ["qui n'est pas du JSON", 'pas du json', /illisible/],
    ['de la mauvaise forme', '[1, 2]', /pas une sauvegarde/],
    ['sans format', JSON.stringify({ version: 1, favoris: [], collections: [] }), /pas une sauvegarde/],
    ['de version inconnue', fichier({ version: 7 }), /version 7/],
    ['sans liste de favoris', fichier({ favoris: undefined }), /incomplet/],
    ['sans liste de collections', fichier({ collections: 'non' }), /incomplet/]
  ])('refuse un fichier %s', (_cas, contenu, message) => {
    ajouterFavori(id(1));

    expect(() => importerSauvegarde(contenu, 'remplacer')).toThrow(ErreurSauvegarde);
    expect(() => importerSauvegarde(contenu, 'fusionner')).toThrow(message);
    expect(estFavori(id(1))).toBe(true);
  });

  it('fusionne : garde les données actuelles, ajoute le reste, ignore les doublons et les éléments invalides', () => {
    ajouterFavori(id(1));
    creerCollection('Existante');
    const existante = collections.collections[0];
    const contenu = fichier({
      favoris: [{ id: id(1) }, { id: id(2) }, { id: '<script>' }, null],
      collections: [col(1, { id: existante.id }), col(2), { id: 'x', nom: '' }]
    });

    const compte = importerSauvegarde(contenu, 'fusionner');

    expect(compte).toEqual({ favorisAjoutes: 1, favorisIgnores: 3, collectionsAjoutees: 1, collectionsIgnorees: 2 });
    expect(favoris.favoris.map(f => f.id)).toEqual([id(1), id(2)]);
    expect(collections.collections.map(c => c.nom)).toEqual(['Existante', 'Collection 2']);
  });

  it('remplace : le fichier se substitue aux données actuelles', () => {
    ajouterFavori(id(1));
    creerCollection('Ancienne');

    const compte = importerSauvegarde(fichier({ favoris: [{ id: id(5) }], collections: [col(5)] }), 'remplacer');

    expect(compte).toMatchObject({ favorisAjoutes: 1, collectionsAjoutees: 1 });
    expect(favoris.favoris.map(f => f.id)).toEqual([id(5)]);
    expect(collections.collections.map(c => c.nom)).toEqual(['Collection 5']);
  });

  it('respecte les limites de favoris et de collections, en fusion comme en remplacement', () => {
    const beaucoup = {
      favoris: Array.from({ length: LIMITE_FAVORIS + 3 }, (_, i) => ({ id: id(i + 1) })),
      collections: Array.from({ length: LIMITE_COLLECTIONS + 2 }, (_, i) => col(i + 1))
    };

    const remplace = importerSauvegarde(fichier(beaucoup), 'remplacer');
    expect(remplace).toMatchObject({ favorisAjoutes: LIMITE_FAVORIS, favorisIgnores: 3, collectionsAjoutees: LIMITE_COLLECTIONS, collectionsIgnorees: 2 });

    const fusion = importerSauvegarde(fichier({ favoris: [{ id: id(9000) }], collections: [col(900)] }), 'fusionner');
    expect(fusion).toMatchObject({ favorisAjoutes: 0, favorisIgnores: 1, collectionsAjoutees: 0, collectionsIgnorees: 1 });
  });

  it("ne modifie rien et le dit quand le stockage refuse l'écriture", () => {
    ajouterFavori(id(1));
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('plein'); });

    expect(() => importerSauvegarde(fichier({ favoris: [{ id: id(2) }] }), 'remplacer')).toThrow(/rien n'a été importé/);
    expect(favoris.favoris.map(f => f.id)).toEqual([id(1)]);
  });

  it('annule les favoris importés si les collections ne peuvent pas être écrites', () => {
    ajouterFavori(id(1));
    const reelle = Storage.prototype.setItem;
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(function (this: Storage, cle: string, valeur: string) {
      if (cle === 'openjoconde:collections') throw new Error('plein');
      return reelle.call(this, cle, valeur);
    });

    expect(() => importerSauvegarde(fichier({ favoris: [{ id: id(2) }], collections: [col(1)] }), 'remplacer')).toThrow(ErreurSauvegarde);
    expect(favoris.favoris.map(f => f.id)).toEqual([id(1)]);
    expect(JSON.parse(window.localStorage.getItem('openjoconde:favoris')!).favoris.map((f: { id: string }) => f.id)).toEqual([id(1)]);
  });

  it('lit un fichier sans rien modifier', () => {
    ajouterOeuvre(creerCollectionId(), id(1));

    const lue = lireSauvegarde(fichier({ favoris: [{ id: id(3), ajouteLe: 'x' }], collections: [col(1), col(1)] }));

    expect(lue).toMatchObject({ favorisIgnores: 0, collectionsIgnorees: 1 });
    expect(lue.favoris).toEqual([{ id: id(3), ajouteLe: 'x' }]);
    expect(collections.collections).toHaveLength(1);
  });
});

function creerCollectionId(): string {
  const r = creerCollection('Une');
  if (!r.ok) throw new Error('échec');
  return r.valeur.id;
}
