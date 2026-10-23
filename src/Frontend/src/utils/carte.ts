import { geoBounds, geoConicConformal, geoContains, geoMercator, geoPath } from 'd3-geo';
import type { GeoProjection } from 'd3-geo';
import type { Feature, FeatureCollection, Geometry } from 'geojson';

/** Une région ou un département du fond de carte (voir scripts/construire-fond-de-carte.py) */
export interface Territoire {
  code: string;
  nom: string;
  outreMer: boolean;
  /** Code de la région, pour un département */
  region?: string;
  geometry: Geometry;
}

export interface FondDeCarte {
  regions: Territoire[];
  departements: Territoire[];
}

export interface MuseeSurCarte {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
}

/** Un musée rangé dans le département dont le contour le contient */
export interface MuseePlace extends MuseeSurCarte {
  region: string;
  departement: string;
}

export type Niveau =
  | { type: 'france' }
  | { type: 'region'; code: string }
  | { type: 'departement'; code: string };

export interface Forme {
  code: string;
  nom: string;
  /** Tracé SVG */
  d: string;
  niveau: 'region' | 'departement';
}

export interface PointCarte {
  id: string;
  nom: string;
  x: number;
  y: number;
}

/** Encadré d'un territoire d'outre-mer, avec son nom */
export interface Cadre {
  x: number;
  y: number;
  largeur: number;
  hauteur: number;
  nom: string;
}

export interface VueCarte {
  formes: Forme[];
  points: PointCarte[];
  cadres: Cadre[];
}

export const LARGEUR = 800;
export const HAUTEUR = 700;
const MARGE = 20;

// Métropole à droite ; outre-mer en colonne à gauche, chaque territoire dans son encadré
const CADRE_METROPOLE = { x: 170, y: 0, largeur: LARGEUR - 170, hauteur: HAUTEUR };
const COTE_ENCADRE = { largeur: 150, hauteur: 128 };
const ESPACE_ENCADRE = 6;

const feature = (t: Territoire): Feature => ({ type: 'Feature', properties: { code: t.code }, geometry: t.geometry });
const collection = (ts: Territoire[]): FeatureCollection => ({ type: 'FeatureCollection', features: ts.map(feature) });

/** Projection qui cadre les territoires dans le rectangle : conique conforme pour la métropole, Mercator ailleurs */
function projeter(ts: Territoire[], x: number, y: number, largeur: number, hauteur: number, marge: number): GeoProjection {
  const projection = ts.some(t => t.outreMer)
    ? geoMercator()
    : geoConicConformal().parallels([44, 49]).rotate([-3, 0]);
  return projection.fitExtent([[x + marge, y + marge], [x + largeur - marge, y + hauteur - marge]], collection(ts));
}

// Les contours sont allégés : un musée du bord de mer peut tomber quelques centaines de mètres hors du trait. Tout
// musée à moins de cette distance (degrés, environ 5 km) d'un contour est rangé dans le département le plus proche.
const TOLERANCE_COTE = 0.05;

function distanceAuSegment(p: [number, number], a: number[], b: number[]): number {
  const dx = b[0] - a[0];
  const dy = b[1] - a[1];
  const longueur = dx * dx + dy * dy;
  const t = longueur === 0 ? 0 : Math.max(0, Math.min(1, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dy) / longueur));
  return Math.hypot(p[0] - (a[0] + t * dx), p[1] - (a[1] + t * dy));
}

/** Distance (degrés) du point au contour extérieur le plus proche du territoire */
function distanceAuContour(point: [number, number], geometry: Geometry): number {
  const polygones = geometry.type === 'MultiPolygon' ? geometry.coordinates : geometry.type === 'Polygon' ? [geometry.coordinates] : [];
  let mini = Infinity;
  for (const polygone of polygones) {
    const anneau = polygone[0];
    for (let i = 1; i < anneau.length; i++) mini = Math.min(mini, distanceAuSegment(point, anneau[i - 1], anneau[i]));
  }
  return mini;
}

/**
 * Range chaque musée dans son département, par le contour qui contient ses coordonnées ; à défaut, par le contour qui
 * les frôle (voir TOLERANCE_COTE). Un musée plus loin que cela de tout contour (coordonnées fausses) n'est pas placé.
 */
export function placerMusees(fond: FondDeCarte, musees: MuseeSurCarte[]): { places: MuseePlace[]; nonPlaces: number } {
  const candidats = fond.departements.map(d => ({ d, feature: feature(d), bornes: geoBounds(feature(d)) }));
  const places: MuseePlace[] = [];
  for (const musee of musees) {
    const point: [number, number] = [musee.longitude, musee.latitude];
    const dans = (marge: number) => candidats.filter(({ bornes: [[ouest, sud], [est, nord]] }) =>
      point[0] >= ouest - marge && point[0] <= est + marge && point[1] >= sud - marge && point[1] <= nord + marge);

    let trouve = dans(0).find(({ feature: f }) => geoContains(f, point))?.d;
    if (!trouve) {
      const proches = dans(TOLERANCE_COTE)
        .map(({ d }) => ({ d, distance: distanceAuContour(point, d.geometry) }))
        .filter(c => c.distance <= TOLERANCE_COTE)
        .sort((a, b) => a.distance - b.distance);
      trouve = proches[0]?.d;
    }
    if (trouve) places.push({ ...musee, region: trouve.region!, departement: trouve.code });
  }
  return { places, nonPlaces: musees.length - places.length };
}

/** Ce que la carte dessine au niveau demandé : les contours, les points des musées de la zone, les encadrés d'outre-mer */
export function composerVue(fond: FondDeCarte, places: MuseePlace[], niveau: Niveau): VueCarte {
  const formes: Forme[] = [];
  const points: PointCarte[] = [];
  const cadres: Cadre[] = [];

  const ajouter = (ts: Territoire[], kind: 'region' | 'departement', projection: GeoProjection, musees: MuseePlace[]) => {
    const chemin = geoPath(projection);
    for (const t of ts) formes.push({ code: t.code, nom: t.nom, d: chemin(feature(t)) ?? '', niveau: kind });
    for (const m of musees) {
      const xy = projection([m.longitude, m.latitude]);
      if (xy) points.push({ id: m.id, nom: m.name, x: xy[0], y: xy[1] });
    }
  };

  if (niveau.type === 'france') {
    const metropole = fond.regions.filter(r => !r.outreMer);
    const m = CADRE_METROPOLE;
    ajouter(metropole, 'region', projeter(metropole, m.x, m.y, m.largeur, m.hauteur, MARGE),
      places.filter(p => metropole.some(r => r.code === p.region)));

    fond.regions.filter(r => r.outreMer).forEach((region, i) => {
      const cadre = {
        x: 10, y: 10 + i * (COTE_ENCADRE.hauteur + ESPACE_ENCADRE),
        largeur: COTE_ENCADRE.largeur, hauteur: COTE_ENCADRE.hauteur, nom: region.nom
      };
      cadres.push(cadre);
      // Sous le nom, en haut de l'encadré
      ajouter([region], 'region', projeter([region], cadre.x, cadre.y + 14, cadre.largeur, cadre.hauteur - 14, 8),
        places.filter(p => p.region === region.code));
    });
  } else if (niveau.type === 'region') {
    const departements = fond.departements.filter(d => d.region === niveau.code);
    ajouter(departements, 'departement', projeter(departements, 0, 0, LARGEUR, HAUTEUR, MARGE),
      places.filter(p => p.region === niveau.code));
  } else {
    const departement = fond.departements.filter(d => d.code === niveau.code);
    ajouter(departement, 'departement', projeter(departement, 0, 0, LARGEUR, HAUTEUR, MARGE),
      places.filter(p => p.departement === niveau.code));
  }
  return { formes, points, cadres };
}
