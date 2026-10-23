import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils';

const { getMuseum, searchArtworks } = vi.hoisted(() => ({ getMuseum: vi.fn(), searchArtworks: vi.fn() }));
vi.mock('@/services/api', () => ({ default: { getMuseum, searchArtworks } }));
vi.mock('vue-router', () => ({ useRoute: () => ({ params: { id: 'm1' } }) }));

import MuseumDetailView from '@/views/MuseumDetailView.vue';

const musee = {
  id: 'm1', name: 'Musée des Confluences', city: 'Lyon', department: 'Rhône', zipCode: '69002', address: '86 quai Perrache',
  phone: '04 28 38 12 12', email: '', website: 'https://www.museedesconfluences.fr', description: 'Un musée de sciences.',
  region: 'Auvergne-Rhône-Alpes', code: 'M0001', latitude: 45.73, longitude: 4.81
};

const oeuvres = (titres: string[], totalItems = titres.length) => ({
  items: titres.map((title, i) => ({ id: `a${i}`, title, creationDate: i === 0 ? '1850' : '' })),
  totalItems, page: 1, pageSize: 20, totalPages: 1
});

async function afficher() {
  const vue = mount(MuseumDetailView, { global: { stubs: { RouterLink: RouterLinkStub } } });
  await flushPromises();
  return vue;
}

describe('MuseumDetailView', () => {
  beforeEach(() => {
    getMuseum.mockReset();
    searchArtworks.mockReset();
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  it("propose d'explorer la collection du musée avec les filtres de la recherche", async () => {
    getMuseum.mockResolvedValue(musee);
    searchArtworks.mockResolvedValue(oeuvres([]));

    const vue = await afficher();

    const lien = vue.getComponent('[data-testid="museum-explore"]');
    expect(lien.props('to')).toEqual({ name: 'search', query: { museumId: 'm1', museumName: 'Musée des Confluences' } });
  });

  it("montre la catégorie, les domaines des collections et les points forts de Muséofile", async () => {
    getMuseum.mockResolvedValue({
      ...musee, category: 'écomusée ; musée en territoire rural', thematicDomains: 'Archéologie;Ethnologie ; Histoire',
      highlights: 'Une salle d\'art sacré.'
    });
    searchArtworks.mockResolvedValue(oeuvres([]));

    const vue = await afficher();

    expect(vue.get('[data-testid="museum-category"]').text()).toBe('Catégorie : écomusée, musée en territoire rural');
    expect(vue.get('[data-testid="museum-domains"]').text()).toBe('Domaines : Archéologie, Ethnologie, Histoire');
    expect(vue.get('[data-testid="museum-highlights"]').text()).toContain("Une salle d'art sacré.");
  });

  it("n'affiche ni collections ni points forts quand Muséofile n'en donne pas", async () => {
    getMuseum.mockResolvedValue(musee);
    searchArtworks.mockResolvedValue(oeuvres([]));

    const vue = await afficher();

    expect(vue.find('[data-testid="museum-collections"]').exists()).toBe(false);
    expect(vue.find('[data-testid="museum-highlights"]').exists()).toBe(false);
  });

  it("propose de préparer la visite : carte et itinéraires, sans répéter les coordonnées déjà données", async () => {
    getMuseum.mockResolvedValue(musee);
    searchArtworks.mockResolvedValue(oeuvres([]));

    const vue = await afficher();

    const visite = vue.get('[data-testid="museum-visit-section"]');
    expect(visite.get('[data-testid="visit-map"]').attributes('href')).toContain('mlat=45.73');
    expect(visite.get('[data-testid="visit-route-osm"]').exists()).toBe(true);
    expect(visite.find('[data-testid="visit-address"]').exists()).toBe(false);
    expect(visite.get('[data-testid="visit-hours"]').text()).toContain('ne figurent pas dans les données publiées');
  });

  it('affiche les coordonnées du musée et son lien de site', async () => {
    getMuseum.mockResolvedValue(musee);
    searchArtworks.mockResolvedValue(oeuvres([]));

    const vue = await afficher();

    expect(vue.get('[data-testid="museum-heading"]').text()).toBe('Musée des Confluences');
    expect(vue.text()).toContain('69002 · Lyon · Rhône');
    expect(vue.text()).toContain('86 quai Perrache');
    expect(vue.text()).toContain('04 28 38 12 12');
    expect(vue.get('a[href="https://www.museedesconfluences.fr"]').attributes('rel')).toContain('noopener');
  });

  it("n'écrit pas de lien pour un site qui n'est pas une adresse web", async () => {
    getMuseum.mockResolvedValue({ ...musee, website: 'javascript:alert(1)' });
    searchArtworks.mockResolvedValue(oeuvres([]));

    const vue = await afficher();

    expect(vue.find('a[href^="javascript"]').exists()).toBe(false);
  });

  it("liste les œuvres du musée, demandées par son identifiant, avec leur nombre", async () => {
    getMuseum.mockResolvedValue(musee);
    searchArtworks.mockResolvedValue(oeuvres(['Les Baigneuses', 'Orphée'], 2));

    const vue = await afficher();

    expect(searchArtworks).toHaveBeenCalledWith({ museumId: 'm1', page: 1, pageSize: 20 });
    expect(vue.get('[data-testid="museum-artworks-count"]').text()).toBe('(2)');
    const lignes = vue.findAll('[data-testid="museum-artwork"]');
    expect(lignes.map(ligne => ligne.text())).toEqual(['Les Baigneuses1850', 'Orphée']);
  });

  it("dit qu'aucune œuvre n'est rattachée au musée", async () => {
    getMuseum.mockResolvedValue(musee);
    searchArtworks.mockResolvedValue(oeuvres([]));

    const vue = await afficher();

    expect(vue.text()).toContain("Aucune œuvre n'est rattachée à ce musée");
  });

  it('garde la fiche quand seules les œuvres ne se chargent pas', async () => {
    getMuseum.mockResolvedValue(musee);
    searchArtworks.mockRejectedValue(new Error('réseau'));

    const vue = await afficher();

    expect(vue.get('[data-testid="museum-heading"]').text()).toBe('Musée des Confluences');
    expect(vue.text()).toContain('Erreur lors du chargement des œuvres du musée');
  });

  it("distingue un musée inconnu (404) d'une panne", async () => {
    getMuseum.mockRejectedValue({ response: { status: 404 } });
    const inconnu = await afficher();
    expect(inconnu.text()).toContain('Musée non trouvé');

    getMuseum.mockRejectedValue({ response: { status: 500 } });
    const panne = await afficher();
    expect(panne.text()).toContain('Erreur lors du chargement du musée');
  });
});
