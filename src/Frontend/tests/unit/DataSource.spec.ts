import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import DataSource from '@/components/DataSource.vue';
import { LICENCE_URL } from '@/utils/sources';

const monter = (props: Record<string, unknown>) =>
  mount(DataSource, { props, global: { stubs: { 'router-link': { template: '<a><slot /></a>' } } } });

describe('DataSource', () => {
  it('cite la notice Joconde, son lieu de conservation, POP et la licence', () => {
    const texte = monter({ kind: 'joconde', reference: 'M0569000236', place: 'musée des Jacobins · Auch' });

    expect(texte.text()).toContain('M0569000236');
    expect(texte.text()).toContain('base Joconde');
    expect(texte.text()).toContain('Conservée : musée des Jacobins · Auch');
    expect(texte.text()).toContain('Licence Ouverte 2.0');
    expect(texte.find('a[href="https://www.pop.culture.gouv.fr/notice/joconde/M0569000236"]').exists()).toBe(true);
    expect(texte.find(`a[href="${LICENCE_URL}"]`).exists()).toBe(true);
  });

  it('prévient que les images et certaines informations peuvent relever de droits de tiers', () => {
    expect(monter({ kind: 'joconde', reference: 'M1' }).text()).toContain('droits de tiers');
  });

  it('sans lieu de conservation, n\'en invente pas', () => {
    expect(monter({ kind: 'joconde', reference: 'M1' }).text()).not.toContain('Conservée');
  });

  it('cite Muséofile pour un musée, sans parler de notice ni d\'images', () => {
    const texte = monter({ kind: 'museofile' }).text();

    expect(texte).toContain('Muséofile');
    expect(texte).toContain('Licence Ouverte 2.0');
    expect(texte).not.toContain('droits de tiers');
  });

  it('une référence inutilisable renvoie vers l\'accueil de POP, jamais vers une adresse construite', () => {
    const texte = monter({ kind: 'joconde', reference: '../x' });

    expect(texte.find('a[href="https://pop.culture.gouv.fr/"]').exists()).toBe(true);
  });
});
