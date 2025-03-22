import { defineStore } from 'pinia';
import { artworkService } from '@/services/artworkService';
import { Artwork, ArtworkDetail, PaginatedResult } from '@/types/artwork';

interface ArtworkState {
  artworks: Artwork[];
  currentArtwork: ArtworkDetail | null;
  relatedArtworks: Artwork[];
  loading: boolean;
  error: string | null;
  totalItems: number;
  page: number;
  pageSize: number;
  totalPages: number;
  searchQuery: string;
  filters: {
    domain: string;
    period: string;
    technique: string;
    museum: string;
    artist: string;
  };
}

export const useArtworkStore = defineStore('artwork', {
  state: (): ArtworkState => ({
    artworks: [],
    currentArtwork: null,
    relatedArtworks: [],
    loading: false,
    error: null,
    totalItems: 0,
    page: 1,
    pageSize: 12,
    totalPages: 0,
    searchQuery: '',
    filters: {
      domain: '',
      period: '',
      technique: '',
      museum: '',
      artist: ''
    }
  }),
  
  getters: {
    hasFilters: (state): boolean => {
      return Boolean(
        state.searchQuery ||
        state.filters.domain ||
        state.filters.period ||
        state.filters.technique ||
        state.filters.museum ||
        state.filters.artist
      );
    }
  },
  
  actions: {
    // Réinitialiser les filtres
    resetFilters() {
      this.searchQuery = '';
      this.filters = {
        domain: '',
        period: '',
        technique: '',
        museum: '',
        artist: ''
      };
    },
    
    // Définir un filtre
    setFilter(key: keyof ArtworkState['filters'], value: string) {
      this.filters[key] = value;
    },
    
    // Définir la requête de recherche
    setSearchQuery(query: string) {
      this.searchQuery = query;
    },
    
    // Définir la page courante
    setPage(page: number) {
      this.page = page;
    },
    
    // Charger les œuvres d'art avec pagination et filtres
    async fetchArtworks() {
      this.loading = true;
      this.error = null;
      
      try {
        const result = await artworkService.getArtworks({
          page: this.page,
          pageSize: this.pageSize,
          search: this.searchQuery,
          domain: this.filters.domain,
          period: this.filters.period,
          technique: this.filters.technique,
          museum: this.filters.museum,
          artist: this.filters.artist
        });
        
        this.artworks = result.items;
        this.totalItems = result.totalItems;
        this.totalPages = result.totalPages;
      } catch (error) {
        this.error = 'Erreur lors du chargement des œuvres d\'art';
        console.error('Error fetching artworks:', error);
      } finally {
        this.loading = false;
      }
    },
    
    // Charger une œuvre d'art par son ID
    async fetchArtworkById(id: string) {
      this.loading = true;
      this.error = null;
      this.currentArtwork = null;
      
      try {
        const artwork = await artworkService.getArtworkById(id);
        this.currentArtwork = artwork;
        
        // Charger les œuvres similaires
        await this.fetchRelatedArtworks(id);
      } catch (error) {
        this.error = 'Erreur lors du chargement de l\'œuvre d\'art';
        console.error('Error fetching artwork by ID:', error);
      } finally {
        this.loading = false;
      }
    },
    
    // Charger les œuvres similaires
    async fetchRelatedArtworks(id: string) {
      try {
        this.relatedArtworks = await artworkService.getSimilarArtworks(id);
      } catch (error) {
        console.error('Error fetching related artworks:', error);
      }
    }
  }
});
