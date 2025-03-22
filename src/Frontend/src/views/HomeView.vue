<template>
  <div class="home">
    <div class="hero">
      <h1>OpenJoconde</h1>
      <p class="subtitle">Explorer les collections des musées de France</p>
      <div class="quick-search">
        <input type="text" v-model="searchText" placeholder="Rechercher une œuvre, un artiste, un musée..." />
        <button @click="search">Rechercher</button>
      </div>
    </div>

    <section class="featured-section">
      <h2>Œuvres à découvrir</h2>
      <div class="artwork-grid" v-if="!artworkStore.loading">
        <ArtworkCard 
          v-for="artwork in artworkStore.artworks" 
          :key="artwork.id" 
          :artwork="artwork" 
        />
      </div>
      <div v-else class="loading">
        Chargement des œuvres...
      </div>
    </section>

    <section class="categories-section">
      <h2>Explorer par catégorie</h2>
      <div class="categories-grid">
        <div class="category-card" @click="navigateTo('/search?domainId=')">
          <h3>Peintures</h3>
        </div>
        <div class="category-card" @click="navigateTo('/search?domainId=')">
          <h3>Sculptures</h3>
        </div>
        <div class="category-card" @click="navigateTo('/search?domainId=')">
          <h3>Arts graphiques</h3>
        </div>
        <div class="category-card" @click="navigateTo('/search?domainId=')">
          <h3>Photographies</h3>
        </div>
      </div>
    </section>

    <section class="museums-section">
      <h2>Musées à explorer</h2>
      <div class="museums-grid">
        <div class="museum-card" v-for="index in 4" :key="index" @click="navigateTo('/museums')">
          <h3>Musée {{ index }}</h3>
          <p>Explorer les collections</p>
        </div>
      </div>
    </section>
  </div>
</template>

<script lang="ts">
import { defineComponent, onMounted, ref } from 'vue';
import { useArtworkStore } from '@/store/artworkStore';
import { useRouter } from 'vue-router';
import ArtworkCard from '@/components/ArtworkCard.vue';

export default defineComponent({
  name: 'HomeView',
  components: {
    ArtworkCard
  },
  setup() {
    const artworkStore = useArtworkStore();
    const router = useRouter();
    const searchText = ref('');

    onMounted(async () => {
      // Load featured artworks on page load
      await artworkStore.fetchArtworks(1, 8);
    });

    const search = () => {
      if (searchText.value.trim()) {
        router.push({
          path: '/search',
          query: { searchText: searchText.value }
        });
      }
    };

    const navigateTo = (path: string) => {
      router.push(path);
    };

    return {
      artworkStore,
      searchText,
      search,
      navigateTo
    };
  }
});
</script>

<style lang="scss" scoped>
.home {
  text-align: center;
}

.hero {
  padding: 60px 20px;
  background-color: #f8f9fa;
  margin-bottom: 40px;

  h1 {
    font-size: 3em;
    margin-bottom: 10px;
  }

  .subtitle {
    font-size: 1.5em;
    color: #666;
    margin-bottom: 30px;
  }

  .quick-search {
    max-width: 600px;
    margin: 0 auto;
    display: flex;

    input {
      flex-grow: 1;
      padding: 12px 15px;
      border: 1px solid #ddd;
      border-right: none;
      border-radius: 4px 0 0 4px;
      font-size: 1em;
    }

    button {
      padding: 12px 20px;
      background-color: #42b983;
      color: white;
      border: none;
      border-radius: 0 4px 4px 0;
      cursor: pointer;
      font-size: 1em;

      &:hover {
        background-color: #3aa876;
      }
    }
  }
}

section {
  margin-bottom: 60px;

  h2 {
    font-size: 2em;
    margin-bottom: 30px;
    text-align: center;
  }
}

.artwork-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 20px;
}

.categories-grid, .museums-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 20px;
}

.category-card, .museum-card {
  background-color: #f8f9fa;
  border-radius: 8px;
  padding: 20px;
  cursor: pointer;
  transition: transform 0.3s, box-shadow 0.3s;

  &:hover {
    transform: translateY(-5px);
    box-shadow: 0 10px 20px rgba(0, 0, 0, 0.1);
  }

  h3 {
    margin-bottom: 10px;
  }
}

.loading {
  text-align: center;
  padding: 40px;
  color: #666;
}
</style>
