import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';

const { getArtists, push } = vi.hoisted(() => ({ getArtists: vi.fn(), push: vi.fn() }));
vi.mock('@/services/api', () => ({ default: { getArtists } }));
vi.mock('vue-router', () => ({ useRouter: () => ({ push }) }));

import ArtistsView from '@/views/ArtistsView.vue';

const page = (items: object[], totalItems = items.length, totalPages = 1) => ({ items, totalItems, page: 1, pageSize: 12, totalPages });
const artiste = (id: string, extra: object = {}) => ({
  id, firstName: 'Claude', lastName: 'Monet', birthDate: '1840-11-14', deathDate: '1926-12-05', nationality: 'française', artworksCount: 2, ...extra
});

async function afficher() {
  const vue = mount(ArtistsView);
  await flushPromises();
  return vue;
}

describe('ArtistsView', () => {
  beforeEach(() => {
    getArtists.mockReset();
    push.mockReset();
    window.scrollTo = vi.fn() as never;
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });
  afterEach(() => vi.useRealTimers());

  it('liste les artistes avec leurs dates, leur nationalité et leur nombre d’œuvres', async () => {
    getArtists.mockResolvedValue(page([artiste('a1'), artiste('a2', { artworksCount: 1, nationality: null })]));

    const vue = await afficher();
    const cartes = vue.findAll('[data-testid="artist-card"]');

    expect(cartes).toHaveLength(2);
    expect(cartes[0].text()).toContain('Claude Monet');
    expect(cartes[0].text()).toContain('1840 - 1926');
    expect(cartes[0].text()).toContain('française');
    expect(cartes[0].get('[data-testid="artist-count"]').text()).toBe('2 œuvres');
    expect(cartes[1].get('[data-testid="artist-count"]').text()).toBe('1 œuvre');
    expect(cartes[1].find('.artist-nationality').exists()).toBe(false);
  });

  it.each([
    [{ deathDate: null }, 'Né(e) en 1840'],
    [{ birthDate: null }, '? - 1926'],
    [{ birthDate: null, deathDate: null }, '']
  ])('formate les dates incomplètes %j en « %s »', async (dates, attendu) => {
    getArtists.mockResolvedValue(page([artiste('a1', dates)]));

    const vue = await afficher();

    expect(vue.get('.artist-dates').text()).toBe(attendu);
  });

  it("ouvre la fiche de l'artiste cliqué", async () => {
    getArtists.mockResolvedValue(page([artiste('a1')]));
    const vue = await afficher();

    await vue.get('[data-testid="artist-card"]').trigger('click');

    expect(push).toHaveBeenCalledWith('/artists/a1');
  });

  it("dit qu'aucun artiste ne correspond", async () => {
    getArtists.mockResolvedValue(page([]));

    const vue = await afficher();

    expect(vue.get('.no-results').text()).toContain('Aucun artiste trouvé');
  });

  it("signale l'erreur et permet de réessayer", async () => {
    getArtists.mockRejectedValueOnce(new Error('réseau'));
    const vue = await afficher();
    expect(vue.get('.error-message').text()).toContain('Erreur lors du chargement des artistes');

    getArtists.mockResolvedValue(page([artiste('a1')]));
    await vue.get('.primary-button').trigger('click');
    await flushPromises();

    expect(vue.findAll('[data-testid="artist-card"]')).toHaveLength(1);
    expect(vue.find('.error-container').exists()).toBe(false);
  });

  it("attend la fin de la saisie avant d'interroger l'API, puis repart de la première page", async () => {
    getArtists.mockResolvedValue(page([artiste('a1')]));
    const vue = await afficher();
    vi.useFakeTimers();

    const saisie = vue.get('input');
    await saisie.setValue('mo');
    await saisie.setValue(' monet ');
    expect(getArtists).toHaveBeenCalledTimes(1);

    await vi.advanceTimersByTimeAsync(300);

    expect(getArtists).toHaveBeenCalledTimes(2);
    expect(getArtists).toHaveBeenLastCalledWith(1, 12, 'monet');
  });

  it('pagine : cinq numéros autour de la page courante, boutons désactivés aux extrémités', async () => {
    getArtists.mockResolvedValue(page([artiste('a1')], 100, 9));
    const vue = await afficher();

    expect(vue.findAll('.pagination-number').map((b) => b.text())).toEqual(['1', '2', '3', '4', '5']);
    expect(vue.findAll('.pagination-button')[0].attributes('disabled')).toBeDefined();

    await vue.findAll('.pagination-number')[3].trigger('click');
    await flushPromises();

    expect(getArtists).toHaveBeenLastCalledWith(4, 12, '');
    expect(window.scrollTo).toHaveBeenCalled();
    expect(vue.findAll('.pagination-number').map((b) => b.text())).toEqual(['2', '3', '4', '5', '6']);
    expect(vue.get('.pagination-number.active').text()).toBe('4');

    await vue.findAll('.pagination-button')[1].trigger('click');
    await flushPromises();
    expect(getArtists).toHaveBeenLastCalledWith(5, 12, '');

    await vue.findAll('.pagination-button')[0].trigger('click');
    await flushPromises();
    expect(getArtists).toHaveBeenLastCalledWith(4, 12, '');
  });

  it('ramène les numéros vers la fin quand la dernière page approche', async () => {
    getArtists.mockResolvedValue(page([artiste('a1')], 100, 9));
    const vue = await afficher();
    (vue.vm as unknown as { currentPage: number }).currentPage = 9;
    await vue.vm.$nextTick();

    expect(vue.findAll('.pagination-number').map((b) => b.text())).toEqual(['5', '6', '7', '8', '9']);
    expect(vue.findAll('.pagination-button')[1].attributes('disabled')).toBeDefined();
  });

  it('ne montre pas de pagination quand tout tient sur une page', async () => {
    getArtists.mockResolvedValue(page([artiste('a1')]));

    const vue = await afficher();

    expect(vue.find('.pagination').exists()).toBe(false);
  });
});
