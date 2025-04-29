<template>
  <div id="app">
    <header>
      <div class="header-content">
        <div class="logo-nav">
          <div class="logo">
            <router-link to="/">OpenJoconde</router-link>
          </div>
          <nav>
            <router-link to="/">Accueil</router-link>
            <router-link to="/artworks">Œuvres</router-link>
            <router-link to="/artists">Artistes</router-link>
            <router-link to="/museums">Musées</router-link>
            <router-link to="/about">À propos</router-link>
          </nav>
        </div>
        <SearchBar 
          @search="handleSearch" 
          :loading="searchLoading" 
          :initial-search-term="currentSearchTerm"
        />
      </div>
    </header>

    <main>
      <router-view />
    </main>

    <footer>
      <p>OpenJoconde - Exploration des collections des musées de France</p>
    </footer>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import SearchBar from '@/components/SearchBar.vue';
import { useArtworkStore } from '@/store/artworkStore';

export default defineComponent({
  components: {
    SearchBar
  },
  setup() {
    const route = useRoute();
    const router = useRouter();
    const artworkStore = useArtworkStore();
    const searchLoading = ref(false);

    const currentSearchTerm = computed(() => {
      return typeof route.query.searchText === 'string' ? route.query.searchText : '';
    });

    const handleSearch = (searchTerm: string) => {
      // Si nous sommes déjà sur la page de recherche, le composant SearchView s'occupera de la recherche
      if (route.name !== 'search') {
        searchLoading.value = true;
        router.push({ 
          name: 'search',
          query: { searchText: searchTerm }
        }).finally(() => {
          searchLoading.value = false;
        });
      }
    };

    return {
      handleSearch,
      searchLoading,
      currentSearchTerm
    };
  }
});
</script>

<style lang="scss">
:root {
  --primary-color: #2c3e50;
  --secondary-color: #42b983;
  --text-color: #333;
  --background-color: #f8f9fa;
  --header-height: 60px;
  --footer-height: 60px;
}

* {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

body {
  font-family: 'Avenir', Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  color: var(--text-color);
  background-color: var(--background-color);
  line-height: 1.6;
}

#app {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

header {
  background-color: var(--primary-color);
  color: white;
  min-height: var(--header-height);
  padding: 10px 20px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);

  .header-content {
    max-width: 1200px;
    margin: 0 auto;
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 15px;
    width: 100%;
  }

  .logo-nav {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 20px;
  }

  .logo {
    font-size: 1.5rem;
    font-weight: bold;
    
    a {
      color: white;
      text-decoration: none;
      &:hover {
        color: var(--secondary-color);
      }
    }
  }

  nav {
    display: flex;
    gap: 15px;
    flex-wrap: wrap;
    
    a {
      color: white;
      text-decoration: none;
      font-weight: bold;
      padding: 5px 0;
      position: relative;
      
      &:after {
        content: '';
        position: absolute;
        width: 0;
        height: 2px;
        background-color: var(--secondary-color);
        bottom: 0;
        left: 0;
        transition: width 0.3s ease;
      }
      
      &:hover:after {
        width: 100%;
      }
      
      &.router-link-active {
        color: var(--secondary-color);
        
        &:after {
          width: 100%;
        }
      }
    }
  }

  @media (max-width: 768px) {
    .header-content {
      flex-direction: column;
      align-items: stretch;
    }
    
    .logo-nav {
      justify-content: space-between;
    }
    
    nav {
      gap: 10px;
      margin-top: 10px;
    }
  }
}

main {
  flex: 1;
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
  width: 100%;
}

footer {
  background-color: var(--primary-color);
  color: white;
  height: var(--footer-height);
  padding: 0 20px;
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>
