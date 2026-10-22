import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

const { searchArtworks } = vi.hoisted(() => ({ searchArtworks: vi.fn() }));
vi.mock('@/services/api', () => ({ default: { searchArtworks } }));

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

  it("n'envoie le tri que pour Z-A : A-Z est le tri par défaut de l'API", async () => {
    searchArtworks.mockResolvedValue(resultat);
    const store = useArtworkStore();

    await store.searchArtworks({ page: 1, pageSize: 12, sortBy: 'title' });

    expect(searchArtworks.mock.calls[0][0].sortBy).toBeUndefined();
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
