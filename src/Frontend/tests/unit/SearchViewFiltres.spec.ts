import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia } from 'pinia';
import { createMemoryHistory, createRouter } from 'vue-router';

const { searchArtworks } = vi.hoisted(() => ({ searchArtworks: vi.fn() }));
vi.mock('@/services/api', () => ({
  default: {
    searchArtworks, getMuseumGeography: vi.fn().mockResolvedValue([]), getMuseum: vi.fn(), getMuseums: vi.fn(),
    getDomains: vi.fn(), getTechniques: vi.fn(), getPeriods: vi.fn(), getArtists: vi.fn()
  }
}));

import SearchView from '@/views/SearchView.vue';

const resultat = {
  items: [{ id: 'a0', title: 'Les Baigneuses', reference: 'R0', creationDate: '', artists: [], hasImage: false }],
  totalItems: 1, page: 1, pageSize: 12, totalPages: 1
};

async function ouvrir(adresse: string) {
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/search', component: SearchView }] });
  await router.push(adresse);
  await router.isReady();
  const vue = mount(SearchView, { global: { plugins: [createPinia(), router] } });
  await flushPromises();
  return { vue, router };
}

const derniereRecherche = () => searchArtworks.mock.calls.at(-1)![0];
const puces = (vue: Awaited<ReturnType<typeof ouvrir>>['vue']) => vue.findAll('[data-testid="filter-chip"]');

describe('SearchView : filtres avancés', () => {
  beforeEach(() => {
    searchArtworks.mockReset();
    searchArtworks.mockResolvedValue(resultat);
  });

  describe('plage d\'années de création', () => {
    it("lit les années dans l'adresse, les affiche dans les champs et les envoie à l'API en nombres", async () => {
      const { vue } = await ouvrir('/search?yearFrom=-500&yearTo=1850');

      expect(derniereRecherche()).toMatchObject({ yearFrom: -500, yearTo: 1850 });
      expect((vue.get('[data-testid="year-from"]').element as HTMLInputElement).value).toBe('-500');
      expect((vue.get('[data-testid="year-to"]').element as HTMLInputElement).value).toBe('1850');
    });

    it("n'envoie aucune année quand il n'y en a pas", async () => {
      await ouvrir('/search');

      expect(derniereRecherche()).toMatchObject({ yearFrom: undefined, yearTo: undefined });
    });

    it("pose une année : première page, reportée dans l'adresse", async () => {
      const { vue, router } = await ouvrir('/search?page=3');

      await vue.get('[data-testid="year-from"]').setValue('1850');
      await vue.get('[data-testid="year-from"]').trigger('change');
      await flushPromises();

      expect(router.currentRoute.value.query).toMatchObject({ yearFrom: '1850' });
      expect(router.currentRoute.value.query.page).toBeUndefined();
      expect(derniereRecherche()).toMatchObject({ yearFrom: 1850, page: 1 });
    });

    it("relance aussi la recherche à la touche Entrée, et accepte une année avant J.-C.", async () => {
      const { vue } = await ouvrir('/search');

      await vue.get('[data-testid="year-to"]').setValue('-300');
      await vue.get('[data-testid="year-to"]').trigger('keyup.enter');
      await flushPromises();

      expect(derniereRecherche()).toMatchObject({ yearTo: -300 });
    });

    it("refuse une plage à l'envers : message, et aucune recherche de plus", async () => {
      const { vue, router } = await ouvrir('/search');

      // Comme au clavier : une année, puis la sortie du champ, puis l'autre
      await vue.get('[data-testid="year-from"]').setValue('1920');
      await vue.get('[data-testid="year-from"]').trigger('change');
      await flushPromises();
      const appels = searchArtworks.mock.calls.length;
      await vue.get('[data-testid="year-to"]').setValue('1850');
      await vue.get('[data-testid="year-to"]').trigger('change');
      await flushPromises();

      expect(vue.get('[data-testid="year-error"]').text()).toContain("L'année de début doit précéder");
      expect(searchArtworks.mock.calls.length).toBe(appels);
      expect(router.currentRoute.value.query.yearTo).toBeUndefined();
    });

    it("refuse un texte qui n'est pas une année", async () => {
      const { vue } = await ouvrir('/search');

      await vue.get('[data-testid="year-from"]').setValue('vers 1850');
      await vue.get('[data-testid="year-from"]').trigger('change');
      await flushPromises();

      expect(vue.get('[data-testid="year-error"]').text()).toContain('en chiffres');
    });

    it("n'affiche pas de message quand la plage est bonne ou vide", async () => {
      const { vue } = await ouvrir('/search?yearFrom=1800&yearTo=1900');

      expect(vue.find('[data-testid="year-error"]').exists()).toBe(false);
    });
  });

  describe('filtres actifs', () => {
    const adresse = '/search?searchText=portrait&domainId=d1&domainName=peinture&region=Bretagne&department=Morbihan&yearFrom=1850&yearTo=1920';

    it("liste chaque filtre actif, avec leur nombre", async () => {
      const { vue } = await ouvrir(adresse);

      expect(puces(vue).map(p => p.text().replace(/\s*×$/, ''))).toEqual([
        'Mots : portrait', 'Domaine : peinture', 'Région : Bretagne', 'Département : Morbihan', 'Créée entre 1850 et 1920'
      ]);
      expect(vue.get('.active-filters-title').text()).toBe('5 filtres actifs :');
    });

    it("accorde « filtre actif » au singulier, et n'affiche rien sans filtre", async () => {
      const seul = await ouvrir('/search?domainId=d1&domainName=peinture');
      expect(seul.vue.get('.active-filters-title').text()).toBe('1 filtre actif :');

      const aucun = await ouvrir('/search');
      expect(aucun.vue.find('[data-testid="active-filters"]').exists()).toBe(false);
    });

    it.each([
      ['/search?yearFrom=1850', 'Créée à partir de 1850'],
      ['/search?yearTo=1920', "Créée jusqu'en 1920"],
      ['/search?domainId=d1', 'Domaine : sélectionné'],
      ['/search?techniqueId=t1&techniqueName=huile', 'Technique : huile'],
      ['/search?periodId=p1&periodName=19e', 'Période : 19e'],
      ['/search?artistId=r1&artistName=Monet', 'Artiste : Monet'],
      ['/search?museumId=m1&museumName=Orsay', 'Musée : Orsay'],
      ['/search?lat=47.3&lon=5.04&sortBy=distance', 'Près de moi']
    ])('%s : puce « %s »', async (url, libelle) => {
      const { vue } = await ouvrir(url);

      expect(puces(vue).map(p => p.text())).toContainEqual(expect.stringContaining(libelle));
    });

    it("retire un seul filtre à la fois, et relance la recherche à la première page", async () => {
      const { vue, router } = await ouvrir(adresse + '&page=2');

      await puces(vue)[1].trigger('click'); // domaine
      await flushPromises();

      expect(derniereRecherche()).toMatchObject({ domainId: undefined, region: 'Bretagne', search: 'portrait', page: 1 });
      expect(router.currentRoute.value.query.domainId).toBeUndefined();
      expect(router.currentRoute.value.query.region).toBe('Bretagne');
      expect(puces(vue)).toHaveLength(4);
    });

    it.each([
      [0, { search: undefined }],
      [2, { region: undefined, department: undefined }],
      [3, { department: undefined, region: 'Bretagne' }],
      [4, { yearFrom: undefined, yearTo: undefined }]
    ])('la puce %i retire ce qu\'elle désigne', async (index, attendu) => {
      const { vue } = await ouvrir(adresse);

      await puces(vue)[index].trigger('click');
      await flushPromises();

      expect(derniereRecherche()).toMatchObject(attendu);
    });

    it.each([
      ['/search?techniqueId=t1&techniqueName=huile', { techniqueId: undefined }],
      ['/search?periodId=p1&periodName=19e', { periodId: undefined }],
      ['/search?artistId=r1&artistName=Monet', { artistId: undefined }],
      ['/search?museumId=m1&museumName=Orsay', { museumId: undefined }],
      ['/search?lat=47.3&lon=5.04&sortBy=distance', { latitude: undefined, longitude: undefined }]
    ])('retire le filtre de %s', async (url, attendu) => {
      const { vue } = await ouvrir(url);

      await puces(vue)[0].trigger('click');
      await flushPromises();

      expect(derniereRecherche()).toMatchObject(attendu);
      expect(puces(vue)).toHaveLength(0);
    });

    it("« Tout effacer » retire les filtres et les mots-clés", async () => {
      const { vue, router } = await ouvrir(adresse);

      await vue.get('[data-testid="clear-all-filters"]').trigger('click');
      await flushPromises();

      expect(derniereRecherche()).toMatchObject({
        search: undefined, domainId: undefined, region: undefined, department: undefined, yearFrom: undefined, yearTo: undefined
      });
      expect(router.currentRoute.value.query).toEqual({});
      expect(vue.find('[data-testid="active-filters"]').exists()).toBe(false);
    });

    it("« Réinitialiser » du panneau efface aussi la plage d'années", async () => {
      const { vue } = await ouvrir('/search?yearFrom=1850&yearTo=1920');

      await vue.get('.reset-button').trigger('click');
      await flushPromises();

      expect(derniereRecherche()).toMatchObject({ yearFrom: undefined, yearTo: undefined });
    });
  });

  describe('sur petit écran', () => {
    it("déplie et replie les filtres à la demande, en annonçant l'état et le nombre de filtres actifs", async () => {
      const { vue } = await ouvrir('/search?domainId=d1&domainName=peinture');
      const bouton = vue.get('[data-testid="filters-toggle"]');

      expect(bouton.attributes('aria-expanded')).toBe('false');
      expect(bouton.text()).toBe('Afficher les filtres (1)');
      expect(vue.get('.search-filters').classes()).not.toContain('is-open');

      await bouton.trigger('click');

      expect(bouton.attributes('aria-expanded')).toBe('true');
      expect(bouton.text()).toBe('Masquer les filtres (1)');
      expect(vue.get('.search-filters').classes()).toContain('is-open');
    });

    it("n'annonce pas de nombre quand aucun filtre n'est actif", async () => {
      const { vue } = await ouvrir('/search');

      expect(vue.get('[data-testid="filters-toggle"]').text()).toBe('Afficher les filtres');
    });
  });
});
