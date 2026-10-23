import { describe, expect, it } from 'vitest';
import { citer, dateEnLettres, initiales } from '@/utils/citation';

const oeuvre = (surcharge: object = {}) => ({
  id: 'a1', title: 'Les Baigneuses', reference: '000PE001234', inventoryNumber: 'RF 1234', denomination: 'tableau',
  creationDate: '1873', museum: { id: 'm1', name: "Musée d'Orsay" },
  artists: [{ id: 'p1', firstName: 'Claude', lastName: 'Monet', role: null }], domains: [], techniques: [], periods: [],
  ...surcharge
}) as never;

const JOUR = new Date(2026, 9, 23);
const NOTICE = 'https://www.pop.culture.gouv.fr/notice/joconde/000PE001234';

describe('dateEnLettres', () => {
  it('écrit la date en toutes lettres, « 1er » pour le premier du mois', () => {
    expect(dateEnLettres(new Date(2026, 9, 23))).toBe('23 octobre 2026');
    expect(dateEnLettres(new Date(2027, 2, 1))).toBe('1er mars 2027');
  });
});

describe('initiales', () => {
  it.each([
    ['Claude', 'C.'],
    ['Alexandre Théodore', 'A. T.'],
    ['Jean-François', 'J.-F.'],
    ['  élodie  ', 'É.'],
    ['', '']
  ])('%s -> %s', (prenom, attendu) => {
    expect(initiales(prenom)).toBe(attendu);
  });
});

describe('citer', () => {
  it('ISO 690 : auteur en capitales, titre, genre, date, lieu de conservation, source, adresse et consultation', () => {
    expect(citer(oeuvre(), 'iso690', JOUR)).toBe(
      `MONET, Claude. Les Baigneuses [tableau]. 1873. Musée d'Orsay, n° d'inventaire RF 1234. In : Base Joconde, ministère de la Culture [en ligne]. Notice 000PE001234. Disponible à l'adresse : ${NOTICE} (consulté le 23 octobre 2026).`
    );
  });

  it('APA : nom et initiales, date entre parenthèses, source et consultation', () => {
    expect(citer(oeuvre(), 'apa', JOUR)).toBe(
      `Monet, C. (1873). Les Baigneuses [tableau]. Musée d'Orsay, n° d'inventaire RF 1234. Base Joconde, ministère de la Culture, notice 000PE001234. ${NOTICE} (consulté le 23 octobre 2026)`
    );
  });

  it('MLA : nom, prénom, titre, date, lieu, source et consultation', () => {
    expect(citer(oeuvre(), 'mla', JOUR)).toBe(
      `Monet, Claude. Les Baigneuses. 1873. Musée d'Orsay, n° d'inventaire RF 1234. Base Joconde, ministère de la Culture, notice 000PE001234, ${NOTICE}. Consulté le 23 octobre 2026.`
    );
  });

  it("utilise ISO 690 pour un format inconnu", () => {
    expect(citer(oeuvre(), 'autre' as never, JOUR)).toBe(citer(oeuvre(), 'iso690', JOUR));
  });

  it('plusieurs artistes : ISO sépare par « ; », APA par « et », MLA garde le premier avec « et al »', () => {
    const deux = oeuvre({
      artists: [
        { id: 'p1', firstName: 'Claude', lastName: 'Monet', role: null },
        { id: 'p2', firstName: 'Jean-François', lastName: 'Millet', role: null }
      ]
    });

    expect(citer(deux, 'iso690', JOUR)).toContain('MONET, Claude ; MILLET, Jean-François. Les Baigneuses');
    expect(citer(deux, 'apa', JOUR)).toContain('Monet, C. et Millet, J.-F. (1873).');
    expect(citer(deux, 'mla', JOUR)).toMatch(/^Monet, Claude, et al\. Les Baigneuses\./);
  });

  it('trois artistes : APA relie les deux derniers par « et »', () => {
    const trois = oeuvre({
      artists: ['A', 'B', 'C'].map((n, i) => ({ id: `p${i}`, firstName: n, lastName: `Nom${n}`, role: null }))
    });

    expect(citer(trois, 'apa', JOUR)).toContain('NomA, A., NomB, B. et NomC, C. (1873).');
  });

  it('plus de trois artistes : les trois premiers en ISO, le premier et « et al. » en APA', () => {
    const cinq = oeuvre({
      artists: ['A', 'B', 'C', 'D', 'E'].map((n, i) => ({ id: `p${i}`, firstName: n, lastName: `Nom${n}`, role: null }))
    });

    expect(citer(cinq, 'iso690', JOUR)).toContain('NOMA, A ; NOMB, B ; NOMC, C et al. Les Baigneuses');
    expect(citer(cinq, 'iso690', JOUR)).not.toContain('NOMD');
    expect(citer(cinq, 'apa', JOUR)).toContain('NomA, A., et al. (1873).');
  });

  it("sans artiste : le titre ouvre la référence, sans auteur inventé", () => {
    const anonyme = oeuvre({ artists: [] });

    expect(citer(anonyme, 'iso690', JOUR)).toMatch(/^Les Baigneuses \[tableau\]\. 1873\./);
    expect(citer(anonyme, 'apa', JOUR)).toMatch(/^Les Baigneuses \[tableau\]\. \(1873\)\./);
    expect(citer(anonyme, 'mla', JOUR)).toMatch(/^Les Baigneuses\. 1873\./);
  });

  it("omet ce que la notice ne donne pas : date, musée, inventaire, genre, prénom, titre", () => {
    const pauvre = oeuvre({
      title: null, denomination: 'Œuvre sans dénomination', creationDate: null, museum: null, inventoryNumber: null,
      artists: [{ id: 'p1', firstName: null, lastName: 'Monet', role: null }]
    });

    expect(citer(pauvre, 'iso690', JOUR)).toBe(
      `MONET. Sans titre. In : Base Joconde, ministère de la Culture [en ligne]. Notice 000PE001234. Disponible à l'adresse : ${NOTICE} (consulté le 23 octobre 2026).`
    );
    expect(citer(pauvre, 'apa', JOUR)).toContain('Monet. (s. d.). Sans titre.');
    expect(citer(pauvre, 'mla', JOUR)).toMatch(/^Monet\. Sans titre\. Base Joconde/);
  });

  it("n'indique que l'inventaire quand le musée est inconnu, et que le musée sans inventaire", () => {
    expect(citer(oeuvre({ museum: null }), 'iso690', JOUR)).toContain("1873. n° d'inventaire RF 1234. In :");
    expect(citer(oeuvre({ inventoryNumber: null }), 'iso690', JOUR)).toContain("1873. Musée d'Orsay. In :");
  });

  it("ne cite pas d'adresse quand la référence n'en permet pas", () => {
    const sansAdresse = oeuvre({ reference: 'réf. étrange/1' });

    const iso = citer(sansAdresse, 'iso690', JOUR);

    expect(iso).not.toContain('Disponible à l\'adresse');
    expect(iso).toContain('Notice réf. étrange/1. (consulté le 23 octobre 2026).');
    expect(citer(sansAdresse, 'apa', JOUR)).toMatch(/notice réf\. étrange\/1\. \(consulté le 23 octobre 2026\)$/);
    expect(citer(sansAdresse, 'mla', JOUR)).toContain('notice réf. étrange/1. Consulté le');
  });

  it("nettoie le titre brut de Joconde comme la fiche le fait", () => {
    expect(citer(oeuvre({ title: ' - Ah ! docteur… / N°18.' }), 'mla', JOUR)).toContain('Ah ! docteur… / N°18.');
  });
});
