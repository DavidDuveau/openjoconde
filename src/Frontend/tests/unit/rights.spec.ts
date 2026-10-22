import { describe, expect, it } from 'vitest';
import { rightsSummary, safeWebUrl } from '@/utils/rights';

describe('rightsSummary', () => {
  it("annonce le domaine public quand l'année est passée", () => {
    expect(rightsSummary(null, 1661, 2026)).toBe('Dans le domaine public depuis 1661.');
    expect(rightsSummary(true, 2026, 2026)).toBe('Dans le domaine public depuis 2026.');
  });

  it("annonce l'entrée future dans le domaine public", () => {
    expect(rightsSummary(true, 2085, 2026)).toBe(
      "Artiste sous droits d'auteur ; l'œuvre entrera dans le domaine public en 2085."
    );
  });

  it("donne la priorité à l'année sur l'indicateur (la source se contredit parfois)", () => {
    expect(rightsSummary(true, 1900, 2026)).toBe('Dans le domaine public depuis 1900.');
  });

  it("dit ce que la notice dit de l'artiste, sans année", () => {
    expect(rightsSummary(true, null, 2026)).toBe("Artiste sous droits d'auteur.");
    expect(rightsSummary(false, undefined, 2026)).toBe("Artiste non sous droits d'auteur, selon la notice.");
  });

  it("ne rend rien quand la notice n'en dit rien", () => {
    expect(rightsSummary(null, null, 2026)).toBeNull();
    expect(rightsSummary(undefined, undefined, 2026)).toBeNull();
  });
});

describe('safeWebUrl', () => {
  it('accepte http et https', () => {
    expect(safeWebUrl('https://example.org/musee')).toBe('https://example.org/musee');
    expect(safeWebUrl('HTTP://example.org')).toBe('HTTP://example.org');
  });

  it('refuse les autres schémas et les valeurs vides', () => {
    expect(safeWebUrl('javascript:alert(1)')).toBeNull();
    expect(safeWebUrl('data:text/html,<b>x</b>')).toBeNull();
    expect(safeWebUrl('//example.org')).toBeNull();
    expect(safeWebUrl('')).toBeNull();
    expect(safeWebUrl(null)).toBeNull();
  });
});
