import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import GlossaryText from '@/components/GlossaryText.vue';

describe('GlossaryText', () => {
  it('explique les termes techniques par une infobulle, focalisable au clavier', () => {
    const vue = mount(GlossaryText, { props: { text: 'Gravure sur bois, eau-forte' } });

    const termes = vue.findAll('[data-testid="glossary-term"]');
    expect(termes.map(t => t.text())).toEqual(['Gravure', 'eau-forte']);
    expect(termes[0].element.tagName).toBe('ABBR');
    expect(termes[0].attributes('title')).toContain('Image obtenue');
    expect(termes[0].attributes('tabindex')).toBe('0');
    expect(vue.text()).toBe('Gravure sur bois, eau-forte');
  });

  it('laisse un texte sans terme technique tel quel', () => {
    const vue = mount(GlossaryText, { props: { text: 'Vue de Rouen' } });

    expect(vue.findAll('[data-testid="glossary-term"]')).toHaveLength(0);
    expect(vue.text()).toBe('Vue de Rouen');
  });
});
