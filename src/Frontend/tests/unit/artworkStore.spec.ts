import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

const { searchArtworks, getArtworks, getArtwork } = vi.hoisted(() => ({
  searchArtworks: vi.fn(),
  getArtworks: vi.fn(),
  getArtwork: vi.fn()
}));
vi.mock('@/services/api', () => ({ default: { searchArtworks, getArtworks, getArtwork } }));

import { useArtworkStore } from '@/store/artworkStore';

const resultat = { items: [{ id: '1' }], totalItems: 42, page: 2, pageSize: 12, totalPages: 4 };

describe('artworkStore.searchArtworks', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    searchArtworks.mockReset();
  });

  it("transmet la recherche, les filtres et le tri à l'API, sans les valeurs vides", async () => {
    searchArtworks.mockResolvedValue(resultat);
    const store = useArtworkStore();

    await store.searchArtworks({
      searchText: 'huile',
      domainId: 'd1',
      techniqueId: '',
      artistId: '',
      periodId: 'p1',
      museumId: '',
      page: 2,
      pageSize: 12,
      sortBy: 'title_desc'
    });

    expect(searchArtworks).toHaveBeenCalledWith({
      page: 2,
      pageSize: 12,
      search: 'huile',
      artistId: undefined,
      domainId: 'd1',
      techniqueId: undefined,
      periodId: 'p1',
      museumId: undefined,
      sortBy: 'title_desc'
    });
  });

  it("n'envoie pas le tri A-Z, défaut de l'API, mais envoie les autres", async () => {
    searchArtworks.mockResolvedValue(resultat);
    const store = useArtworkStore();

    await store.searchArtworks({ page: 1, pageSize: 12, sortBy: 'title' });

    expect(searchArtworks.mock.calls[0][0].sortBy).toBeUndefined();

    await store.searchArtworks({ page: 1, pageSize: 12, sortBy: 'date_desc' });

    expect(searchArtworks.mock.calls[1][0].sortBy).toBe('date_desc');
  });

  it('garde les œuvres et le total rendus par une recherche réussie', async () => {
    searchArtworks.mockResolvedValue(resultat);
    const store = useArtworkStore();

    await store.searchArtworks({ page: 2, pageSize: 12 });

    expect(store.artworks).toEqual(resultat.items);
    expect(store.totalItems).toBe(42);
    expect(store.currentPage).toBe(2);
    expect(store.totalPages).toBe(4);
    expect(store.error).toBeNull();
    expect(store.loading).toBe(false);
  });

  it("vide la liste et signale l'erreur quand l'API échoue", async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    searchArtworks.mockRejectedValue(new Error('réseau'));
    const store = useArtworkStore();
    store.artworks = [{ id: 'ancien' }] as never;

    await store.searchArtworks({ page: 1, pageSize: 12 });

    expect(store.artworks).toEqual([]);
    expect(store.error).toContain('recherche');
    expect(store.loading).toBe(false);
  });
});

describe('artworkStore.fetchArtworks', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    getArtworks.mockReset();
  });

  it('charge une page et retient la pagination renvoyée', async () => {
    getArtworks.mockResolvedValue(resultat);
    const store = useArtworkStore();

    await store.fetchArtworks(2, 12);

    expect(getArtworks).toHaveBeenCalledWith(2, 12);
    expect(store.artworks).toEqual(resultat.items);
    expect(store.totalItems).toBe(42);
    expect(store.currentPage).toBe(2);
    expect(store.pageSize).toBe(12);
    expect(store.error).toBeNull();
    expect(store.loading).toBe(false);
  });

  it("vide la liste et signale l'erreur quand l'API échoue", async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    getArtworks.mockRejectedValue(new Error('réseau'));
    const store = useArtworkStore();
    store.artworks = [{ id: 'ancien' }] as never;

    await store.fetchArtworks();

    expect(store.artworks).toEqual([]);
    expect(store.error).toContain('chargement des œuvres');
    expect(store.loading).toBe(false);
  });
});

describe('artworkStore.fetchArtworkById et getters', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    getArtwork.mockReset();
  });

  it("charge une œuvre, puis l'oublie sur demande", async () => {
    getArtwork.mockResolvedValue({ id: 'a1', title: 'Les Nymphéas' });
    const store = useArtworkStore();

    await store.fetchArtworkById('a1');
    expect(store.currentArtwork).toEqual({ id: 'a1', title: 'Les Nymphéas' });
    expect(store.error).toBeNull();

    store.clearCurrentArtwork();
    expect(store.currentArtwork).toBeNull();
  });

  it("signale l'erreur quand l'œuvre ne se charge pas", async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    getArtwork.mockRejectedValue(new Error('404'));
    const store = useArtworkStore();
    store.currentArtwork = { id: 'ancienne' } as never;

    await store.fetchArtworkById('a1');

    expect(store.currentArtwork).toBeNull();
    expect(store.error).toContain("de l'œuvre");
    expect(store.loading).toBe(false);
  });

  it('retrouve une œuvre chargée par son identifiant', () => {
    const store = useArtworkStore();
    store.artworks = [{ id: 'a1' }, { id: 'a2' }] as never;

    expect(store.getArtworkById('a2')).toEqual({ id: 'a2' });
    expect(store.getArtworkById('absente')).toBeUndefined();
  });
});
