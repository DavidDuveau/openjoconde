<template>
  <div class="artist-detail">
    <div v-if="loading" class="loading-container">
      <div class="loader"></div>
      <p>Chargement de l'artiste...</p>
    </div>
    
    <div v-else-if="error" class="error-container">
      <div class="error-icon">⚠️</div>
      <div class="error-message">
        <h3>Une erreur est survenue</h3>
        <p>{{ error }}</p>
        <button class="primary-button" @click="fetchArtist">Réessayer</button>
        <router-link to="/artists" class="secondary-button">Retour aux artistes</router-link>
      </div>
    </div>
    
    <div v-else-if="!artist" class="not-found">
      <h2>Artiste non trouvé</h2>
      <p>L'artiste que vous recherchez n'existe pas ou a été supprimé.</p>
      <router-link to="/artists" class="primary-button">Retour aux artistes</router-link>
    </div>
    
    <div v-else class="artist-content">
      <div class="back-link">
        <router-link to="/artists">
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M19 12H5M12 19l-7-7 7-7"></path>
          </svg>
          Retour aux artistes
        </router-link>
      </div>
      
      <header class="artist-header">
        <h1>{{ artist.firstName }} {{ artist.lastName }}</h1>
        <div class="artist-meta">
          <div class="artist-dates">
            {{ formatArtistDates(artist.birthDate, artist.deathDate) }}
          </div>
          <div class="artist-nationality" v-if="artist.nationality">
            {{ artist.nationality }}
          </div>
        </div>
      </header>
      
      <div class="artist-biography" v-if="artist.biography">
        <h2>Biographie</h2>
        <div class="biography-content">
          {{ artist.biography }}
        </div>
      </div>
      
      <div class="artist-artworks">
        <h2>Œuvres de l'artiste</h2>
        
        <div v-if="artist.artworks && artist.artworks.length === 0" class="no-artworks">
          <p>Aucune œuvre n'est actuellement disponible pour cet artiste.</p>
        </div>
        
        <div v-else-if="artist.artworks && artist.artworks.length > 0" class="artworks-grid">
          <div 
            v-for="artwork in artist.artworks" 
            :key="artwork.artworkId"
            class="artwork-card-container"
            @click="navigateToArtwork(artwork.artworkId)"
          >
            <div class="artwork-card">
              <div class="artwork-image" v-if="artwork.artwork && artwork.artwork.imageUrl">
                <img :src="artwork.artwork.imageUrl" :alt="artwork.artwork.title" />
              </div>
              <div class="artwork-info">
                <h3>{{ artwork.artwork ? artwork.artwork.title : 'Œuvre sans titre' }}</h3>
                <p class="artwork-date" v-if="artwork.artwork && artwork.artwork.creationDate">
                  {{ artwork.artwork.creationDate }}
                </p>
                <p class="artwork-role" v-if="artwork.role">
                  Rôle: {{ artwork.role }}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import ApiService from '@/services/api';
import { Artist } from '@/types/models';

export default defineComponent({
  name: 'ArtistDetailView',
  setup() {
    const route = useRoute();
    const router = useRouter();
    const artist = ref<Artist | null>(null);
    const loading = ref<boolean>(false);
    const error = ref<string | null>(null);
    
    // Fetch artist data when component is mounted or route changes
    onMounted(() => {
      fetchArtist();
    });
    
    // Watch for route changes to reload artist if ID changes
    watch(() => route.params.id, (newId) => {
      if (newId) {
        fetchArtist();
      }
    });
    
    // Fetch artist data from API
    const fetchArtist = async () => {
      const artistId = route.params.id as string;
      if (!artistId) return;
      
      loading.value = true;
      error.value = null;
      
      try {
        artist.value = await ApiService.getArtist(artistId);
      } catch (err) {
        console.error('Error fetching artist:', err);
        error.value = 'Erreur lors du chargement de l\'artiste. Veuillez réessayer.';
        artist.value = null;
      } finally {
        loading.value = false;
      }
    };
    
    // Helper to format artist dates
    const formatArtistDates = (birthDate?: string, deathDate?: string) => {
      if (!birthDate && !deathDate) return '';
      
      const birth = birthDate ? new Date(birthDate).getFullYear() : '?';
      const death = deathDate ? new Date(deathDate).getFullYear() : '';
      
      return death ? `${birth} - ${death}` : `Né(e) en ${birth}`;
    };
    
    // Navigate to artwork detail
    const navigateToArtwork = (artworkId: string) => {
      router.push(`/artworks/${artworkId}`);
    };
    
    return {
      artist,
      loading,
      error,
      fetchArtist,
      formatArtistDates,
      navigateToArtwork
    };
  }
});
</script>

<style lang="scss" scoped>
.artist-detail {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
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

.error-container, .not-found {
  background-color: #fff8f8;
  border: 1px solid #ffd2d2;
  border-radius: 8px;
  padding: 20px;
  margin: 20px 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  
  .error-icon {
    font-size: 2rem;
    margin-bottom: 10px;
  }
  
  h3, h2 {
    color: #dc3545;
    margin-bottom: 10px;
  }
  
  p {
    margin-bottom: 20px;
  }
  
  .primary-button, .secondary-button {
    display: inline-block;
    padding: 10px 15px;
    border-radius: 4px;
    text-decoration: none;
    font-weight: bold;
    margin: 5px;
  }
  
  .primary-button {
    background-color: var(--secondary-color);
    color: white;
    
    &:hover {
      background-color: darken(#42b983, 5%);
    }
  }
  
  .secondary-button {
    background-color: #f8f9fa;
    border: 1px solid #ddd;
    color: #333;
    
    &:hover {
      background-color: #e9ecef;
    }
  }
}

.not-found {
  background-color: #f8faff;
  border: 1px solid #e6f0ff;
}

.back-link {
  margin-bottom: 20px;
  
  a {
    display: inline-flex;
    align-items: center;
    color: var(--primary-color);
    text-decoration: none;
    font-weight: 500;
    
    svg {
      margin-right: 5px;
    }
    
    &:hover {
      text-decoration: underline;
    }
  }
}

.artist-header {
  margin-bottom: 30px;
  
  h1 {
    color: var(--primary-color);
    margin-bottom: 10px;
  }
  
  .artist-meta {
    display: flex;
    gap: 15px;
    color: #666;
    
    .artist-dates, .artist-nationality {
      font-size: 1.1rem;
    }
  }
}

.artist-biography {
  margin-bottom: 40px;
  
  h2 {
    color: var(--primary-color);
    margin-bottom: 15px;
    font-size: 1.4rem;
  }
  
  .biography-content {
    line-height: 1.6;
    color: #333;
    white-space: pre-line;
  }
}

.artist-artworks {
  h2 {
    color: var(--primary-color);
    margin-bottom: 20px;
    font-size: 1.4rem;
  }
  
  .no-artworks {
    background-color: #f8faff;
    border: 1px solid #e6f0ff;
    border-radius: 8px;
    padding: 20px;
    text-align: center;
    color: #666;
  }
}

.artworks-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 20px;
  margin-bottom: 30px;
  
  .artwork-card-container {
    cursor: pointer;
    transition: transform 0.2s;
    
    &:hover {
      transform: translateY(-3px);
    }
  }
  
  .artwork-card {
    background-color: #fff;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    overflow: hidden;
    height: 100%;
    display: flex;
    flex-direction: column;
    
    .artwork-image {
      height: 200px;
      overflow: hidden;
      
      img {
        width: 100%;
        height: 100%;
        object-fit: cover;
        transition: transform 0.3s;
      }
    }
    
    &:hover .artwork-image img {
      transform: scale(1.05);
    }
    
    .artwork-info {
      padding: 15px;
      flex-grow: 1;
      display: flex;
      flex-direction: column;
      
      h3 {
        margin-bottom: 8px;
        color: var(--primary-color);
        font-size: 1.1rem;
      }
      
      .artwork-date {
        color: #666;
        font-size: 0.9rem;
        margin-bottom: 5px;
      }
      
      .artwork-role {
        margin-top: auto;
        font-size: 0.85rem;
        color: var(--secondary-color);
        font-weight: 500;
      }
    }
  }
}

@media (max-width: 768px) {
  .artist-meta {
    flex-direction: column;
    gap: 5px;
  }
  
  .artworks-grid {
    grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
  }
}
</style>