/** Ce qu'il faut pour situer un musée : des coordonnées, ou à défaut une adresse */
export interface LieuDeMusee {
  latitude?: number | null;
  longitude?: number | null;
  address?: string | null;
  zipCode?: string | null;
  city?: string | null;
}

const aDesCoordonnees = (lieu: LieuDeMusee) =>
  typeof lieu.latitude === 'number' && typeof lieu.longitude === 'number' && Number.isFinite(lieu.latitude) && Number.isFinite(lieu.longitude);

const adresseComplete = (lieu: LieuDeMusee) =>
  [lieu.address, [lieu.zipCode, lieu.city].filter(Boolean).join(' ')].filter(Boolean).join(', ');

/** Le musée sur la carte d'OpenStreetMap : par ses coordonnées, sinon par son adresse ; null s'il n'a ni l'un ni l'autre */
export function lienCarte(lieu: LieuDeMusee): string | null {
  if (aDesCoordonnees(lieu)) {
    const { latitude, longitude } = lieu;
    return `https://www.openstreetmap.org/?mlat=${latitude}&mlon=${longitude}#map=16/${latitude}/${longitude}`;
  }
  const adresse = adresseComplete(lieu);
  return adresse ? `https://www.openstreetmap.org/search?query=${encodeURIComponent(adresse)}` : null;
}

/** Itinéraire jusqu'au musée, depuis la position que l'utilisateur choisira sur le site de cartes ; sans destination précise, null */
export function lienItineraire(lieu: LieuDeMusee, service: 'osm' | 'google'): string | null {
  const destination = aDesCoordonnees(lieu) ? `${lieu.latitude},${lieu.longitude}` : adresseComplete(lieu);
  if (!destination) return null;
  return service === 'osm'
    ? `https://www.openstreetmap.org/directions?route=${encodeURIComponent(`;${destination}`)}`
    : `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(destination)}`;
}
