import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils';

const { getMuseum } = vi.hoisted(() => ({ getMuseum: vi.fn() }));
vi.mock('@/services/api', () => ({ default: { getMuseum } }));

import MuseumSummary from '@/components/MuseumSummary.vue';

const musee = {
  id: 'm1', name: "Musée d'Orsay", address: "1 rue de la Légion d'Honneur", zipCode: '75007', city: 'Paris',
  website: 'https://www.musee-orsay.fr'
};

const monter = (museumId = 'm1') =>
  mount(MuseumSummary, { props: { museumId }, global: { stubs: { RouterLink: RouterLinkStub } } });

async function afficher(museumId = 'm1') {
  const vue = monter(museumId);
  await flushPromises();
  return vue;
}

describe('MuseumSummary', () => {
  beforeEach(() => {
    getMuseum.mockReset();
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  it('résume le musée : nom lié à sa fiche, adresse complète et site web', async () => {
    getMuseum.mockResolvedValue(musee);

    const vue = await afficher();

    expect(getMuseum).toHaveBeenCalledWith('m1');
    expect(vue.get('[data-testid="museum-summary-name"]').text()).toBe("Musée d'Orsay");
    expect(vue.getComponent(RouterLinkStub).props('to')).toEqual({ name: 'museum-detail', params: { id: 'm1' } });
    expect(vue.get('[data-testid="museum-summary-address"]').text()).toBe("1 rue de la Légion d'Honneur, 75007 Paris");
    expect(vue.get('[data-testid="museum-summary-site"]').attributes('href')).toBe('https://www.musee-orsay.fr');
  });

  it("omet l'adresse et le site quand le musée n'en donne pas, et refuse un site douteux", async () => {
    getMuseum.mockResolvedValue({ id: 'm1', name: 'Musée sans adresse', website: 'javascript:alert(1)' });

    const vue = await afficher();

    expect(vue.find('[data-testid="museum-summary-address"]').exists()).toBe(false);
    expect(vue.find('[data-testid="museum-summary-site"]').exists()).toBe(false);
  });

  it("n'affiche rien quand le musée ne se charge pas", async () => {
    getMuseum.mockRejectedValue(new Error('panne'));

    const vue = await afficher();

    expect(vue.find('[data-testid="museum-summary"]').exists()).toBe(false);
  });

  it('recharge quand le musée change, sans montrer la réponse du musée quitté', async () => {
    let repondre: (valeur: unknown) => void = () => undefined;
    getMuseum
      .mockReturnValueOnce(new Promise(r => { repondre = r; }))
      .mockResolvedValueOnce({ id: 'm2', name: 'Nouveau musée' });
    const vue = monter('m1');

    await vue.setProps({ museumId: 'm2' });
    await flushPromises();
    repondre({ id: 'm1', name: 'Ancien musée' });
    await flushPromises();

    expect(vue.get('[data-testid="museum-summary-name"]').text()).toBe('Nouveau musée');
  });
});
