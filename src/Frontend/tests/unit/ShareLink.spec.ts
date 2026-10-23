import { afterEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import ShareLink from '@/components/ShareLink.vue';

const afficher = (props: Record<string, string> = {}) => mount(ShareLink, { props: { title: 'Les Baigneuses', ...props } });

const simuler = (navigateur: Record<string, unknown>) => vi.stubGlobal('navigator', { ...navigator, share: undefined, clipboard: undefined, ...navigateur });

describe('ShareLink', () => {
  afterEach(() => vi.unstubAllGlobals());

  it("propose le partage du système quand le navigateur l'a, avec le titre et l'adresse de la page", async () => {
    const share = vi.fn().mockResolvedValue(undefined);
    simuler({ share });
    const vue = afficher({ shareUrl: 'https://exemple.test/artworks/a1' });

    await vue.get('[data-testid="share-native"]').trigger('click');

    expect(share).toHaveBeenCalledWith({ title: 'Les Baigneuses', url: 'https://exemple.test/artworks/a1' });
    expect(vue.get('[data-testid="share-status"]').text()).toBe('');
  });

  it("n'a pas de bouton « Partager » sans partage du système", () => {
    simuler({});

    expect(afficher().find('[data-testid="share-native"]').exists()).toBe(false);
  });

  it("partage l'adresse de la page quand aucune n'est donnée", async () => {
    const share = vi.fn().mockResolvedValue(undefined);
    simuler({ share });

    await afficher().get('[data-testid="share-native"]').trigger('click');

    expect(share).toHaveBeenCalledWith({ title: 'Les Baigneuses', url: window.location.href });
  });

  it("ne dit rien quand l'utilisateur ferme la fenêtre de partage", async () => {
    const writeText = vi.fn();
    simuler({ share: vi.fn().mockRejectedValue(new DOMException('annulé', 'AbortError')), clipboard: { writeText } });
    const vue = afficher();

    await vue.get('[data-testid="share-native"]').trigger('click');
    await flushPromises();

    expect(writeText).not.toHaveBeenCalled();
    expect(vue.get('[data-testid="share-status"]').text()).toBe('');
  });

  it("copie le lien quand le partage du système échoue", async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    simuler({ share: vi.fn().mockRejectedValue(new Error('refusé')), clipboard: { writeText } });
    const vue = afficher({ shareUrl: 'https://exemple.test/a' });

    await vue.get('[data-testid="share-native"]').trigger('click');
    await flushPromises();

    expect(writeText).toHaveBeenCalledWith('https://exemple.test/a');
    expect(vue.get('[data-testid="share-status"]').text()).toContain('Lien copié');
  });

  it("copie le lien et le dit, dans une zone annoncée aux lecteurs d'écran", async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    simuler({ clipboard: { writeText } });
    const vue = afficher({ shareUrl: 'https://exemple.test/a' });

    await vue.get('[data-testid="share-copy"]').trigger('click');
    await flushPromises();

    expect(writeText).toHaveBeenCalledWith('https://exemple.test/a');
    expect(vue.get('[data-testid="share-status"]').text()).toBe('Lien copié dans le presse-papiers.');
    expect(vue.get('[data-testid="share-status"]').attributes('aria-live')).toBe('polite');
    expect(vue.find('[data-testid="share-manual"]').exists()).toBe(false);
  });

  it("donne l'adresse à copier à la main quand le presse-papiers est refusé", async () => {
    simuler({ clipboard: { writeText: vi.fn().mockRejectedValue(new Error('refusé')) } });
    const vue = afficher({ shareUrl: 'https://exemple.test/a' });

    await vue.get('[data-testid="share-copy"]').trigger('click');
    await flushPromises();

    expect(vue.get('[data-testid="share-status"]').text()).toContain("n'a pas fonctionné");
    expect((vue.get('[data-testid="share-manual"]').element as HTMLInputElement).value).toBe('https://exemple.test/a');
  });

  it("donne l'adresse à copier à la main quand le navigateur n'a pas de presse-papiers, puis la masque après une copie réussie", async () => {
    simuler({});
    const vue = afficher({ shareUrl: 'https://exemple.test/a' });

    await vue.get('[data-testid="share-copy"]').trigger('click');
    await flushPromises();
    expect(vue.find('[data-testid="share-manual"]').exists()).toBe(true);

    simuler({ clipboard: { writeText: vi.fn().mockResolvedValue(undefined) } });
    await vue.get('[data-testid="share-copy"]').trigger('click');
    await flushPromises();
    expect(vue.find('[data-testid="share-manual"]').exists()).toBe(false);
  });

  it("sélectionne l'adresse quand le champ prend le focus", async () => {
    simuler({});
    const vue = afficher({ shareUrl: 'https://exemple.test/a' });
    await vue.get('[data-testid="share-copy"]').trigger('click');
    await flushPromises();
    const champ = vue.get('[data-testid="share-manual"]');
    const select = vi.spyOn(champ.element as HTMLInputElement, 'select');

    await champ.trigger('focus');

    expect(select).toHaveBeenCalled();
  });
});
