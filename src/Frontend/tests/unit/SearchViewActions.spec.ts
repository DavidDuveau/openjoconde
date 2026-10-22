import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia } from 'pinia';
import { createMemoryHistory, createRouter } from 'vue-router';

const { searchArtworks, getDomains, getTechniques, getPeriods, getArtists } = vi.hoisted(() => ({
  searchArtworks: vi.fn(),
  getDomains: vi.fn(),
  getTechniques: vi.fn(),
  getPeriods: vi.fn(),
  getArtists: vi.fn()
}));
vi.mock('@/services/api', () => ({
  default: { searchArtworks, getDomains, getTechniques, getPeriods, getArtists }
}));

import SearchView from '@/views/SearchView.vue';
import FilterPicker from '@/components/FilterPicker.vue';

const resultat = (titres: string[], totalItems = titres.length, page = 1) => ({
  items: titres.map((title, i) => ({ id: `a${i}`, title, reference: `R${i}`, creationDate: '', artists: [], hasImage: false })),
  totalItems,
  page,
  pageSize: 12,
  totalPages: Math.ceil(totalItems / 12)
});

/** Monte la vue sur l'adresse donnée, avec un vrai routeur : l'adresse est la source des paramètres de recherche */
async function ouvrir(adresse: string) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/search', component: SearchView }, { path: '/artworks/:id', component: { template: '<div />' } }]
  });
  await router.push(adresse);
  await router.isReady();

  const vue = mount(SearchView, { global: { plugins: [createPinia(), router] } });
  await flushPromises();
  return { vue, router };
}

const derniereRecherche = () => searchArtworks.mock.calls.at(-1)![0];
const proposeur = (vue: Awaited<ReturnType<typeof ouvrir>>['vue'], index: number) =>
  vue.findAllComponents(FilterPicker)[index].props('fetchOptions') as (texte: string) => Promise<unknown>;

describe('SearchView : filtres, pagination et actions', () => {
  beforeEach(() => {
    [searchArtworks, getDomains, getTechniques, getPeriods, getArtists].forEach((m) => m.mockReset());
    searchArtworks.mockResolvedValue(resultat(['Les Baigneuses', 'Orphée']));
    window.scrollTo = vi.fn() as never;
  });

  it("pose un filtre choisi dans la liste : première page, avec son libellé dans l'adresse", async () => {
    const { vue, router } = await ouvrir('/search?searchText=huile&page=3');

    const domaine = vue.findAllComponents(FilterPicker)[0];
    domaine.vm.$emit('update:label', 'Peinture');
    domaine.vm.$emit('update:modelValue', 'd1');
    await flushPromises();

    expect(derniereRecherche()).toMatchObject({ domainId: 'd1', page: 1 });
    expect(router.currentRoute.value.query).toMatchObject({ domainId: 'd1', domainName: 'Peinture' });
    expect(router.currentRoute.value.query.page).toBeUndefined();
  });

  it("propose domaines, techniques et périodes d'après le texte saisi", async () => {
    getDomains.mockResolvedValue({ items: [{ id: 'd1', name: 'Peinture' }] });
    getTechniques.mockResolvedValue({ items: [{ id: 't1', name: 'Huile' }] });
    getPeriods.mockResolvedValue({ items: [{ id: 'p1', name: 'Baroque' }] });
    const { vue } = await ouvrir('/search');

    expect(await proposeur(vue, 0)('pein')).toEqual([{ id: 'd1', label: 'Peinture' }]);
    expect(await proposeur(vue, 1)('hui')).toEqual([{ id: 't1', label: 'Huile' }]);
    expect(await proposeur(vue, 2)('bar')).toEqual([{ id: 'p1', label: 'Baroque' }]);
    expect(getDomains).toHaveBeenCalledWith('pein');
  });

  it('propose les artistes par leur nom complet, même sans prénom', async () => {
    getArtists.mockResolvedValue({
      items: [
        { id: 'r1', firstName: 'Claude', lastName: 'Monet' },
        { id: 'r2', firstName: null, lastName: 'Anonyme' }
      ]
    });
    const { vue } = await ouvrir('/search');

    expect(await proposeur(vue, 3)('mo')).toEqual([{ id: 'r1', label: 'Claude Monet' }, { id: 'r2', label: 'Anonyme' }]);
    expect(getArtists).toHaveBeenCalledWith(1, 20, 'mo');
  });

  it('cherche par mots à la touche Entrée comme au bouton', async () => {
    const { vue, router } = await ouvrir('/search');

    await vue.get('.inline-search input').setValue('bronze');
    await vue.get('.inline-search input').trigger('keyup.enter');
    await flushPromises();
    expect(derniereRecherche()).toMatchObject({ search: 'bronze' });

    await vue.get('.inline-search button').trigger('click');
    await flushPromises();
    expect(router.currentRoute.value.query.searchText).toBe('bronze');
  });

  it('réinitialise les filtres et le tri, mais garde les mots', async () => {
    const { vue, router } = await ouvrir('/search?searchText=huile&domainId=d1&domainName=Peinture&sortBy=title_desc&page=2');

    await vue.get('.reset-button').trigger('click');
    await flushPromises();

    expect(derniereRecherche()).toMatchObject({ search: 'huile', domainId: undefined, page: 1 });
    expect(router.currentRoute.value.query).toEqual({ searchText: 'huile' });
  });

  it('relance la recherche avec le bouton « Appliquer les filtres »', async () => {
    const { vue } = await ouvrir('/search?searchText=huile');
    searchArtworks.mockClear();

    await vue.get('.filter-buttons .search-button').trigger('click');
    await flushPromises();

    expect(searchArtworks).toHaveBeenCalledTimes(1);
  });

  it('propose des recherches proches quand rien ne correspond, sans filtre hérité', async () => {
    searchArtworks.mockResolvedValue(resultat([]));
    const { vue, router } = await ouvrir('/search?searchText=zzz&domainId=d1');
    searchArtworks.mockResolvedValue(resultat(['Portrait de femme']));

    await vue.findAll('a').find((a) => a.text() === 'Rechercher "portrait"')!.trigger('click');
    await flushPromises();

    expect(derniereRecherche()).toMatchObject({ search: 'portrait', domainId: undefined });
    expect(router.currentRoute.value.query).toEqual({ searchText: 'portrait' });
  });

  it('propose « Réessayer » après une panne, qui relance la recherche', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    searchArtworks.mockRejectedValueOnce(new Error('réseau'));
    const { vue } = await ouvrir('/search?searchText=huile');

    searchArtworks.mockResolvedValue(resultat(['Orphée']));
    await vue.get('.error-message button').trigger('click');
    await flushPromises();

    expect(vue.find('.error-container').exists()).toBe(false);
    expect(vue.find('.results-content').exists()).toBe(true);
  });

  it('pagine avec Précédent et Suivant, désactivés aux extrémités, et remonte en haut de page', async () => {
    searchArtworks.mockResolvedValue(resultat(['A', 'B'], 100, 1));
    const { vue } = await ouvrir('/search?searchText=huile');
    expect(vue.findAll('.pagination-button')[0].attributes('disabled')).toBeDefined();
    expect(vue.findAll('.pagination-number').map((b) => b.text())).toEqual(['1', '2', '3', '4', '5']);

    searchArtworks.mockResolvedValue(resultat(['A', 'B'], 100, 2));
    await vue.findAll('.pagination-button')[1].trigger('click');
    await flushPromises();

    expect(derniereRecherche().page).toBe(2);
    expect(window.scrollTo).toHaveBeenCalledWith({ top: 0, behavior: 'smooth' });
    expect(vue.findAll('.pagination-button')[0].attributes('disabled')).toBeUndefined();

    await vue.findAll('.pagination-button')[0].trigger('click');
    await flushPromises();
    expect(derniereRecherche().page).toBe(1);
  });

  it('désactive Suivant sur la dernière page et décale les numéros', async () => {
    searchArtworks.mockResolvedValue(resultat(['A'], 100, 9));
    const { vue } = await ouvrir('/search?searchText=huile&page=9');

    expect(vue.findAll('.pagination-button')[1].attributes('disabled')).toBeDefined();
    expect(vue.findAll('.pagination-number').map((b) => b.text())).toEqual(['5', '6', '7', '8', '9']);
  });

  it("ouvre la fiche de l'œuvre cliquée", async () => {
    const { vue, router } = await ouvrir('/search?searchText=huile');

    await vue.findAllComponents({ name: 'ArtworkCard' })[1].trigger('click');
    await flushPromises();

    expect(router.currentRoute.value.path).toBe('/artworks/a1');
  });

  it("reprend l'adresse quand elle change : les paramètres disparus sont effacés", async () => {
    const { router } = await ouvrir('/search?searchText=huile&artistId=r1&artistName=Monet&museumId=m1&techniqueId=t1&periodId=p1');
    expect(derniereRecherche()).toMatchObject({ artistId: 'r1', museumId: 'm1', techniqueId: 't1', periodId: 'p1' });

    await router.push('/search');
    await flushPromises();

    expect(derniereRecherche()).toMatchObject({ search: undefined, artistId: undefined, museumId: undefined });
  });
});
