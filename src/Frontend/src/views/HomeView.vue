<template>
  <div class="home">
    <div class="hero">
      <h1>OpenJoconde</h1>
      <p class="subtitle">Explorez les collections des musées de France</p>
      <div class="search-container">
        <SearchBar 
          placeholder="Rechercher une œuvre, un artiste, un musée..." 
          buttonText="Rechercher" 
          @search="searchArtworks" 
        />
      </div>
    </div>

    <section class="featured">
      <h2>Catégories</h2>
      <div class="categories">
        <div class="category-card" @click="navigateTo('/artworks?domain=peinture')">
          <h3>Peinture</h3>
        </div>
        <div class="category-card" @click="navigateTo('/artworks?domain=sculpture')">
          <h3>Sculpture</h3>
        </div>
        <div class="category-card" @click="navigateTo('/artworks?domain=graphique')">
          <h3>Arts Graphiques</h3>
        </div>
        <div class="category-card" @click="navigateTo('/artworks?domain=photographie')">
          <h3>Photographie</h3>
        </div>
      </div>
    </section>

    <section class="about-section">
      <h2>À propos d'OpenJoconde</h2>
      <p>
        OpenJoconde est une plateforme qui vous permet d'explorer les richesses des collections 
        des musées de France issues de la base Joconde. Découvrez des œuvres d'art, des objets 
        patrimoniaux, et enrichissez votre connaissance du patrimoine culturel français.
      </p>
      <router-link to="/about" class="learn-more">En savoir plus</router-link>
    </section>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref } from 'vue';
import { useRouter } from 'vue-router';
import SearchBar from '@/components/SearchBar.vue';

export default defineComponent({
  name: 'HomeView',
  components: {
    SearchBar
  },
  setup() {
    const router = useRouter();
    const searchQuery = ref('');

    const searchArtworks = (searchText: string) => {
      if (searchText.trim()) {
        router.push({ name: 'search', query: { searchText } });
      }
    };

    const navigateTo = (path: string) => {
      router.push(path);
    };

    return {
      searchQuery,
      searchArtworks,
      navigateTo
    };
  }
});
</script>

<style scoped lang="scss">
.home {
  .hero {
    text-align: center;
    padding: 60px 20px;
    background-color: #f0f2f5;
    border-radius: 8px;
    margin-bottom: 30px;

    h1 {
      font-size: 3rem;
      margin-bottom: 16px;
      color: var(--primary-color);
    }

    .subtitle {
      font-size: 1.5rem;
      color: #666;
      margin-bottom: 30px;
    }

    .search-container {
      max-width: 600px;
      margin: 0 auto;
      
      :deep(.search-bar) {
        width: 100%;
        
        .search-form {
          padding: 0;
          
          .search-input {
            padding: 12px 16px;
            font-size: 1rem;
            border-radius: 4px 0 0 4px;
            height: auto;
          }
          
          .search-button {
            padding: 12px 24px;
            border-radius: 0 4px 4px 0;
            font-weight: bold;
            font-size: 1rem;
          }
        }
      }
    }
  }

  .featured {
    margin: 40px 0;

    h2 {
      font-size: 1.8rem;
      margin-bottom: 20px;
      color: var(--primary-color);
    }

    .categories {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
      gap: 20px;

      .category-card {
        background-color: white;
        border-radius: 8px;
        padding: 30px;
        box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
        text-align: center;
        cursor: pointer;
        transition: transform 0.3s, box-shadow 0.3s;

        &:hover {
          transform: translateY(-5px);
          box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
        }

        h3 {
          font-size: 1.2rem;
          color: var(--primary-color);
        }
      }
    }
  }

  .about-section {
    background-color: white;
    border-radius: 8px;
    padding: 30px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
    margin: 40px 0;

    h2 {
      font-size: 1.8rem;
      margin-bottom: 20px;
      color: var(--primary-color);
    }

    p {
      margin-bottom: 20px;
      line-height: 1.6;
    }

    .learn-more {
      display: inline-block;
      padding: 10px 20px;
      background-color: var(--secondary-color);
      color: white;
      text-decoration: none;
      border-radius: 4px;
      font-weight: bold;
      transition: background-color 0.3s;

      &:hover {
        background-color: darken(#42b983, 10%);
      }
    }
  }
}
</style>
