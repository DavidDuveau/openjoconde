<template>
  <div class="search">
    <h1>Recherche d'œuvres</h1>

    <div class="search-container">
      <div class="search-filters">
        <div class="search-form">
          <div class="form-group">
            <label>Recherche par mot-clé</label>
            <input type="text" v-model="searchParams.searchText" placeholder="Titre, artiste, description..." />
          </div>

          <div class="form-group">
            <label>Domaine</label>
            <select v-model="searchParams.domainId">
              <option value="">Tous les domaines</option>
              <option v-for="domain in domains" :key="domain.id" :value="domain.id">
                {{ domain.name }}
              </option>
            </select>
          </div>

          <div class="form-group">
            <label>Technique</label>
            <select v-model="searchParams.techniqueId">
              <option value="">Toutes les techniques</option>
              <option v-for="technique in techniques" :key="technique.id" :value="technique.id">
                {{ technique.name }}
              </option>
            </select>
          </div>

          <div class="form-group">
            <label>Période</label>
            <select v-model="searchParams.periodId">
              <option value="">Toutes les périodes</option>
              <option v-for="period in periods" :key="period.id" :value="period.id">
                {{ period.name }}
              </option>
            </select>
          </div>

          <div class="form-group">
            <label>Artiste</label>
            <select v-model="searchParams.artistId">
              <option value="">Tous les artistes</option>
              <option v-for="artist in artists" :key="artist.id" :value="artist.id">
                {{ artist.firstName }} {{ artist.lastName }}
              </option>
            </select>
          </div>

          <button class="search-button" @click="search">Rechercher</button>
          <button class="reset-button" @click="resetFilters">Réinitialiser</button>
        </div>
      </div>

      <div class="search-results">
        <div v-if="artworkStore.loading" class="loading-container">
          Chargement des résultats...
        </div>
        <div v-else-if="artworkStore.error" class="error-container">
          {{ artworkStore.error }}
        </div>
        <div v-else-if="artworkStore.artworks.length === 0" class="no-results">
          Aucun résultat trouvé pour votre recherche.
        </div>
        <div v-else>
          <div class="results-header">
            <div class="results-count">
              {{ artworkStore.totalCount }} résultat(s) trouvé(s)
            </div>
            <div class="results-sort">
              <label>Trier par:</label>
              <select v-model="sortBy">
                <option value="title">Titre</option>
                <option value="creationDate">Date de création</option>
                <option value="artist">Artiste</option>
              </select>
            </div>
          </div>

          <div class="artwork-grid">
            <ArtworkCard 
              v-for="artwork in artworkStore.artworks" 
              :key="artwork.id" 
              :artwork="artwork" 
            />
          </div>

          <div class="pagination" v-if="artworkStore.totalPages > 1">
            <button 
              :disabled="artworkStore.currentPage === 1"
              @click="goToPage(artworkStore.currentPage - 1)"
            >
              Précédent
            </button>
            <div class="page-numbers">
              <button 
                v-for="page in pageNumbers" 
                :key="page"
                :class="{ 'active': page === artworkStore.currentPage }"
                @click="goToPage(page)"
              >
                {{ page }}
              </button>
            </div>
            <button 
              :disabled="artworkStore.currentPage === artworkStore.totalPages"
              @click="goToPage(artworkStore.currentPage + 1)"
            >
              Suivant
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref, onMounted, computed, reactive, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useArtworkStore } from '@/store/artworkStore';
import { SearchParams, Artist, Domain, Technique, Period } from '@/types/models';
import ApiService from '@/services/api';
import ArtworkCard from '@/components/ArtworkCard.vue';

export default defineComponent({
  name: 'SearchView',
  components: {
    ArtworkCard
  },
  setup() {
    const artworkStore = useArtworkStore();
    const route = useRoute();
    const router = useRouter();
    
    const artists = ref<Artist[]>([]);
    const domains = ref<Domain[]>([]);
    const techniques = ref<Technique[]>([]);
    const periods = ref<Period[]>([]);
    const sortBy = ref('title');
    
    const searchParams = reactive<SearchParams>({
      searchText: '',
      artistId: '',
      domainId: '',
      techniqueId: '',
      periodId: '',
      museumId: '',
      page: 1,
      pageSize: 12
    });

    // Initialize from route query params
    onMounted(async () => {
      if (route.query.searchText) searchParams.searchText = route.query.searchText as string;
      if (route.query.artistId) searchParams.artistId = route.query.artistId as string;
      if (route.query.domainId) searchParams.domainId = route.query.domainId as string;
      if (route.query.techniqueId) searchParams.techniqueId = route.query.techniqueId as string;
      if (route.query.periodId) searchParams.periodId = route.query.periodId as string;
      if (route.query.museumId) searchParams.museumId = route.query.museumId as string;
      if (route.query.page) searchParams.page = parseInt(route.query.page as string, 10);
      
      // Load filter options
      await Promise.all([
        loadArtists(),
        loadDomains(),
        loadTechniques(),
        loadPeriods()
      ]);
      
      // Perform initial search
      search();
    });
    
    // Methods to load filter options
    const loadArtists = async () => {
      try {
        const result = await ApiService.getArtists(1, 100);
        artists.value = result.items;
      } catch (error) {
        console.error('Error loading artists:', error);
      }
    };
    
    const loadDomains = async () => {
      try {
        domains.value = await ApiService.getDomains();
      } catch (error) {
        console.error('Error loading domains:', error);
      }
    };
    
    const loadTechniques = async () => {
      try {
        techniques.value = await ApiService.getTechniques();
      } catch (error) {
        console.error('Error loading techniques:', error);
      }
    };
    
    const loadPeriods = async () => {
      try {
        periods.value = await ApiService.getPeriods();
      } catch (error) {
        console.error('Error loading periods:', error);
      }
    };
    
    // Search function
    const search = () => {
      // Update route query params
      router.push({ 
        query: {
          ...route.query,
          searchText: searchParams.searchText || undefined,
          artistId: searchParams.artistId || undefined,
          domainId: searchParams.domainId || undefined,
          techniqueId: searchParams.techniqueId || undefined,
          periodId: searchParams.periodId || undefined,
          museumId: searchParams.museumId || undefined,
          page: searchParams.page.toString()
        }
      });
      
      // Perform search
      artworkStore.searchArtworks(searchParams);
    };
    
    // Reset filters
    const resetFilters = () => {
      searchParams.searchText = '';
      searchParams.artistId = '';
      searchParams.domainId = '';
      searchParams.techniqueId = '';
      searchParams.periodId = '';
      searchParams.museumId = '';
      searchParams.page = 1;
      
      search();
    };
    
    // Pagination
    const goToPage = (page: number) => {
      searchParams.page = page;
      search();
      // Scroll to top
      window.scrollTo({ top: 0, behavior: 'smooth' });
    };
    
    // Computed page numbers
    const pageNumbers = computed(() => {
      const pages = [];
      const totalPages = artworkStore.totalPages;
      const currentPage = artworkStore.currentPage;
      
      let startPage = Math.max(1, currentPage - 2);
      let endPage = Math.min(totalPages, startPage + 4);
      
      if (endPage - startPage < 4) {
        startPage = Math.max(1, endPage - 4);
      }
      
      for (let i = startPage; i <= endPage; i++) {
        pages.push(i);
      }
      
      return pages;
    });
    
    // Watch for sort changes
    watch(sortBy, () => {
      // In a real app, you'd update the sort param and call search
      search();
    });
    
    return {
      artworkStore,
      searchParams,
      artists,
      domains,
      techniques,
      periods,
      sortBy,
      search,
      resetFilters,
      goToPage,
      pageNumbers
    };
  }
});
</script>

<style lang="scss" scoped>
.search {
  padding: 20px;
}

h1 {
  margin-bottom: 30px;
  text-align: center;
}

.search-container {
  display: flex;
  gap: 30px;
}

.search-filters {
  flex: 0 0 300px;
  background-color: #f8f9fa;
  border-radius: 8px;
  padding: 20px;
}

.search-results {
  flex-grow: 1;
}

.form-group {
  margin-bottom: 15px;
  
  label {
    display: block;
    margin-bottom: 5px;
    font-weight: bold;
  }
  
  input, select {
    width: 100%;
    padding: 8px;
    border: 1px solid #ddd;
    border-radius: 4px;
  }
}

button {
  padding: 10px 15px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-weight: bold;
  margin-right: 10px;
}

.search-button {
  background-color: #42b983;
  color: white;
  
  &:hover {
    background-color: #3aa876;
  }
}

.reset-button {
  background-color: #f8f9fa;
  border: 1px solid #ddd;
  
  &:hover {
    background-color: #e9ecef;
  }
}

.results-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.results-sort {
  display: flex;
  align-items: center;
  gap: 10px;
  
  select {
    padding: 5px 10px;
    border: 1px solid #ddd;
    border-radius: 4px;
  }
}

.artwork-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 20px;
  margin-bottom: 30px;
}

.loading-container, .error-container, .no-results {
  text-align: center;
  padding: 50px;
  color: #666;
}

.error-container {
  color: #dc3545;
}

.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  margin-top: 30px;
  
  button {
    background-color: #f8f9fa;
    border: 1px solid #ddd;
    padding: 8px 15px;
    
    &:hover:not(:disabled) {
      background-color: #e9ecef;
    }
    
    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    
    &.active {
      background-color: #42b983;
      color: white;
      border-color: #42b983;
    }
  }
  
  .page-numbers {
    display: flex;
    margin: 0 10px;
    
    button {
      margin: 0 5px;
    }
  }
}

@media (max-width: 768px) {
  .search-container {
    flex-direction: column;
  }
  
  .search-filters {
    flex: none;
    width: 100%;
  }
}
</style>
