import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils';

const { getArtwork } = vi.hoisted(() => ({ getArtwork: vi.fn() }));
vi.mock('@/services/api', () => ({ default: { getArtwork } }));
vi.mock('vue-router', () => ({ useRoute: () => ({ params: { id: 'a1' } }) }));

import ArtworkDetailView from '@/views/ArtworkDetailView.vue';

const oeuvre = {
  id: 'a1', title: 'Les Baigneuses', reference: '00000001', inventoryNumber: null, denomination: 'tableau',
  imageUrl: null, description: null, dimensions: null, creationDate: null, creationPlace: null,
  conservationPlace: null, copyright: null, associatedSiteUrl: null, artistUnderCopyright: null,
  publicDomainYear: null, hasImage: null, museum: null, artists: [], domains: [], techniques: [], periods: []
};

function monter() {
  return mount(ArtworkDetailView, { global: { stubs: { RouterLink: RouterLinkStub } } });
}

async function afficher(surcharge: object = {}) {
  getArtwork.mockResolvedValue({ ...oeuvre, ...surcharge });
  const vue = monter();
  await flushPromises();
  return vue;
}

describe('ArtworkDetailView', () => {
  beforeEach(() => {
    getArtwork.mockReset();
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  it("demande à l'API l'œuvre de l'adresse et affiche son titre et sa référence", async () => {
    const vue = await afficher();

    expect(getArtwork).toHaveBeenCalledWith('a1');
    expect(vue.get('[data-testid="artwork-heading"]').text()).toBe('Les Baigneuses');
    expect(vue.get('[data-testid="artwork-reference"]').text()).toBe('00000001');
  });

  it("dit quand l'œuvre entrera dans le domaine public", async () => {
    const vue = await afficher({ artistUnderCopyright: true, publicDomainYear: 2080 });

    expect(vue.get('[data-testid="artwork-rights"]').text())
      .toBe("Artiste sous droits d'auteur ; l'œuvre entrera dans le domaine public en 2080.");
  });

  it("n'affiche aucune mention de droits quand la notice n'en dit rien", async () => {
    const vue = await afficher();

    expect(vue.find('[data-testid="artwork-rights"]').exists()).toBe(false);
  });

  it("renvoie vers la notice POP, en promettant une image seulement si la notice l'annonce", async () => {
    const avec = await afficher({ hasImage: true });
    expect(avec.get('[data-testid="pop-link"]').text()).toBe("Voir l'image sur POP");
    expect(avec.get('[data-testid="pop-link"]').attributes('href')).toContain('/notice/joconde/00000001');

    const sans = await afficher({ hasImage: false });
    expect(sans.get('[data-testid="pop-link"]').text()).toBe('Voir la notice sur POP');
  });

  it('propose le site du musée pour une adresse web, jamais pour un autre schéma', async () => {
    const sain = await afficher({ associatedSiteUrl: 'https://example.org/musee' });
    expect(sain.get('[data-testid="museum-site-link"]').attributes('href')).toBe('https://example.org/musee');
    expect(sain.get('[data-testid="museum-site-link"]').attributes('rel')).toContain('noopener');

    const piege = await afficher({ associatedSiteUrl: 'javascript:alert(1)' });
    expect(piege.find('[data-testid="museum-site-link"]').exists()).toBe(false);
  });

  it("signale une panne de l'API et propose de réessayer", async () => {
    getArtwork.mockRejectedValue({ response: { status: 500 } });
    const vue = monter();
    await flushPromises();

    expect(vue.text()).toContain('Une erreur est survenue');

    getArtwork.mockResolvedValue(oeuvre);
    await vue.get('button').trigger('click');
    await flushPromises();

    expect(vue.get('[data-testid="artwork-heading"]').text()).toBe('Les Baigneuses');
  });

  it("dit qu'une œuvre inconnue n'existe pas, sans parler de panne", async () => {
    getArtwork.mockRejectedValue({ response: { status: 404 } });
    const vue = monter();
    await flushPromises();

    expect(vue.text()).toContain("n'existe pas");
    expect(vue.text()).not.toContain('Une erreur est survenue');
  });

  it('renvoie chaque période vers la recherche des œuvres de cette période', async () => {
    const vue = await afficher({ periods: [{ id: 'p1', name: '19e siècle' }, { id: 'p2', name: '20e siècle' }] });

    const liens = vue.findAllComponents(RouterLinkStub).filter((l) => l.attributes('data-testid') === 'period-tag');

    expect(liens.map((l) => l.text())).toEqual(['19e siècle', '20e siècle']);
    expect(liens[0].props('to')).toEqual({ name: 'search', query: { periodId: 'p1', periodName: '19e siècle' } });
  });
});
