import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils';
import { reactive } from 'vue';

const { getArtist, searchArtworks, push, route } = vi.hoisted(() => ({
  getArtist: vi.fn(),
  searchArtworks: vi.fn(),
  push: vi.fn(),
  route: { current: { params: { id: 'r1' } as Record<string, string | undefined> } }
}));
vi.mock('@/services/api', () => ({ default: { getArtist, searchArtworks } }));
vi.mock('vue-router', () => ({ useRoute: () => route.current, useRouter: () => ({ push }) }));

import ArtistDetailView from '@/views/ArtistDetailView.vue';

const artiste = (extra: object = {}) => ({
  id: 'r1', firstName: 'Claude', lastName: 'Monet', birthDate: '1840-11-14', deathDate: '1926-12-05',
  nationality: 'française', biography: 'Chef de file de l’impressionnisme.', ...extra
});

const oeuvres = (titres: string[], totalItems = titres.length, totalPages = 1) => ({
  items: titres.map((title, i) => ({ id: `a${i}`, title, creationDate: '1900' })),
  totalItems, page: 1, pageSize: 12, totalPages
});

const carte = { props: ['id', 'title'], emits: ['click'], template: '<div class="carte" @click="$emit(\'click\')">{{ title }}</div>' };

async function afficher() {
  const vue = mount(ArtistDetailView, { global: { stubs: { RouterLink: RouterLinkStub, ArtworkCard: carte } } });
  await flushPromises();
  return vue;
}

describe('ArtistDetailView', () => {
  beforeEach(() => {
    getArtist.mockReset();
    searchArtworks.mockReset();
    push.mockReset();
    route.current = reactive({ params: { id: 'r1' } });
    searchArtworks.mockResolvedValue(oeuvres([]));
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  it("affiche l'artiste, ses dates, sa nationalité et sa biographie", async () => {
    getArtist.mockResolvedValue(artiste());

    const vue = await afficher();

    expect(getArtist).toHaveBeenCalledWith('r1');
    expect(vue.get('[data-testid="artist-heading"]').text()).toBe('Claude Monet');
    expect(vue.get('.artist-dates').text()).toBe('1840 - 1926');
    expect(vue.get('.artist-nationality').text()).toBe('française');
    expect(vue.get('.biography-content').text()).toContain('impressionnisme');
  });

  it.each([
    [{ deathDate: null }, 'Né(e) en 1840'],
    [{ birthDate: null }, '? - 1926'],
    [{ birthDate: null, deathDate: null }, '']
  ])('formate les dates incomplètes %j en « %s »', async (dates, attendu) => {
    getArtist.mockResolvedValue(artiste(dates));

    const vue = await afficher();

    expect(vue.get('.artist-dates').text()).toBe(attendu);
  });

  it('omet la nationalité et la biographie quand elles manquent', async () => {
    getArtist.mockResolvedValue(artiste({ nationality: null, biography: null }));

    const vue = await afficher();

    expect(vue.find('.artist-nationality').exists()).toBe(false);
    expect(vue.find('.artist-biography').exists()).toBe(false);
  });

  it("liste les œuvres de l'artiste, demandées par son identifiant, avec leur nombre", async () => {
    getArtist.mockResolvedValue(artiste());
    searchArtworks.mockResolvedValue(oeuvres(['Les Nymphéas', 'Impression, soleil levant']));

    const vue = await afficher();

    expect(searchArtworks).toHaveBeenCalledWith({ artistId: 'r1', page: 1, pageSize: 12 });
    expect(vue.findAll('.carte').map((c) => c.text())).toEqual(['Les Nymphéas', 'Impression, soleil levant']);
    expect(vue.get('.artist-artworks h2').text()).toContain('(2)');
  });

  it("ouvre la fiche de l'œuvre cliquée", async () => {
    getArtist.mockResolvedValue(artiste());
    searchArtworks.mockResolvedValue(oeuvres(['Les Nymphéas']));
    const vue = await afficher();

    await vue.get('.carte').trigger('click');

    expect(push).toHaveBeenCalledWith('/artworks/a0');
  });

  it("dit qu'aucune œuvre n'est disponible", async () => {
    getArtist.mockResolvedValue(artiste());

    const vue = await afficher();

    expect(vue.get('[data-testid="no-artworks"]').text()).toContain("Aucune œuvre n'est actuellement disponible");
  });

  it("signale l'erreur de chargement des œuvres sans perdre la fiche", async () => {
    getArtist.mockResolvedValue(artiste());
    searchArtworks.mockRejectedValue(new Error('réseau'));

    const vue = await afficher();

    expect(vue.get('[data-testid="artist-heading"]').text()).toBe('Claude Monet');
    expect(vue.get('.no-artworks').text()).toContain("œuvres de l'artiste");
  });

  it("pagine les œuvres", async () => {
    getArtist.mockResolvedValue(artiste());
    searchArtworks.mockResolvedValue(oeuvres(['A'], 30, 3));
    const vue = await afficher();

    await vue.findAll('.pagination-number')[1].trigger('click');
    await flushPromises();

    expect(searchArtworks).toHaveBeenLastCalledWith({ artistId: 'r1', page: 2, pageSize: 12 });
  });

  it("signale l'erreur de chargement de l'artiste et permet de réessayer", async () => {
    getArtist.mockRejectedValueOnce(new Error('réseau'));
    const vue = await afficher();
    expect(vue.get('.error-message').text()).toContain("Erreur lors du chargement de l'artiste");

    getArtist.mockResolvedValue(artiste());
    await vue.get('.primary-button').trigger('click');
    await flushPromises();

    expect(vue.get('[data-testid="artist-heading"]').text()).toBe('Claude Monet');
  });

  it("dit que l'artiste est introuvable quand l'API ne renvoie rien", async () => {
    getArtist.mockResolvedValue(null);

    const vue = await afficher();

    expect(vue.get('.not-found').text()).toContain('Artiste non trouvé');
  });

  it("n'interroge rien sans identifiant", async () => {
    route.current = reactive({ params: {} });

    const vue = await afficher();

    expect(getArtist).not.toHaveBeenCalled();
    expect(vue.find('.not-found').exists()).toBe(true);
  });

  it("recharge l'artiste quand l'identifiant de l'URL change, mais pas quand il disparaît", async () => {
    getArtist.mockResolvedValue(artiste());
    await afficher();

    route.current.params.id = 'r2';
    await flushPromises();
    expect(getArtist).toHaveBeenLastCalledWith('r2');

    getArtist.mockClear();
    route.current.params.id = undefined;
    await flushPromises();
    expect(getArtist).not.toHaveBeenCalled();
  });
});
