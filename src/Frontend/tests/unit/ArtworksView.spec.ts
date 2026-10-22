import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { reactive } from 'vue';

const { getArtworks, searchArtworks, push, replace, route } = vi.hoisted(() => ({
  getArtworks: vi.fn(),
  searchArtworks: vi.fn(),
  push: vi.fn(),
  replace: vi.fn(),
  route: { current: { query: {} as Record<string, string> } }
}));
vi.mock('@/services/api', () => ({ default: { getArtworks, searchArtworks } }));
vi.mock('vue-router', () => ({ useRoute: () => route.current, useRouter: () => ({ push, replace }) }));

import ArtworksView from '@/views/ArtworksView.vue';

const resultat = (items: object[], totalItems = items.length) => ({ items, totalItems, page: 1, pageSize: 12, totalPages: Math.ceil(totalItems / 12) });
const oeuvre = (id: string) => ({ id, title: `Œuvre ${id}`, creationDate: '1900', artists: [] });

const carte = { props: ['id', 'title'], emits: ['click'], template: '<div class="carte" @click="$emit(\'click\')">{{ title }}</div>' };

async function afficher(query: Record<string, string> = {}) {
  route.current = reactive({ query });
  setActivePinia(createPinia());
  const vue = mount(ArtworksView, { global: { stubs: { ArtworkCard: carte } } });
  await flushPromises();
  return vue;
}

describe('ArtworksView', () => {
  beforeEach(() => {
    [getArtworks, searchArtworks, push, replace].forEach((m) => m.mockReset());
    getArtworks.mockResolvedValue(resultat([oeuvre('1'), oeuvre('2')]));
    searchArtworks.mockResolvedValue(resultat([oeuvre('3')]));
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  it('liste les œuvres de la première page et garde une URL propre', async () => {
    const vue = await afficher();

    expect(getArtworks).toHaveBeenCalledWith(1, 12);
    expect(vue.findAll('.carte').map((c) => c.text())).toEqual(['Œuvre 1', 'Œuvre 2']);
    expect(replace).toHaveBeenCalledWith({ query: {} });
  });

  it("reprend recherche, filtres et page de l'URL", async () => {
    const vue = await afficher({ search: 'huile', domain: 'peinture', period: 'baroque', museum: 'louvre', page: '2' });

    expect(searchArtworks).toHaveBeenCalledWith(expect.objectContaining({ search: 'huile', page: 2, pageSize: 12 }));
    expect(vue.findAll('.carte')).toHaveLength(1);
    expect(replace).toHaveBeenCalledWith({
      query: { search: 'huile', domain: 'peinture', period: 'baroque', museum: 'louvre', page: '2' }
    });
  });

  it('relance la recherche au bouton ou à la touche Entrée', async () => {
    const vue = await afficher();

    await vue.get('.search-bar input').setValue('bronze');
    await vue.get('.search-bar button').trigger('click');
    await flushPromises();
    await vue.get('.search-bar input').trigger('keyup.enter');
    await flushPromises();

    expect(searchArtworks).toHaveBeenCalledTimes(2);
    expect(searchArtworks).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'bronze' }));
  });

  it("suit les changements de l'URL", async () => {
    const vue = await afficher();

    route.current.query = ({ search: 'marbre', domain: 'sculpture', period: 'moderne', museum: 'orsay', page: '3' });
    await flushPromises();

    expect(searchArtworks).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'marbre', page: 3 }));
    const etat = vue.vm as unknown as { selectedDomain: string; selectedPeriod: string; selectedMuseum: string };
    expect(etat.selectedDomain).toBe('sculpture');
    expect(etat.selectedPeriod).toBe('moderne');
    expect(etat.selectedMuseum).toBe('orsay');
  });

  it("ouvre la fiche de l'œuvre cliquée", async () => {
    const vue = await afficher();

    await vue.findAll('.carte')[1].trigger('click');

    expect(push).toHaveBeenCalledWith('/artworks/2');
  });

  it("dit qu'aucune œuvre ne correspond", async () => {
    getArtworks.mockResolvedValue(resultat([]));

    const vue = await afficher();

    expect(vue.get('.no-results').text()).toContain('Aucune œuvre');
  });

  it("signale l'erreur et permet de réessayer", async () => {
    getArtworks.mockRejectedValueOnce(new Error('réseau'));
    const vue = await afficher();
    expect(vue.get('.error').text()).toContain('Une erreur est survenue');

    await vue.get('.error button').trigger('click');
    await flushPromises();

    expect(vue.findAll('.carte')).toHaveLength(2);
  });

  it('change de page avec les boutons, désactivés aux extrémités', async () => {
    getArtworks.mockResolvedValue(resultat([oeuvre('1')], 30));
    const vue = await afficher();
    const [precedent, suivant] = vue.findAll('.pagination button');

    expect(vue.get('.page-info').text()).toBe('Page 1 sur 3');
    expect(precedent.attributes('disabled')).toBeDefined();

    await suivant.trigger('click');
    await flushPromises();

    expect(getArtworks).toHaveBeenLastCalledWith(2, 12);
    expect(replace).toHaveBeenLastCalledWith({ query: { page: '2' } });
  });
});
