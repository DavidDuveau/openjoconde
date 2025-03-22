import api from './api';
import { PaginatedResult, Artwork, ArtworkDetail } from '@/types/artwork';

export interface ArtworkSearchParams {
  page?: number;
  pageSize?: number;
  search?: string;
  domain?: string;
  period?: string;
  technique?: string;
  museum?: string;
  artist?: string;
}

export const artworkService = {
  /**
   * Récupère une liste paginée d'œuvres d'art avec filtres optionnels
   */
  async getArtworks(params: ArtworkSearchParams = {}): Promise<PaginatedResult<Artwork>> {
    const response = await api.get('/artworks', { params });
    return response.data;
  },

  /**
   * Récupère les détails d'une œuvre d'art par son identifiant
   */
  async getArtworkById(id: string): Promise<ArtworkDetail> {
    const response = await api.get(`/artworks/${id}`);
    return response.data;
  },

  /**
   * Récupère des œuvres similaires à une œuvre donnée
   */
  async getSimilarArtworks(id: string, limit: number = 4): Promise<Artwork[]> {
    const response = await api.get(`/artworks/${id}/similar`, { params: { limit } });
    return response.data;
  },

  /**
   * Recherche des œuvres d'art par terme de recherche
   */
  async searchArtworks(term: string, limit: number = 10): Promise<Artwork[]> {
    const response = await api.get('/artworks/search', { params: { term, limit } });
    return response.data;
  }
};
