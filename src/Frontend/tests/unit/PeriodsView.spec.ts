import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';

const { getPeriods, push } = vi.hoisted(() => ({ getPeriods: vi.fn(), push: vi.fn() }));
vi.mock('@/services/api', () => ({ default: { getPeriods } }));
vi.mock('vue-router', () => ({ useRouter: () => ({ push }) }));

import PeriodsView from '@/views/PeriodsView.vue';

const page = (items: object[], totalItems = items.length, totalPages = 1) => ({ items, totalItems, page: 1, pageSize: 24, totalPages });
const periode = (id: string, name: string) => ({ id, name });

async function afficher() {
  const vue = mount(PeriodsView);
  await flushPromises();
  return vue;
}

describe('PeriodsView', () => {
  beforeEach(() => {
    getPeriods.mockReset();
    push.mockReset();
    window.scrollTo = vi.fn() as never;
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });
  afterEach(() => vi.useRealTimers());

  it('liste les périodes de la première page, avec leur nombre au pluriel', async () => {
    getPeriods.mockResolvedValue(page([periode('p1', '19e siècle'), periode('p2', '20e siècle')]));

    const vue = await afficher();

    expect(getPeriods).toHaveBeenCalledWith('', 24, 1);
    expect(vue.get('[data-testid="period-count"]').text()).toBe('2 périodes');
    expect(vue.findAll('[data-testid="period-card"]').map((c) => c.text())).toEqual(['19e siècle', '20e siècle']);
  });

  it('écrit « 1 période » au singulier', async () => {
    getPeriods.mockResolvedValue(page([periode('p1', '19e siècle')]));

    const vue = await afficher();

    expect(vue.get('[data-testid="period-count"]').text()).toBe('1 période');
  });

  it("ouvre la recherche des œuvres de la période cliquée, avec son libellé dans l'adresse", async () => {
    getPeriods.mockResolvedValue(page([periode('p1', '19e siècle')]));
    const vue = await afficher();

    await vue.get('[data-testid="period-card"]').trigger('click');

    expect(push).toHaveBeenCalledWith({ name: 'search', query: { periodId: 'p1', periodName: '19e siècle' } });
  });

  it("attend la fin de la saisie avant d'interroger l'API, puis repart de la première page", async () => {
    getPeriods.mockResolvedValue(page([periode('p1', '19e siècle')]));
    const vue = await afficher();
    vi.useFakeTimers();

    const saisie = vue.get('[data-testid="period-search"]');
    await saisie.setValue('19');
    await saisie.setValue(' 19e ');
    expect(getPeriods).toHaveBeenCalledTimes(1);

    await vi.advanceTimersByTimeAsync(300);

    expect(getPeriods).toHaveBeenCalledTimes(2);
    expect(getPeriods).toHaveBeenLastCalledWith('19e', 24, 1);
  });

  it('pagine et remonte en haut de page', async () => {
    getPeriods.mockResolvedValue(page([periode('p1', '19e siècle')], 60, 3));
    const vue = await afficher();

    await vue.findAll('.pagination-number')[1].trigger('click');
    await flushPromises();

    expect(getPeriods).toHaveBeenLastCalledWith('', 24, 2);
    expect(window.scrollTo).toHaveBeenCalled();
  });

  it("dit qu'aucune période ne correspond", async () => {
    getPeriods.mockResolvedValue(page([]));

    const vue = await afficher();

    expect(vue.get('.no-results').text()).toContain('Aucune période trouvée');
  });

  it("signale l'erreur et permet de réessayer", async () => {
    getPeriods.mockRejectedValueOnce(new Error('réseau'));
    const vue = await afficher();
    expect(vue.get('.error-container').text()).toContain('Erreur lors du chargement des périodes');

    getPeriods.mockResolvedValue(page([periode('p1', '19e siècle')]));
    await vue.get('.primary-button').trigger('click');
    await flushPromises();

    expect(vue.findAll('[data-testid="period-card"]')).toHaveLength(1);
  });
});
