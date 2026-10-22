import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia } from 'pinia';
import { createMemoryHistory, createRouter } from 'vue-router';

const { searchArtworks } = vi.hoisted(() => ({ searchArtworks: vi.fn() }));
vi.mock('@/services/api', () => ({
  default: { searchArtworks, getDomains: vi.fn(), getTechniques: vi.fn(), getPeriods: vi.fn(), getArtists: vi.fn() }
}));

import SearchView from '@/views/SearchView.vue';

const resultat = (titres: string[], totalItems = titres.length) => ({
  items: titres.map((title, i) => ({ id: `a${i}`, title, reference: `R${i}`, creationDate: '', artists: [], hasImage: false })),
  totalItems,
  page: 1,
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

describe('SearchView', () => {
  beforeEach(() => {
    searchArtworks.mockReset();
    searchArtworks.mockResolvedValue(resultat(['Les Baigneuses', 'Orphée']));
    window.scrollTo = vi.fn();
  });

  it("lance la recherche décrite par l'adresse : mots, filtres, page et tri", async () => {
    await ouvrir('/search?searchText=huile&techniqueId=t1&periodId=p1&sortBy=date_desc&page=2');

    expect(derniereRecherche()).toMatchObject({
      search: 'huile', techniqueId: 't1', periodId: 'p1', sortBy: 'date_desc', page: 2, domainId: undefined, museumId: undefined
    });
  });

  it('affiche le nombre de résultats, au pluriel', async () => {
    const { vue } = await ouvrir('/search?searchText=huile');

    expect(vue.get('[data-testid="results-total"]').text()).toBe('2');
    expect(vue.text()).toContain('résultats trouvés');
  });

  it("retombe sur le tri par titre quand la pertinence n'a plus de recherche à classer", async () => {
    const { router } = await ouvrir('/search?sortBy=relevance');

    expect(derniereRecherche().sortBy).toBeUndefined();
    expect(router.currentRoute.value.query.sortBy).toBeUndefined();
  });

  it("garde le tri par pertinence quand l'adresse porte une recherche", async () => {
    await ouvrir('/search?searchText=huile&sortBy=relevance');

    expect(derniereRecherche().sortBy).toBe('relevance');
  });

  it("change le tri depuis la liste déroulante et le reporte dans l'adresse", async () => {
    const { vue, router } = await ouvrir('/search?searchText=huile');

    await vue.get('[data-testid="sort"]').setValue('title_desc');
    await flushPromises();

    expect(derniereRecherche()).toMatchObject({ search: 'huile', sortBy: 'title_desc' });
    expect(router.currentRoute.value.query.sortBy).toBe('title_desc');
  });

  it("propose le tri par artiste seulement avec une recherche ou un filtre", async () => {
    const sans = await ouvrir('/search');
    expect(sans.vue.find('option[value="artist"]').exists()).toBe(false);
    expect(sans.vue.find('option[value="relevance"]').exists()).toBe(false);

    const avec = await ouvrir('/search?searchText=huile');
    expect(avec.vue.find('option[value="artist"]').exists()).toBe(true);
    expect(avec.vue.find('option[value="relevance"]').exists()).toBe(true);
  });

  it("dit qu'aucune œuvre ne correspond, et efface les filtres au clic", async () => {
    searchArtworks.mockResolvedValue(resultat([]));
    const { vue, router } = await ouvrir('/search?searchText=zzz&techniqueId=t1&techniqueName=huile');
    expect(vue.get('[data-testid="no-results"]').text()).toContain('Aucune œuvre ne correspond à "zzz"');

    const lien = vue.findAll('a').find(a => a.text() === 'Effacer tous les filtres')!;
    await lien.trigger('click');
    await flushPromises();

    expect(derniereRecherche()).toMatchObject({ search: 'zzz', techniqueId: undefined });
    expect(router.currentRoute.value.query.techniqueId).toBeUndefined();
  });

  it("n'affiche la pagination que s'il y a plus d'une page, et passe à la page demandée", async () => {
    searchArtworks.mockResolvedValue(resultat(['A', 'B'], 30));
    const { vue, router } = await ouvrir('/search?searchText=huile');

    expect(vue.find('[data-testid="pagination"]').exists()).toBe(true);

    await vue.findAll('.pagination-number').find(b => b.text() === '2')!.trigger('click');
    await flushPromises();

    expect(derniereRecherche().page).toBe(2);
    expect(router.currentRoute.value.query.page).toBe('2');
  });

  it("n'affiche pas de pagination pour une seule page", async () => {
    const { vue } = await ouvrir('/search?searchText=huile');

    expect(vue.find('[data-testid="pagination"]').exists()).toBe(false);
  });

  it("signale une panne de l'API", async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    searchArtworks.mockRejectedValue(new Error('réseau'));

    const { vue } = await ouvrir('/search?searchText=huile');

    expect(vue.text()).toContain("Erreur lors de la recherche d'œuvres d'art");
  });
});
