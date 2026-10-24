import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import FavoriteButton from '@/components/FavoriteButton.vue';
import { ajouterFavori, chargerFavoris, estFavori, LIMITE_FAVORIS, oublierProbleme } from '@/utils/favoris';

const id = (n: number) => `00000000-0000-0000-0000-${String(n).padStart(12, '0')}`;

describe('FavoriteButton', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    window.localStorage.clear();
    chargerFavoris();
    oublierProbleme();
  });

  it("ajoute puis retire l'œuvre, avec son état annoncé", async () => {
    const vue = mount(FavoriteButton, { props: { id: id(1), title: 'Les Baigneuses' } });
    const bouton = vue.get('[data-testid="favorite-button"]');
    expect(bouton.attributes('aria-pressed')).toBe('false');
    expect(bouton.text()).toContain('Ajouter aux favoris');

    await bouton.trigger('click');

    expect(estFavori(id(1))).toBe(true);
    expect(bouton.attributes('aria-pressed')).toBe('true');
    expect(bouton.attributes('aria-label')).toBe('Retirer des favoris : Les Baigneuses');
    expect(bouton.text()).toContain('Retirer des favoris');

    await bouton.trigger('click');

    expect(estFavori(id(1))).toBe(false);
  });

  it("reflète l'état partout : deux boutons pour la même œuvre", async () => {
    const un = mount(FavoriteButton, { props: { id: id(1) } });
    const deux = mount(FavoriteButton, { props: { id: id(1), compact: true } });

    await un.get('button').trigger('click');

    expect(deux.get('button').attributes('aria-pressed')).toBe('true');
  });

  it('ne déclenche pas le clic de la carte qui le contient', async () => {
    const parent = vi.fn();
    const vue = mount({
      components: { FavoriteButton },
      template: '<div @click="parent"><FavoriteButton id="x" /></div>',
      methods: { parent }
    });

    await vue.get('button').trigger('click');

    expect(parent).not.toHaveBeenCalled();
  });

  it('masque le texte en version compacte', () => {
    const vue = mount(FavoriteButton, { props: { id: id(1), compact: true } });

    expect(vue.find('.favorite-text').exists()).toBe(false);
  });

  it('explique que le stockage est indisponible', async () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('bloqué'); });
    const vue = mount(FavoriteButton, { props: { id: id(1) } });

    await vue.get('button').trigger('click');

    expect(vue.get('[data-testid="favorite-message"]').text()).toContain('ne peuvent pas être gardés sur cet appareil');
    expect(vue.get('button').attributes('aria-pressed')).toBe('false');
  });

  it('explique la limite de favoris', async () => {
    for (let n = 1; n <= LIMITE_FAVORIS; n++) ajouterFavori(id(n));
    const vue = mount(FavoriteButton, { props: { id: id(9999) } });

    await vue.get('button').trigger('click');

    expect(vue.get('[data-testid="favorite-message"]').text()).toContain(`${LIMITE_FAVORIS} favoris`);
  });
});
