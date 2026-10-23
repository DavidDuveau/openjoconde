import { describe, expect, it } from 'vitest';
import { THEMATIQUES, libelleSaison, saisonEnCours, trouverThematique } from '@/utils/thematiques';

const noel = { debut: { mois: 12, jour: 1 }, fin: { mois: 1, jour: 6 } };
const ete = { debut: { mois: 6, jour: 21 }, fin: { mois: 9, jour: 22 } };
const jour = (annee: number, mois: number, j: number) => new Date(annee, mois - 1, j);

describe('saisonEnCours', () => {
  it.each([
    [jour(2026, 12, 1), true],
    [jour(2026, 12, 25), true],
    [jour(2027, 1, 1), true],
    [jour(2027, 1, 6), true],
    [jour(2027, 1, 7), false],
    [jour(2026, 11, 30), false],
    [jour(2026, 7, 14), false]
  ])('une saison qui enjambe le Nouvel An : %s -> %s', (date, attendu) => {
    expect(saisonEnCours(noel, date)).toBe(attendu);
  });

  it.each([
    [jour(2026, 6, 20), false],
    [jour(2026, 6, 21), true],
    [jour(2026, 8, 15), true],
    [jour(2026, 9, 22), true],
    [jour(2026, 9, 23), false],
    [jour(2026, 12, 25), false]
  ])('une saison dans la même année : %s -> %s', (date, attendu) => {
    expect(saisonEnCours(ete, date)).toBe(attendu);
  });
});

describe('libelleSaison', () => {
  it('écrit les dates en toutes lettres, avec « 1er » pour le premier du mois', () => {
    expect(libelleSaison(noel)).toBe('Du 1er décembre au 6 janvier');
    expect(libelleSaison(ete)).toBe('Du 21 juin au 22 septembre');
  });
});

describe('catalogue des thématiques', () => {
  it('a des identifiants uniques et retrouve chaque thématique par le sien', () => {
    const slugs = THEMATIQUES.map(t => t.slug);

    expect(new Set(slugs).size).toBe(slugs.length);
    for (const t of THEMATIQUES) expect(trouverThematique(t.slug)).toBe(t);
    expect(trouverThematique('inconnue')).toBeUndefined();
  });

  it("explique chaque thème : le thème, son importance, ses critères de sélection, et au moins une étape", () => {
    for (const t of THEMATIQUES) {
      expect(t.resume.length, t.slug).toBeGreaterThan(10);
      expect(t.explication.length, t.slug).toBeGreaterThan(30);
      expect(t.importance.length, t.slug).toBeGreaterThan(30);
      expect(t.criteres.length, t.slug).toBeGreaterThan(30);
      expect(t.sections.length, t.slug).toBeGreaterThan(0);
    }
  });

  it("donne à chaque étape un titre, un texte, et de quoi chercher (des mots ou un domaine)", () => {
    for (const t of THEMATIQUES) {
      for (const s of t.sections) {
        expect(s.titre, t.slug).not.toBe('');
        expect(s.texte, t.slug).not.toBe('');
        expect(Boolean(s.recherche || s.domaine), `${t.slug} / ${s.titre}`).toBe(true);
      }
    }
  });

  it('a au moins une collection mise en avant et une collection de saison', () => {
    expect(THEMATIQUES.some(t => t.enAvant)).toBe(true);
    expect(THEMATIQUES.some(t => t.saison)).toBe(true);
  });
});
