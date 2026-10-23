import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';

const { push } = vi.hoisted(() => ({ push: vi.fn() }));
vi.mock('vue-router', () => ({ useRouter: () => ({ push, currentRoute: { value: { name: 'home' } } }) }));

import AboutView from '@/views/AboutView.vue';
import HomeView from '@/views/HomeView.vue';
import NotFoundView from '@/views/NotFoundView.vue';
import { LICENCE_URL, SOURCES } from '@/utils/sources';

const stubs = { 'router-link': { props: ['to'], template: '<a :href="to"><slot /></a>' } };

describe('NotFoundView', () => {
  it("annonce l'erreur et ramène à l'accueil", () => {
    const vue = mount(NotFoundView, { global: { stubs } });

    expect(vue.get('h1').text()).toBe('404');
    expect(vue.get('.home-link').attributes('href')).toBe('/');
  });
});

describe('AboutView', () => {
  it('liste chaque source avec son lien et rappelle la licence', () => {
    const vue = mount(AboutView);
    const liens = vue.findAll('[data-testid="about-sources"] a').map((a) => a.attributes('href'));

    for (const source of SOURCES) expect(liens).toContain(source.url);
    expect(liens).toContain(LICENCE_URL);
    expect(vue.get('[data-testid="about-sources"]').text()).toContain("n'affiche donc aucune image");
  });

  it("n'ouvre ses liens externes que sans fuite vers la page d'origine", () => {
    const vue = mount(AboutView);

    for (const lien of vue.findAll('a[target="_blank"]')) {
      expect(lien.attributes('rel')).toContain('noopener');
    }
  });
});

describe('HomeView', () => {
  beforeEach(() => push.mockReset());

  it('ouvre la recherche avec le texte saisi', async () => {
    const vue = mount(HomeView, { global: { stubs: { ...stubs, FranceMap: true } } });

    await vue.get('.search-input').setValue('monet');
    await vue.get('form').trigger('submit');

    expect(push).toHaveBeenCalledWith({ name: 'search', query: { searchText: 'monet' } });
  });

  it("ne cherche pas un texte réduit à des espaces", () => {
    const vue = mount(HomeView, { global: { stubs: { ...stubs, FranceMap: true } } });

    (vue.vm as unknown as { searchArtworks: (t: string) => void }).searchArtworks('   ');

    expect(push).not.toHaveBeenCalled();
  });

  it.each([
    [0, 'peinture'],
    [1, 'sculpture'],
    [2, 'graphique'],
    [3, 'photographie']
  ])('ouvre la collection de la catégorie %i (%s)', async (index, domaine) => {
    const vue = mount(HomeView, { global: { stubs: { ...stubs, FranceMap: true } } });

    await vue.findAll('.category-card')[index].trigger('click');

    expect(push).toHaveBeenCalledWith('/artworks?domain=' + domaine);
  });
});
