import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount, RouterLinkStub } from '@vue/test-utils';

const { getMuseumLocations, push } = vi.hoisted(() => ({ getMuseumLocations: vi.fn(), push: vi.fn() }));
vi.mock('@/services/api', () => ({ default: { getMuseumLocations } }));
vi.mock('vue-router', () => ({ useRouter: () => ({ push }) }));

import FranceMap from '@/components/FranceMap.vue';

const locations = {
  total: 4,
  items: [
    { id: 'm-vannes', name: 'Musée de Vannes', latitude: 47.658, longitude: -2.76 },
    { id: 'm-brest', name: 'Musée de Brest', latitude: 48.39, longitude: -4.486 },
    { id: 'm-paris', name: 'Musée de Paris', latitude: 48.8566, longitude: 2.3522 }
  ]
};

type Vue = Awaited<ReturnType<typeof afficher>>;

// Le fond de carte se charge par import dynamique, qui demande plus qu'un tour de promesses
const attendreFinDeChargement = (vue: { find: (s: string) => { exists: () => boolean } }) =>
  vi.waitFor(() => expect(vue.find('[data-testid="map-loading"]').exists()).toBe(false));

async function afficher() {
  const vue = mount(FranceMap, { global: { stubs: { RouterLink: RouterLinkStub } } });
  await attendreFinDeChargement(vue);
  return vue;
}

const formes = (vue: Vue) => vue.findAll('[data-testid="map-shape"]');
const forme = (vue: Vue, code: string) => vue.get(`[data-testid="map-shape"][data-code="${code}"]`);
const points = (vue: Vue) => vue.findAll('[data-testid="map-point"]');
const point = (vue: Vue, nom: string) => points(vue).find(p => p.attributes('data-name') === nom)!;

describe('FranceMap', () => {
  beforeEach(() => {
    getMuseumLocations.mockReset();
    getMuseumLocations.mockResolvedValue(locations);
    push.mockReset();
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  it('dessine les régions, outre-mer compris, et un point par musée localisé', async () => {
    const vue = await afficher();

    expect(formes(vue)).toHaveLength(18);
    expect(points(vue).map(p => p.attributes('data-name')).sort()).toEqual(['Musée de Brest', 'Musée de Paris', 'Musée de Vannes']);
  });

  it('annonce le nombre de musées placés, et ceux qui ne le sont pas, avec un lien vers la liste', async () => {
    const vue = await afficher();

    expect(vue.get('[data-testid="map-count"]').text()).toContain('3 musées sur la carte');
    expect(vue.get('[data-testid="map-unlocated"]').text()).toContain('1 non localisé');
    expect(vue.getComponent(RouterLinkStub).props('to')).toEqual({ name: 'museums' });
  });

  it("n'annonce aucun musée non localisé quand tous sont placés", async () => {
    getMuseumLocations.mockResolvedValue({ ...locations, total: 3 });

    const vue = await afficher();

    expect(vue.find('[data-testid="map-unlocated"]').exists()).toBe(false);
  });

  it('montre le nom du musée au survol de son point, et le retire en le quittant', async () => {
    const vue = await afficher();

    await point(vue, 'Musée de Vannes').trigger('mouseenter');
    expect(vue.get('[data-testid="map-tooltip"]').text()).toBe('Musée de Vannes');

    await point(vue, 'Musée de Vannes').trigger('mouseleave');
    expect(vue.find('[data-testid="map-tooltip"]').exists()).toBe(false);
  });

  it('montre le nom du musée au focus clavier de son point', async () => {
    const vue = await afficher();

    await point(vue, 'Musée de Brest').trigger('focus');
    expect(vue.get('[data-testid="map-tooltip"]').text()).toBe('Musée de Brest');

    await point(vue, 'Musée de Brest').trigger('blur');
    expect(vue.find('[data-testid="map-tooltip"]').exists()).toBe(false);
  });

  it('ouvre la fiche du musée au clic sur son point, ou à la touche Entrée', async () => {
    const vue = await afficher();

    await point(vue, 'Musée de Vannes').trigger('click');
    await point(vue, 'Musée de Vannes').trigger('keydown.enter');

    expect(push).toHaveBeenNthCalledWith(1, { name: 'museum-detail', params: { id: 'm-vannes' } });
    expect(push).toHaveBeenCalledTimes(2);
  });

  it('zoome sur la région cliquée : ses départements et ses musées seulement', async () => {
    const vue = await afficher();

    await forme(vue, '53').trigger('click');

    expect(formes(vue).map(f => f.find('title').text()).sort()).toEqual(["Côtes-d'Armor", 'Finistère', 'Ille-et-Vilaine', 'Morbihan']);
    expect(points(vue).map(p => p.attributes('data-name')).sort()).toEqual(['Musée de Brest', 'Musée de Vannes']);
    expect(vue.findAll('[data-testid="map-crumb"]').map(c => c.text())).toEqual(['France', 'Bretagne']);
    expect(vue.find('[data-testid="map-unlocated"]').exists()).toBe(false);
  });

  it('zoome sur le département cliqué, puis ne descend pas plus bas', async () => {
    const vue = await afficher();
    await forme(vue, '53').trigger('click');

    await forme(vue, '56').trigger('click');

    expect(formes(vue)).toHaveLength(1);
    expect(points(vue).map(p => p.attributes('data-name'))).toEqual(['Musée de Vannes']);
    expect(vue.findAll('[data-testid="map-crumb"]').map(c => c.text())).toEqual(['France', 'Bretagne', 'Morbihan']);
    expect(formes(vue)[0].attributes('role')).toBeUndefined();

    await formes(vue)[0].trigger('click');
    expect(formes(vue)).toHaveLength(1);
  });

  it("remonte d'un niveau par le fil d'Ariane", async () => {
    const vue = await afficher();
    await forme(vue, '53').trigger('click');
    await forme(vue, '56').trigger('click');

    await vue.findAll('[data-testid="map-crumb"]')[1].trigger('click');
    expect(formes(vue)).toHaveLength(4);

    await vue.findAll('[data-testid="map-crumb"]')[0].trigger('click');
    expect(formes(vue)).toHaveLength(18);
  });

  it('zoome aussi au clavier, avec la touche Entrée', async () => {
    const vue = await afficher();

    await forme(vue, '53').trigger('keydown.enter');

    expect(formes(vue)).toHaveLength(4);
  });

  it('annonce le chargement, puis une panne avec la possibilité de réessayer', async () => {
    getMuseumLocations.mockRejectedValueOnce(new Error('panne'));
    const vue = mount(FranceMap, { global: { stubs: { RouterLink: RouterLinkStub } } });
    expect(vue.find('[data-testid="map-loading"]').exists()).toBe(true);
    await attendreFinDeChargement(vue);
    expect(vue.get('[data-testid="map-error"]').text()).toContain("n'a pas pu être chargée");

    await vue.get('.retry').trigger('click');
    await attendreFinDeChargement(vue);

    expect(formes(vue)).toHaveLength(18);
  });
});
