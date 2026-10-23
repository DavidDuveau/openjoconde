import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils';

const { searchArtworks, push } = vi.hoisted(() => ({ searchArtworks: vi.fn(), push: vi.fn() }));
vi.mock('@/services/api', () => ({ default: { searchArtworks } }));
vi.mock('vue-router', () => ({ useRouter: () => ({ push }) }));

import SimilarArtworks from '@/components/SimilarArtworks.vue';

const oeuvre = (surcharge: object = {}) => ({
  id: 'a1', title: 'Les Baigneuses', reference: 'R1', inventoryNumber: null, denomination: null, imageUrl: null,
  description: null, dimensions: null, creationDate: null, creationPlace: null, conservationPlace: null, copyright: null,
  associatedSiteUrl: null, artistUnderCopyright: null, publicDomainYear: null, hasImage: null, museum: null,
  artists: [], domains: [], techniques: [], periods: [], ...surcharge
});
const complete = oeuvre({
  artists: [{ id: 'p1', firstName: 'Claude', lastName: 'Monet', role: null }],
  periods: [{ id: 'e1', name: '19e siècle' }],
  techniques: [{ id: 't1', name: 'huile' }],
  museum: { id: 'm1', name: "Musée d'Orsay" }
});
const voisine = (id: string) => ({ id, title: `Titre ${id}`, imageUrl: '', creationDate: '1870', artists: [] });
const page = (...ids: string[]) => ({ items: ids.map(voisine), totalItems: ids.length, page: 1, pageSize: 7, totalPages: 1 });

async function afficher(artwork: object) {
  const vue = mount(SimilarArtworks, { props: { artwork } as never, global: { stubs: { RouterLink: RouterLinkStub } } });
  await flushPromises();
  return vue;
}

describe('SimilarArtworks', () => {
  beforeEach(() => {
    searchArtworks.mockReset();
    push.mockReset();
  });

  it('interroge un critère par information de la notice, du plus proche au plus large', async () => {
    searchArtworks.mockResolvedValue(page('x1'));

    await afficher(complete);

    expect(searchArtworks.mock.calls.map(([params]) => Object.keys(params).find(k => k.endsWith('Id'))))
      .toEqual(['artistId', 'periodId', 'techniqueId', 'museumId']);
    expect(searchArtworks).toHaveBeenCalledWith(expect.objectContaining({ artistId: 'p1', pageSize: 7 }));
  });

  it('dit sur quel critère chaque groupe repose', async () => {
    searchArtworks.mockResolvedValue(page('x1'));

    const vue = await afficher(complete);

    expect(vue.findAll('[data-testid="similar-criterion"]').map(c => c.text())).toEqual([
      'Même artiste : Claude Monet', 'Même période : 19e siècle', 'Même technique : huile', "Même musée : Musée d'Orsay"
    ]);
  });

  it("n'interroge que les critères renseignés", async () => {
    searchArtworks.mockResolvedValue(page('x1'));

    await afficher(oeuvre({ periods: [{ id: 'e1', name: '19e siècle' }] }));

    expect(searchArtworks).toHaveBeenCalledTimes(1);
  });

  it("écarte l'œuvre consultée et limite chaque groupe à six œuvres", async () => {
    searchArtworks.mockResolvedValue(page('a1', 'x1', 'x2', 'x3', 'x4', 'x5', 'x6', 'x7'));

    const vue = await afficher(oeuvre({ periods: [{ id: 'e1', name: '19e siècle' }] }));

    const titres = vue.findAll('[data-testid="artwork-title"]').map(t => t.text());
    expect(titres).toEqual(['Titre x1', 'Titre x2', 'Titre x3', 'Titre x4', 'Titre x5', 'Titre x6']);
  });

  it("omet un groupe sans autre œuvre et un critère en panne, sans priver des autres", async () => {
    searchArtworks
      .mockResolvedValueOnce(page('a1'))
      .mockRejectedValueOnce(new Error('panne'))
      .mockResolvedValueOnce(page('x1'))
      .mockResolvedValueOnce(page());

    const vue = await afficher(complete);

    expect(vue.findAll('[data-testid="similar-criterion"]').map(c => c.text())).toEqual(['Même technique : huile']);
  });

  it("explique qu'aucune œuvre n'est proche, et propose de parcourir les œuvres", async () => {
    const vue = await afficher(oeuvre());

    expect(searchArtworks).not.toHaveBeenCalled();
    expect(vue.get('[data-testid="similar-empty"]').text()).toContain("Aucune œuvre directement similaire");
    expect(vue.getComponent(RouterLinkStub).props('to')).toEqual({ name: 'artworks' });
  });

  it("ouvre la fiche de l'œuvre cliquée par une navigation qui garde l'historique", async () => {
    searchArtworks.mockResolvedValue(page('x1'));
    const vue = await afficher(oeuvre({ periods: [{ id: 'e1', name: '19e siècle' }] }));

    await vue.get('[data-testid="artwork-card"]').trigger('click');

    expect(push).toHaveBeenCalledWith({ name: 'artwork-detail', params: { id: 'x1' } });
  });

  it("annonce la recherche en cours, puis recharge quand l'œuvre change", async () => {
    searchArtworks.mockResolvedValue(page('x1'));
    const vue = mount(SimilarArtworks, {
      props: { artwork: oeuvre({ periods: [{ id: 'e1', name: 'A' }] }) } as never,
      global: { stubs: { RouterLink: RouterLinkStub } }
    });
    expect(vue.find('[data-testid="similar-loading"]').exists()).toBe(true);
    await flushPromises();

    await vue.setProps({ artwork: oeuvre({ id: 'a2', periods: [{ id: 'e2', name: 'B' }] }) } as never);
    await flushPromises();

    expect(searchArtworks).toHaveBeenCalledTimes(2);
    expect(vue.get('[data-testid="similar-criterion"]').text()).toBe('Même période : B');
  });

  it("n'affiche pas la réponse d'une œuvre quittée entre-temps", async () => {
    let repondre: (valeur: unknown) => void = () => undefined;
    searchArtworks
      .mockReturnValueOnce(new Promise(r => { repondre = r; }))
      .mockResolvedValueOnce(page('y1'));
    const vue = mount(SimilarArtworks, {
      props: { artwork: oeuvre({ periods: [{ id: 'e1', name: 'ANCIENNE' }] }) } as never,
      global: { stubs: { RouterLink: RouterLinkStub } }
    });

    await vue.setProps({ artwork: oeuvre({ id: 'a2', periods: [{ id: 'e2', name: 'NOUVELLE' }] }) } as never);
    await flushPromises();
    repondre(page('x1'));
    await flushPromises();

    expect(vue.findAll('[data-testid="similar-criterion"]').map(c => c.text())).toEqual(['Même période : NOUVELLE']);
  });
});
