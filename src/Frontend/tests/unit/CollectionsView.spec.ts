import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils';
import CollectionsView from '@/views/CollectionsView.vue';
import { ajouterFavori, chargerFavoris, estFavori } from '@/utils/favoris';
import { chargerCollections, collections, creerCollection, oublierProblemeCollections } from '@/utils/collections';
import { sauvegardeJson } from '@/utils/sauvegarde';
import * as exportOeuvre from '@/utils/exportOeuvre';

const id = (n: number) => `00000000-0000-0000-0000-${String(n).padStart(12, '0')}`;
const montees: { unmount(): void }[] = [];

function afficher() {
  const vue = mount(CollectionsView, { global: { stubs: { RouterLink: RouterLinkStub } } });
  montees.push(vue);
  return vue;
}

async function choisir(vue: ReturnType<typeof afficher>, contenu: string, taille?: number) {
  const fichier = new File([contenu], 'sauvegarde.json', { type: 'application/json' });
  if (taille !== undefined) Object.defineProperty(fichier, 'size', { value: taille });
  const champ = vue.get('[data-testid="sauvegarde-fichier"]');
  Object.defineProperty(champ.element, 'files', { value: [fichier], configurable: true });
  await champ.trigger('change');
  await vi.waitFor(() => expect(vue.find('[data-testid="sauvegarde-importer"], [data-testid="sauvegarde-erreur"]').exists()).toBe(true));
}

describe('CollectionsView', () => {
  beforeEach(() => {
    window.localStorage.clear();
    chargerFavoris();
    chargerCollections();
    oublierProblemeCollections();
  });
  afterEach(() => {
    montees.splice(0).forEach(v => v.unmount());
    vi.restoreAllMocks();
  });

  it('dit que les collections restent sur cet appareil et invite à en créer une', () => {
    const vue = afficher();

    expect(vue.get('[data-testid="collections-note"]').text()).toContain('restent sur cet appareil');
    expect(vue.find('[data-testid="collections-vide"]').exists()).toBe(true);
  });

  it('crée une collection avec son nombre d’œuvres, et refuse un nom vide ou trop long', async () => {
    const vue = afficher();

    await vue.get('[data-testid="collection-form"]').trigger('submit');
    expect(vue.get('[data-testid="collection-erreur"]').text()).toContain('Donnez un nom');

    await vue.get('[data-testid="collection-nom"]').setValue('x'.repeat(81));
    await vue.get('[data-testid="collection-form"]').trigger('submit');
    expect(vue.get('[data-testid="collection-erreur"]').text()).toContain('80 caractères');

    await vue.get('[data-testid="collection-nom"]').setValue('Nature');
    await vue.get('[data-testid="collection-description"]').setValue('Arbres et rivières');
    await vue.get('[data-testid="collection-form"]').trigger('submit');

    expect(vue.find('[data-testid="collection-erreur"]').exists()).toBe(false);
    const ligne = vue.get('[data-testid="collection-item"]');
    expect(ligne.text()).toContain('Nature');
    expect(ligne.text()).toContain('0 œuvre');
    expect(ligne.text()).toContain('Arbres et rivières');
    expect((vue.get('[data-testid="collection-nom"]').element as HTMLInputElement).value).toBe('');
  });

  it('accorde le nombre d’œuvres', () => {
    creerCollection('Deux', '', [id(1), id(2)]);

    expect(afficher().get('[data-testid="collection-item"]').text()).toContain('2 œuvres');
  });

  it('signale un stockage indisponible', async () => {
    const vue = afficher();
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('plein'); });
    await vue.get('[data-testid="collection-nom"]').setValue('X');

    await vue.get('[data-testid="collection-form"]').trigger('submit');

    expect(vue.get('[data-testid="collections-probleme"]').text()).toContain('ne peuvent pas être gardées');
  });

  it('exporte un fichier de sauvegarde', async () => {
    const telecharger = vi.spyOn(exportOeuvre, 'telecharger').mockImplementation(() => undefined);
    ajouterFavori(id(1));
    const vue = afficher();

    await vue.get('[data-testid="sauvegarde-export"]').trigger('click');

    expect(telecharger).toHaveBeenCalledWith(expect.stringMatching(/^openjoconde-sauvegarde-\d{4}-\d{2}-\d{2}\.json$/), expect.any(String), 'application/json');
    expect(JSON.parse(telecharger.mock.calls[0][1]).favoris).toHaveLength(1);
  });

  it('importe en fusionnant et donne un compte rendu', async () => {
    ajouterFavori(id(1));
    creerCollection('A', '', [id(2)]);
    const export1 = sauvegardeJson();
    window.localStorage.clear();
    chargerFavoris();
    chargerCollections();
    ajouterFavori(id(9));
    const vue = afficher();

    await choisir(vue, export1);
    await vue.get('[data-testid="sauvegarde-importer"]').trigger('click');

    expect(vue.get('[data-testid="sauvegarde-compte-rendu"]').text()).toContain('1 favori ajouté, 1 collection ajoutée');
    expect(estFavori(id(1))).toBe(true);
    expect(estFavori(id(9))).toBe(true);
    expect(collections.collections).toHaveLength(1);
    expect(vue.find('[data-testid="sauvegarde-importer"]').exists()).toBe(false);
  });

  it('importe en remplaçant', async () => {
    ajouterFavori(id(9));
    const contenu = JSON.stringify({ format: 'openjoconde-sauvegarde', version: 1, favoris: [{ id: id(1) }], collections: [] });
    const vue = afficher();

    await choisir(vue, contenu);
    await vue.get('[data-testid="mode-remplacer"]').setValue(true);
    await vue.get('[data-testid="sauvegarde-importer"]').trigger('click');

    expect(estFavori(id(9))).toBe(false);
    expect(estFavori(id(1))).toBe(true);
  });

  it('refuse un fichier invalide en le disant et sans rien modifier', async () => {
    ajouterFavori(id(9));
    const vue = afficher();

    await choisir(vue, 'pas du json');
    await vue.get('[data-testid="sauvegarde-importer"]').trigger('click');

    expect(vue.get('[data-testid="sauvegarde-erreur"]').text()).toContain("Vos données n'ont pas été modifiées");
    expect(estFavori(id(9))).toBe(true);
  });

  it('refuse un fichier trop gros sans le lire', async () => {
    const vue = afficher();

    await choisir(vue, '{}', 2_000_000);

    expect(vue.get('[data-testid="sauvegarde-erreur"]').text()).toContain('trop gros');
    expect(vue.find('[data-testid="sauvegarde-importer"]').exists()).toBe(false);
  });

  it("ne fait rien quand aucun fichier n'est choisi, et signale un fichier illisible", async () => {
    const vue = afficher();
    const champ = vue.get('[data-testid="sauvegarde-fichier"]');
    Object.defineProperty(champ.element, 'files', { value: [], configurable: true });
    await champ.trigger('change');
    expect(vue.find('[data-testid="sauvegarde-importer"]').exists()).toBe(false);

    vi.spyOn(FileReader.prototype, 'readAsText').mockImplementation(function (this: FileReader) {
      setTimeout(() => this.onerror?.(new ProgressEvent('error') as ProgressEvent<FileReader>));
    });
    await choisir(vue, '{}');
    await flushPromises();

    expect(vue.get('[data-testid="sauvegarde-erreur"]').text()).toContain('ne peut pas être lu');
  });
});
