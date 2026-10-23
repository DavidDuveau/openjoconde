import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils';

const { getArtist } = vi.hoisted(() => ({ getArtist: vi.fn() }));
vi.mock('@/services/api', () => ({ default: { getArtist } }));

import ArtworkContext from '@/components/ArtworkContext.vue';

const oeuvre = (surcharge: object = {}) => ({
  id: 'a1', title: 'Les Baigneuses', reference: 'R1', inventoryNumber: null, denomination: null, imageUrl: null,
  description: null, dimensions: null, creationDate: null, creationPlace: null, conservationPlace: null, copyright: null,
  associatedSiteUrl: null, artistUnderCopyright: null, publicDomainYear: null, hasImage: null, museum: null,
  artists: [], domains: [], techniques: [], periods: [], ...surcharge
});
const avecArtiste = oeuvre({
  denomination: 'peinture',
  artists: [{ id: 'p1', firstName: 'Claude', lastName: 'Monet', role: null }],
  techniques: [{ id: 't1', name: 'huile sur toile' }],
  periods: [{ id: 'e1', name: '19e siècle' }]
});
const autreOeuvre = {
  ...avecArtiste, id: 'a2', artists: [{ id: 'p2', firstName: 'Auguste', lastName: 'Renoir', role: null }]
};
const monet = (surcharge: object = {}) => ({
  id: 'p1', firstName: 'Claude', lastName: 'Monet', birthDate: '1840', deathDate: '1926', nationality: 'France',
  biography: 'Claude Monet est un peintre français.', movement: 'impressionnisme',
  wikipediaUrl: 'https://fr.wikipedia.org/wiki/Claude_Monet', artworksCount: 1, ...surcharge
});
const renoir = monet({ id: 'p2', lastName: 'Renoir', firstName: 'Auguste' });

async function afficher(artwork: object) {
  const vue = mount(ArtworkContext, { props: { artwork } as never, global: { stubs: { RouterLink: RouterLinkStub } } });
  await flushPromises();
  return vue;
}

describe('ArtworkContext', () => {
  beforeEach(() => {
    getArtist.mockReset();
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  it("montre l'artiste : dates, nationalité, courant, biographie sourcée et lien vers sa page", async () => {
    getArtist.mockResolvedValue(monet());

    const vue = await afficher(avecArtiste);

    expect(getArtist).toHaveBeenCalledWith('p1');
    expect(vue.get('[data-testid="context-facts"]').text()).toBe('1840 - 1926 · France · impressionnisme');
    expect(vue.get('[data-testid="context-biography"]').text()).toContain('peintre français');
    expect(vue.get('[data-testid="context-source"] a').attributes('href')).toBe('https://fr.wikipedia.org/wiki/Claude_Monet');
    expect(vue.get('[data-testid="context-source"]').text()).toContain('CC BY-SA 4.0');
    expect(vue.get('[data-testid="context-artist-link"]').text()).toBe('Claude Monet');
    expect(vue.getComponent('[data-testid="context-artist-link"]').props('to')).toEqual({ name: 'artist-detail', params: { id: 'p1' } });
    expect(vue.get('.context-more').text()).toContain('1 œuvre dans la base');
  });

  it("explique les termes techniques de l'œuvre par des infobulles", async () => {
    getArtist.mockResolvedValue(monet());

    const vue = await afficher(avecArtiste);

    expect(vue.findAll('[data-testid="glossary-term"]').map(t => t.text())).toEqual(['peinture', 'huile sur toile']);
    expect(vue.findAll('[data-testid="context-term"]').map(t => t.text())).toEqual(['peinture', 'huile sur toile', '19e siècle']);
  });

  it("n'invente ni biographie ni source quand l'artiste n'est pas enrichi, et accorde « œuvres »", async () => {
    getArtist.mockResolvedValue(monet({ biography: null, wikipediaUrl: null, movement: null, nationality: null, deathDate: null, artworksCount: 3 }));

    const vue = await afficher(avecArtiste);

    expect(vue.get('[data-testid="context-facts"]').text()).toBe('né(e) en 1840');
    expect(vue.find('[data-testid="context-biography"]').exists()).toBe(false);
    expect(vue.find('[data-testid="context-source"]').exists()).toBe(false);
    expect(vue.get('.context-more').text()).toContain('3 œuvres');
  });

  it("n'affiche pas de ligne de faits quand l'artiste n'en a aucun", async () => {
    getArtist.mockResolvedValue(monet({ birthDate: null, deathDate: null, nationality: null, movement: null }));

    const vue = await afficher(avecArtiste);

    expect(vue.find('[data-testid="context-facts"]').exists()).toBe(false);
  });

  it("garde les termes techniques quand la fiche de l'artiste est en panne", async () => {
    getArtist.mockRejectedValue(new Error('panne'));

    const vue = await afficher(avecArtiste);

    expect(vue.find('[data-testid="context-artist"]').exists()).toBe(false);
    expect(vue.findAll('[data-testid="context-term"]').length).toBeGreaterThan(0);
  });

  it("n'appelle pas l'API et reste absent pour une œuvre sans artiste ni terme", async () => {
    const vue = await afficher(oeuvre());

    expect(getArtist).not.toHaveBeenCalled();
    expect(vue.find('[data-testid="artwork-context"]').exists()).toBe(false);
  });

  it("recharge l'artiste quand l'œuvre change", async () => {
    getArtist.mockResolvedValueOnce(monet()).mockResolvedValueOnce(renoir);
    const vue = await afficher(avecArtiste);

    await vue.setProps({ artwork: autreOeuvre });
    await flushPromises();

    expect(vue.get('[data-testid="context-artist-link"]').text()).toBe('Auguste Renoir');
  });

  it("ignore la réponse tardive d'une œuvre déjà quittée", async () => {
    let tard!: (a: unknown) => void;
    getArtist.mockReturnValueOnce(new Promise(r => { tard = r; })).mockResolvedValueOnce(renoir);
    const vue = mount(ArtworkContext, { props: { artwork: avecArtiste } as never, global: { stubs: { RouterLink: RouterLinkStub } } });

    await vue.setProps({ artwork: autreOeuvre });
    await flushPromises();
    tard(monet());
    await flushPromises();

    expect(vue.get('[data-testid="context-artist-link"]').text()).toBe('Auguste Renoir');
  });
});
