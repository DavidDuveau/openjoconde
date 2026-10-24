import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';

const { lire } = vi.hoisted(() => ({ lire: vi.fn() }));
vi.mock('@/utils/nouveautes', async (original) => ({ ...(await original<typeof import('@/utils/nouveautes')>()), lireChangelog: lire }));

import NouveautesView from '@/views/NouveautesView.vue';

const afficher = () => mount(NouveautesView);

describe('NouveautesView', () => {
  it('présente chaque version, la plus récente en premier, avec sa date, ses sections et ses lignes', () => {
    lire.mockReturnValue([
      {
        version: '0.7.0', date: '2026-10-24',
        sections: [{ titre: 'Nouveautés', elements: [{ portee: 'compte', texte: 'favoris dans le navigateur', reference: 'US-COMPTE-04' }] }]
      },
      { version: '0.6.0', date: '', sections: [] }
    ]);

    const vue = afficher();

    const versions = vue.findAll('[data-testid="nouveautes-version"]');
    expect(versions).toHaveLength(2);
    expect(vue.get('[data-testid="nouveautes-derniere"]').text()).toContain('Version 0.7.0');
    expect(versions[0].text()).toContain('24 octobre 2026');
    expect(versions[0].get('h3').text()).toBe('Nouveautés');
    const ligne = versions[0].get('[data-testid="nouveaute"]').text();
    expect(ligne).toContain('compte');
    expect(ligne).toContain('favoris dans le navigateur');
    expect(ligne).toContain('US-COMPTE-04');
    expect(versions[1].text()).toContain('Pas de changement visible');
    expect(versions[1].find('.date').exists()).toBe(false);
  });

  it('affiche les lignes comme du texte, sans interpréter le HTML', () => {
    lire.mockReturnValue([
      { version: '1.0.0', date: '', sections: [{ titre: 'Nouveautés', elements: [{ portee: '', texte: '<img src=x onerror=alert(1)>', reference: '' }] }] }
    ]);

    const vue = afficher();

    expect(vue.get('[data-testid="nouveaute"]').text()).toBe('<img src=x onerror=alert(1)>');
    expect(vue.find('[data-testid="nouveaute"] img').exists()).toBe(false);
    expect(vue.find('.portee').exists()).toBe(false);
    expect(vue.find('.reference').exists()).toBe(false);
  });

  it("dit qu'aucune version n'a été publiée", () => {
    lire.mockReturnValue([]);

    const vue = afficher();

    expect(vue.find('[data-testid="nouveautes-vide"]').exists()).toBe(true);
    expect(vue.find('[data-testid="nouveautes-version"]').exists()).toBe(false);
  });
});
