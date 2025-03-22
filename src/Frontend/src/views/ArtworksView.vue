<template>
  <div class="artworks">
    <h1>Œuvres d'art</h1>

    <div class="search-filters">
      <div class="search-bar">
        <input 
          type="text" 
          v-model="searchQuery" 
          placeholder="Rechercher une œuvre..." 
          @keyup.enter="fetchArtworks"
        />
        <button @click="fetchArtworks">Rechercher</button>
      </div>

      <div class="filters">
        <div class="filter-group">
          <label>Domaine</label>
          <select v-model="selectedDomain">
            <option value="">Tous</option>
            <option value="peinture">Peinture</option>
            <option value="sculpture">Sculpture</option>
            <option value="graphique">Arts Graphiques</option>
            <!-- Plus d'options à ajouter en fonction des données réelles -->
          </select>
        </div>

        <div class="filter-group">
          <label>Période</label>
          <select v-model="selectedPeriod">
            <option value="">Toutes</option>
            <option value="renaissance">Renaissance</option>
            <option value="baroque">Baroque</option>
            <option value="moderne">Art Moderne</option>
            <!-- Plus d'options à ajouter en fonction des données réelles -->
          </select>
        </div>

        <div class="filter-group">
          <label>Musée</label>
          <select v-model="selectedMuseum">
            <option value="">Tous</option>
            <option value="louvre">Louvre</option>
            <option value="orsay">Musée d'Orsay</option>
            <!-- Plus d'options à ajouter en fonction des données réelles -->
          </select>
        </div>
      </div>
    </div>

    <div v-if="loading" class="loading">
      <p>Chargement des œuvres...</p>
    </div>

    <div v-else-if="error" class="error">
      <p>Une erreur est survenue lors du chargement des œuvres. Veuillez réessayer.</p>
      <button @click="fetchArtworks">Réessayer</button>
    </div>

    <div v-else-if="artworks.length === 0" class="no-results">
      <p>Aucune œuvre ne correspond à votre recherche.</p>
    </div>

    <div v-else class="artworks-grid">
      <div 
        v-for="artwork in artworks" 
        :key="artwork.id" 
        class="artwork-card"
        @click="viewArtworkDetails(artwork.id)"
      >
        <div class="artwork-image">
          <img :src="artwork.imageUrl || '/placeholder-image.jpg'" :alt="artwork.title" />
        </div>
        <div class="artwork-info">
          <h3>{{ artwork.title || 'Sans titre' }}</h3>
          <p v-if="artwork.artists && artwork.artists.length">
            {{ artwork.artists.map(a => `${a.firstName} ${a.lastName}`).join(', ') }}
          </p>
          <p>{{ artwork.creationDate || 'Date inconnue' }}</p>
        </div>
      </div>
    </div>

    <div class="pagination" v-if="totalPages > 1">
      <button 
        :disabled="currentPage === 1" 
        @click="changePage(currentPage - 1)"
      >
        Précédent
      </button>
      
      <span class="page-info">Page {{ currentPage }} sur {{ totalPages }}</span>
      
      <button 
        :disabled="currentPage === totalPages" 
        @click="changePage(currentPage + 1)"
      >
        Suivant
      </button>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
// Dans une implémentation complète, on importerait ici un service pour l'API

// Type pour les œuvres d'art
interface Artwork {
  id: string;
  title: string;
  reference: string;
  inventoryNumber: string;
  imageUrl: string;
  creationDate: string;
  artists: { id: string; firstName: string; lastName: string }[];
}

// Type pour la réponse paginée
interface PaginatedResult<T> {
  items: T[];
  totalItems: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export default defineComponent({
  name: 'ArtworksView',
  setup() {
    const route = useRoute();
    const router = useRouter();
    
    // État
    const artworks = ref<Artwork[]>([]);
    const loading = ref(false);
    const error = ref(false);
    const searchQuery = ref('');
    const selectedDomain = ref('');
    const selectedPeriod = ref('');
    const selectedMuseum = ref('');
    const currentPage = ref(1);
    const pageSize = ref(12);
    const totalPages = ref(0);
    
    // Initialisation depuis les paramètres d'URL
    onMounted(() => {
      if (route.query.search) {
        searchQuery.value = route.query.search as string;
      }
      
      if (route.query.domain) {
        selectedDomain.value = route.query.domain as string;
      }
      
      if (route.query.period) {
        selectedPeriod.value = route.query.period as string;
      }
      
      if (route.query.museum) {
        selectedMuseum.value = route.query.museum as string;
      }
      
      if (route.query.page) {
        currentPage.value = parseInt(route.query.page as string, 10);
      }
      
      fetchArtworks();
    });
    
    // Surveiller les changements de route
    watch(() => route.query, (newQuery) => {
      if (newQuery.search !== undefined) {
        searchQuery.value = newQuery.search as string;
      }
      
      if (newQuery.domain !== undefined) {
        selectedDomain.value = newQuery.domain as string;
      }
      
      if (newQuery.period !== undefined) {
        selectedPeriod.value = newQuery.period as string;
      }
      
      if (newQuery.museum !== undefined) {
        selectedMuseum.value = newQuery.museum as string;
      }
      
      if (newQuery.page !== undefined) {
        currentPage.value = parseInt(newQuery.page as string, 10);
      }
      
      fetchArtworks();
    });
    
    // Méthodes
    const fetchArtworks = async () => {
      loading.value = true;
      error.value = false;
      
      try {
        // Dans une implémentation réelle, on ferait un appel API ici
        // Exemple: const response = await artworkService.getArtworks({ ... });
        
        // Simulation d'une réponse d'API pour le moment
        await new Promise(resolve => setTimeout(resolve, 800));
        
        // Données factices pour le prototype
        const mockResponse: PaginatedResult<Artwork> = {
          items: [
            {
              id: '1',
              title: 'La Joconde',
              reference: 'INV779',
              inventoryNumber: 'INV779',
              imageUrl: 'https://via.placeholder.com/300x400',
              creationDate: '1503-1519',
              artists: [{ id: '1', firstName: 'Leonardo', lastName: 'da Vinci' }]
            },
            {
              id: '2',
              title: 'La Liberté guidant le peuple',
              reference: 'INV7300',
              inventoryNumber: 'INV7300',
              imageUrl: 'https://via.placeholder.com/300x400',
              creationDate: '1830',
              artists: [{ id: '2', firstName: 'Eugène', lastName: 'Delacroix' }]
            },
            // Ajouter plus d'œuvres fictives pour le prototype
          ],
          totalItems: 120,
          page: currentPage.value,
          pageSize: pageSize.value,
          totalPages: 10
        };
        
        artworks.value = mockResponse.items;
        totalPages.value = mockResponse.totalPages;
        
        // Mettre à jour l'URL avec les paramètres de recherche
        updateUrlParams();
      } catch (err) {
        console.error('Erreur lors du chargement des œuvres:', err);
        error.value = true;
      } finally {
        loading.value = false;
      }
    };
    
    const changePage = (page: number) => {
      currentPage.value = page;
      fetchArtworks();
    };
    
    const viewArtworkDetails = (id: string) => {
      router.push(`/artworks/${id}`);
    };
    
    const updateUrlParams = () => {
      const query: Record<string, string> = {};
      
      if (searchQuery.value) {
        query.search = searchQuery.value;
      }
      
      if (selectedDomain.value) {
        query.domain = selectedDomain.value;
      }
      
      if (selectedPeriod.value) {
        query.period = selectedPeriod.value;
      }
      
      if (selectedMuseum.value) {
        query.museum = selectedMuseum.value;
      }
      
      if (currentPage.value > 1) {
        query.page = currentPage.value.toString();
      }
      
      router.replace({ query });
    };
    
    return {
      artworks,
      loading,
      error,
      searchQuery,
      selectedDomain,
      selectedPeriod,
      selectedMuseum,
      currentPage,
      totalPages,
      fetchArtworks,
      changePage,
      viewArtworkDetails
    };
  }
});
</script>

<style scoped lang="scss">
.artworks {
  h1 {
    margin-bottom: 24px;
    color: var(--primary-color);
  }

  .search-filters {
    background-color: white;
    border-radius: 8px;
    padding: 20px;
    margin-bottom: 24px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);

    .search-bar {
      display: flex;
      margin-bottom: 16px;

      input {
        flex: 1;
        padding: 10px 16px;
        font-size: 1rem;
        border: 1px solid #ddd;
        border-radius: 4px 0 0 4px;
        outline: none;

        &:focus {
          border-color: var(--secondary-color);
        }
      }

      button {
        padding: 10px 20px;
        background-color: var(--secondary-color);
        color: white;
        border: none;
        border-radius: 0 4px 4px 0;
        cursor: pointer;
        font-weight: bold;
        transition: background-color 0.3s;

        &:hover {
          background-color: darken(#42b983, 10%);
        }
      }
    }

    .filters {
      display: flex;
      flex-wrap: wrap;
      gap: 16px;

      .filter-group {
        flex: 1;
        min-width: 200px;

        label {
          display: block;
          margin-bottom: 8px;
          font-weight: bold;
        }

        select {
          width: 100%;
          padding: 10px;
          border: 1px solid #ddd;
          border-radius: 4px;
          background-color: white;
          outline: none;

          &:focus {
            border-color: var(--secondary-color);
          }
        }
      }
    }
  }

  .loading, .error, .no-results {
    text-align: center;
    padding: 40px;
    background-color: white;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);

    button {
      margin-top: 16px;
      padding: 10px 20px;
      background-color: var(--secondary-color);
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: bold;
    }
  }

  .artworks-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
    gap: 24px;
    margin-bottom: 24px;

    .artwork-card {
      background-color: white;
      border-radius: 8px;
      overflow: hidden;
      box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
      cursor: pointer;
      transition: transform 0.3s, box-shadow 0.3s;

      &:hover {
        transform: translateY(-5px);
        box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
      }

      .artwork-image {
        position: relative;
        width: 100%;
        padding-top: 75%; // 4:3 ratio

        img {
          position: absolute;
          top: 0;
          left: 0;
          width: 100%;
          height: 100%;
          object-fit: cover;
        }
      }

      .artwork-info {
        padding: 16px;

        h3 {
          margin-bottom: 8px;
          color: var(--primary-color);
          font-size: 1.1rem;
        }

        p {
          color: #666;
          font-size: 0.9rem;
          margin-bottom: 4px;
        }
      }
    }
  }

  .pagination {
    display: flex;
    justify-content: center;
    align-items: center;
    margin-top: 24px;

    button {
      padding: 8px 16px;
      background-color: white;
      border: 1px solid #ddd;
      border-radius: 4px;
      cursor: pointer;
      margin: 0 8px;
      
      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
      
      &:hover:not(:disabled) {
        background-color: var(--secondary-color);
        color: white;
        border-color: var(--secondary-color);
      }
    }

    .page-info {
      margin: 0 16px;
    }
  }
}
</style>
