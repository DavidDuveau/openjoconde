import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils';

const { getArtwork, push } = vi.hoisted(() => ({ getArtwork: vi.fn(), push: vi.fn() }));
vi.mock('@/services/api', () => ({ default: { getArtwork } }));
vi.mock('vue-router', () => ({ useRouter: () => ({ push }) }));

import FavorisView from '@/views/FavorisView.vue';
import { ajouterFavori, chargerFavoris, estFavori, LIMITE_FAVORIS, oublierProbleme } from '@/utils/favoris';

const id = (n: number) => `00000000-0000-0000-0000-${String(n).padStart(12, '0')}`;
const detail = (n: number) => ({
  id: id(n), title: `Œuvre ${n}`, imageUrl: '', creationDate: '1850', hasImage: false,
  artists: [{ id: 'a', firstName: 'Claude', lastName: 'Monet', role: null }]
});

// Les favoris sont partagés : une vue montée par un test précédent réagirait aux favoris du suivant
const montees: { unmount(): void }[] = [];

async function afficher() {
  const vue = mount(FavorisView, { global: { stubs: { RouterLink: RouterLinkStub } } });
  montees.push(vue);
  await flushPromises();
  return vue;
}

const favoriser = (...numeros: number[]) => numeros.forEach(n => ajouterFavori(id(n)));

describe('FavorisView', () => {
  beforeEach(() => {
    getArtwork.mockReset();
    push.mockReset();
    getArtwork.mockImplementation(async (identifiant: string) => detail(Number(identifiant.slice(-12))));
    window.localStorage.clear();
    chargerFavoris();
    oublierProbleme();
    vi.restoreAllMocks();
  });
  afterEach(() => {
    montees.splice(0).forEach(vue => vue.unmount());
    vi.restoreAllMocks();
  });

  it('dit que les favoris restent sur cet appareil et sont perdus si on vide le navigateur', async () => {
    const vue = await afficher();

    const note = vue.get('[data-testid="favoris-note"]').text();
    expect(note).toContain('restent sur cet appareil');
    expect(note).toContain('perdus si vous videz');
  });

  it("invite à en ajouter quand il n'y en a aucun", async () => {
    const vue = await afficher();

    expect(vue.find('[data-testid="favoris-vide"]').exists()).toBe(true);
    expect(getArtwork).not.toHaveBeenCalled();
  });

  it('montre les œuvres sous forme de cartes, la dernière ajoutée en premier, avec leur nombre', async () => {
    favoriser(1, 2);

    const vue = await afficher();

    expect(vue.get('[data-testid="favoris-compte"]').text()).toBe('2 œuvres favorites');
    expect(vue.findAll('[data-testid="artwork-title"]').map(t => t.text())).toEqual(['Œuvre 2', 'Œuvre 1']);
  });

  it('accorde le compte au singulier', async () => {
    favoriser(1);

    const vue = await afficher();

    expect(vue.get('[data-testid="favoris-compte"]').text()).toBe('1 œuvre favorite');
  });

  it('ouvre la fiche au clic sur une carte', async () => {
    favoriser(1);
    const vue = await afficher();

    await vue.get('[data-testid="artwork-card"]').trigger('click');

    expect(push).toHaveBeenCalledWith({ name: 'artwork-detail', params: { id: id(1) } });
  });

  it("retire un favori immédiatement depuis sa carte, sans recharger les autres", async () => {
    favoriser(1, 2);
    const vue = await afficher();

    await vue.findAll('[data-testid="favorite-button"]')[0].trigger('click');

    expect(estFavori(id(2))).toBe(false);
    expect(vue.findAll('[data-testid="artwork-title"]').map(t => t.text())).toEqual(['Œuvre 1']);
    expect(getArtwork).toHaveBeenCalledTimes(2);
  });

  it('signale une œuvre retirée de la base, la laisse supprimer, et affiche le reste', async () => {
    favoriser(1, 2);
    getArtwork.mockImplementation(async (identifiant: string) => {
      if (identifiant === id(2)) throw { response: { status: 404 } };
      return detail(1);
    });

    const vue = await afficher();

    expect(vue.get('[data-testid="favori-retiree"]').text()).toContain('Œuvre retirée de la base');
    expect(vue.findAll('[data-testid="artwork-title"]')).toHaveLength(1);

    await vue.get('[data-testid="favori-retiree"] button').trigger('click');

    expect(estFavori(id(2))).toBe(false);
    expect(vue.find('[data-testid="favori-retiree"]').exists()).toBe(false);
  });

  it("propose de réessayer quand une œuvre n'a pas pu être chargée, sans la croire retirée", async () => {
    favoriser(1);
    getArtwork.mockRejectedValueOnce(new Error('réseau')).mockResolvedValueOnce(detail(1));
    const vue = await afficher();
    expect(vue.get('[data-testid="favori-panne"]').text()).toContain("n'a pas pu être chargée");

    await vue.get('[data-testid="favori-panne"] button').trigger('click');
    await flushPromises();

    expect(vue.find('[data-testid="favori-panne"]').exists()).toBe(false);
    expect(vue.get('[data-testid="artwork-title"]').text()).toBe('Œuvre 1');
  });

  it("permet de supprimer une œuvre qui ne se charge pas", async () => {
    favoriser(1);
    getArtwork.mockRejectedValue(new Error('réseau'));
    const vue = await afficher();

    await vue.findAll('[data-testid="favori-panne"] button')[1].trigger('click');

    expect(estFavori(id(1))).toBe(false);
  });

  it('pagine par douze et revient en arrière quand la dernière page se vide', async () => {
    favoriser(...Array.from({ length: 13 }, (_, i) => i + 1));
    const vue = await afficher();
    expect(vue.findAll('[data-testid="artwork-card"]')).toHaveLength(12);

    await vue.findAll('.pagination-number')[1].trigger('click');
    await flushPromises();
    expect(vue.findAll('[data-testid="artwork-card"]')).toHaveLength(1);

    await vue.get('[data-testid="favorite-button"]').trigger('click');
    await flushPromises();

    expect(vue.findAll('[data-testid="artwork-card"]')).toHaveLength(12);
    expect(vue.get('[data-testid="favoris-compte"]').text()).toBe('12 œuvres favorites');
  });

  it('explique un problème de stockage', async () => {
    favoriser(1);
    const vue = await afficher();
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('plein'); });

    await vue.get('[data-testid="favorite-button"]').trigger('click');

    expect(vue.get('[data-testid="favoris-probleme"]').text()).toContain('ne peuvent pas être gardés');
  });

  it('explique la limite quand elle est atteinte', async () => {
    favoriser(...Array.from({ length: LIMITE_FAVORIS }, (_, i) => i + 1));
    ajouterFavori(id(LIMITE_FAVORIS + 1));

    const vue = await afficher();

    expect(vue.get('[data-testid="favoris-probleme"]').text()).toContain(`${LIMITE_FAVORIS} favoris`);
  });
});
