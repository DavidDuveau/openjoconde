<template>
  <div class="artwork-detail">
    <div v-if="artworkStore.loading" class="loading">
      Chargement de l'œuvre...
    </div>
    <div v-else-if="artworkStore.error" class="error">
      {{ artworkStore.error }}
    </div>
    <div v-else-if="!artwork" class="not-found">
      Œuvre non trouvée
    </div>
    <div v-else class="artwork-container">
      <nav class="breadcrumb">
        <router-link to="/">Accueil</router-link> &gt;
        <router-link to="/search">Recherche</router-link> &gt;
        <span>{{ artwork.title || 'Œuvre sans titre' }}</span>
      </nav>

      <div class="artwork-header">
        <h1>{{ artwork.title || 'Œuvre sans titre' }}</h1>
        <div class="artwork-artists">
          <template v-if="artwork.artists && artwork.artists.length > 0">
            par 
            <span v-for="(artistWork, index) in artwork.artists" :key="artistWork.artistId">
              <router-link :to="`/artist/${artistWork.artistId}`">
                {{ artistWork.artist?.firstName }} {{ artistWork.artist?.lastName }}
              </router-link>
              <span v-if="artistWork.role"> ({{ artistWork.role }})</span>
              <span v-if="index < artwork.artists.length - 1">, </span>
            </span>
          </template>
          <template v-else>
            Artiste inconnu
          </template>
        </div>
      </div>

      <div class="artwork-content">
        <div class="artwork-image-container">
          <img 
            v-if="artwork.imageUrl" 
            :src="artwork.imageUrl" 
            :alt="artwork.title" 
            class="artwork-image"
          />
          <div v-else class="no-image">
            Aucune image disponible
          </div>
        </div>

        <div class="artwork-info">
          <div class="info-section">
            <h2>Informations générales</h2>
            <table>
              <tr v-if="artwork.reference">
                <th>Référence</th>
                <td>{{ artwork.reference }}</td>
              </tr>
              <tr v-if="artwork.inventoryNumber">
                <th>Numéro d'inventaire</th>
                <td>{{ artwork.inventoryNumber }}</td>
              </tr>
              <tr v-if="artwork.denomination">
                <th>Dénomination</th>
                <td>{{ artwork.denomination }}</td>
              </tr>
              <tr v-if="artwork.dimensions">
                <th>Dimensions</th>
                <td>{{ artwork.dimensions }}</td>
              </tr>
              <tr v-if="artwork.creationDate">
                <th>Date de création</th>
                <td>{{ artwork.creationDate }}</td>
              </tr>
              <tr v-if="artwork.creationPlace">
                <th>Lieu de création</th>
                <td>{{ artwork.creationPlace }}</td>
              </tr>
              <tr v-if="artwork.conservationPlace">
                <th>Lieu de conservation</th>
                <td>{{ artwork.conservationPlace }}</td>
              </tr>
              <tr v-if="artwork.copyright">
                <th>Copyright</th>
                <td>{{ artwork.copyright }}</td>
              </tr>
            </table>
          </div>

          <div class="info-section" v-if="artwork.domains && artwork.domains.length > 0">
            <h2>Domaines</h2>
            <div class="tags">
              <span 
                v-for="domain in artwork.domains" 
                :key="domain.id" 
                class="tag"
                @click="navigateToSearch({ domainId: domain.id })"
              >
                {{ domain.name }}
              </span>
            </div>
          </div>

          <div class="info-section" v-if="artwork.techniques && artwork.techniques.length > 0">
            <h2>Techniques</h2>
            <div class="tags">
              <span 
                v-for="technique in artwork.techniques" 
                :key="technique.id" 
                class="tag"
                @click="navigateToSearch({ techniqueId: technique.id })"
              >
                {{ technique.name }}
              </span>
            </div>
          </div>

          <div class="info-section" v-if="artwork.periods && artwork.periods.length > 0">
            <h2>Périodes</h2>
            <div class="tags">
              <span 
                v-for="period in artwork.periods" 
                :key="period.id" 
                class="tag"
                @click="navigateToSearch({ periodId: period.id })"
              >
                {{ period.name }}
              </span>
            </div>
          </div>
        </div>
      </div>

      <div class="artwork-description" v-if="artwork.description">
        <h2>Description</h2>
        <p>{{ artwork.description }}</p>
      </div>

      <div class="related-artworks" v-if="relatedArtworks.length > 0">
        <h2>Œuvres similaires</h2>
        <div class="artwork-grid">
          <ArtworkCard 
            v-for="relatedArtwork in relatedArtworks" 
            :key="relatedArtwork.id" 
            :artwork="relatedArtwork" 
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useArtworkStore } from '@/store/artworkStore';
import { Artwork } from '@/types/models';
import ArtworkCard from '@/components/ArtworkCard.vue';

export default defineComponent({
  name: 'ArtworkDetailView',
  components: {
    ArtworkCard
  },
  props: {
    id: {
      type: String,
      required: true
    }
  },
  setup(props) {
    const artworkStore = useArtworkStore();
    const route = useRoute();
    const router = useRouter();
    const relatedArtworks = ref<Artwork[]>([]);

    const artwork = computed(() => artworkStore.currentArtwork);

    onMounted(async () => {
      // Clear current artwork before loading
      artworkStore.clearCurrentArtwork();
      
      // Load artwork details
      await artworkStore.fetchArtworkById(props.id);
      
      // TODO: Load related artworks based on domains, techniques, or artists
      // This is a placeholder, in a real app you'd call an API
      // relatedArtworks.value = await ApiService.getRelatedArtworks(props.id);
    });

    const navigateToSearch = (params: Record<string, string>) => {
      router.push({
        path: '/search',
        query: params
      });
    };

    return {
      artworkStore,
      artwork,
      relatedArtworks,
      navigateToSearch
    };
  }
});
</script>

<style lang="scss" scoped>
.artwork-detail {
  padding: 20px;
}

.loading, .error, .not-found {
  text-align: center;
  padding: 50px;
  color: #666;
}

.error {
  color: #dc3545;
}

.breadcrumb {
  margin-bottom: 20px;
  color: #666;
  
  a {
    color: #666;
    text-decoration: none;
    
    &:hover {
      text-decoration: underline;
    }
  }
}

.artwork-header {
  margin-bottom: 30px;
  
  h1 {
    margin-bottom: 10px;
  }
  
  .artwork-artists {
    font-size: 1.2em;
    color: #666;
    
    a {
      color: #42b983;
      text-decoration: none;
      
      &:hover {
        text-decoration: underline;
      }
    }
  }
}

.artwork-content {
  display: flex;
  gap: 30px;
  margin-bottom: 40px;
}

.artwork-image-container {
  flex: 0 0 50%;
  
  .artwork-image {
    width: 100%;
    border-radius: 8px;
    box-shadow: 0 4px 10px rgba(0, 0, 0, 0.1);
  }
  
  .no-image {
    width: 100%;
    aspect-ratio: 4/3;
    background-color: #f8f9fa;
    display: flex;
    align-items: center;
    justify-content: center;
    color: #666;
    border-radius: 8px;
  }
}

.artwork-info {
  flex: 1;
}

.info-section {
  margin-bottom: 20px;
  
  h2 {
    font-size: 1.5em;
    margin-bottom: 15px;
    padding-bottom: 8px;
    border-bottom: 1px solid #eee;
  }
  
  table {
    width: 100%;
    border-collapse: collapse;
    
    th, td {
      padding: 8px 0;
      text-align: left;
    }
    
    th {
      width: 40%;
      color: #666;
      font-weight: normal;
    }
  }
}

.tags {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  
  .tag {
    background-color: #f8f9fa;
    padding: 5px 10px;
    border-radius: 20px;
    font-size: 0.9em;
    cursor: pointer;
    
    &:hover {
      background-color: #42b983;
      color: white;
    }
  }
}

.artwork-description {
  margin-bottom: 40px;
  
  h2 {
    font-size: 1.5em;
    margin-bottom: 15px;
  }
  
  p {
    line-height: 1.6;
  }
}

.related-artworks {
  h2 {
    font-size: 1.5em;
    margin-bottom: 20px;
  }
}

.artwork-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 20px;
}

@media (max-width: 768px) {
  .artwork-content {
    flex-direction: column;
  }
  
  .artwork-image-container {
    flex: none;
    margin-bottom: 20px;
  }
}
</style>
