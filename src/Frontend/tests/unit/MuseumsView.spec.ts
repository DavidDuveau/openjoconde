import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';

const { getMuseums, push } = vi.hoisted(() => ({ getMuseums: vi.fn(), push: vi.fn() }));
vi.mock('@/services/api', () => ({ default: { getMuseums } }));
vi.mock('vue-router', () => ({ useRouter: () => ({ push }) }));

import MuseumsView from '@/views/MuseumsView.vue';

const page = (items: object[], totalItems = items.length) => ({ items, totalItems, page: 1, pageSize: 12, totalPages: 1 });
const musee = (id: string, name: string) => ({ id, name, city: 'Lyon', department: 'Rhône', region: 'Auvergne-Rhône-Alpes' });

async function afficher() {
  const vue = mount(MuseumsView);
  await flushPromises();
  return vue;
}

describe('MuseumsView', () => {
  beforeEach(() => {
    getMuseums.mockReset();
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
    expect(getMuseums).toHaveBeenLastCalledWith(1, 12, 'guimet');
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
