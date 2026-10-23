import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';

const { getMuseums, getMuseumCollections, push } = vi.hoisted(() => ({ getMuseums: vi.fn(), getMuseumCollections: vi.fn(), push: vi.fn() }));
vi.mock('@/services/api', () => ({ default: { getMuseums, getMuseumCollections } }));
vi.mock('vue-router', () => ({ useRouter: () => ({ push }) }));

import MuseumsView from '@/views/MuseumsView.vue';
import GeographyFilter from '@/components/GeographyFilter.vue';

const page = (items: object[], totalItems = items.length) => ({ items, totalItems, page: 1, pageSize: 12, totalPages: 1 });
const musee = (id: string, name: string) => ({ id, name, city: 'Lyon', department: 'Rhône', region: 'Auvergne-Rhône-Alpes' });

async function afficher() {
  const vue = mount(MuseumsView, { global: { stubs: { GeographyFilter: true, FranceMap: true } } });
  await flushPromises();
  return vue;
}

describe('MuseumsView', () => {
  beforeEach(() => {
    getMuseums.mockReset();
    getMuseumCollections.mockReset();
    getMuseumCollections.mockResolvedValue([]);
    push.mockReset();
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });
  afterEach(() => vi.useRealTimers());

  it('liste les musées avec leur lieu et leur nombre, au pluriel quand il y en a plusieurs', async () => {
    getMuseums.mockResolvedValue(page([musee('m1', 'Musée des Confluences'), musee('m2', 'Musée Guimet')]));

    const vue = await afficher();

    expect(vue.get('[data-testid="museum-count"]').text()).toBe('2 musées');
    expect(vue.findAll('[data-testid="museum-card"]')).toHaveLength(2);
    expect(vue.findAll('[data-testid="museum-card"]')[0].text()).toContain('Lyon · Rhône');
  });

  it('écrit « 1 musée » au singulier', async () => {
    getMuseums.mockResolvedValue(page([musee('m1', 'Musée des Confluences')]));

    const vue = await afficher();

    expect(vue.get('[data-testid="museum-count"]').text()).toBe('1 musée');
  });

  it('ouvre la fiche du musée cliqué', async () => {
    getMuseums.mockResolvedValue(page([musee('m1', 'Musée des Confluences')]));
    const vue = await afficher();

    await vue.get('[data-testid="museum-card"]').trigger('click');

    expect(push).toHaveBeenCalledWith('/museums/m1');
  });

  it("attend la fin de la saisie avant d'interroger l'API, puis repart de la première page", async () => {
    getMuseums.mockResolvedValue(page([musee('m1', 'Musée des Confluences')]));
    const vue = await afficher();
    vi.useFakeTimers();

    const saisie = vue.get('[data-testid="museum-search"]');
    await saisie.setValue('guim');
    await saisie.setValue('guimet');
    expect(getMuseums).toHaveBeenCalledTimes(1); // seulement le chargement initial

    await vi.advanceTimersByTimeAsync(300);

    expect(getMuseums).toHaveBeenCalledTimes(2);
    expect(getMuseums).toHaveBeenLastCalledWith(1, 12, 'guimet', '', '', '');
  });

  it('affiche la carte et les filtres de région et de département au-dessus de la liste', async () => {
    getMuseums.mockResolvedValue(page([musee('m1', 'Musée des Confluences')]));

    const vue = await afficher();

    expect(vue.findComponent({ name: 'FranceMap' }).exists()).toBe(true);
    expect(vue.findComponent(GeographyFilter).exists()).toBe(true);
  });

  it('filtre la liste par région et département, et repart de la première page', async () => {
    getMuseums.mockResolvedValue(page([musee('m1', 'Musée des Confluences')]));
    const vue = await afficher();
    const filtre = vue.findComponent(GeographyFilter);

    filtre.vm.$emit('update:region', 'Bretagne');
    filtre.vm.$emit('update:department', 'Morbihan');
    filtre.vm.$emit('change');
    await flushPromises();

    expect(getMuseums).toHaveBeenLastCalledWith(1, 12, '', 'Bretagne', 'Morbihan', '');
  });

  it("propose les types de collection avec leur nombre de musées, et filtre la liste par le type choisi", async () => {
    getMuseums.mockResolvedValue(page([musee('m1', 'Musée des Confluences')]));
    getMuseumCollections.mockResolvedValue([{ name: 'Archéologie', museums: 12 }, { name: 'Beaux-Arts', museums: 40 }]);
    const vue = await afficher();

    const options = vue.findAll('[data-testid="collection-select"] option').map(o => o.text());
    expect(options).toEqual(['Tous les types de collection', 'Archéologie (12)', 'Beaux-Arts (40)']);

    await vue.get('[data-testid="collection-select"]').setValue('Archéologie');
    await flushPromises();

    expect(getMuseums).toHaveBeenLastCalledWith(1, 12, '', '', '', 'Archéologie');
  });

  it("revient à la première page quand le type de collection change", async () => {
    getMuseums.mockResolvedValue({ ...page([musee('m1', 'Musée des Confluences')]), totalPages: 3 });
    getMuseumCollections.mockResolvedValue([{ name: 'Archéologie', museums: 12 }]);
    const vue = await afficher();
    window.scrollTo = vi.fn();
    (vue.vm as unknown as { goToPage: (p: number) => void }).goToPage(3);
    await flushPromises();
    expect(getMuseums).toHaveBeenLastCalledWith(3, 12, '', '', '', '');

    await vue.get('[data-testid="collection-select"]').setValue('Archéologie');
    await flushPromises();

    expect(getMuseums).toHaveBeenLastCalledWith(1, 12, '', '', '', 'Archéologie');
  });

  it("ne propose pas le filtre quand aucun type n'est connu, et la liste fonctionne", async () => {
    getMuseums.mockResolvedValue(page([musee('m1', 'Musée des Confluences')]));
    getMuseumCollections.mockResolvedValue([]);

    const vue = await afficher();

    expect(vue.find('[data-testid="collection-select"]').exists()).toBe(false);
    expect(vue.findAll('[data-testid="museum-card"]')).toHaveLength(1);
  });

  it("garde la liste quand les types de collection ne se chargent pas", async () => {
    getMuseums.mockResolvedValue(page([musee('m1', 'Musée des Confluences')]));
    getMuseumCollections.mockRejectedValue(new Error('panne'));

    const vue = await afficher();

    expect(vue.find('[data-testid="collection-select"]').exists()).toBe(false);
    expect(vue.findAll('[data-testid="museum-card"]')).toHaveLength(1);
  });

  it("dit qu'aucun musée ne correspond", async () => {
    getMuseums.mockResolvedValue(page([]));

    const vue = await afficher();

    expect(vue.text()).toContain('Aucun musée trouvé');
  });

  it("signale une panne de l'API et recharge au clic sur « Réessayer »", async () => {
    getMuseums.mockRejectedValue(new Error('réseau'));
    const vue = await afficher();
    expect(vue.text()).toContain('Erreur lors du chargement des musées');

    getMuseums.mockResolvedValue(page([musee('m1', 'Musée des Confluences')]));
    await vue.get('button').trigger('click');
    await flushPromises();

    expect(vue.findAll('[data-testid="museum-card"]')).toHaveLength(1);
  });
});
