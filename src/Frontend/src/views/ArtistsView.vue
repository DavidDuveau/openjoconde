<template>
  <div class="artists-page">
    <h1>Artistes</h1>
    
    <div class="search-filter">
      <input 
        type="text" 
        v-model="searchQuery" 
        placeholder="Rechercher un artiste..." 
        @input="debounceSearch"
      />
    </div>
    
    <div v-if="loading" class="loading-container">
      <div class="loader"></div>
      <p>Chargement des artistes...</p>
    </div>
    
    <div v-else-if="error" class="error-container">
      <div class="error-icon">⚠️</div>
      <div class="error-message">
        <h3>Une erreur est survenue</h3>
        <p>{{ error }}</p>
        <button class="primary-button" @click="fetchArtists">Réessayer</button>
      </div>
    </div>
    
    <div v-else-if="artists.length === 0" class="no-results">
      <h3>Aucun artiste trouvé</h3>
      <p>Modifiez votre recherche ou essayez avec un autre terme.</p>
    </div>
    
    <div v-else class="artists-container">
      <div class="artists-grid">
        <div 
          v-for="artist in artists" 
          :key="artist.id"
          class="artist-card"
          @click="navigateToArtist(artist.id)"
        >
          <div class="artist-info">
            <h3>{{ artist.firstName }} {{ artist.lastName }}</h3>
            <p class="artist-dates">
              {{ formatArtistDates(artist.birthDate, artist.deathDate) }}
            </p>
            <p class="artist-nationality" v-if="artist.nationality">
              {{ artist.nationality }}
            </p>
            <p class="artist-count" v-if="artist.artworks">
              {{ artist.artworks.length }} œuvre{{ artist.artworks.length !== 1 ? 's' : '' }}
            </p>
          </div>
        </div>
      </div>
      
      <div class="pagination" v-if="totalCount > pageSize">
        <button 
          class="pagination-button"
          :disabled="currentPage === 1"
          @click="goToPage(currentPage - 1)"
        >
          &laquo; Précédent
        </button>
        
        <div class="page-numbers">
          <button 
            v-for="page in pageNumbers" 
            :key="page"
            class="pagination-number"
            :class="{ 'active': page === currentPage }"
            @click="goToPage(page)"
          >
            {{ page }}
          </button>
        </div>
        
        <button 
          class="pagination-button"
          :disabled="currentPage === totalPages"
          @click="goToPage(currentPage + 1)"
        >
          Suivant &raquo;
        </button>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref, onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import ApiService from '@/services/api';
import { Artist } from '@/types/models';

export default defineComponent({
  name: 'ArtistsView',
  setup() {
    const router = useRouter();
    const artists = ref<Artist[]>([]);
    const loading = ref<boolean>(false);
    const error = ref<string | null>(null);
    const currentPage = ref<number>(1);
    const pageSize = ref<number>(12);
    const totalCount = ref<number>(0);
    const totalPages = ref<number>(1);
    const searchQuery = ref<string>('');
    let searchTimeout: number | null = null;
    
    // Fetch artists on component mount
    onMounted(() => {
      fetchArtists();
    });
    
    // Method to fetch artists from API
    const fetchArtists = async () => {
      loading.value = true;
      error.value = null;
      
      try {
        const result = await ApiService.getArtists(currentPage.value, pageSize.value);
        artists.value = result.items;
        totalCount.value = result.totalCount;
        totalPages.value = result.totalPages;
      } catch (err) {
        console.error('Error fetching artists:', err);
        error.value = 'Erreur lors du chargement des artistes. Veuillez réessayer.';
        artists.value = [];
      } finally {
        loading.value = false;
      }
    };
    
    // Search with debounce
    const debounceSearch = () => {
      if (searchTimeout) {
        clearTimeout(searchTimeout);
      }
      
      searchTimeout = setTimeout(() => {
        currentPage.value = 1;
        fetchArtists();
      }, 300) as unknown as number;
    };
    
    // Navigation to artist detail
    const navigateToArtist = (id: string) => {
      router.push(`/artists/${id}`);
    };
    
    // Pagination
    const goToPage = (page: number) => {
      currentPage.value = page;
      fetchArtists();
      // Scroll to top
      window.scrollTo({ top: 0, behavior: 'smooth' });
    };
    
    // Helper to format artist dates
    const formatArtistDates = (birthDate?: string, deathDate?: string) => {
      if (!birthDate && !deathDate) return '';
      
      const birth = birthDate ? new Date(birthDate).getFullYear() : '?';
      const death = deathDate ? new Date(deathDate).getFullYear() : '';
      
      return death ? `${birth} - ${death}` : `Né(e) en ${birth}`;
    };
    
    // Computed page numbers for pagination
    const pageNumbers = computed(() => {
      const pages = [];
      const totalPagesVal = totalPages.value;
      const currentPageVal = currentPage.value;
      
      let startPage = Math.max(1, currentPageVal - 2);
      let endPage = Math.min(totalPagesVal, startPage + 4);
      
      if (endPage - startPage < 4) {
        startPage = Math.max(1, endPage - 4);
      }
      
      for (let i = startPage; i <= endPage; i++) {
        pages.push(i);
      }
      
      return pages;
    });
    
    return {
      artists,
      loading,
      error,
      currentPage,
      pageSize,
      totalCount,
      totalPages,
      searchQuery,
      pageNumbers,
      fetchArtists,
      debounceSearch,
      navigateToArtist,
      goToPage,
      formatArtistDates
    };
  }
});
</script>

<style lang="scss" scoped>
.artists-page {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
  
  h1 {
    text-align: center;
    margin-bottom: 30px;
    color: var(--primary-color);
  }
}

.search-filter {
  margin-bottom: 20px;
  max-width: 500px;
  margin-left: auto;
  margin-right: auto;
  
  input {
    width: 100%;
    padding: 10px 15px;
    border: 1px solid #ddd;
    border-radius: 4px;
    font-size: 1rem;
    
    &:focus {
      outline: none;
      border-color: var(--secondary-color);
      box-shadow: 0 0 0 2px rgba(66, 185, 131, 0.2);
    }
  }
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 50px;
  color: #666;
  
  .loader {
    width: 40px;
    height: 40px;
    border: 4px solid rgba(66, 185, 131, 0.3);
    border-radius: 50%;
    border-top-color: var(--secondary-color);
    animation: spin 1s linear infinite;
    margin-bottom: 15px;
  }
  
  @keyframes spin {
    to {
      transform: rotate(360deg);
    }
  }
}

.error-container {
  background-color: #fff8f8;
  border: 1px solid #ffd2d2;
  border-radius: 8px;
  padding: 20px;
  margin: 20px 0;
  display: flex;
  gap: 15px;
  
  .error-icon {
    font-size: 2rem;
  }
  
  .error-message {
    flex: 1;
    
    h3 {
      color: #dc3545;
      margin-bottom: 10px;
    }
    
    p {
      margin-bottom: 15px;
    }
  }
}

.no-results {
  background-color: #f8faff;
  border: 1px solid #e6f0ff;
  border-radius: 8px;
  padding: 30px;
  margin: 20px 0;
  text-align: center;
  
  h3 {
    margin-bottom: 10px;
    color: #333;
  }
}

.artists-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 20px;
  margin-bottom: 30px;
}

.artist-card {
  background-color: #fff;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  transition: transform 0.2s, box-shadow 0.2s;
  cursor: pointer;
  
  &:hover {
    transform: translateY(-3px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  }
  
  .artist-info {
    padding: 20px;
    
    h3 {
      margin-bottom: 8px;
      color: var(--primary-color);
    }
    
    .artist-dates, .artist-nationality {
      color: #666;
      margin-bottom: 5px;
      font-size: 0.9rem;
    }
    
    .artist-count {
      margin-top: 10px;
      font-size: 0.85rem;
      color: var(--secondary-color);
      font-weight: bold;
    }
  }
}

.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  margin-top: 30px;
  
  .pagination-button {
    background-color: #f8f9fa;
    border: 1px solid #ddd;
    padding: 8px 15px;
    border-radius: 4px;
    cursor: pointer;
    
    &:hover:not(:disabled) {
      background-color: #e9ecef;
    }
    
    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
  }
  
  .page-numbers {
    display: flex;
    margin: 0 10px;
    
    .pagination-number {
      margin: 0 3px;
      min-width: 32px;
      height: 32px;
      display: flex;
      align-items: center;
      justify-content: center;
      background-color: #f8f9fa;
      border: 1px solid #ddd;
      border-radius: 4px;
      cursor: pointer;
      
      &:hover:not(.active) {
        background-color: #e9ecef;
      }
      
      &.active {
        background-color: var(--secondary-color);
        color: white;
        border-color: var(--secondary-color);
      }
    }
  }
}

.primary-button {
  background-color: var(--secondary-color);
  color: white;
  border: none;
  padding: 8px 15px;
  border-radius: 4px;
  cursor: pointer;
  font-weight: bold;
  
  &:hover {
    background-color: darken(#42b983, 5%);
  }
}

@media (max-width: 768px) {
  .artists-grid {
    grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  }
  
  .pagination {
    flex-wrap: wrap;
    gap: 10px;
    
    .pagination-button {
      flex: 1;
      text-align: center;
    }
    
    .page-numbers {
      width: 100%;
      justify-content: center;
      margin: 0;
    }
  }
}
</style>
