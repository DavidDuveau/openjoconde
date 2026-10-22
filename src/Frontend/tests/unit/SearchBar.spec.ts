import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';

const { push, route } = vi.hoisted(() => ({ push: vi.fn(), route: { name: 'home' } }));
vi.mock('vue-router', () => ({ useRouter: () => ({ push, currentRoute: { value: route } }) }));

import SearchBar from '@/components/SearchBar.vue';

describe('SearchBar', () => {
  beforeEach(() => {
    push.mockReset();
    route.name = 'home';
  });
  afterEach(() => vi.useRealTimers());

  it('émet la recherche nettoyée et ouvre la page de recherche', async () => {
    const vue = mount(SearchBar);

    await vue.get('input').setValue('  monet ');
    await vue.get('form').trigger('submit');

    expect(vue.emitted('search')).toEqual([['monet']]);
    expect(push).toHaveBeenCalledWith({ name: 'search', query: { searchText: 'monet' } });
  });

  it('ne navigue pas quand on est déjà sur la page de recherche', async () => {
    route.name = 'search';
    const vue = mount(SearchBar);

    await vue.get('input').setValue('monet');
    await vue.get('form').trigger('submit');

    expect(vue.emitted('search')).toEqual([['monet']]);
    expect(push).not.toHaveBeenCalled();
  });

  it('refuse un terme trop court, puis efface l’erreur à la saisie', async () => {
    const vue = mount(SearchBar);

    await vue.get('input').setValue('a');
    await vue.get('form').trigger('submit');

    expect(vue.emitted('search')).toBeUndefined();
    expect(vue.get('.error-message').text()).toContain('Veuillez saisir');

    await vue.get('input').trigger('input');
    expect(vue.find('.error-message').exists()).toBe(false);
  });

  it('refuse une saisie vide', async () => {
    const vue = mount(SearchBar);

    await vue.get('form').trigger('submit');

    expect(vue.find('.error-message').exists()).toBe(true);
  });

  it('efface la saisie et le prévient', async () => {
    const vue = mount(SearchBar, { props: { initialSearchTerm: 'monet' } });

    await vue.get('.clear-button').trigger('click');

    expect((vue.get('input').element as HTMLInputElement).value).toBe('');
    expect(vue.emitted('clear')).toHaveLength(1);
    expect(vue.find('.clear-button').exists()).toBe(false);
  });

  it('suit le terme initial quand il change', async () => {
    const vue = mount(SearchBar, { props: { initialSearchTerm: 'a' } });

    await vue.setProps({ initialSearchTerm: 'renoir' });

    expect((vue.get('input').element as HTMLInputElement).value).toBe('renoir');
  });

  it('désactive le bouton pendant le chargement', () => {
    const vue = mount(SearchBar, { props: { loading: true } });

    expect(vue.get('.search-button').attributes('disabled')).toBeDefined();
    expect(vue.find('.loader').exists()).toBe(true);
  });

  it("recherche en différé, une seule fois après la fin de la saisie, et seulement si le terme est assez long", async () => {
    vi.useFakeTimers();
    const vue = mount(SearchBar);
    const vm = vue.vm as unknown as { searchTerm: string; debouncedSearch: () => void };

    vm.searchTerm = 'x';
    vm.debouncedSearch();
    vm.searchTerm = 'mo';
    vm.debouncedSearch();
    vm.searchTerm = 'monet ';
    vm.debouncedSearch();
    await vi.advanceTimersByTimeAsync(300);

    expect(vue.emitted('search')).toEqual([['monet']]);
  });
});
