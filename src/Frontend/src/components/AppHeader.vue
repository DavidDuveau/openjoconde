<template>
  <header class="app-header">
    <div class="container">
      <div class="header-left">
        <router-link to="/" class="logo">
          <h1>OpenJoconde</h1>
        </router-link>
        <nav class="main-nav">
          <ul>
            <li><router-link to="/">Accueil</router-link></li>
            <li><router-link to="/artworks">Collections</router-link></li>
            <li><router-link to="/artists">Artistes</router-link></li>
            <li><router-link to="/about">À propos</router-link></li>
          </ul>
        </nav>
      </div>
      <div class="header-right">
        <div class="search-section" :class="{ 'expanded': isSearchExpanded }">
          <button 
            v-if="!isSearchExpanded && isMobile" 
            @click="expandSearch" 
            class="search-toggle"
            aria-label="Ouvrir la recherche"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="11" cy="11" r="8"></circle>
              <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
            </svg>
          </button>
          <SearchBar 
            v-if="!isMobile || (isMobile && isSearchExpanded)"
            :loading="isSearching" 
            :initial-search-term="searchTerm"
            @search="handleSearch"
            @clear="handleClearSearch"
            placeholder="Rechercher"
            buttonText=""
          />
          <button 
            v-if="isSearchExpanded && isMobile" 
            @click="collapseSearch" 
            class="close-search-btn"
            aria-label="Fermer la recherche"
          >
            <span>&times;</span>
          </button>
        </div>
      </div>
    </div>
  </header>
</template>

<script lang="ts">
import { defineComponent, ref, computed, onMounted, onUnmounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import SearchBar from '@/components/SearchBar.vue';
import { useArtworkStore } from '@/store/artworkStore';

export default defineComponent({
  name: 'AppHeader',
  components: {
    SearchBar
  },
  setup() {
    const route = useRoute();
    const router = useRouter();
    const artworkStore = useArtworkStore();
    
    const windowWidth = ref(window.innerWidth);
    const isMobile = computed(() => windowWidth.value < 768);
    const isSearchExpanded = ref(false);
    const isSearching = ref(false);
    
    // Initialiser le terme de recherche depuis les paramètres de l'URL
    const searchTerm = computed(() => {
      return typeof route.query.searchText === 'string' ? route.query.searchText : '';
    });
    
    // Gestionnaire pour la mise à jour de la taille de la fenêtre
    const handleResize = () => {
      windowWidth.value = window.innerWidth;
      // Réinitialiser l'état de recherche étendue si nécessaire
      if (!isMobile.value) {
        isSearchExpanded.value = false;
      }
    };
    
    // Étendre la recherche en mode mobile
    const expandSearch = () => {
      isSearchExpanded.value = true;
    };
    
    // Réduire la recherche en mode mobile
    const collapseSearch = () => {
      isSearchExpanded.value = false;
    };
    
    // Gestionnaire de recherche
    const handleSearch = async (query: string) => {
      isSearching.value = true;
      
      try {
        // Naviguer vers la page de recherche avec le terme
        router.push({
          name: 'search',
          query: { 
            searchText: query,
            page: '1'
          }
        });
      } catch (error) {
        console.error('Error during search navigation:', error);
      } finally {
        isSearching.value = false;
      }
    };
    
    // Effacer la recherche
    const handleClearSearch = () => {
      if (route.name === 'search') {
        const newQuery = { ...route.query };
        delete newQuery.searchText;
        
        router.push({
          name: 'search',
          query: newQuery
        });
      }
    };
    
    // Ajouter et supprimer les gestionnaires d'événements
    onMounted(() => {
      window.addEventListener('resize', handleResize);
    });
    
    onUnmounted(() => {
      window.removeEventListener('resize', handleResize);
    });
    
    return {
      isMobile,
      isSearchExpanded,
      isSearching,
      searchTerm,
      expandSearch,
      collapseSearch,
      handleSearch,
      handleClearSearch
    };
  }
});
</script>

<style lang="scss" scoped>
.app-header {
  background-color: white;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
  position: sticky;
  top: 0;
  z-index: 1000;
  padding: 12px 0;
  
  .container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 0 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
  }
  
  .header-left {
    display: flex;
    align-items: center;
  }
  
  .logo {
    display: flex;
    align-items: center;
    text-decoration: none;
    
    h1 {
      font-size: 1.5rem;
      color: var(--primary-color);
      margin: 0;
    }
  }
  
  .main-nav {
    margin-left: 30px;
    
    ul {
      display: flex;
      list-style: none;
      padding: 0;
      margin: 0;
      
      li {
        margin-right: 20px;
        
        a {
          color: #555;
          text-decoration: none;
          font-weight: 500;
          transition: color 0.2s;
          
          &:hover, &.router-link-active {
            color: var(--secondary-color);
          }
        }
      }
    }
  }
  
  .header-right {
    display: flex;
    align-items: center;
  }
  
  .search-section {
    position: relative;
    display: flex;
    align-items: center;
    
    .search-toggle {
      background: none;
      border: none;
      cursor: pointer;
      padding: 8px;
      color: #555;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: color 0.2s;
      
      &:hover {
        color: var(--secondary-color);
      }
    }
    
    .close-search-btn {
      background: none;
      border: none;
      cursor: pointer;
      font-size: 1.5rem;
      color: #555;
      padding: 0 10px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
  }
}

@media (max-width: 768px) {
  .app-header {
    .container {
      flex-wrap: wrap;
    }
    
    .header-left {
      width: 100%;
      justify-content: space-between;
      margin-bottom: 8px;
    }
    
    .main-nav {
      display: none;
    }
    
    .header-right {
      width: 100%;
      justify-content: flex-end;
    }
    
    .search-section {
      &.expanded {
        width: 100%;
        
        .search-bar {
          width: 100%;
        }
      }
    }
  }
}
</style>
