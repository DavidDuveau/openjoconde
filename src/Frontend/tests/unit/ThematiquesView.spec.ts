import { afterEach, describe, expect, it, vi } from 'vitest';
import { mount, RouterLinkStub } from '@vue/test-utils';
import ThematiquesView from '@/views/ThematiquesView.vue';
import { THEMATIQUES } from '@/utils/thematiques';

const afficher = () => mount(ThematiquesView, { global: { stubs: { RouterLink: RouterLinkStub } } });

describe('ThematiquesView', () => {
  afterEach(() => vi.useRealTimers());

  it('présente toutes les thématiques, une vignette chacune', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2026, 6, 14));

    const vue = afficher();

    expect(vue.findAll('[data-testid="thematique-card"]')).toHaveLength(THEMATIQUES.length);
    expect(vue.find('[data-testid="thematiques-en-cours"]').exists()).toBe(false);
  });

  it('met en évidence une collection de saison tant qu\'elle est ouverte, avec ses dates', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2026, 11, 20));

    const vue = afficher();

    const enCours = vue.get('[data-testid="thematiques-en-cours"]');
    expect(enCours.text()).toContain('Noël et l\'Épiphanie');
    expect(enCours.text()).toContain('Du 1er décembre au 6 janvier');
    // Elle n'est pas montrée deux fois
    expect(vue.findAll('[data-testid="thematique-card"]')).toHaveLength(THEMATIQUES.length);
  });
});
