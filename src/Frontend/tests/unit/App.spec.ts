import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';

vi.mock('@/components/AppHeader.vue', () => ({ default: { template: '<header data-testid="entete" />' } }));

import App from '@/App.vue';

describe('App', () => {
  it("compose l'en-tête, la vue courante et le pied de page avec le lien vers les sources", () => {
    const vue = mount(App, {
      global: {
        stubs: { 'router-view': { template: '<p data-testid="vue" />' }, 'router-link': { props: ['to'], template: '<a :href="to"><slot /></a>' } }
      }
    });

    expect(vue.find('[data-testid="entete"]').exists()).toBe(true);
    expect(vue.find('main [data-testid="vue"]').exists()).toBe(true);
    expect(vue.get('[data-testid="footer-source"]').text()).toContain('Licence Ouverte 2.0');
    expect(vue.get('[data-testid="footer-source"] a').attributes('href')).toBe('/about#sources');
  });
});
