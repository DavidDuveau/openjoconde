import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils';

const { getArtwork, route } = vi.hoisted(() => ({ getArtwork: vi.fn(), route: { current: { hash: '' } } }));
vi.mock('@/services/api', () => ({ default: { getArtwork } }));
vi.mock('vue-router', () => ({ useRoute: () => route.current, useRouter: () => ({ push: vi.fn() }) }));

import CollectionPartageeView from '@/views/CollectionPartageeView.vue';
import { chargerCollections, collections, LIMITE_COLLECTIONS, creerCollection, oublierProblemeCollections } from '@/utils/collections';
import { encoderPartage } from '@/utils/partage';

const id = (n: number) => `00000000-0000-4000-8000-${String(n).padStart(12, '0')}`;
const detail = (n: number) => ({ id: id(n), title: `Œuvre ${n}`, imageUrl: '', creationDate: '1850', hasImage: false, artists: [] });
const montees: { unmount(): void }[] = [];

async function afficher(hash: string) {
  route.current = { hash };
  const vue = mount(CollectionPartageeView, { global: { stubs: { RouterLink: RouterLinkStub } } });
  montees.push(vue);
  await flushPromises();
  return vue;
}
const lien = (nom = 'Nature', description = 'Arbres', oeuvres = [id(1), id(2)]) => `#${encoderPartage({ nom, description, oeuvres })}`;

describe('CollectionPartageeView', () => {
  beforeEach(() => {
    window.localStorage.clear();
    chargerCollections();
    oublierProblemeCollections();
    getArtwork.mockReset();
    getArtwork.mockImplementation(async (identifiant: string) => detail(Number(identifiant.slice(-12))));
  });
  afterEach(() => {
    montees.splice(0).forEach(v => v.unmount());
    vi.restoreAllMocks();
  });

  it('montre la collection du lien en lecture seule, sans retirer ni déplacer', async () => {
    const vue = await afficher(lien());

    expect(vue.get('[data-testid="partage-bandeau"]').text()).toContain('lecture seule');
    expect(vue.get('[data-testid="partage-titre"]').text()).toBe('Nature');
    expect(vue.get('[data-testid="partage-texte"]').text()).toBe('Arbres');
    expect(vue.get('[data-testid="partage-compte"]').text()).toBe('2 œuvres');
    expect(vue.findAll('[data-testid="artwork-title"]').map(t => t.text())).toEqual(['Œuvre 1', 'Œuvre 2']);
    expect(vue.find('[data-testid="oeuvre-retirer"]').exists()).toBe(false);
  });

  it("ne propose pas de supprimer une œuvre retirée de la base", async () => {
    getArtwork.mockRejectedValue({ response: { status: 404 } });

    const vue = await afficher(lien());

    expect(vue.findAll('[data-testid="oeuvre-retiree"]')).toHaveLength(2);
    expect(vue.find('[data-testid="oeuvre-retiree"] button').exists()).toBe(false);
  });

  it('affiche le texte du lien échappé, sans l’interpréter', async () => {
    const vue = await afficher(lien('<img src=x onerror=alert(1)>', '<b>gras</b>'));

    expect(vue.get('[data-testid="partage-titre"]').text()).toBe('<img src=x onerror=alert(1)>');
    expect(vue.find('[data-testid="partage-titre"] img').exists()).toBe(false);
    expect(vue.find('[data-testid="partage-texte"] b').exists()).toBe(false);
  });

  it('accorde le nombre d’œuvres et dit quand la collection est vide', async () => {
    const vue = await afficher(lien('Vide', '', []));

    expect(vue.get('[data-testid="partage-compte"]').text()).toBe('0 œuvre');
    expect(vue.text()).toContain('Cette collection est vide');
    expect(vue.find('[data-testid="partage-texte"]').exists()).toBe(false);
  });

  it.each(['', '#', '#<script>', `#${'A'.repeat(9000)}`, '#AAAA'])('affiche une erreur claire et rien d’autre pour le lien %#', async (hash) => {
    const vue = await afficher(hash);

    expect(vue.find('[data-testid="partage-erreur"]').exists()).toBe(true);
    expect(vue.find('[data-testid="partage-titre"]').exists()).toBe(false);
  });

  it('ajoute la collection aux collections de l’utilisateur', async () => {
    const vue = await afficher(lien());

    await vue.get('[data-testid="partage-ajouter"]').trigger('click');

    expect(vue.get('[data-testid="partage-statut"]').text()).toContain('Collection ajoutée');
    expect(collections.collections[0]).toMatchObject({ nom: 'Nature', description: 'Arbres', oeuvres: [id(1), id(2)] });
    expect(vue.getComponent('[data-testid="partage-voir"]').props('to')).toEqual({ name: 'collection', params: { id: collections.collections[0].id } });
  });

  it("explique pourquoi l'ajout est refusé", async () => {
    for (let n = 0; n < LIMITE_COLLECTIONS; n++) creerCollection(`C${n}`);
    const vue = await afficher(lien());

    await vue.get('[data-testid="partage-ajouter"]').trigger('click');

    expect(vue.get('[data-testid="partage-statut"]').text()).toContain('limite de 50 collections');
    expect(vue.find('[data-testid="partage-voir"]').exists()).toBe(false);
  });
});
