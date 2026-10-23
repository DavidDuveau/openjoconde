import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';

const { telecharger } = vi.hoisted(() => ({ telecharger: vi.fn() }));
vi.mock('@/utils/exportOeuvre', async (original) => ({ ...(await original<typeof import('@/utils/exportOeuvre')>()), telecharger }));

import ArtworkExport from '@/components/ArtworkExport.vue';

const oeuvre = {
  id: 'a1', title: 'Les Baigneuses', reference: '000PE001234', inventoryNumber: null, denomination: null, description: null,
  dimensions: null, creationDate: '1873', creationPlace: null, conservationPlace: null, copyright: null, associatedSiteUrl: null,
  artistUnderCopyright: null, publicDomainYear: null, hasImage: null, museum: null, artists: [], domains: [], techniques: [], periods: []
};

const afficher = () => mount(ArtworkExport, { props: { artwork: oeuvre as never, today: new Date(2026, 9, 23) } });

describe('ArtworkExport', () => {
  beforeEach(() => telecharger.mockReset());
  afterEach(() => vi.restoreAllMocks());

  it('télécharge la fiche en JSON, sous le nom de la référence', async () => {
    await afficher().get('[data-testid="export-json"]').trigger('click');

    const [nom, contenu, type] = telecharger.mock.calls[0];
    expect(nom).toBe('joconde-000PE001234.json');
    expect(type).toBe('application/json');
    expect(JSON.parse(contenu)).toMatchObject({ reference: '000PE001234', source: { consulteLe: '23 octobre 2026' } });
  });

  it('télécharge la fiche en CSV, avec la marque UTF-8', async () => {
    await afficher().get('[data-testid="export-csv"]').trigger('click');

    const [nom, contenu, type] = telecharger.mock.calls[0];
    expect(nom).toBe('joconde-000PE001234.csv');
    expect(type).toBe('text/csv');
    expect(contenu.startsWith('\uFEFF"reference";')).toBe(true);
  });

  it("rappelle la licence à côté des boutons", () => {
    expect(afficher().get('.note').text()).toContain('Licence Ouverte 2.0');
  });

  it("prend la date du jour sans date donnée", async () => {
    const vue = mount(ArtworkExport, { props: { artwork: oeuvre as never } });

    await vue.get('[data-testid="export-json"]').trigger('click');

    expect(JSON.parse(telecharger.mock.calls[0][1]).source.consulteLe).toMatch(/\d{4}$/);
  });
});
