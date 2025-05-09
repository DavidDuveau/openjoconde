<template>
  <div class="search">
    <div class="search-header">
      <h1>Résultats de recherche</h1>
      <div v-if="searchParams.searchText" class="search-query">
        pour "<span>{{ searchParams.searchText }}</span>"
      </div>
    </div>

    <div class="search-container">
      <div class="search-filters">
        <h2>Filtres</h2>
        <div class="search-form">
          <div class="form-group">
            <label>Recherche par mot-clé</label>
            <div class="inline-search">
              <input 
                type="text" 
                v-model="searchParams.searchText" 
                placeholder="Titre, artiste, description..." 
                @keyup.enter="search"
              />
              <button class="secondary-button" @click="search">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <circle cx="11" cy="11" r="8"></circle>
                  <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
                </svg>
              </button>
            </div>
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

          <div class="filter-buttons">
            <button class="search-button" @click="search">Appliquer les filtres</button>
            <button class="reset-button" @click="resetFilters">Réinitialiser</button>
          </div>
        </div>
      </div>

      <div class="search-results">
        <div v-if="artworkStore.loading" class="loading-container">
          <div class="loader"></div>
          <p>Recherche en cours...</p>
        </div>
        
        <div v-else-if="artworkStore.error" class="error-container">
          <div class="error-icon">⚠️</div>
          <div class="error-message">
            <h3>Une erreur est survenue</h3>
            <p>{{ artworkStore.error }}</p>
            <button class="search-button" @click="search">Réessayer</button>
          </div>
        </div>
        
        <div v-else-if="artworkStore.artworks.length === 0" class="no-results">
          <div class="no-results-icon">🔍</div>
          <h3>Aucun résultat trouvé</h3>
          <p v-if="searchParams.searchText">Aucune œuvre ne correspond à "<strong>{{ searchParams.searchText }}</strong>".</p>
          <p>Essayez avec d'autres termes de recherche ou filtres moins restrictifs.</p>
          <div class="suggestions">
            <h4>Suggestions :</h4>
            <ul>
              <li>Vérifiez l'orthographe de votre recherche</li>
              <li>Utilisez des termes plus généraux</li>
              <li>Réduisez le nombre de filtres</li>
              <li v-if="hasFiltersEnabled"><a href="#" @click.prevent="resetFilters">Effacer tous les filtres</a></li>
              <li v-if="searchParams.searchText"><a href="#" @click.prevent="searchSimilar('peinture')">Rechercher "peinture"</a></li>
              <li v-if="searchParams.searchText"><a href="#" @click.prevent="searchSimilar('portrait')">Rechercher "portrait"</a></li>
              <li v-if="searchParams.searchText"><a href="#" @click.prevent="searchSimilar('sculpture')">Rechercher "sculpture"</a></li>
            </ul>
          </div>
        </div>
        
        <div v-else class="results-content">
          <div class="results-header">
            <div class="results-count">
              <strong>{{ artworkStore.totalCount }}</strong> résultat{{ artworkStore.totalCount > 1 ? 's' : '' }} trouvé{{ artworkStore.totalCount > 1 ? 's' : '' }}
              <span v-if="searchParams.searchText" class="search-terms">pour "<em>{{ searchParams.searchText }}</em>"</span>
            </div>
            <div class="results-sort">
              <label>Trier par :</label>
              <select v-model="sortBy" @change="search">
                <option value="relevance">Pertinence</option>
                <option value="title">Titre (A-Z)</option>
                <option value="title_desc">Titre (Z-A)</option>
                <option value="date">Date (ancienne → récente)</option>
                <option value="date_desc">Date (récente → ancienne)</option>
                <option value="artist">Artiste (A-Z)</option>
              </select>
            </div>
          </div>

          <div class="artwork-grid">
            <div 
              v-for="artwork in artworkStore.artworks" 
              :key="artwork.id"
              class="artwork-card-container"
            >
              <ArtworkCard 
                :id="artwork.id"
                :title="artwork.title"
                :imageUrl="artwork.imageUrl"
                :date="artwork.creationDate"
                :artists="artwork.artists"
                @click="navigateToArtwork(artwork.id)"
              />
            </div>
          </div>

          <div class="pagination" v-if="artworkStore.totalCount > searchParams.pageSize">
            <button 
              class="pagination-button"
              :disabled="artworkStore.currentPage === 1"
              @click="goToPage(artworkStore.currentPage - 1)"
            >
              &laquo; Précédent
            </button>
            
            <div class="page-numbers">
              <button 
                v-for="page in pageNumbers" 
                :key="page"
                class="pagination-number"
                :class="{ 'active': page === artworkStore.currentPage }"
                @click="goToPage(page)"
              >
                {{ page }}
              </button>
            </div>
            
            <button 
              class="pagination-button"
              :disabled="artworkStore.currentPage === artworkStore.totalPages"
              @click="goToPage(artworkStore.currentPage + 1)"
            >
              Suivant &raquo;
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
    const sortBy = ref('relevance');
    
    const searchParams = reactive<SearchParams>({
      searchText: '',
      artistId: '',
      domainId: '',
      techniqueId: '',
      periodId: '',
      museumId: '',
      page: 1,
      pageSize: 12,
      sortBy: 'relevance'
    });

    // Vérifier si des filtres sont activés
    const hasFiltersEnabled = computed(() => {
      return searchParams.artistId !== '' || 
             searchParams.domainId !== '' || 
             searchParams.techniqueId !== '' || 
             searchParams.periodId !== '' || 
             searchParams.museumId !== '';
    });

    // Observer les changements dans les paramètres d'URL
    watch(() => route.query, (newQuery) => {
      if (typeof newQuery.searchText === 'string') {
        searchParams.searchText = newQuery.searchText;
      } else if (newQuery.searchText === undefined) {
        searchParams.searchText = '';
      }
      
      if (typeof newQuery.artistId === 'string') {
        searchParams.artistId = newQuery.artistId;
      } else if (newQuery.artistId === undefined) {
        searchParams.artistId = '';
      }
      
      if (typeof newQuery.domainId === 'string') {
        searchParams.domainId = newQuery.domainId;
      } else if (newQuery.domainId === undefined) {
        searchParams.domainId = '';
      }
      
      if (typeof newQuery.techniqueId === 'string') {
        searchParams.techniqueId = newQuery.techniqueId;
      } else if (newQuery.techniqueId === undefined) {
        searchParams.techniqueId = '';
      }
      
      if (typeof newQuery.periodId === 'string') {
        searchParams.periodId = newQuery.periodId;
      } else if (newQuery.periodId === undefined) {
        searchParams.periodId = '';
      }
      
      if (typeof newQuery.museumId === 'string') {
        searchParams.museumId = newQuery.museumId;
      } else if (newQuery.museumId === undefined) {
        searchParams.museumId = '';
      }
      
      if (typeof newQuery.page === 'string') {
        searchParams.page = parseInt(newQuery.page, 10);
      } else {
        searchParams.page = 1;
      }
      
      if (typeof newQuery.sortBy === 'string') {
        sortBy.value = newQuery.sortBy;
        searchParams.sortBy = newQuery.sortBy;
      } else {
        sortBy.value = 'relevance';
        searchParams.sortBy = 'relevance';
      }
      
      // Effectuer la recherche uniquement si nous venons d'arriver sur la page
      // ou si le terme de recherche a changé
      search();
    }, { immediate: true });

    // Initialize from route query params
    onMounted(async () => {
      // Load filter options
      await Promise.all([
        loadArtists(),
        loadDomains(),
        loadTechniques(),
        loadPeriods()
      ]);
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
      // Mettre à jour les paramètres de tri
      searchParams.sortBy = sortBy.value;
      
      // Update route query params
      router.push({ 
        query: {
          searchText: searchParams.searchText || undefined,
          artistId: searchParams.artistId || undefined,
          domainId: searchParams.domainId || undefined,
          techniqueId: searchParams.techniqueId || undefined,
          periodId: searchParams.periodId || undefined,
          museumId: searchParams.museumId || undefined,
          page: searchParams.page > 1 ? searchParams.page.toString() : undefined,
          sortBy: searchParams.sortBy !== 'relevance' ? searchParams.sortBy : undefined
        }
      });
      
      // Perform search
      artworkStore.searchArtworks(searchParams);
    };
    
    // Reset filters
    const resetFilters = () => {
      searchParams.artistId = '';
      searchParams.domainId = '';
      searchParams.techniqueId = '';
      searchParams.periodId = '';
      searchParams.museumId = '';
      searchParams.page = 1;
      sortBy.value = 'relevance';
      searchParams.sortBy = 'relevance';
      
      search();
    };
    
    // Search for similar terms
    const searchSimilar = (term: string) => {
      searchParams.searchText = term;
      resetFilters();
    };
    
    // Pagination
    const goToPage = (page: number) => {
      searchParams.page = page;
      search();
      // Scroll to top
      window.scrollTo({ top: 0, behavior: 'smooth' });
    };
    
    // Navigate to artwork detail
    const navigateToArtwork = (id: string) => {
      router.push(`/artworks/${id}`);
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
    
    return {
      artworkStore,
      searchParams,
      artists,
      domains,
      techniques,
      periods,
      sortBy,
      hasFiltersEnabled,
      search,
      resetFilters,
      searchSimilar,
      goToPage,
      pageNumbers,
      navigateToArtwork
    };
  }
});
</script>

<style lang="scss" scoped>
.search {
  padding: 0 0 30px 0;
}

.search-header {
  margin-bottom: 30px;
  text-align: center;
  
  h1 {
    margin-bottom: 5px;
  }
  
  .search-query {
    font-size: 1.1rem;
    color: #666;
    
    span {
      font-weight: bold;
      color: var(--secondary-color);
    }
  }
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
  align-self: flex-start;
  position: sticky;
  top: 20px;
  
  h2 {
    margin-bottom: 20px;
    font-size: 1.2rem;
    color: var(--primary-color);
  }
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
    font-size: 0.9rem;
  }
  
  input, select {
    width: 100%;
    padding: 8px;
    border: 1px solid #ddd;
    border-radius: 4px;
    font-size: 0.9rem;
    
    &:focus {
      outline: none;
      border-color: var(--secondary-color);
      box-shadow: 0 0 0 2px rgba(66, 185, 131, 0.2);
    }
  }
  
  .inline-search {
    display: flex;
    gap: 5px;
    
    input {
      flex: 1;
    }
    
    button {
      padding: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
  }
}

.filter-buttons {
  display: flex;
  gap: 10px;
  margin-top: 20px;
}

button {
  padding: 10px 15px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-weight: bold;
  transition: background-color 0.2s, transform 0.1s;
  
  &:active {
    transform: translateY(1px);
  }
  
  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

.search-button {
  background-color: var(--secondary-color);
  color: white;
  
  &:hover {
    background-color: darken(#42b983, 5%);
  }
}

.reset-button {
  background-color: #f8f9fa;
  border: 1px solid #ddd;
  
  &:hover {
    background-color: #e9ecef;
  }
}

.secondary-button {
  background-color: #e9ecef;
  color: #666;
  
  &:hover {
    background-color: darken(#e9ecef, 5%);
  }
}

.results-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  padding-bottom: 10px;
  border-bottom: 1px solid #eee;
}

.results-count {
  font-weight: bold;
  color: #555;
  
  .search-terms {
    font-weight: normal;
    margin-left: 5px;
    
    em {
      color: var(--secondary-color);
      font-style: normal;
      font-weight: bold;
    }
  }
}

.results-sort {
  display: flex;
  align-items: center;
  gap: 10px;
  
  select {
    padding: 5px 10px;
    border: 1px solid #ddd;
    border-radius: 4px;
    font-size: 0.9rem;
  }
}

.artwork-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 20px;
  margin-bottom: 30px;
}

.artwork-card-container {
  height: 100%;
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
  
  .no-results-icon {
    font-size: 3rem;
    margin-bottom: 15px;
  }
  
  h3 {
    margin-bottom: 10px;
    color: #333;
  }
  
  p {
    margin-bottom: 15px;
    color: #666;
  }
  
  .suggestions {
    text-align: left;
    max-width: 500px;
    margin: 0 auto;
    
    h4 {
      margin-bottom: 10px;
    }
    
    ul {
      list-style-type: disc;
      padding-left: 20px;
      
      li {
        margin-bottom: 5px;
        
        a {
          color: var(--secondary-color);
          text-decoration: none;
          
          &:hover {
            text-decoration: underline;
          }
        }
      }
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
    
    &:hover:not(:disabled) {
      background-color: #e9ecef;
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

@media (max-width: 768px) {
  .search-container {
    flex-direction: column;
  }
  
  .search-filters {
    position: static;
    width: 100%;
    margin-bottom: 20px;
  }
  
  .filter-buttons {
    flex-direction: column;
    
    button {
      width: 100%;
    }
  }
  
  .results-header {
    flex-direction: column;
    gap: 10px;
    align-items: flex-start;
  }
  
  .artwork-grid {
    grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
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
