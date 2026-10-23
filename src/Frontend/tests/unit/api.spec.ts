import { beforeEach, describe, expect, it, vi } from 'vitest';

const { get, use, create } = vi.hoisted(() => {
  const use = vi.fn();
  const get = vi.fn();
  return { get, use, create: vi.fn(() => ({ get, interceptors: { response: { use } } })) };
});
vi.mock('axios', () => ({ default: { create } }));

import api from '@/services/api';

// Les appels de configuration ont lieu à l'import du module : on les relève avant tout nettoyage
const configuration = create.mock.calls[0] as unknown[];
const intercepteurs = use.mock.calls[0] as [(r: unknown) => unknown, (e: unknown) => Promise<never>];

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

    expect(get).toHaveBeenCalledWith(route, { params: { name: 'huile', page: 1, pageSize: 5 } });
  });

  it("n'envoie pas de nom vide", async () => {
    await api.getTechniques('');

    expect(get).toHaveBeenCalledWith('/techniques', { params: { name: undefined, page: 1, pageSize: 20 } });
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

  it.each([
    ['getArtworks', [2, 5], '/artworks', { params: { page: 2, pageSize: 5 } }],
    ['getArtwork', ['a1'], '/artworks/a1', undefined],
    ['getArtist', ['r1'], '/artists/r1', undefined],
    ['getMuseum', ['m1'], '/museums/m1', undefined],
    ['getMuseumGeography', [], '/museums/geography', undefined]
  ] as const)('%s appelle %s', async (methode, args, route, options) => {
    get.mockResolvedValue({ data: { id: 'x' } });

    const resultat = await (api[methode] as (...a: unknown[]) => Promise<unknown>)(...args);

    expect(resultat).toEqual({ id: 'x' });
    if (options) expect(get).toHaveBeenCalledWith(route, options);
    else expect(get).toHaveBeenCalledWith(route);
  });

  it('cherche les musées par nom, sans nom vide', async () => {
    await api.getMuseums(2, 12, 'guimet');
    expect(get).toHaveBeenLastCalledWith('/museums', { params: { page: 2, pageSize: 12, name: 'guimet' } });

    await api.getMuseums();
    expect(get).toHaveBeenLastCalledWith('/museums', { params: { page: 1, pageSize: 10, name: undefined } });
  });

  it('crée le client avec la base de l’API et du JSON', () => {
    expect(configuration[0]).toEqual(expect.objectContaining({
      baseURL: expect.any(String),
      headers: expect.objectContaining({ Accept: 'application/json' })
    }));
  });

  it('laisse passer les réponses et rejette les erreurs en les journalisant', async () => {
    const [succes, echec] = intercepteurs;
    const journal = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const reponse = { data: 1 };
    const erreur = { response: { status: 500 } };

    expect(succes(reponse)).toBe(reponse);
    await expect(echec(erreur)).rejects.toBe(erreur);
    expect(journal).toHaveBeenCalledWith('API Error:', erreur.response);
  });
});
