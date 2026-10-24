import { describe, expect, it } from 'vitest';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import pkg from '../../package.json';
import { dateLisible, lireChangelog } from '@/utils/nouveautes';

const lien = (h: string) => `([${h}](https://github.com/x/y/commit/${h}0000000000000000000000000000000000))`;

const EXEMPLE = [
  '# Changelog',
  '',
  '## [0.7.0](https://github.com/x/y/compare/v0.6.0...v0.7.0) (2026-10-24)',
  '',
  '',
  '### Features',
  '',
  `* collections personnelles (US-COMPTE-03) ${lien('fa2aacf')}`,
  `* collections personnelles (US-COMPTE-03) ${lien('369cdf5')}`,
  `* **export:** télécharger les données (US-EXPORT-01, US-EXPORT-02) ${lien('1f86166')}`,
  '',
  '### Bug Fixes',
  '',
  `* corriger la pagination ${lien('abcdef1')}`,
  '',
  '## 0.1.0 (2026-01-02)',
  '',
  '### Nouveautés',
  '',
  '* première version',
  '* texte avec lien ([#12](https://github.com/x/y/pull/12)), closes [#3](https://github.com/x/y/issues/3)',
  '',
  '### Maintenance',
  ''
].join('\n');

describe('lireChangelog', () => {
  it('découpe les versions, leurs dates et leurs sections, la plus récente en premier', () => {
    const versions = lireChangelog(EXEMPLE);

    expect(versions.map(v => [v.version, v.date])).toEqual([['0.7.0', '2026-10-24'], ['0.1.0', '2026-01-02']]);
    expect(versions[0].sections.map(s => s.titre)).toEqual(['Nouveautés', 'Corrections']);
  });

  it('présente les titres anglais de release-please comme les titres français', () => {
    const [v] = lireChangelog(['## 1.0.0 (2026-01-01)', '### Features', '* a', '### Bug Fixes', '* b', '### Performance Improvements', '* c', '### Reverts', '* d', '### Documentation', '* e', '### Autre', '* f'].join('\n'));

    expect(v.sections.map(s => s.titre)).toEqual(['Nouveautés', 'Corrections', 'Performances', 'Retraits', 'Documentation', 'Autre']);
  });

  it('retire les liens vers les commits et les tickets, garde la portée et la référence de la story', () => {
    const [v] = lireChangelog(EXEMPLE);

    expect(v.sections[0].elements).toContainEqual({ portee: 'export', texte: 'télécharger les données', reference: 'US-EXPORT-01, US-EXPORT-02' });
    expect(v.sections[1].elements).toEqual([{ portee: '', texte: 'corriger la pagination', reference: '' }]);
    const ancienne = lireChangelog(EXEMPLE)[1];
    expect(ancienne.sections[0].elements.map(e => e.texte)).toEqual(['première version', 'texte avec lien']);
  });

  it("fusionne les lignes identiques d'une version (commit de branche et commit de fusion)", () => {
    const [v] = lireChangelog(EXEMPLE);

    expect(v.sections[0].elements.filter(e => e.texte === 'collections personnelles')).toHaveLength(1);
  });

  it('garde la même ligne dans deux versions différentes', () => {
    const versions = lireChangelog(['## 2.0.0 (2026-02-01)', '### Features', '* a (US-A-1)', '## 1.0.0 (2026-01-01)', '### Features', '* a (US-A-1)'].join('\n'));

    expect(versions.map(v => v.sections[0].elements.length)).toEqual([1, 1]);
  });

  it('écarte les sections vides et les lignes hors section', () => {
    const versions = lireChangelog(['* orpheline', '## 1.0.0 (2026-01-01)', '* hors section', '### Maintenance', '', '### Features', '* a'].join('\n'));

    expect(versions[0].sections.map(s => s.titre)).toEqual(['Nouveautés']);
  });

  it('accepte une version sans date, des fins de ligne Windows et un texte vide', () => {
    expect(lireChangelog('## [1.2.3](https://x)\r\n### Features\r\n* a\r\n')).toEqual([
      { version: '1.2.3', date: '', sections: [{ titre: 'Nouveautés', elements: [{ portee: '', texte: 'a', reference: '' }] }] }
    ]);
    expect(lireChangelog('')).toEqual([]);
    expect(lireChangelog('# Changelog\n\nrien')).toEqual([]);
  });

  it('ne garde pas une ligne réduite à ses liens', () => {
    const [v] = lireChangelog(['## 1.0.0 (2026-01-01)', '### Features', `* ${lien('abc1234')}`, '* a'].join('\n'));

    expect(v.sections[0].elements.map(e => e.texte)).toEqual(['a']);
  });
});

describe('dateLisible', () => {
  it('écrit la date en français', () => {
    expect(dateLisible('2026-10-24')).toBe('24 octobre 2026');
  });

  it("rend la valeur telle quelle quand ce n'est pas une date", () => {
    expect(dateLisible('bientôt')).toBe('bientôt');
    expect(dateLisible('2026-13-45')).toBe('2026-13-45');
  });
});

// Le test sur le vrai fichier : il échoue si release-please change de format, ou si la page n'a plus rien à montrer
describe('CHANGELOG.md du dépôt', () => {
  const texte = readFileSync(resolve(process.cwd(), '../../CHANGELOG.md'), 'utf8');
  const versions = lireChangelog(texte);

  it("est injecté au build, tel qu'il est dans le dépôt", () => {
    expect(__CHANGELOG__).toBe(texte);
  });

  it('donne au moins une version, avec des lignes lisibles', () => {
    expect(versions.length).toBeGreaterThan(0);
    for (const v of versions) {
      expect(v.version).toMatch(/^\d+\.\d+\.\d+$/);
      expect(v.date).toMatch(/^\d{4}-\d{2}-\d{2}$/);
      expect(v.sections.length).toBeGreaterThan(0);
      for (const e of v.sections.flatMap(s => s.elements)) {
        expect(e.texte).not.toMatch(/\]\(https?:/);
        expect(e.texte.length).toBeGreaterThan(3);
      }
    }
  });

  it('ne répète pas une ligne dans une même version', () => {
    for (const v of versions) {
      const lignes = v.sections.flatMap(s => s.elements.map(e => `${s.titre}|${e.portee}|${e.texte}|${e.reference}`));
      expect(new Set(lignes).size).toBe(lignes.length);
    }
  });

  it('commence par une version qui ne dépasse pas celle du produit', () => {
    const [majeure, mineure, correctif] = versions[0].version.split('.').map(Number);
    const [pMajeure, pMineure, pCorrectif] = pkg.version.split('.').map(Number);

    expect(majeure * 1e6 + mineure * 1e3 + correctif).toBeLessThanOrEqual(pMajeure * 1e6 + pMineure * 1e3 + pCorrectif);
  });
});
