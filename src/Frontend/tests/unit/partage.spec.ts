import { describe, expect, it } from 'vitest';
import { adressePartage, decoderPartage, encoderPartage, ErreurPartage, LIMITE_PARTAGE, LONGUEUR_FRAGMENT } from '@/utils/partage';

const id = (n: number) => `00000000-0000-4000-8000-${String(n).padStart(12, '0')}`;

const base64Url = (texte: string) => btoa(String.fromCharCode(...new TextEncoder().encode(texte)))
  .replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');

describe('partage', () => {
  it('encode puis décode une collection, accents compris', () => {
    const collection = { nom: 'Été à Étretat', description: 'Vues de la falaise — « les Impressionnistes »', oeuvres: [id(1), id(2)] };

    const fragment = encoderPartage(collection)!;

    expect(fragment).toMatch(/^[A-Za-z0-9_-]+$/);
    expect(decoderPartage(fragment)).toEqual(collection);
    expect(decoderPartage(`#${fragment}`)).toEqual(collection);
  });

  it('refuse d’encoder plus de 100 œuvres, et accepte exactement 100', () => {
    const oeuvres = (n: number) => Array.from({ length: n }, (_, i) => id(i + 1));

    expect(encoderPartage({ nom: 'N', description: '', oeuvres: oeuvres(LIMITE_PARTAGE + 1) })).toBeNull();
    expect(encoderPartage({ nom: 'N', description: '', oeuvres: oeuvres(LIMITE_PARTAGE) })).not.toBeNull();
    expect(adressePartage({ nom: 'N', description: '', oeuvres: oeuvres(LIMITE_PARTAGE + 1) }, 'https://x.test', '/')).toBeNull();
  });

  it("construit l'adresse de la page de lecture, la collection étant après le #", () => {
    const adresse = adressePartage({ nom: 'N', description: '', oeuvres: [id(1)] }, 'https://x.test', '/')!;

    expect(adresse).toMatch(/^https:\/\/x\.test\/partage#[A-Za-z0-9_-]+$/);
    expect(adressePartage({ nom: 'N', description: '', oeuvres: [] }, 'https://x.test', '/app/')).toMatch(/^https:\/\/x\.test\/app\/partage#/);
  });

  it('refuse un lien vide, trop long ou aux caractères interdits', () => {
    expect(() => decoderPartage('')).toThrow(ErreurPartage);
    expect(() => decoderPartage('#')).toThrow(/aucune collection/);
    expect(() => decoderPartage('A'.repeat(LONGUEUR_FRAGMENT + 1))).toThrow(/trop long/);
    expect(() => decoderPartage('<script>alert(1)</script>')).toThrow(/valide/);
  });

  it('refuse du contenu qui n’est pas une collection', () => {
    const cas = [
      'AAAA',
      base64Url('pas du json'),
      base64Url('null'),
      base64Url(JSON.stringify({ v: 2, n: 'N', d: '', o: [] })),
      base64Url(JSON.stringify({ v: 1, n: '', d: '', o: [] })),
      base64Url(JSON.stringify({ v: 1, n: 'x'.repeat(81), d: '', o: [] })),
      base64Url(JSON.stringify({ v: 1, n: 'N', d: 'y'.repeat(501), o: [] })),
      base64Url(JSON.stringify({ v: 1, n: 'N', d: '', o: 'pas une liste' })),
      base64Url(JSON.stringify({ v: 1, n: 5, d: '', o: [] })),
      base64Url(JSON.stringify({ v: 1, n: 'N', d: '', o: ['zz'] })),
      base64Url(JSON.stringify({ v: 1, n: 'N', d: '', o: [42] })),
      base64Url(JSON.stringify({ v: 1, n: 'N', d: '', o: Array.from({ length: 101 }, (_, i) => id(i).replace(/-/g, '')) }))
    ];

    for (const fragment of cas) expect(() => decoderPartage(fragment)).toThrow(ErreurPartage);
  });

  it('refuse des octets qui ne sont pas de l’UTF-8', () => {
    expect(() => decoderPartage('_-8')).toThrow(ErreurPartage);
  });

  it('écarte les doublons et garde le texte tel quel, sans l’interpréter', () => {
    const fragment = base64Url(JSON.stringify({ v: 1, n: '<img src=x onerror=alert(1)>', d: '', o: [id(1).replace(/-/g, ''), id(1).replace(/-/g, '')] }));

    expect(decoderPartage(fragment)).toEqual({ nom: '<img src=x onerror=alert(1)>', description: '', oeuvres: [id(1)] });
  });
});
