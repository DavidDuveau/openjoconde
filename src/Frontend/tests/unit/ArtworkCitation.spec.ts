import { afterEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import ArtworkCitation from '@/components/ArtworkCitation.vue';

const oeuvre = {
  id: 'a1', title: 'Les Baigneuses', reference: '000PE001234', inventoryNumber: 'RF 1234', denomination: 'tableau',
  creationDate: '1873', museum: { id: 'm1', name: "Musée d'Orsay" },
  artists: [{ id: 'p1', firstName: 'Claude', lastName: 'Monet', role: null }], domains: [], techniques: [], periods: []
};

const afficher = () => mount(ArtworkCitation, { props: { artwork: oeuvre as never, today: new Date(2026, 9, 23) } });

describe('ArtworkCitation', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('propose trois formats, ISO 690 d\'abord, et montre la citation correspondante avec la date de consultation', () => {
    const vue = afficher();

    expect(vue.findAll('[data-testid="citation-format"]')).toHaveLength(3);
    expect(vue.get('[data-testid="citation-text"]').text()).toMatch(/^MONET, Claude\. Les Baigneuses \[tableau\]\. 1873\./);
    expect(vue.get('[data-testid="citation-text"]').text()).toContain('consulté le 23 octobre 2026');
  });

  it('change la citation avec le format choisi', async () => {
    const vue = afficher();

    await vue.findAll('[data-testid="citation-format"]')[1].setValue(true);
    expect(vue.get('[data-testid="citation-text"]').text()).toMatch(/^Monet, C\. \(1873\)\./);

    await vue.findAll('[data-testid="citation-format"]')[2].setValue(true);
    expect(vue.get('[data-testid="citation-text"]').text()).toMatch(/^Monet, Claude\. Les Baigneuses\./);
  });

  it('copie la citation affichée, dans le format choisi, et le dit', async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    vi.stubGlobal('navigator', { ...navigator, clipboard: { writeText } });
    const vue = afficher();
    await vue.findAll('[data-testid="citation-format"]')[1].setValue(true);

    await vue.get('[data-testid="citation-copy"]').trigger('click');
    await flushPromises();

    expect(writeText).toHaveBeenCalledWith(vue.get('[data-testid="citation-text"]').text());
    expect(vue.get('[data-testid="citation-status"]').text()).toBe('Citation copiée.');
    expect(vue.get('[data-testid="citation-status"]').attributes('aria-live')).toBe('polite');
  });

  it("invite à sélectionner la citation à la main quand le presse-papiers est refusé ou absent", async () => {
    vi.stubGlobal('navigator', { ...navigator, clipboard: undefined });
    const vue = afficher();

    await vue.get('[data-testid="citation-copy"]').trigger('click');
    await flushPromises();

    expect(vue.get('[data-testid="citation-status"]').text()).toContain('sélectionnez la citation');
  });

  it("prend la date du jour quand aucune n'est donnée", () => {
    const vue = mount(ArtworkCitation, { props: { artwork: oeuvre as never } });

    expect(vue.get('[data-testid="citation-text"]').text()).toMatch(/consulté le \d{1,2}(er)? \p{L}+ \d{4}/u);
  });
});
