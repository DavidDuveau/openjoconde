import axios from 'axios';
import { PaginatedResult, Artwork, ArtworkDetail } from '@/types/artwork';
import { Artist, NamedItem, Museum, MuseumLocations, RegionGeography } from '@/types/models';

// Création de l'instance axios avec la configuration de base
const http = axios.create({
  baseURL: import.meta.env.VUE_APP_API_URL || 'https://localhost:5001/api',
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  }
});

// Intercepteur pour gérer les erreurs globalement
http.interceptors.response.use(
  (response) => response,
  (error) => {
    // Log de l'erreur en console
    console.error('API Error:', error.response);
    return Promise.reject(error);
  }
);

// Méthodes pour accéder aux endpoints d'API
const api = {
  async getArtworks(page = 1, pageSize = 10): Promise<PaginatedResult<Artwork>> {
    const response = await http.get('/artworks', { params: { page, pageSize } });
    return response.data;
  },

  async getArtwork(id: string): Promise<ArtworkDetail> {
    const response = await http.get(`/artworks/${id}`);
    return response.data;
  },

  async searchArtworks(params: Record<string, unknown>): Promise<PaginatedResult<Artwork>> {
    const response = await http.get('/artworks', { params });
    return response.data;
  },

  async getArtists(page = 1, pageSize = 10, name?: string): Promise<PaginatedResult<Artist>> {
    const response = await http.get('/artists', { params: { page, pageSize, name: name || undefined } });
    return response.data;
  },

  async getArtist(id: string): Promise<Artist> {
    const response = await http.get(`/artists/${id}`);
    return response.data;
  },

  // Listes de référence des filtres : paginées, cherchées par une partie du nom
  async getDomains(name?: string, pageSize = 20, page = 1): Promise<PaginatedResult<NamedItem>> {
    const response = await http.get('/domains', { params: { name: name || undefined, page, pageSize } });
    return response.data;
  },

  async getTechniques(name?: string, pageSize = 20, page = 1): Promise<PaginatedResult<NamedItem>> {
    const response = await http.get('/techniques', { params: { name: name || undefined, page, pageSize } });
    return response.data;
  },

  async getPeriods(name?: string, pageSize = 20, page = 1): Promise<PaginatedResult<NamedItem>> {
    const response = await http.get('/periods', { params: { name: name || undefined, page, pageSize } });
    return response.data;
  },

  async getMuseums(page = 1, pageSize = 10, name?: string, region?: string, department?: string): Promise<PaginatedResult<Museum>> {
    const response = await http.get('/museums', {
      params: { page, pageSize, name: name || undefined, region: region || undefined, department: department || undefined }
    });
    return response.data;
  },

  // Régions et départements qui ont des musées, avec leur nombre de musées
  async getMuseumGeography(): Promise<RegionGeography[]> {
    const response = await http.get('/museums/geography');
    return response.data;
  },

  // Musées qui ont des coordonnées (nom et position), et nombre total de musées : de quoi dessiner la carte
  async getMuseumLocations(): Promise<MuseumLocations> {
    const response = await http.get('/museums/locations');
    return response.data;
  },

  async getMuseum(id: string): Promise<Museum> {
    const response = await http.get(`/museums/${id}`);
    return response.data;
  }
};

export default api;
