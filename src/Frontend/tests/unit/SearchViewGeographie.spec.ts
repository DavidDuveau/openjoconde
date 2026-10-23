import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia } from 'pinia';
import { createMemoryHistory, createRouter } from 'vue-router';

const { searchArtworks, getMuseumGeography, getMuseum, getMuseums } = vi.hoisted(() => ({
  searchArtworks: vi.fn(), getMuseumGeography: vi.fn(), getMuseum: vi.fn(), getMuseums: vi.fn()
}));
vi.mock('@/services/api', () => ({
  default: {
    searchArtworks, getMuseumGeography, getMuseum, getMuseums,
    getDomains: vi.fn(), getTechniques: vi.fn(), getPeriods: vi.fn(), getArtists: vi.fn()
  }
}));

import SearchView from '@/views/SearchView.vue';
import FilterPicker from '@/components/FilterPicker.vue';
import GeographyFilter from '@/components/GeographyFilter.vue';

const resultat = {
  items: [{ id: 'a0', title: 'Les Baigneuses', reference: 'R0', creationDate: '', artists: [], hasImage: false }],
  totalItems: 1, page: 1, pageSize: 12, totalPages: 1
};

async function ouvrir(adresse: string) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/search', component: SearchView },
      { path: '/museums/:id', name: 'museum-detail', component: { template: '<div />' } }
    ]
  });
  await router.push(adresse);
  await router.isReady();
  const vue = mount(SearchView, { global: { plugins: [createPinia(), router] } });
  await flushPromises();
  return { vue, router };
}

const derniereRecherche = () => searchArtworks.mock.calls.at(-1)![0];

function simulerPosition(reponse: 'ok' | 'refus' | 'absente') {
  if (reponse === 'absente') {
    vi.stubGlobal('navigator', { ...navigator, geolocation: undefined });
    return;
  }
  const getCurrentPosition = vi.fn((succes: (p: unknown) => void, echec: () => void) =>
    reponse === 'ok' ? succes({ coords: { latitude: 47.32212, longitude: 5.04157 } }) : echec());
  vi.stubGlobal('navigator', { ...navigator, geolocation: { getCurrentPosition } });
}

describe('SearchView : recherche géographique', () => {
  beforeEach(() => {
    [searchArtworks, getMuseumGeography, getMuseum, getMuseums].forEach((m) => m.mockReset());
    searchArtworks.mockResolvedValue(resultat);
    getMuseumGeography.mockResolvedValue([
      { name: 'Bretagne', museums: 2, departments: [{ name: 'Morbihan', museums: 2 }] }
    ]);
    getMuseum.mockResolvedValue({ id: 'm1', name: "Musée d'Orsay", city: 'Paris' });
  });
  afterEach(() => vi.unstubAllGlobals());

  it("lit la région, le département et la position dans l'adresse, et les envoie à l'API", async () => {
    await ouvrir('/search?region=Bretagne&department=Morbihan&lat=47.3&lon=5.04&sortBy=distance');

    expect(derniereRecherche()).toMatchObject({
      region: 'Bretagne', department: 'Morbihan', latitude: 47.3, longitude: 5.04, sortBy: 'distance'
    });
  });

  it("pose la région choisie : première page, reportée dans l'adresse", async () => {
    const { vue, router } = await ouvrir('/search?page=3');

    const filtre = vue.getComponent(GeographyFilter);
    filtre.vm.$emit('update:region', 'Bretagne');
    filtre.vm.$emit('change');
    await flushPromises();

    expect(derniereRecherche()).toMatchObject({ region: 'Bretagne', page: 1 });
    expect(router.currentRoute.value.query).toMatchObject({ region: 'Bretagne' });
  });

  it("propose le tri par artiste dès qu'une région filtre", async () => {
    const { vue } = await ouvrir('/search?region=Bretagne');

    expect(vue.get('[data-testid="sort"]').findAll('option').map(o => o.attributes('value'))).toContain('artist');
  });

  it('propose les musées par leur nom, et pose celui qui est choisi avec son libellé', async () => {
    getMuseums.mockResolvedValue({ items: [{ id: 'm1', name: "Musée d'Orsay" }] });
    const { vue, router } = await ouvrir('/search');
    const musee = vue.findAllComponents(FilterPicker).find(p => p.props('placeholder') === 'Rechercher un musée')!;

    expect(await (musee.props('fetchOptions') as (t: string) => Promise<unknown>)('ors')).toEqual([{ id: 'm1', label: "Musée d'Orsay" }]);
    musee.vm.$emit('update:label', "Musée d'Orsay");
    musee.vm.$emit('update:modelValue', 'm1');
    await flushPromises();

    expect(derniereRecherche()).toMatchObject({ museumId: 'm1' });
    expect(router.currentRoute.value.query).toMatchObject({ museumId: 'm1', museumName: "Musée d'Orsay" });
  });

  it("résume le musée quand les œuvres sont celles d'un musée, et pas autrement", async () => {
    const avec = await ouvrir('/search?museumId=m1');
    expect(avec.vue.find('[data-testid="museum-summary"]').exists()).toBe(true);
    expect(getMuseum).toHaveBeenCalledWith('m1');

    const sans = await ouvrir('/search');
    expect(sans.vue.find('[data-testid="museum-summary"]').exists()).toBe(false);
  });

  it("« Près de moi » trie par distance depuis la position, arrondie, et la reporte dans l'adresse", async () => {
    simulerPosition('ok');
    const { vue, router } = await ouvrir('/search?page=2');

    await vue.get('[data-testid="near-me"]').trigger('click');
    await flushPromises();

    expect(derniereRecherche()).toMatchObject({ latitude: 47.322, longitude: 5.042, sortBy: 'distance', page: 1 });
    expect(router.currentRoute.value.query).toMatchObject({ lat: '47.322', lon: '5.042', sortBy: 'distance' });
    expect(vue.find('[data-testid="position-active"]').exists()).toBe(true);
    expect(vue.get('[data-testid="sort"]').findAll('option').map(o => o.attributes('value'))).toContain('distance');
  });

  it('retire la position : fin du tri par distance', async () => {
    const { vue, router } = await ouvrir('/search?lat=47.3&lon=5.04&sortBy=distance');

    await vue.get('[data-testid="position-clear"]').trigger('click');
    await flushPromises();

    expect(derniereRecherche()).toMatchObject({ latitude: undefined, longitude: undefined, sortBy: undefined });
    expect(router.currentRoute.value.query.lat).toBeUndefined();
    expect(vue.find('[data-testid="position-active"]').exists()).toBe(false);
  });

  it("retombe sur le tri par titre quand l'adresse demande la distance sans position", async () => {
    await ouvrir('/search?sortBy=distance');

    expect(derniereRecherche().sortBy).toBeUndefined();
  });

  it("ignore une position illisible dans l'adresse", async () => {
    await ouvrir('/search?lat=abc&lon=5');

    expect(derniereRecherche()).toMatchObject({ latitude: undefined, longitude: undefined });
  });

  it('explique quand la position est refusée, sans changer la recherche', async () => {
    simulerPosition('refus');
    const { vue } = await ouvrir('/search');
    searchArtworks.mockClear();

    await vue.get('[data-testid="near-me"]').trigger('click');
    await flushPromises();

    expect(vue.get('[data-testid="position-error"]').text()).toContain("n'a pas pu être obtenue");
    expect(searchArtworks).not.toHaveBeenCalled();
  });

  it("explique quand le navigateur n'a pas de géolocalisation", async () => {
    simulerPosition('absente');
    const { vue } = await ouvrir('/search');

    await vue.get('[data-testid="near-me"]').trigger('click');

    expect(vue.get('[data-testid="position-error"]').text()).toContain('ne permet pas');
  });

  it('la réinitialisation efface région, département, musée et position', async () => {
    const { vue, router } = await ouvrir('/search?region=Bretagne&department=Morbihan&museumId=m1&museumName=Orsay&lat=47.3&lon=5.04&sortBy=distance');

    await vue.get('.reset-button').trigger('click');
    await flushPromises();

    expect(router.currentRoute.value.query).toEqual({});
    expect(derniereRecherche()).toMatchObject({ region: undefined, department: undefined, museumId: undefined, latitude: undefined });
  });
});
