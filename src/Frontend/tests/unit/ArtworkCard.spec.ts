import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import ArtworkCard from '@/components/ArtworkCard.vue';

const base = { id: '1', imageUrl: '/x.jpg' };

describe('ArtworkCard', () => {
  it('affiche « Sans titre » pour une œuvre sans titre', () => {
    const card = mount(ArtworkCard, { props: { ...base, title: '' } });

    expect(card.find('h3').text()).toBe('Sans titre');
  });

  it('affiche le titre nettoyé et garde le titre brut en infobulle', () => {
    const card = mount(ArtworkCard, { props: { ...base, title: '- Ah ! docteur' } });

    expect(card.find('h3').text()).toBe('Ah ! docteur');
    expect(card.find('h3').attributes('title')).toBe('- Ah ! docteur');
  });

  it('affiche les artistes, ou « Date inconnue » sans date', () => {
    const card = mount(ArtworkCard, {
      props: {
        ...base,
        title: 'T',
        artists: [
          { id: 'a', firstName: 'Claude', lastName: 'Monet' },
          { id: 'b', firstName: '', lastName: 'Zola' }
        ]
      }
    });

    expect(card.text()).toContain('Claude Monet, Zola');
    expect(card.find('.date').text()).toBe('Date inconnue');
  });

  it('signale le clic', async () => {
    const card = mount(ArtworkCard, { props: { ...base, title: 'T' } });

    await card.trigger('click');

    expect(card.emitted('click')).toHaveLength(1);
  });

  it("propose l'étoile des favoris sans ouvrir la fiche", async () => {
    const card = mount(ArtworkCard, { props: { ...base, title: 'T' } });

    await card.get('[data-testid="favorite-button"]').trigger('click');

    expect(card.emitted('click')).toBeUndefined();
  });
});
