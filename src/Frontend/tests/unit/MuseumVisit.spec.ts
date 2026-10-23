import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import MuseumVisit from '@/components/MuseumVisit.vue';

const complet = {
  address: '1 rue du Port', zipCode: '56000', city: 'Vannes', phone: '02 97 00 00 00', website: 'https://musee.example/',
  latitude: 47.658, longitude: -2.76
};

const afficher = (museum: object, showContact?: boolean) =>
  mount(MuseumVisit, { props: { museum, ...(showContact === undefined ? {} : { showContact }) } });

describe('MuseumVisit', () => {
  it("donne l'adresse, la carte, deux itinéraires, le site et le téléphone", () => {
    const vue = afficher(complet);

    expect(vue.get('[data-testid="visit-address"]').text()).toBe('1 rue du Port, 56000 Vannes');
    expect(vue.get('[data-testid="visit-map"]').attributes('href')).toContain('openstreetmap.org/?mlat=47.658');
    expect(vue.get('[data-testid="visit-route-osm"]').attributes('href')).toContain('openstreetmap.org/directions');
    expect(vue.get('[data-testid="visit-route-google"]').attributes('href')).toContain('google.com/maps/dir');
    expect(vue.get('[data-testid="visit-site"]').attributes('href')).toBe('https://musee.example/');
    expect(vue.get('[data-testid="visit-phone"]').text()).toContain('02 97 00 00 00');
  });

  it("n'ouvre ses liens externes que sans fuite vers la page d'origine", () => {
    const vue = afficher(complet);

    for (const lien of vue.findAll('a')) {
      expect(lien.attributes('target')).toBe('_blank');
      expect(lien.attributes('rel')).toContain('noopener');
    }
  });

  it("dit que les horaires ne sont pas publiés et renvoie au site du musée quand il en a un", () => {
    const vue = afficher(complet);

    expect(vue.get('[data-testid="visit-hours"]').text()).toContain('ne figurent pas dans les données publiées');
    expect(vue.get('[data-testid="visit-hours"]').text()).toContain('consultez le site du musée');
  });

  it("renvoie au musée lui-même quand il n'a pas de site", () => {
    const vue = afficher({ city: 'Albi', latitude: 43.9, longitude: 2.1 });

    expect(vue.get('[data-testid="visit-hours"]').text()).toContain("renseignez-vous auprès du musée");
    expect(vue.find('[data-testid="visit-site"]').exists()).toBe(false);
  });

  it("refuse un site qui n'est pas web, et ne montre ni carte ni itinéraire sans lieu", () => {
    const vue = afficher({ website: 'javascript:alert(1)' });

    expect(vue.find('[data-testid="visit-site"]').exists()).toBe(false);
    expect(vue.find('[data-testid="visit-map"]').exists()).toBe(false);
    expect(vue.find('[data-testid="visit-route-osm"]').exists()).toBe(false);
  });

  it("peut masquer adresse, site et téléphone quand la page les montre déjà", () => {
    const vue = afficher(complet, false);

    expect(vue.find('[data-testid="visit-address"]').exists()).toBe(false);
    expect(vue.find('[data-testid="visit-site"]').exists()).toBe(false);
    expect(vue.find('[data-testid="visit-phone"]').exists()).toBe(false);
    expect(vue.find('[data-testid="visit-map"]').exists()).toBe(true);
  });
});
