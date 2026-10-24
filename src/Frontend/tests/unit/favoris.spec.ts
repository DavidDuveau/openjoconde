import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  ajouterFavori, basculerFavori, chargerFavoris, CLE_FAVORIS, estFavori, favoris, LIMITE_FAVORIS, oublierProbleme,
  retirerFavori
} from '@/utils/favoris';

const id = (n: number) => `00000000-0000-0000-0000-${String(n).padStart(12, '0')}`;

describe('favoris', () => {
  beforeEach(() => {
    window.localStorage.clear();
    chargerFavoris();
    oublierProbleme();
  });
  afterEach(() => vi.restoreAllMocks());

  it('ajoute un favori, le plus récent en premier, et le garde après rechargement', () => {
    expect(ajouterFavori(id(1))).toBe(true);
    expect(ajouterFavori(id(2))).toBe(true);

    expect(favoris.favoris.map(f => f.id)).toEqual([id(2), id(1)]);
    chargerFavoris();
    expect(favoris.favoris.map(f => f.id)).toEqual([id(2), id(1)]);
    expect(JSON.parse(window.localStorage.getItem(CLE_FAVORIS)!).version).toBe(1);
  });

  it("n'ajoute pas deux fois la même œuvre", () => {
    ajouterFavori(id(1));
    ajouterFavori(id(1));

    expect(favoris.favoris).toHaveLength(1);
  });

  it('retire un favori et bascule', () => {
    ajouterFavori(id(1));

    expect(basculerFavori(id(1))).toBe(true);
    expect(estFavori(id(1))).toBe(false);
    basculerFavori(id(1));
    expect(estFavori(id(1))).toBe(true);
    retirerFavori(id(1));
    expect(estFavori(id(1))).toBe(false);
  });

  it('refuse au-delà de la limite et le dit', () => {
    for (let n = 1; n <= LIMITE_FAVORIS; n++) ajouterFavori(id(n));

    expect(ajouterFavori(id(LIMITE_FAVORIS + 1))).toBe(false);
    expect(favoris.probleme).toBe('limite');
    expect(favoris.favoris).toHaveLength(LIMITE_FAVORIS);
  });

  it("signale un stockage indisponible sans rien casser, et n'ajoute pas le favori", () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new DOMException('plein', 'QuotaExceededError'); });

    expect(ajouterFavori(id(1))).toBe(false);
    expect(favoris.probleme).toBe('stockage');
    expect(estFavori(id(1))).toBe(false);
  });

  it("garde le retrait en session quand l'écriture échoue", () => {
    ajouterFavori(id(1));
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('refusé'); });

    retirerFavori(id(1));

    expect(estFavori(id(1))).toBe(false);
    expect(favoris.probleme).toBe('stockage');
  });

  it('survit à un stockage illisible', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => { throw new Error('bloqué'); });

    chargerFavoris();

    expect(favoris.favoris).toEqual([]);
  });

  it.each([
    ['du texte qui nest pas du JSON', 'pas du json'],
    ['une version inconnue', JSON.stringify({ version: 99, favoris: [{ id: id(1) }] })],
    ['une liste absente', JSON.stringify({ version: 1 })]
  ])('ignore %s', (_nom, contenu) => {
    window.localStorage.setItem(CLE_FAVORIS, contenu);

    chargerFavoris();

    expect(favoris.favoris).toEqual([]);
  });

  it('écarte les identifiants invalides et les doublons', () => {
    window.localStorage.setItem(CLE_FAVORIS, JSON.stringify({
      version: 1,
      favoris: [{ id: id(1), ajouteLe: '2026-01-01' }, { id: id(1) }, { id: '<script>' }, { id: 42 }, null, { id: id(2), ajouteLe: 5 }]
    }));

    chargerFavoris();

    expect(favoris.favoris).toEqual([{ id: id(1), ajouteLe: '2026-01-01' }, { id: id(2), ajouteLe: '' }]);
  });

  it('se met à jour quand un autre onglet modifie les favoris', () => {
    window.localStorage.setItem(CLE_FAVORIS, JSON.stringify({ version: 1, favoris: [{ id: id(7), ajouteLe: '' }] }));

    window.dispatchEvent(new StorageEvent('storage', { key: CLE_FAVORIS }));

    expect(estFavori(id(7))).toBe(true);
  });

  it("ignore la modification d'une autre clé", () => {
    ajouterFavori(id(1));
    window.dispatchEvent(new StorageEvent('storage', { key: 'autre' }));

    expect(estFavori(id(1))).toBe(true);
  });
});
