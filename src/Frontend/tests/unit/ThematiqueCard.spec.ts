import { describe, expect, it } from 'vitest';
import { mount, RouterLinkStub } from '@vue/test-utils';
import ThematiqueCard from '@/components/ThematiqueCard.vue';
import { THEMATIQUES } from '@/utils/thematiques';

const portraits = THEMATIQUES.find(t => t.slug === 'portraits')!;
const noel = THEMATIQUES.find(t => t.slug === 'noel')!;

const afficher = (thematique: typeof portraits, maintenant = new Date(2026, 6, 14)) =>
  mount(ThematiqueCard, { props: { thematique, maintenant }, global: { stubs: { RouterLink: RouterLinkStub } } });

describe('ThematiqueCard', () => {
  it('mène à la thématique, avec son titre et son résumé, sans date quand elle n\'est pas saisonnière', () => {
    const vue = afficher(portraits);

    expect(vue.getComponent(RouterLinkStub).props('to')).toEqual({ name: 'thematique', params: { slug: 'portraits' } });
    expect(vue.get('[data-testid="thematique-title"]').text()).toBe('Portraits');
    expect(vue.text()).toContain(portraits.resume);
    expect(vue.find('[data-testid="thematique-saison"]').exists()).toBe(false);
  });

  it('annonce les dates d\'une collection de saison, et « En ce moment » quand elle est ouverte', () => {
    const ouverte = afficher(noel, new Date(2026, 11, 20));

    expect(ouverte.get('[data-testid="thematique-saison"]').text()).toContain('Du 1er décembre au 6 janvier');
    expect(ouverte.find('[data-testid="thematique-en-cours"]').exists()).toBe(true);
  });

  it('ne dit pas « En ce moment » hors saison, mais garde les dates', () => {
    const fermee = afficher(noel, new Date(2026, 6, 14));

    expect(fermee.get('[data-testid="thematique-saison"]').text()).toContain('Du 1er décembre au 6 janvier');
    expect(fermee.find('[data-testid="thematique-en-cours"]').exists()).toBe(false);
  });

  it('utilise la date du jour quand aucune n\'est donnée', () => {
    const vue = mount(ThematiqueCard, { props: { thematique: noel }, global: { stubs: { RouterLink: RouterLinkStub } } });

    expect(vue.find('[data-testid="thematique-saison"]').exists()).toBe(true);
  });
});
