import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils';

const { searchArtworks, getDomains, push, route } = vi.hoisted(() => ({
  searchArtworks: vi.fn(),
  getDomains: vi.fn(),
  push: vi.fn(),
  route: { current: { params: { slug: 'portraits' } as Record<string, string> } }
}));
vi.mock('@/services/api', () => ({ default: { searchArtworks, getDomains } }));
vi.mock('vue-router', () => ({ useRoute: () => route.current, useRouter: () => ({ push }) }));

import ThematiqueView from '@/views/ThematiqueView.vue';
import { trouverThematique } from '@/utils/thematiques';

const portraits = trouverThematique('portraits')!;
const oeuvre = (id: string) => ({ id, title: `Portrait ${id}`, imageUrl: '', creationDate: '1850', artists: [] });
const page = (ids: string[], totalItems = ids.length) => ({ items: ids.map(oeuvre), totalItems, page: 1, pageSize: 6, totalPages: 1 });
const domaines = (...noms: string[]) => ({ items: noms.map((name, i) => ({ id: `d${i}`, name })), totalItems: noms.length, page: 1, pageSize: 20, totalPages: 1 });

async function afficher() {
  const vue = mount(ThematiqueView, { global: { stubs: { RouterLink: RouterLinkStub } } });
  await flushPromises();
  return vue;
}

describe('ThematiqueView', () => {
  beforeEach(() => {
    searchArtworks.mockReset();
    getDomains.mockReset();
    push.mockReset();
    route.current = { params: { slug: 'portraits' } };
    // Le domaine demandé existe dans la base, sous le nom demandé
    getDomains.mockImplementation(async (nom: string) => domaines(nom));
    searchArtworks.mockResolvedValue(page(['a1', 'a2'], 14));
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });
  afterEach(() => vi.restoreAllMocks());

  it('présente le thème, son importance et ses critères de sélection avant les œuvres', async () => {
    const vue = await afficher();

    expect(vue.get('[data-testid="thematique-heading"]').text()).toBe('Portraits');
    expect(vue.get('[data-testid="thematique-explication"]').text()).toBe(portraits.explication);
    expect(vue.get('[data-testid="thematique-importance"]').text()).toBe(portraits.importance);
    expect(vue.get('[data-testid="thematique-criteres"]').text()).toBe(portraits.criteres);
  });

  it('déroule les étapes dans l\'ordre du récit, chacune avec son texte puis ses œuvres', async () => {
    const vue = await afficher();

    const etapes = vue.findAll('[data-testid="thematique-section"]');
    expect(etapes).toHaveLength(portraits.sections.length);
    portraits.sections.forEach((section, i) => {
      expect(etapes[i].text()).toContain(section.titre);
      expect(etapes[i].text()).toContain(section.texte);
    });
    expect(etapes[0].find('h2').text()).toContain('1');
  });

  it('cherche chaque étape par ses mots et son domaine, les plus pertinents d\'abord, par six', async () => {
    await afficher();

    expect(getDomains).toHaveBeenCalledWith('peinture', 20);
    expect(searchArtworks).toHaveBeenCalledWith({ search: 'portrait', domainId: 'd0', pageSize: 6, sortBy: 'relevance' });
  });

  it('propose de voir toutes les œuvres de l\'étape dans la recherche, avec les mêmes critères', async () => {
    const vue = await afficher();

    const lien = vue.findAllComponents(RouterLinkStub).find(l => (l.props('to') as { name?: string }).name === 'search')!;
    expect(lien.props('to')).toEqual({ name: 'search', query: { searchText: 'portrait', domainId: 'd0', domainName: 'peinture' } });
    expect(vue.findAll('[data-testid="section-voir-tout"]')[0].text()).toContain('14 œuvres');
  });

  it('accorde « œuvre » au singulier', async () => {
    searchArtworks.mockResolvedValue(page(['a1'], 1));

    const vue = await afficher();

    expect(vue.findAll('[data-testid="section-voir-tout"]')[0].text()).toContain('1 œuvre de cette étape');
  });

  it('une étape par domaine seul n\'envoie pas de mots, et trie par titre', async () => {
    route.current = { params: { slug: 'prehistoire-antiquite' } };

    await afficher();

    expect(searchArtworks).toHaveBeenCalledWith({ search: undefined, domainId: 'd0', pageSize: 6, sortBy: undefined });
  });

  it('une étape sans domaine ne demande pas de domaine, et son lien ne porte que les mots', async () => {
    route.current = { params: { slug: 'art-religieux' } };

    const vue = await afficher();

    const lien = vue.findAllComponents(RouterLinkStub).find(l => (l.props('to') as { name?: string }).name === 'search')!;
    expect(lien.props('to')).toEqual({ name: 'search', query: { searchText: 'vierge' } });
    expect(getDomains).not.toHaveBeenCalled();
  });

  it('dit qu\'une étape n\'a aucune notice, sans élargir la recherche, quand son domaine est inconnu de la base', async () => {
    getDomains.mockResolvedValue(domaines());

    const vue = await afficher();

    expect(vue.findAll('[data-testid="section-vide"]')).toHaveLength(portraits.sections.length);
    expect(searchArtworks).not.toHaveBeenCalled();
  });

  it('dit qu\'une étape est vide quand la recherche ne trouve rien', async () => {
    route.current = { params: { slug: 'art-religieux' } };
    searchArtworks.mockResolvedValue(page([]));

    const vue = await afficher();

    expect(vue.findAll('[data-testid="section-vide"]')).toHaveLength(4);
    expect(vue.find('[data-testid="section-voir-tout"]').exists()).toBe(false);
  });

  it('une panne sur une étape n\'empêche pas les autres, et se réessaie', async () => {
    route.current = { params: { slug: 'art-religieux' } };
    searchArtworks.mockRejectedValueOnce(new Error('panne')).mockResolvedValue(page(['a1'], 1));

    const vue = await afficher();

    expect(vue.findAll('[data-testid="section-panne"]')).toHaveLength(1);
    expect(vue.findAll('[data-testid="section-voir-tout"]')).toHaveLength(3);

    await vue.get('.retry').trigger('click');
    await flushPromises();

    expect(vue.find('[data-testid="section-panne"]').exists()).toBe(false);
    expect(vue.findAll('[data-testid="section-voir-tout"]')).toHaveLength(4);
  });

  it('annonce l\'attente pendant la recherche', async () => {
    searchArtworks.mockReturnValue(new Promise(() => undefined));
    route.current = { params: { slug: 'art-religieux' } };

    const vue = await afficher();

    expect(vue.findAll('[data-testid="section-chargement"]')).toHaveLength(4);
  });

  it('ouvre la fiche de l\'œuvre cliquée', async () => {
    route.current = { params: { slug: 'art-religieux' } };
    const vue = await afficher();

    await vue.findAllComponents({ name: 'ArtworkCard' })[0].trigger('click');

    expect(push).toHaveBeenCalledWith({ name: 'artwork-detail', params: { id: 'a1' } });
  });

  it('dit qu\'une thématique inconnue n\'existe pas, et ne cherche rien', async () => {
    route.current = { params: { slug: 'inconnue' } };

    const vue = await afficher();

    expect(vue.get('[data-testid="thematique-introuvable"]').text()).toContain("n'existe pas");
    expect(searchArtworks).not.toHaveBeenCalled();
  });

  it('annonce les dates d\'une collection de saison', async () => {
    route.current = { params: { slug: 'noel' } };

    const vue = await afficher();

    expect(vue.get('[data-testid="thematique-saison"]').text()).toContain('Du 1er décembre au 6 janvier');
  });
});
