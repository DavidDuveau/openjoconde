import { describe, expect, it } from 'vitest';
import { GLOSSAIRE, decouper } from '@/utils/glossaire';

describe('decouper', () => {
  it('rend le texte brut quand il ne contient aucun terme du glossaire', () => {
    expect(decouper('Paysage breton')).toEqual([{ texte: 'Paysage breton' }]);
  });

  it('isole un terme et lui donne sa définition, en gardant le texte autour', () => {
    expect(decouper('Dessin à la sanguine, vers 1700')).toEqual([
      { texte: 'Dessin', definition: GLOSSAIRE.dessin },
      { texte: ' à la ' },
      { texte: 'sanguine', definition: GLOSSAIRE.sanguine },
      { texte: ', vers 1700' }
    ]);
  });

  it('préfère le terme le plus long : « huile sur toile » et non « huile »', () => {
    expect(decouper('huile sur toile')).toEqual([{ texte: 'huile sur toile', definition: GLOSSAIRE['huile sur toile'] }]);
  });

  it('ignore la casse et les accents, et garde le texte tel qu’il est écrit', () => {
    expect(decouper('FAÏENCE')).toEqual([{ texte: 'FAÏENCE', definition: GLOSSAIRE.faience }]);
    expect(decouper('Tempéra')).toEqual([{ texte: 'Tempéra', definition: GLOSSAIRE.tempera }]);
  });

  it('ne reconnaît pas un terme au milieu d’un mot', () => {
    expect(decouper('huileux')).toEqual([{ texte: 'huileux' }]);
    expect(decouper('porcelainier')).toEqual([{ texte: 'porcelainier' }]);
  });

  it('rend une liste vide pour un texte vide', () => {
    expect(decouper('')).toEqual([]);
  });
});
