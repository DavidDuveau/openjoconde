import { describe, expect, it } from 'vitest';
import fond from '@/assets/geo/france.json';
import { FondDeCarte, HAUTEUR, LARGEUR, MuseeSurCarte, composerVue, placerMusees } from '@/utils/carte';

const france = fond as unknown as FondDeCarte;

const musee = (id: string, latitude: number, longitude: number): MuseeSurCarte => ({ id, name: id, latitude, longitude });
const PARIS = musee('Paris', 48.8566, 2.3522);
const VANNES = musee('Vannes', 47.658, -2.76);
const AJACCIO = musee('Ajaccio', 41.919, 8.738);
const SAINT_DENIS = musee('Saint-Denis', -20.879, 55.448);
const POINTE_A_PITRE = musee('Pointe-à-Pitre', 16.241, -61.533);
const EN_MER = musee('En mer', 0, 0);
const tous = [PARIS, VANNES, AJACCIO, SAINT_DENIS, POINTE_A_PITRE, EN_MER];

describe('fond de carte', () => {
  it('compte 18 régions et 101 départements, outre-mer compris, chaque département dans une région connue', () => {
    expect(france.regions).toHaveLength(18);
    expect(france.departements).toHaveLength(101);
    const codes = new Set(france.regions.map(r => r.code));
    expect(france.departements.every(d => d.region && codes.has(d.region))).toBe(true);
    expect(france.regions.filter(r => r.outreMer).map(r => r.nom).sort())
      .toEqual(['Guadeloupe', 'Guyane', 'La Réunion', 'Martinique', 'Mayotte']);
  });
});

describe('placerMusees', () => {
  it('range chaque musée dans le département et la région dont le contour le contient', () => {
    const { places } = placerMusees(france, tous);

    const range = Object.fromEntries(places.map(p => [p.id, [p.departement, p.region]]));
    expect(range).toEqual({
      Paris: ['75', '11'], Vannes: ['56', '53'], Ajaccio: ['2A', '94'], 'Saint-Denis': ['974', '974'], 'Pointe-à-Pitre': ['971', '971']
    });
  });

  it('ne place pas un musée dont les coordonnées tombent hors de tout contour, et le compte', () => {
    const resultat = placerMusees(france, tous);

    expect(resultat.places.map(p => p.id)).not.toContain('En mer');
    expect(resultat.nonPlaces).toBe(1);
  });
});

describe('composerVue', () => {
  const { places } = placerMusees(france, tous);

  it("au niveau France : 13 régions de métropole, 5 territoires d'outre-mer encadrés, un point par musée placé", () => {
    const vue = composerVue(france, places, { type: 'france' });

    expect(vue.formes).toHaveLength(18);
    expect(vue.formes.every(f => f.niveau === 'region' && f.d.startsWith('M'))).toBe(true);
    expect(vue.cadres.map(c => c.nom).sort()).toEqual(['Guadeloupe', 'Guyane', 'La Réunion', 'Martinique', 'Mayotte']);
    expect(vue.points.map(p => p.id).sort()).toEqual(['Ajaccio', 'Paris', 'Pointe-à-Pitre', 'Saint-Denis', 'Vannes']);
  });

  it("place chaque point dans le dessin : la métropole dans son cadre, un musée d'outre-mer dans l'encadré de son territoire", () => {
    const vue = composerVue(france, places, { type: 'france' });
    const point = (id: string) => vue.points.find(p => p.id === id)!;

    for (const id of ['Paris', 'Vannes', 'Ajaccio']) {
      expect(point(id).x).toBeGreaterThan(170);
      expect(point(id).x).toBeLessThan(LARGEUR);
      expect(point(id).y).toBeGreaterThan(0);
      expect(point(id).y).toBeLessThan(HAUTEUR);
    }
    // Paris au nord et à l'est de Vannes, Ajaccio au sud-est
    expect(point('Paris').y).toBeLessThan(point('Vannes').y);
    expect(point('Paris').x).toBeGreaterThan(point('Vannes').x);
    expect(point('Ajaccio').y).toBeGreaterThan(point('Paris').y);

    const encadre = (nom: string) => vue.cadres.find(c => c.nom === nom)!;
    for (const [id, nom] of [['Saint-Denis', 'La Réunion'], ['Pointe-à-Pitre', 'Guadeloupe']]) {
      const cadre = encadre(nom);
      expect(point(id).x).toBeGreaterThan(cadre.x);
      expect(point(id).x).toBeLessThan(cadre.x + cadre.largeur);
      expect(point(id).y).toBeGreaterThan(cadre.y);
      expect(point(id).y).toBeLessThan(cadre.y + cadre.hauteur);
    }
  });

  it('au niveau région : les départements de la région seulement, avec les musées de la région', () => {
    const vue = composerVue(france, places, { type: 'region', code: '53' });

    expect(vue.formes.map(f => f.nom).sort()).toEqual(["Côtes-d'Armor", 'Finistère', 'Ille-et-Vilaine', 'Morbihan']);
    expect(vue.formes.every(f => f.niveau === 'departement')).toBe(true);
    expect(vue.points.map(p => p.id)).toEqual(['Vannes']);
    expect(vue.cadres).toEqual([]);
  });

  it('au niveau département : le contour du département et ses musées', () => {
    const vue = composerVue(france, places, { type: 'departement', code: '75' });

    expect(vue.formes.map(f => f.nom)).toEqual(['Paris']);
    expect(vue.points.map(p => p.id)).toEqual(['Paris']);
    expect(vue.points[0].x).toBeGreaterThan(0);
    expect(vue.points[0].x).toBeLessThan(LARGEUR);
  });

  it("une région d'outre-mer se zoome comme les autres : un seul département, son musée", () => {
    const vue = composerVue(france, places, { type: 'region', code: '974' });

    expect(vue.formes.map(f => f.nom)).toEqual(['La Réunion']);
    expect(vue.points.map(p => p.id)).toEqual(['Saint-Denis']);
  });
});
