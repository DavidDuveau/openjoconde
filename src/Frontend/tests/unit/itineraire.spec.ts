import { describe, expect, it } from 'vitest';
import { lienCarte, lienItineraire } from '@/utils/itineraire';

const avecCoordonnees = { latitude: 47.658, longitude: -2.76, address: '1 rue du Port', zipCode: '56000', city: 'Vannes' };
const avecAdresse = { address: '1 rue du Port', zipCode: '56000', city: 'Vannes' };

describe('lienCarte', () => {
  it('place le musée par ses coordonnées', () => {
    expect(lienCarte(avecCoordonnees)).toBe('https://www.openstreetmap.org/?mlat=47.658&mlon=-2.76#map=16/47.658/-2.76');
  });

  it("cherche l'adresse, encodée, quand il n'y a pas de coordonnées", () => {
    expect(lienCarte(avecAdresse)).toBe('https://www.openstreetmap.org/search?query=1%20rue%20du%20Port%2C%2056000%20Vannes');
  });

  it("tolère une adresse sans rue, ou des coordonnées partielles ou absurdes", () => {
    expect(lienCarte({ city: 'Albi' })).toBe('https://www.openstreetmap.org/search?query=Albi');
    expect(lienCarte({ latitude: 47.6, longitude: null, city: 'Albi' })).toContain('search?query=Albi');
    expect(lienCarte({ latitude: Number.NaN, longitude: 2, city: 'Albi' })).toContain('search?query=Albi');
  });

  it("ne propose rien d'un musée sans coordonnées ni adresse", () => {
    expect(lienCarte({})).toBeNull();
    expect(lienCarte({ address: '', zipCode: null, city: undefined })).toBeNull();
  });
});

describe('lienItineraire', () => {
  it("mène aux coordonnées du musée, sur OpenStreetMap comme sur Google Maps", () => {
    expect(lienItineraire(avecCoordonnees, 'osm')).toBe('https://www.openstreetmap.org/directions?route=%3B47.658%2C-2.76');
    expect(lienItineraire(avecCoordonnees, 'google')).toBe('https://www.google.com/maps/dir/?api=1&destination=47.658%2C-2.76');
  });

  it("se rabat sur l'adresse quand il n'y a pas de coordonnées", () => {
    expect(lienItineraire(avecAdresse, 'osm')).toContain(encodeURIComponent(';1 rue du Port, 56000 Vannes'));
    expect(lienItineraire(avecAdresse, 'google')).toContain(encodeURIComponent('1 rue du Port, 56000 Vannes'));
  });

  it("ne propose pas d'itinéraire sans destination", () => {
    expect(lienItineraire({}, 'osm')).toBeNull();
    expect(lienItineraire({}, 'google')).toBeNull();
  });
});
