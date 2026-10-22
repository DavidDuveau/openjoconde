import { beforeEach, describe, expect, it, vi } from 'vitest';

const { get } = vi.hoisted(() => ({ get: vi.fn() }));
vi.mock('axios', () => ({
  default: { create: () => ({ get, interceptors: { response: { use: vi.fn() } } }) }
}));

import api from '@/services/api';

describe('client API', () => {
  beforeEach(() => {
    get.mockReset();
    get.mockResolvedValue({ data: { items: [], totalItems: 0, page: 1, pageSize: 20, totalPages: 0 } });
  });

  it.each([
    ['getDomains', '/domains'],
    ['getTechniques', '/techniques'],
    ['getPeriods', '/periods']
  ] as const)('%s cherche par nom dans une liste paginée', async (methode, route) => {
    await api[methode]('huile', 5);

    expect(get).toHaveBeenCalledWith(route, { params: { name: 'huile', pageSize: 5 } });
  });

  it("n'envoie pas de nom vide", async () => {
    await api.getTechniques('');

    expect(get).toHaveBeenCalledWith('/techniques', { params: { name: undefined, pageSize: 20 } });
  });

  it('cherche les artistes par nom', async () => {
    await api.getArtists(1, 20, 'monet');

    expect(get).toHaveBeenCalledWith('/artists', { params: { page: 1, pageSize: 20, name: 'monet' } });
  });

  it("transmet les paramètres de recherche d'œuvres tels quels", async () => {
    const params = { page: 3, pageSize: 12, search: 'x', domainId: 'd' };

    await api.searchArtworks(params);

    expect(get).toHaveBeenCalledWith('/artworks', { params });
  });
});
