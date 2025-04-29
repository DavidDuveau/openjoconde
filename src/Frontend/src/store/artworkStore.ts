import { defineStore } from 'pinia';
import { Artwork, SearchParams, PaginatedResult } from '@/types/models';
import ApiService from '@/services/api';

interface ArtworkState {
  artworks: Artwork[];
  currentArtwork: Artwork | null;
  loading: boolean;
  error: string | null;
  totalCount: number;
  currentPage: number;
  pageSize: number;
  searchParams: SearchParams;
}

export const useArtworkStore = defineStore('artwork', {
  state: (): ArtworkState => ({
    artworks: [],
    currentArtwork: null,
    loading: false,
    error: null,
    totalCount: 0,
    currentPage: 1,
    pageSize: 10,
    searchParams: {
      page: 1,
      pageSize: 10,
      sortBy: 'relevance'
    }
  }),
  
  getters: {
    getArtworkById: (state) => (id: string) => {
      return state.artworks.find(artwork => artwork.id === id);
    },
    totalPages: (state) => {
      return Math.ceil(state.totalCount / state.pageSize);
    }
  },
  
  actions: {
    async fetchArtworks(page = 1, pageSize = 10) {
      this.loading = true;
      try {
        const result = await ApiService.getArtworks(page, pageSize);
        this.artworks = result.items;
        this.totalCount = result.totalCount;
        this.currentPage = result.page;
        this.pageSize = result.pageSize;
        this.error = null;
      } catch (error) {
        this.artworks = [];
        this.error = 'Erreur lors du chargement des œuvres d\'art';
        console.error(error);
      } finally {
        this.loading = false;
      }
    },
    
    async fetchArtworkById(id: string) {
      this.loading = true;
      try {
        this.currentArtwork = await ApiService.getArtwork(id);
        this.error = null;
      } catch (error) {
        this.currentArtwork = null;
        this.error = 'Erreur lors du chargement de l\'œuvre d\'art';
        console.error(error);
      } finally {
        this.loading = false;
      }
    },
    
    async searchArtworks(params: SearchParams) {
      this.loading = true;
      try {
        const result = await ApiService.searchArtworks(params);
        this.artworks = result.items;
        this.totalCount = result.totalCount;
        this.currentPage = result.page;
        this.pageSize = result.pageSize;
        this.searchParams = params;
        this.error = null;
      } catch (error) {
        this.artworks = [];
        this.error = 'Erreur lors de la recherche d\'œuvres d\'art';
        console.error(error);
      } finally {
        this.loading = false;
      }
    },
    
    clearCurrentArtwork() {
      this.currentArtwork = null;
    }
  }
});
