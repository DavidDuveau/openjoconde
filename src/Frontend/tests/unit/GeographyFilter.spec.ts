import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';

const { getMuseumGeography } = vi.hoisted(() => ({ getMuseumGeography: vi.fn() }));
vi.mock('@/services/api', () => ({ default: { getMuseumGeography } }));

import GeographyFilter from '@/components/GeographyFilter.vue';

const geographie = [
  { name: 'Bretagne', museums: 5, departments: [{ name: 'Morbihan', museums: 2 }, { name: 'Finistère', museums: 3 }] },
  { name: 'Occitanie', museums: 4, departments: [{ name: 'Tarn', museums: 4 }] }
];

async function afficher(props: { region?: string; department?: string } = {}) {
  const vue = mount(GeographyFilter, { props });
  await flushPromises();
  return vue;
}

const options = (vue: Awaited<ReturnType<typeof afficher>>, testid: string) =>
  vue.get(`[data-testid="${testid}"]`).findAll('option').map(o => o.text());

describe('GeographyFilter', () => {
  beforeEach(() => {
    getMuseumGeography.mockReset();
    getMuseumGeography.mockResolvedValue(geographie);
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  it('propose les régions avec leur nombre de musées', async () => {
    const vue = await afficher();

    expect(options(vue, 'region-select')).toEqual(['Toutes les régions', 'Bretagne (5)', 'Occitanie (4)']);
  });

  it("propose tous les départements, par ordre alphabétique, tant qu'aucune région n'est choisie", async () => {
    const vue = await afficher();

    expect(options(vue, 'department-select')).toEqual(['Tous les départements', 'Finistère (3)', 'Morbihan (2)', 'Tarn (4)']);
  });

  it('restreint les départements à ceux de la région choisie', async () => {
    const vue = await afficher({ region: 'Occitanie' });

    expect(options(vue, 'department-select')).toEqual(['Tous les départements', 'Tarn (4)']);
  });

  it('annonce la région choisie et le changement', async () => {
    const vue = await afficher();

    await vue.get('[data-testid="region-select"]').setValue('Bretagne');

    expect(vue.emitted('update:region')).toEqual([['Bretagne']]);
    expect(vue.emitted('change')).toHaveLength(1);
  });

  it("efface le département quand il n'est pas de la nouvelle région, et le garde sinon", async () => {
    const hors = await afficher({ department: 'Tarn' });
    await hors.get('[data-testid="region-select"]').setValue('Bretagne');
    expect(hors.emitted('update:department')).toEqual([['']]);

    const dedans = await afficher({ department: 'Morbihan' });
    await dedans.get('[data-testid="region-select"]').setValue('Bretagne');
    expect(dedans.emitted('update:department')).toBeUndefined();
  });

  it('garde le département en retirant la région', async () => {
    const vue = await afficher({ region: 'Bretagne', department: 'Morbihan' });

    await vue.get('[data-testid="region-select"]').setValue('');

    expect(vue.emitted('update:region')).toEqual([['']]);
    expect(vue.emitted('update:department')).toBeUndefined();
  });

  it('annonce le département choisi et le changement', async () => {
    const vue = await afficher();

    await vue.get('[data-testid="department-select"]').setValue('Tarn');

    expect(vue.emitted('update:department')).toEqual([['Tarn']]);
    expect(vue.emitted('change')).toHaveLength(1);
  });

  it('laisse les listes vides, sans casser, quand la liste géographique est en panne', async () => {
    getMuseumGeography.mockRejectedValue(new Error('panne'));

    const vue = await afficher();

    expect(options(vue, 'region-select')).toEqual(['Toutes les régions']);
  });
});
