import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import AddToCollection from '@/components/AddToCollection.vue';
import {
  chargerCollections, collections, creerCollection, LIMITE_COLLECTIONS, oublierProblemeCollections, supprimerCollection,
  trouverCollection
} from '@/utils/collections';

const id = (n: number) => `00000000-0000-0000-0000-${String(n).padStart(12, '0')}`;
const montees: { unmount(): void }[] = [];

function afficher(oeuvre = id(1)) {
  const vue = mount(AddToCollection, { props: { id: oeuvre } });
  montees.push(vue);
  return vue;
}
const ouvrir = async (vue: ReturnType<typeof afficher>) => vue.get('[data-testid="add-to-collection-toggle"]').trigger('click');
const statut = (vue: ReturnType<typeof afficher>) => vue.get('[data-testid="add-to-collection-status"]').text();

describe('AddToCollection', () => {
  beforeEach(() => {
    window.localStorage.clear();
    chargerCollections();
    oublierProblemeCollections();
  });
  afterEach(() => {
    montees.splice(0).forEach(v => v.unmount());
    vi.restoreAllMocks();
  });

  it('ouvre et referme le panneau', async () => {
    const vue = afficher();
    expect(vue.find('[data-testid="add-to-collection-panel"]').exists()).toBe(false);

    await ouvrir(vue);
    expect(vue.get('[data-testid="add-to-collection-toggle"]').attributes('aria-expanded')).toBe('true');
    expect(vue.find('[data-testid="add-to-collection-panel"]').exists()).toBe(true);

    await ouvrir(vue);
    expect(vue.find('[data-testid="add-to-collection-panel"]').exists()).toBe(false);
  });

  it("crée une collection au passage quand il n'y en a aucune", async () => {
    const vue = afficher();
    await ouvrir(vue);
    expect(vue.find('[data-testid="add-to-collection-select"]').exists()).toBe(false);

    await vue.get('[data-testid="add-to-collection-name"]').setValue('Mes paysages');
    await vue.get('[data-testid="add-to-collection-submit"]').trigger('submit');

    expect(statut(vue)).toContain('Collection « Mes paysages » créée');
    expect(collections.collections[0].oeuvres).toEqual([id(1)]);
  });

  it('refuse un nom vide pour une nouvelle collection', async () => {
    const vue = afficher();
    await ouvrir(vue);

    await vue.get('[data-testid="add-to-collection-submit"]').trigger('submit');

    expect(statut(vue)).toContain('Donnez un nom');
    expect(collections.collections).toHaveLength(0);
  });

  it('ajoute à une collection existante, signale un doublon et indique les collections qui la contiennent', async () => {
    const r = creerCollection('Portraits');
    if (!r.ok) throw new Error('échec');
    const vue = afficher();
    await ouvrir(vue);

    await vue.get('[data-testid="add-to-collection-submit"]').trigger('submit');
    expect(statut(vue)).toBe('Œuvre ajoutée à la collection « Portraits ».');
    expect(trouverCollection(r.valeur.id)!.oeuvres).toEqual([id(1)]);

    await vue.get('[data-testid="add-to-collection-submit"]').trigger('submit');
    expect(statut(vue)).toBe('Cette œuvre est déjà dans la collection « Portraits ».');
    expect(vue.get('[data-testid="add-to-collection-select"]').text()).toContain('déjà ajoutée');
  });

  it('permet de choisir « Nouvelle collection » parmi les existantes', async () => {
    creerCollection('Portraits');
    const vue = afficher();
    await ouvrir(vue);

    await vue.get('[data-testid="add-to-collection-select"]').setValue('__nouvelle__');
    await vue.get('[data-testid="add-to-collection-name"]').setValue('Autre');
    await vue.get('[data-testid="add-to-collection-submit"]').trigger('submit');

    expect(collections.collections.map(c => c.nom)).toEqual(['Portraits', 'Autre']);
  });

  it('explique la limite de collections et un stockage refusé', async () => {
    for (let n = 0; n < LIMITE_COLLECTIONS; n++) creerCollection(`C${n}`);
    const vue = afficher();
    await ouvrir(vue);
    await vue.get('[data-testid="add-to-collection-select"]').setValue('__nouvelle__');
    await vue.get('[data-testid="add-to-collection-name"]').setValue('De trop');
    await vue.get('[data-testid="add-to-collection-submit"]').trigger('submit');
    expect(statut(vue)).toContain('limite de 50 collections');

    await vue.get('[data-testid="add-to-collection-select"]').setValue(collections.collections[0].id);
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('plein'); });
    await vue.get('[data-testid="add-to-collection-submit"]').trigger('submit');
    expect(statut(vue)).toContain('ne peuvent pas être gardées');
  });

  it('revient à « nouvelle collection » quand la collection choisie disparaît', async () => {
    const r = creerCollection('Éphémère');
    if (!r.ok) throw new Error('échec');
    const vue = afficher();
    await ouvrir(vue);

    supprimerCollection(r.valeur.id);
    await vue.vm.$nextTick();

    expect(vue.find('[data-testid="add-to-collection-select"]').exists()).toBe(false);
    expect(vue.find('[data-testid="add-to-collection-name"]').exists()).toBe(true);
  });

  it("choisit une autre collection quand celle qui était choisie disparaît", async () => {
    const a = creerCollection('A');
    creerCollection('B');
    if (!a.ok) throw new Error('échec');
    const vue = afficher();
    await ouvrir(vue);

    supprimerCollection(a.valeur.id);
    await vue.vm.$nextTick();

    expect((vue.get('[data-testid="add-to-collection-select"]').element as HTMLSelectElement).selectedOptions[0].text).toContain('B');
  });
});
