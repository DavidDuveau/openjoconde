import { afterEach, describe, expect, it, vi } from 'vitest';
import { celluleCsv, donneesOeuvre, nomDeFichier, telecharger, versCsv, versJson } from '@/utils/exportOeuvre';

const oeuvre = (surcharge: object = {}) => ({
  id: 'a1', title: 'Les Baigneuses', reference: '000PE001234', inventoryNumber: 'RF 1234', denomination: 'tableau',
  description: 'Deux "baigneuses"; au bord de l\'eau', dimensions: 'H. 40 cm', creationDate: '1873', creationPlace: 'Argenteuil',
  conservationPlace: 'salle 12', copyright: '© ADAGP', associatedSiteUrl: null, artistUnderCopyright: true, publicDomainYear: 2040,
  hasImage: null, museum: { id: 'm1', name: "Musée d'Orsay" },
  artists: [{ id: 'p1', firstName: 'Claude', lastName: 'Monet', role: 'peintre' }, { id: 'p2', firstName: null, lastName: 'Atelier', role: null }],
  domains: [{ id: 'd1', name: 'peinture' }], techniques: [{ id: 't1', name: 'huile' }, { id: 't2', name: 'toile' }],
  periods: [{ id: 'e1', name: '19e siècle' }], ...surcharge
}) as never;

const JOUR = new Date(2026, 9, 23);

describe('donneesOeuvre', () => {
  it("reprend les champs de la fiche, avec la source, la licence et la date de consultation", () => {
    const d = donneesOeuvre(oeuvre(), JOUR);

    expect(d).toMatchObject({
      reference: '000PE001234', titre: 'Les Baigneuses', musee: "Musée d'Orsay", domaines: ['peinture'], techniques: ['huile', 'toile'],
      artistes: [{ nom: 'Claude Monet', role: 'peintre' }, { nom: 'Atelier', role: null }],
      droits: { mention: '© ADAGP', artisteSousDroits: true, anneeDomainePublic: 2040 },
      source: {
        base: 'Base Joconde, ministère de la Culture', licence: 'Licence Ouverte 2.0', consulteLe: '23 octobre 2026',
        notice: 'https://www.pop.culture.gouv.fr/notice/joconde/000PE001234'
      }
    });
    expect(d.source.adresseLicence).toContain('licence-ouverte');
  });

  it("rend null le musée absent, et pas d'adresse de notice pour une référence qui n'en permet pas", () => {
    const d = donneesOeuvre(oeuvre({ museum: null, reference: 'réf/1' }), JOUR);

    expect(d.musee).toBeNull();
    expect(d.source.notice).toBeNull();
  });

  it("prend la date du jour sans date donnée", () => {
    expect(donneesOeuvre(oeuvre()).source.consulteLe).toMatch(/^\d{1,2}(er)? \p{L}+ \d{4}$/u);
  });
});

describe('versJson', () => {
  it("donne un JSON lisible qui se relit à l'identique", () => {
    const texte = versJson(oeuvre(), JOUR);

    expect(texte.endsWith('\n')).toBe(true);
    expect(JSON.parse(texte)).toEqual(JSON.parse(JSON.stringify(donneesOeuvre(oeuvre(), JOUR))));
    expect(texte).toContain('\n  "reference": "000PE001234"');
  });
});

describe('celluleCsv', () => {
  it.each([
    ['simple', '"simple"'],
    ['a "b" c', '"a ""b"" c"'],
    ['a;b\nc', '"a;b\nc"'],
    [null, '""'],
    [undefined, '""'],
    [1873, '"1873"'],
    [true, '"true"']
  ])('%j -> %s', (valeur, attendu) => {
    expect(celluleCsv(valeur as never)).toBe(attendu);
  });

  it.each(['=1+1', '+33 1 23', '-2+3', '@SUM(A1)', '\tcmd', '\rcmd'])(
    "neutralise la formule %j par une apostrophe",
    (valeur) => {
      expect(celluleCsv(valeur)).toBe(`"'${valeur}"`);
    }
  );

  it("ne touche pas un tiret ou un signe qui n'est pas en tête", () => {
    expect(celluleCsv('Paris - Lyon')).toBe('"Paris - Lyon"');
  });
});

describe('versCsv', () => {
  const lignes = (texte: string) => texte.replace('\uFEFF', '').split('\r\n').filter(Boolean);

  it("commence par la marque UTF-8, puis un en-tête et une ligne de données de même largeur, séparées par « ; »", () => {
    const csv = versCsv(oeuvre(), JOUR);

    expect(csv.startsWith('\uFEFF')).toBe(true);
    const [entete, donnees] = lignes(csv);
    expect(entete.startsWith('"reference";"titre";"denomination"')).toBe(true);
    expect(entete.split('";"')).toHaveLength(21);
    expect(donnees.startsWith('"000PE001234";"Les Baigneuses";"tableau";"Claude Monet | Atelier"')).toBe(true);
    expect(csv.endsWith('\r\n')).toBe(true);
  });

  it("joint les listes par « | », double les guillemets et garde les accents", () => {
    const donnees = lignes(versCsv(oeuvre(), JOUR))[1];

    expect(donnees).toContain('"huile | toile"');
    expect(donnees).toContain('"Deux ""baigneuses""; au bord de l\'eau"');
    expect(donnees).toContain("\"Musée d'Orsay\"");
    expect(donnees).toContain('"23 octobre 2026"');
  });

  it("neutralise une notice qui commencerait par une formule", () => {
    const donnees = lignes(versCsv(oeuvre({ title: '=HYPERLINK("http://mal.example")' }), JOUR))[1];

    expect(donnees).toContain('"\'=HYPERLINK(""http://mal.example"")"');
  });

  it("laisse vides les champs absents", () => {
    const donnees = lignes(versCsv(oeuvre({ dimensions: null, museum: null, creationPlace: null }), JOUR))[1];

    // date, lieu de création vide, dimensions vides, puis l'inventaire
    expect(donnees).toContain('"1873";"";"";"RF 1234"');
    expect(donnees).toContain('"RF 1234";"";"salle 12"');
  });
});

describe('nomDeFichier', () => {
  it.each([
    ['000PE001234', 'json', 'joconde-000PE001234.json'],
    ['réf. étrange/1', 'csv', 'joconde-r_f_trange_1.csv'],
    ['../../etc', 'json', 'joconde-etc.json'],
    ['', 'csv', 'joconde-oeuvre.csv'],
    ['///', 'csv', 'joconde-oeuvre.csv']
  ])('%j -> %s', (reference, extension, attendu) => {
    expect(nomDeFichier(oeuvre({ reference }), extension as 'json' | 'csv')).toBe(attendu);
  });
});

describe('telecharger', () => {
  afterEach(() => vi.restoreAllMocks());

  it("crée un lien de téléchargement du contenu, le clique, puis libère l'adresse", async () => {
    let blob: Blob | undefined;
    const creer = vi.fn((b: Blob) => { blob = b; return 'blob:test'; });
    const revoquer = vi.fn();
    Object.assign(URL, { createObjectURL: creer, revokeObjectURL: revoquer });
    const clic = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      expect(this.download).toBe('joconde-x.json');
      expect(this.href).toBe('blob:test');
    });

    telecharger('joconde-x.json', '{"a":1}', 'application/json');

    expect(clic).toHaveBeenCalledTimes(1);
    expect(blob!.type).toBe('application/json;charset=utf-8');
    // jsdom n'a pas Blob.text() : on relit le contenu comme le ferait un fichier
    const contenu = await new Promise<string>(resolve => {
      const lecteur = new FileReader();
      lecteur.onload = () => resolve(String(lecteur.result));
      lecteur.readAsText(blob!);
    });
    expect(contenu).toBe('{"a":1}');
    expect(revoquer).toHaveBeenCalledWith('blob:test');
    expect(document.querySelector('a[download]')).toBeNull();
  });
});
