import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';

const { getPeriods, getTechniques, push } = vi.hoisted(() => ({ getPeriods: vi.fn(), getTechniques: vi.fn(), push: vi.fn() }));
vi.mock('@/services/api', () => ({ default: { getPeriods, getTechniques } }));
vi.mock('vue-router', () => ({ useRouter: () => ({ push }) }));

import ReferenceListView, { ReferenceKind } from '@/views/ReferenceListView.vue';

const page = (items: object[], totalItems = items.length, totalPages = 1) => ({ items, totalItems, page: 1, pageSize: 24, totalPages });
const element = (id: string, name: string) => ({ id, name });

async function afficher(kind: ReferenceKind) {
  const vue = mount(ReferenceListView, { props: { kind } });
  await flushPromises();
  return vue;
}

// La même vue sert aux périodes et aux techniques : chaque scénario est rejoué pour les deux
const genres = [
  { kind: 'periods' as const, api: getPeriods, titre: 'Périodes', un: 'période', plusieurs: 'périodes', aucune: 'Aucune période trouvée',
    panne: 'Erreur lors du chargement des périodes', nom: '19e siècle', requete: { periodId: 'x1', periodName: '19e siècle' } },
  { kind: 'techniques' as const, api: getTechniques, titre: 'Techniques', un: 'technique', plusieurs: 'techniques', aucune: 'Aucune technique trouvée',
    panne: 'Erreur lors du chargement des techniques', nom: 'huile sur toile', requete: { techniqueId: 'x1', techniqueName: 'huile sur toile' } }
];

describe.each(genres)('ReferenceListView ($kind)', ({ kind, api, titre, un, plusieurs, aucune, panne, nom, requete }) => {
  beforeEach(() => {
    getPeriods.mockReset();
    getTechniques.mockReset();
    push.mockReset();
    window.scrollTo = vi.fn() as never;
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });
  afterEach(() => vi.useRealTimers());

  it('liste les éléments de la première page, avec leur nombre au pluriel', async () => {
    api.mockResolvedValue(page([element('x1', nom), element('x2', 'autre')]));

    const vue = await afficher(kind);

    expect(vue.get('h1').text()).toBe(titre);
    expect(api).toHaveBeenCalledWith('', 24, 1);
    expect(vue.get('[data-testid="reference-count"]').text()).toBe(`2 ${plusieurs}`);
    expect(vue.findAll('[data-testid="reference-card"]').map((c) => c.text())).toEqual([nom, 'autre']);
  });

  it('écrit le nombre au singulier', async () => {
    api.mockResolvedValue(page([element('x1', nom)]));

    const vue = await afficher(kind);

    expect(vue.get('[data-testid="reference-count"]').text()).toBe(`1 ${un}`);
  });

  it("ouvre la recherche des œuvres de l'élément cliqué, avec son libellé dans l'adresse", async () => {
    api.mockResolvedValue(page([element('x1', nom)]));
    const vue = await afficher(kind);

    await vue.get('[data-testid="reference-card"]').trigger('click');

    expect(push).toHaveBeenCalledWith({ name: 'search', query: requete });
  });

  it("attend la fin de la saisie avant d'interroger l'API, puis repart de la première page", async () => {
    api.mockResolvedValue(page([element('x1', nom)]));
    const vue = await afficher(kind);
    vi.useFakeTimers();

    const saisie = vue.get('[data-testid="reference-search"]');
    await saisie.setValue('19');
    await saisie.setValue(' 19e ');
    expect(api).toHaveBeenCalledTimes(1);

    await vi.advanceTimersByTimeAsync(300);

    expect(api).toHaveBeenCalledTimes(2);
    expect(api).toHaveBeenLastCalledWith('19e', 24, 1);
  });

  it('pagine et remonte en haut de page', async () => {
    api.mockResolvedValue(page([element('x1', nom)], 60, 3));
    const vue = await afficher(kind);

    await vue.findAll('.pagination-number')[1].trigger('click');
    await flushPromises();

    expect(api).toHaveBeenLastCalledWith('', 24, 2);
    expect(window.scrollTo).toHaveBeenCalled();
  });

  it("dit qu'aucun élément ne correspond", async () => {
    api.mockResolvedValue(page([]));

    const vue = await afficher(kind);

    expect(vue.get('.no-results').text()).toContain(aucune);
  });

  it("signale l'erreur et permet de réessayer", async () => {
    api.mockRejectedValueOnce(new Error('réseau'));
    const vue = await afficher(kind);
    expect(vue.get('.error-container').text()).toContain(panne);

    api.mockResolvedValue(page([element('x1', nom)]));
    await vue.get('.primary-button').trigger('click');
    await flushPromises();

    expect(vue.findAll('[data-testid="reference-card"]')).toHaveLength(1);
  });
});
