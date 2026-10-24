import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils';

const { getArtwork, push, route } = vi.hoisted(() => ({
  getArtwork: vi.fn(),
  push: vi.fn(),
  route: { current: { params: { id: '' } as Record<string, string> } }
}));
vi.mock('@/services/api', () => ({ default: { getArtwork } }));
vi.mock('vue-router', () => ({ useRoute: () => route.current, useRouter: () => ({ push }) }));

import CollectionView from '@/views/CollectionView.vue';
import {
  chargerCollections, collections, creerCollection, oublierProblemeCollections, trouverCollection
} from '@/utils/collections';
import { decoderPartage } from '@/utils/partage';

const id = (n: number) => `00000000-0000-0000-0000-${String(n).padStart(12, '0')}`;
const detail = (n: number) => ({ id: id(n), title: `Œuvre ${n}`, imageUrl: '', creationDate: '1850', hasImage: false, artists: [] });
const montees: { unmount(): void }[] = [];

async function afficher(oeuvres: string[] = [id(1), id(2), id(3)]) {
  const r = creerCollection('Impressionnisme', 'Lumière et couleur', oeuvres);
  if (!r.ok) throw new Error('échec');
  route.current = { params: { id: r.valeur.id } };
  const vue = mount(CollectionView, { global: { stubs: { RouterLink: RouterLinkStub } } });
  montees.push(vue);
  await flushPromises();
  return { vue, collection: r.valeur };
}
const titres = (vue: Awaited<ReturnType<typeof afficher>>['vue']) => vue.findAll('[data-testid="artwork-title"]').map(t => t.text());

describe('CollectionView', () => {
  beforeEach(() => {
    window.localStorage.clear();
    chargerCollections();
    oublierProblemeCollections();
    getArtwork.mockReset();
    push.mockReset();
    getArtwork.mockImplementation(async (identifiant: string) => detail(Number(identifiant.slice(-12))));
  });
  afterEach(() => {
    montees.splice(0).forEach(v => v.unmount());
    vi.restoreAllMocks();
  });

  it('montre le nom, la description, le nombre et les œuvres dans l’ordre choisi', async () => {
    const { vue } = await afficher();

    expect(vue.get('[data-testid="collection-titre"]').text()).toBe('Impressionnisme');
    expect(vue.get('[data-testid="collection-texte"]').text()).toBe('Lumière et couleur');
    expect(vue.get('[data-testid="collection-compte"]').text()).toBe('3 œuvres');
    expect(titres(vue)).toEqual(['Œuvre 1', 'Œuvre 2', 'Œuvre 3']);
  });

  it('dit quand la collection est introuvable', async () => {
    route.current = { params: { id: 'inconnue' } };
    const vue = mount(CollectionView, { global: { stubs: { RouterLink: RouterLinkStub } } });
    montees.push(vue);

    expect(vue.get('[data-testid="collection-introuvable"]').text()).toContain("n'existe pas");
  });

  it('dit quand la collection est vide', async () => {
    const { vue } = await afficher([]);

    expect(vue.find('[data-testid="collection-vide"]').exists()).toBe(true);
    expect(vue.get('[data-testid="collection-compte"]').text()).toBe('0 œuvre');
  });

  it('descend et monte une œuvre, et désactive les boutons aux extrémités', async () => {
    const { vue, collection } = await afficher();
    const monter = () => vue.findAll('[data-testid="oeuvre-monter"]');
    const descendre = () => vue.findAll('[data-testid="oeuvre-descendre"]');
    expect(monter()[0].attributes('disabled')).toBeDefined();
    expect(descendre()[2].attributes('disabled')).toBeDefined();

    await descendre()[0].trigger('click');
    expect(trouverCollection(collection.id)!.oeuvres).toEqual([id(2), id(1), id(3)]);
    expect(titres(vue)).toEqual(['Œuvre 2', 'Œuvre 1', 'Œuvre 3']);

    await monter()[2].trigger('click');
    expect(trouverCollection(collection.id)!.oeuvres).toEqual([id(2), id(3), id(1)]);
  });

  it('retire une œuvre', async () => {
    const { vue, collection } = await afficher();

    await vue.findAll('[data-testid="oeuvre-retirer"]')[1].trigger('click');

    expect(trouverCollection(collection.id)!.oeuvres).toEqual([id(1), id(3)]);
    expect(vue.get('[data-testid="collection-compte"]').text()).toBe('2 œuvres');
  });

  it('retire une œuvre retirée de la base, le reste s’affichant normalement', async () => {
    getArtwork.mockImplementation(async (identifiant: string) => {
      if (identifiant === id(2)) throw { response: { status: 404 } };
      return detail(Number(identifiant.slice(-12)));
    });
    const { vue, collection } = await afficher();
    expect(titres(vue)).toEqual(['Œuvre 1', 'Œuvre 3']);

    await vue.get('[data-testid="oeuvre-retiree"] button').trigger('click');

    expect(trouverCollection(collection.id)!.oeuvres).toEqual([id(1), id(3)]);
  });

  it('renomme la collection, refuse un nom vide, et annule', async () => {
    const { vue } = await afficher();
    await vue.get('[data-testid="collection-modifier"]').trigger('click');
    expect((vue.get('[data-testid="collection-edition-nom"]').element as HTMLInputElement).value).toBe('Impressionnisme');

    await vue.get('[data-testid="collection-edition-nom"]').setValue('');
    await vue.get('[data-testid="collection-edition"]').trigger('submit');
    expect(vue.get('[data-testid="collection-erreur"]').text()).toContain('Donnez un nom');

    await vue.get('[data-testid="collection-edition-nom"]').setValue('Post-impressionnisme');
    await vue.get('[data-testid="collection-edition-description"]').setValue('Cézanne');
    await vue.get('[data-testid="collection-edition"]').trigger('submit');

    expect(vue.get('[data-testid="collection-titre"]').text()).toBe('Post-impressionnisme');
    expect(vue.get('[data-testid="collection-texte"]').text()).toBe('Cézanne');

    await vue.get('[data-testid="collection-modifier"]').trigger('click');
    await vue.findAll('[data-testid="collection-edition"] button')[1].trigger('click');
    expect(vue.find('[data-testid="collection-edition"]').exists()).toBe(false);
  });

  it('demande confirmation avant de supprimer, puis retourne à la liste', async () => {
    const { vue, collection } = await afficher();

    await vue.get('[data-testid="collection-supprimer"]').trigger('click');
    await vue.get('[data-testid="collection-annuler"]').trigger('click');
    expect(trouverCollection(collection.id)).toBeDefined();

    await vue.get('[data-testid="collection-supprimer"]').trigger('click');
    await vue.get('[data-testid="collection-confirmer"]').trigger('click');

    expect(collections.collections).toHaveLength(0);
    expect(push).toHaveBeenCalledWith({ name: 'collections' });
  });

  it('propose un lien de partage qui contient la collection', async () => {
    const { vue } = await afficher();

    const adresse = vue.getComponent({ name: 'ShareLink' }).props('shareUrl');

    expect(adresse).toContain('/partage#');
    expect(decoderPartage(adresse.split('#')[1])).toEqual({
      nom: 'Impressionnisme', description: 'Lumière et couleur', oeuvres: [id(1), id(2), id(3)]
    });
  });

  it("propose l'export à la place du lien au-delà de 100 œuvres", async () => {
    const { vue } = await afficher(Array.from({ length: 101 }, (_, i) => id(i + 1)));

    expect(vue.find('[data-testid="share-copy"]').exists()).toBe(false);
    expect(vue.get('[data-testid="collection-trop-grande"]').text()).toContain('Exportez-la');
  });

  it('explique un stockage refusé lors d’une modification', async () => {
    const { vue } = await afficher();
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('plein'); });

    await vue.findAll('[data-testid="oeuvre-retirer"]')[0].trigger('click');

    expect(vue.get('[data-testid="collection-erreur"]').text()).toContain('ne peuvent pas être gardées');
    expect(vue.get('[data-testid="collection-compte"]').text()).toBe('3 œuvres');
  });
});
