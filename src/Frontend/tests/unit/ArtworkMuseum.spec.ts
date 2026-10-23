import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils';

const { getMuseum } = vi.hoisted(() => ({ getMuseum: vi.fn() }));
vi.mock('@/services/api', () => ({ default: { getMuseum } }));

import ArtworkMuseum from '@/components/ArtworkMuseum.vue';

const oeuvre = (surcharge: object = {}) => ({
  id: 'a1', title: 'Les Baigneuses', reference: 'R1', conservationPlace: null,
  museum: { id: 'm1', name: "Musée d'Orsay" }, artists: [], domains: [], techniques: [], periods: [], ...surcharge
});
const orsay = (surcharge: object = {}) => ({
  id: 'm1', name: "Musée d'Orsay", city: 'Paris', department: 'Paris', region: 'Île-de-France', address: '1 rue de la Légion d\'Honneur',
  zipCode: '75007', website: 'https://www.musee-orsay.fr/', latitude: 48.86, longitude: 2.326, ...surcharge
});
const autre = { id: 'm2', name: 'Musée Fabre', city: 'Montpellier', department: 'Hérault', region: 'Occitanie' };

async function afficher(artwork: object) {
  const vue = mount(ArtworkMuseum, { props: { artwork } as never, global: { stubs: { RouterLink: RouterLinkStub } } });
  await flushPromises();
  return vue;
}

describe('ArtworkMuseum', () => {
  beforeEach(() => {
    getMuseum.mockReset();
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  it("montre le nom du musée comme lien vers sa page, sa localisation, de quoi s'y rendre et le lieu de conservation", async () => {
    getMuseum.mockResolvedValue(orsay());

    const vue = await afficher(oeuvre({ conservationPlace: 'salle 12' }));

    expect(getMuseum).toHaveBeenCalledWith('m1');
    expect(vue.getComponent('[data-testid="artwork-museum-link"]').props('to')).toEqual({ name: 'museum-detail', params: { id: 'm1' } });
    expect(vue.get('[data-testid="artwork-museum-link"]').text()).toBe("Musée d'Orsay");
    expect(vue.get('[data-testid="artwork-museum-place"]').text()).toBe('Paris · Paris · Île-de-France');
    expect(vue.get('[data-testid="artwork-conservation"]').text()).toContain('salle 12');
    expect(vue.get('[data-testid="visit-route-osm"]').exists()).toBe(true);
    expect(vue.get('[data-testid="visit-site"]').attributes('href')).toBe('https://www.musee-orsay.fr/');
  });

  it("garde le nom et le lien quand la fiche du musée ne se charge pas", async () => {
    getMuseum.mockRejectedValue(new Error('panne'));

    const vue = await afficher(oeuvre());

    expect(vue.get('[data-testid="artwork-museum-link"]').text()).toBe("Musée d'Orsay");
    expect(vue.find('[data-testid="museum-visit"]').exists()).toBe(false);
    expect(vue.find('[data-testid="artwork-museum-place"]').exists()).toBe(false);
    expect(vue.find('[data-testid="artwork-conservation"]').exists()).toBe(false);
  });

  it("n'affiche rien pour une œuvre sans musée, et n'appelle pas l'API", async () => {
    const vue = await afficher(oeuvre({ museum: null }));

    expect(getMuseum).not.toHaveBeenCalled();
    expect(vue.find('[data-testid="artwork-museum"]').exists()).toBe(false);
  });

  it("recharge le musée quand l'œuvre change", async () => {
    getMuseum.mockResolvedValueOnce(orsay()).mockResolvedValueOnce(autre);
    const vue = await afficher(oeuvre());

    await vue.setProps({ artwork: oeuvre({ id: 'a2', museum: { id: 'm2', name: 'Musée Fabre' } }) });
    await flushPromises();

    expect(vue.get('[data-testid="artwork-museum-link"]').text()).toBe('Musée Fabre');
    expect(vue.get('[data-testid="artwork-museum-place"]').text()).toBe('Montpellier · Hérault · Occitanie');
  });

  it("ignore la réponse tardive d'un musée déjà quitté", async () => {
    let tard!: (m: unknown) => void;
    getMuseum.mockReturnValueOnce(new Promise(r => { tard = r; })).mockResolvedValueOnce(autre);
    const vue = mount(ArtworkMuseum, { props: { artwork: oeuvre() } as never, global: { stubs: { RouterLink: RouterLinkStub } } });

    await vue.setProps({ artwork: oeuvre({ id: 'a2', museum: { id: 'm2', name: 'Musée Fabre' } }) });
    await flushPromises();
    tard(orsay());
    await flushPromises();

    expect(vue.get('[data-testid="artwork-museum-place"]').text()).toBe('Montpellier · Hérault · Occitanie');
  });
});
