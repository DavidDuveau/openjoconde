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
            <FilterPicker
              v-model="searchParams.domainId"
              v-model:label="searchParams.domainName"
              placeholder="Rechercher un domaine"
              :fetchOptions="findDomains"
              @update:modelValue="applyFilters"
            />
          </div>

          <div class="form-group">
            <label>Technique</label>
            <FilterPicker
              v-model="searchParams.techniqueId"
              v-model:label="searchParams.techniqueName"
              placeholder="Rechercher une technique"
              :fetchOptions="findTechniques"
              @update:modelValue="applyFilters"
            />
          </div>

          <div class="form-group">
            <label>Période</label>
            <FilterPicker
              v-model="searchParams.periodId"
              v-model:label="searchParams.periodName"
              placeholder="Rechercher une période"
              :fetchOptions="findPeriods"
              @update:modelValue="applyFilters"
            />
          </div>

          <div class="form-group">
            <label>Artiste</label>
            <FilterPicker
              v-model="searchParams.artistId"
              v-model:label="searchParams.artistName"
              placeholder="Rechercher un artiste"
              :fetchOptions="findArtists"
              @update:modelValue="applyFilters"
            />
          </div>

          <div class="form-group">
            <label>Musée</label>
            <FilterPicker
              v-model="searchParams.museumId"
              v-model:label="searchParams.museumName"
              placeholder="Rechercher un musée"
              :fetchOptions="findMuseums"
              @update:modelValue="applyFilters"
            />
          </div>

          <div class="form-group">
            <label>Région et département</label>
            <GeographyFilter
              v-model:region="searchParams.region"
              v-model:department="searchParams.department"
              @change="applyFilters"
            />
          </div>

          <div class="form-group">
            <label>Près de moi</label>
            <button type="button" class="secondary-button near-me" data-testid="near-me" @click="nearMe">Trier par distance</button>
            <p v-if="hasPosition" class="position-note" data-testid="position-active">
              Votre position sert au tri.
              <a href="#" data-testid="position-clear" @click.prevent="clearPosition">La retirer</a>
            </p>
            <p v-if="positionError" class="position-note" data-testid="position-error">{{ positionError }}</p>
          </div>

          <div class="filter-buttons">
            <button class="search-button" @click="search">Appliquer les filtres</button>
            <button class="reset-button" @click="resetFilters">Réinitialiser</button>
          </div>
        </div>
      </div>

      <div class="search-results">
        <MuseumSummary v-if="searchParams.museumId" :museumId="searchParams.museumId" />

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
        
        <div v-else-if="artworkStore.artworks.length === 0" class="no-results" data-testid="no-results">
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
              <strong data-testid="results-total">{{ artworkStore.totalItems }}</strong> résultat{{ artworkStore.totalItems > 1 ? 's' : '' }} trouvé{{ artworkStore.totalItems > 1 ? 's' : '' }}
              <span v-if="searchParams.searchText" class="search-terms">pour "<em>{{ searchParams.searchText }}</em>"</span>
            </div>
            <div class="results-sort">
              <label>Trier par :</label>
              <select v-model="sortBy" data-testid="sort" @change="search">
                <option value="title">Titre (A-Z)</option>
                <option value="title_desc">Titre (Z-A)</option>
                <option value="date">Date (plus ancien)</option>
                <option value="date_desc">Date (plus récent)</option>
                <option v-if="canSortByRelevance" value="relevance">Pertinence</option>
                <option v-if="canSortByArtist" value="artist">Artiste</option>
                <option v-if="hasPosition" value="distance">Distance (musée le plus proche)</option>
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
                :hasImage="artwork.hasImage === true"
                :date="artwork.creationDate"
                :artists="artwork.artists"
                @click="navigateToArtwork(artwork.id)"
              />
            </div>
          </div>

          <div class="pagination" data-testid="pagination" v-if="artworkStore.totalItems > searchParams.pageSize">
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
import { defineComponent, ref, computed, reactive, watch, nextTick } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useArtworkStore } from '@/store/artworkStore';
import { SearchParams, NamedItem } from '@/types/models';
import ApiService from '@/services/api';
import ArtworkCard from '@/components/ArtworkCard.vue';
import FilterPicker, { PickerOption } from '@/components/FilterPicker.vue';
import GeographyFilter from '@/components/GeographyFilter.vue';
import MuseumSummary from '@/components/MuseumSummary.vue';

export default defineComponent({
  name: 'SearchView',
  components: {
    ArtworkCard,
    FilterPicker,
    GeographyFilter,
    MuseumSummary
  },
  setup() {
    const artworkStore = useArtworkStore();
    const route = useRoute();
    const router = useRouter();
    
    const sortBy = ref('title');
    
    const searchParams = reactive<SearchParams>({
      searchText: '',
      artistId: '',
      domainId: '',
      techniqueId: '',
      periodId: '',
      museumId: '',
      museumName: '',
      region: '',
      department: '',
      latitude: undefined,
      longitude: undefined,
      artistName: '',
      domainName: '',
      techniqueName: '',
      periodName: '',
      page: 1,
      pageSize: 12,
      sortBy: 'title'
    });

    // Vérifier si des filtres sont activés
    const hasFiltersEnabled = computed(() => {
      return searchParams.artistId !== '' || 
             searchParams.domainId !== '' || 
             searchParams.techniqueId !== '' || 
             searchParams.periodId !== '' || 
             searchParams.museumId !== '' ||
             searchParams.region !== '' ||
             searchParams.department !== '';
    });

    // « Près de moi » : le tri par distance suppose une position
    const hasPosition = computed(() => searchParams.latitude !== undefined && searchParams.longitude !== undefined);
    const positionError = ref('');

    // La pertinence suppose une recherche par mots ; le tri par artiste, une recherche ou un filtre (sinon l'API refuse)
    const canSortByRelevance = computed(() => /[\p{L}\p{N}]/u.test(searchParams.searchText ?? ''));
    const canSortByArtist = computed(() => (searchParams.searchText ?? '').trim() !== '' || hasFiltersEnabled.value);

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
      
      searchParams.region = typeof newQuery.region === 'string' ? newQuery.region : '';
      searchParams.department = typeof newQuery.department === 'string' ? newQuery.department : '';
      searchParams.museumName = typeof newQuery.museumName === 'string' ? newQuery.museumName : '';
      const latitude = Number(newQuery.lat);
      const longitude = Number(newQuery.lon);
      const located = typeof newQuery.lat === 'string' && typeof newQuery.lon === 'string' && Number.isFinite(latitude) && Number.isFinite(longitude);
      searchParams.latitude = located ? latitude : undefined;
      searchParams.longitude = located ? longitude : undefined;

      searchParams.artistName = typeof newQuery.artistName === 'string' ? newQuery.artistName : '';
      searchParams.domainName = typeof newQuery.domainName === 'string' ? newQuery.domainName : '';
      searchParams.techniqueName = typeof newQuery.techniqueName === 'string' ? newQuery.techniqueName : '';
      searchParams.periodName = typeof newQuery.periodName === 'string' ? newQuery.periodName : '';

      if (typeof newQuery.page === 'string') {
        searchParams.page = parseInt(newQuery.page, 10);
      } else {
        searchParams.page = 1;
      }
      
      if (typeof newQuery.sortBy === 'string') {
        sortBy.value = newQuery.sortBy;
        searchParams.sortBy = newQuery.sortBy;
      } else {
        sortBy.value = 'title';
        searchParams.sortBy = 'title';
      }
      
      // Effectuer la recherche uniquement si nous venons d'arriver sur la page
      // ou si le terme de recherche a changé
      search();
    }, { immediate: true });

    // Propositions des filtres : une page courte de résultats pour le texte saisi
    const asOptions = (items: NamedItem[]): PickerOption[] => items.map(item => ({ id: item.id, label: item.name }));
    const findDomains = async (text: string) => asOptions((await ApiService.getDomains(text)).items);
    const findTechniques = async (text: string) => asOptions((await ApiService.getTechniques(text)).items);
    const findPeriods = async (text: string) => asOptions((await ApiService.getPeriods(text)).items);
    const findMuseums = async (text: string) =>
      (await ApiService.getMuseums(1, 20, text)).items.map(museum => ({ id: museum.id, label: museum.name }));
    const findArtists = async (text: string) =>
      (await ApiService.getArtists(1, 20, text)).items.map(artist => ({
        id: artist.id,
        label: `${artist.firstName ?? ''} ${artist.lastName}`.trim()
      }));

    // Search function (déclaration de fonction : le watch immédiat ci-dessus l'appelle avant cette ligne)
    function search() {
      // Un tri devenu impossible (recherche effacée) retombe sur le titre
      if ((sortBy.value === 'relevance' && !canSortByRelevance.value) || (sortBy.value === 'artist' && !canSortByArtist.value)) {
        sortBy.value = 'title';
      }
      if (sortBy.value === 'distance' && !hasPosition.value) {
        sortBy.value = 'title';
      }
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
          museumName: searchParams.museumId ? searchParams.museumName || undefined : undefined,
          region: searchParams.region || undefined,
          department: searchParams.department || undefined,
          lat: hasPosition.value ? String(searchParams.latitude) : undefined,
          lon: hasPosition.value ? String(searchParams.longitude) : undefined,
          artistName: searchParams.artistId ? searchParams.artistName || undefined : undefined,
          domainName: searchParams.domainId ? searchParams.domainName || undefined : undefined,
          techniqueName: searchParams.techniqueId ? searchParams.techniqueName || undefined : undefined,
          periodName: searchParams.periodId ? searchParams.periodName || undefined : undefined,
          page: searchParams.page > 1 ? searchParams.page.toString() : undefined,
          sortBy: searchParams.sortBy !== 'title' ? searchParams.sortBy : undefined
        }
      });
      
      // Perform search
      artworkStore.searchArtworks(searchParams);
    }
    
    // Un filtre posé ou retiré relance la recherche à la première page, une fois la valeur reportée dans les paramètres
    const applyFilters = () => {
      nextTick(() => {
        searchParams.page = 1;
        search();
      });
    };

    // Reset filters
    const resetFilters = () => {
      searchParams.artistId = '';
      searchParams.domainId = '';
      searchParams.techniqueId = '';
      searchParams.periodId = '';
      searchParams.museumId = '';
      searchParams.museumName = '';
      searchParams.region = '';
      searchParams.department = '';
      searchParams.latitude = undefined;
      searchParams.longitude = undefined;
      positionError.value = '';
      searchParams.artistName = '';
      searchParams.domainName = '';
      searchParams.techniqueName = '';
      searchParams.periodName = '';
      searchParams.page = 1;
      sortBy.value = 'title';
      searchParams.sortBy = 'title';
      
      search();
    };
    
    // Position de l'utilisateur, arrondie à 3 décimales (une centaine de mètres) : assez pour classer des musées, et
    // l'adresse partagée ne désigne pas un domicile
    const nearMe = () => {
      positionError.value = '';
      if (!navigator.geolocation) {
        positionError.value = 'Votre navigateur ne permet pas de connaître votre position.';
        return;
      }
      navigator.geolocation.getCurrentPosition(
        (position) => {
          searchParams.latitude = Math.round(position.coords.latitude * 1000) / 1000;
          searchParams.longitude = Math.round(position.coords.longitude * 1000) / 1000;
          sortBy.value = 'distance';
          searchParams.page = 1;
          search();
        },
        () => {
          positionError.value = "Votre position n'a pas pu être obtenue : autorisez la localisation pour trier par distance.";
        }
      );
    };

    const clearPosition = () => {
      searchParams.latitude = undefined;
      searchParams.longitude = undefined;
      searchParams.page = 1;
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
      applyFilters,
      findArtists,
      findDomains,
      findTechniques,
      findPeriods,
      findMuseums,
      nearMe,
      clearPosition,
      hasPosition,
      positionError,
      sortBy,
      canSortByRelevance,
      canSortByArtist,
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
