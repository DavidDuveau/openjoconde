import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { reactive } from 'vue';

const { push, route } = vi.hoisted(() => ({
  push: vi.fn(),
  route: { name: 'home' as string, query: {} as Record<string, unknown> }
}));
vi.mock('vue-router', () => ({
  useRoute: () => route,
  useRouter: () => ({ push, currentRoute: { value: route } })
}));

import AppHeader from '@/components/AppHeader.vue';

const stubs = { 'router-link': { template: '<a><slot /></a>' } };
const largeur = (px: number) => Object.defineProperty(window, 'innerWidth', { value: px, configurable: true });

const afficher = () => mount(AppHeader, { global: { stubs } });

describe('AppHeader', () => {
  beforeEach(() => {
    push.mockReset();
    route.name = 'home';
    route.query = {};
    largeur(1200);
  });
  afterEach(() => largeur(1024));

  it('affiche la navigation et la recherche sur grand écran', () => {
    const vue = afficher();

    expect(vue.findAll('.main-nav li')).toHaveLength(5);
    expect(vue.find('.search-input').exists()).toBe(true);
    expect(vue.find('.search-toggle').exists()).toBe(false);
  });

  it("reprend le terme cherché dans l'URL", () => {
    route.query = { searchText: 'monet' };

    const vue = afficher();

    expect((vue.get('.search-input').element as HTMLInputElement).value).toBe('monet');
  });

  it('ignore un paramètre de recherche qui n’est pas un texte', () => {
    route.query = { searchText: ['a', 'b'] };

    const vue = afficher();

    expect((vue.get('.search-input').element as HTMLInputElement).value).toBe('');
  });

  it('ouvre la page de recherche à la première page', async () => {
    const vue = afficher();

    await vue.get('.search-input').setValue('monet');
    await vue.get('form').trigger('submit');

    expect(push).toHaveBeenCalledWith({ name: 'search', query: { searchText: 'monet', page: '1' } });
  });

  it("garde le reste de l'URL en effaçant la recherche, sur la page de recherche seulement", async () => {
    route.name = 'search';
    route.query = reactive({ searchText: 'monet', domainId: 'd1' });
    const vue = afficher();

    await vue.get('.clear-button').trigger('click');

    expect(push).toHaveBeenCalledWith({ name: 'search', query: { domainId: 'd1' } });
  });

  it("n'efface rien ailleurs que sur la page de recherche", async () => {
    route.query = { searchText: 'monet' };
    const vue = afficher();

    await vue.get('.clear-button').trigger('click');

    expect(push).not.toHaveBeenCalled();
  });

  it('replie la recherche derrière une loupe sur mobile, et la déplie à la demande', async () => {
    largeur(500);
    const vue = afficher();

    expect(vue.find('.search-input').exists()).toBe(false);
    await vue.get('.search-toggle').trigger('click');
    expect(vue.find('.search-input').exists()).toBe(true);

    await vue.get('.close-search-btn').trigger('click');
    expect(vue.find('.search-input').exists()).toBe(false);
  });

  it("referme la recherche dépliée quand la fenêtre redevient large", async () => {
    largeur(500);
    const vue = afficher();
    await vue.get('.search-toggle').trigger('click');

    largeur(1200);
    window.dispatchEvent(new Event('resize'));
    await vue.vm.$nextTick();

    expect(vue.find('.search-section').classes()).not.toContain('expanded');
    expect(vue.find('.search-input').exists()).toBe(true);
  });

  it("garde l'état mobile quand la fenêtre reste étroite au redimensionnement", async () => {
    largeur(500);
    const vue = afficher();
    await vue.get('.search-toggle').trigger('click');

    largeur(400);
    window.dispatchEvent(new Event('resize'));
    await vue.vm.$nextTick();

    expect(vue.find('.search-section').classes()).toContain('expanded');
  });

  it('cesse d’écouter la fenêtre une fois démonté', () => {
    const retrait = vi.spyOn(window, 'removeEventListener');
    const vue = afficher();

    vue.unmount();

    expect(retrait).toHaveBeenCalledWith('resize', expect.any(Function));
  });
});
