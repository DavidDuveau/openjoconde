import { describe, expect, it } from 'vitest';
import { displayTitle, UNTITLED } from '@/utils/title';

describe('displayTitle', () => {
  it('rend « Sans titre » pour un titre absent, vide ou fait de ponctuation', () => {
    expect(displayTitle(null)).toBe(UNTITLED);
    expect(displayTitle(undefined)).toBe(UNTITLED);
    expect(displayTitle('   ')).toBe(UNTITLED);
    expect(displayTitle(' - . ; ')).toBe(UNTITLED);
  });

  it('retire la ponctuation initiale et resserre les espaces', () => {
    expect(displayTitle('- Ah ! docteur…')).toBe('Ah ! docteur…');
    expect(displayTitle('«  Les   Baigneuses »')).toBe('Les Baigneuses »');
  });

  it('garde un titre court tel quel', () => {
    expect(displayTitle('Portrait de femme')).toBe('Portrait de femme');
  });

  it('coupe un long titre sur une limite de mot, avec une ellipse', () => {
    const long = 'mot '.repeat(60).trim();

    const court = displayTitle(long, 50);

    expect(court.endsWith('…')).toBe(true);
    expect(court.length).toBeLessThanOrEqual(51);
    expect(court.slice(0, -1).endsWith(' ')).toBe(false);
    expect(court.slice(0, -1).split(' ').every(m => m === 'mot')).toBe(true);
  });

  it('ne coupe pas quand la longueur maximale est infinie', () => {
    const long = 'x '.repeat(500).trim();

    expect(displayTitle(long, Infinity)).toBe(long);
  });
});
