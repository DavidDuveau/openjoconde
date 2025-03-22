<template>
  <div class="artwork-detail">
    <div v-if="loading" class="loading">
      <p>Chargement de l'œuvre...</p>
    </div>

    <div v-else-if="error" class="error">
      <p>Une erreur est survenue lors du chargement de l'œuvre. Veuillez réessayer.</p>
      <button @click="fetchArtwork">Réessayer</button>
      <router-link to="/artworks" class="back-link">Retour à la liste</router-link>
    </div>

    <div v-else-if="!artwork" class="not-found">
      <p>Cette œuvre n'existe pas ou a été supprimée.</p>
      <router-link to="/artworks" class="back-link">Retour à la liste</router-link>
    </div>

    <div v-else class="artwork-content">
      <div class="artwork-header">
        <router-link to="/artworks" class="back-link">
          &larr; Retour à la liste
        </router-link>
      </div>

      <div class="artwork-main">
        <div class="artwork-image">
          <img :src="artwork.imageUrl || '/placeholder-image.jpg'" :alt="artwork.title" />
        </div>

        <div class="artwork-info">
          <h1>{{ artwork.title || 'Sans titre' }}</h1>
          
          <div class="metadata">
            <div class="metadata-item" v-if="artwork.artists && artwork.artists.length">
              <h3>Artiste(s)</h3>
              <p>
                <span v-for="(artist, index) in artwork.artists" :key="artist.id">
                  <router-link :to="`/artists/${artist.id}`">
                    {{ `${artist.firstName || ''} ${artist.lastName || ''}`.trim() }}
                  </router-link>
                  <span v-if="index < artwork.artists.length - 1">, </span>
                </span>
              </p>
            </div>

            <div class="metadata-item" v-if="artwork.creationDate">
              <h3>Date</h3>
              <p>{{ artwork.creationDate }}</p>
            </div>

            <div class="metadata-item" v-if="artwork.dimensions">
              <h3>Dimensions</h3>
              <p>{{ artwork.dimensions }}</p>
            </div>

            <div class="metadata-item" v-if="artwork.inventoryNumber">
              <h3>Numéro d'inventaire</h3>
              <p>{{ artwork.inventoryNumber }}</p>
            </div>

            <div class="metadata-item" v-if="artwork.reference">
              <h3>Référence</h3>
              <p>{{ artwork.reference }}</p>
            </div>

            <div class="metadata-item" v-if="artwork.museum">
              <h3>Conservation</h3>
              <p>
                <router-link :to="`/museums/${artwork.museum.id}`">
                  {{ artwork.museum.name }}
                </router-link>
              </p>
            </div>
          </div>

          <div class="description" v-if="artwork.description">
            <h3>Description</h3>
            <p>{{ artwork.description }}</p>
          </div>

          <div class="tags">
            <div class="tag-group" v-if="artwork.domains && artwork.domains.length">
              <h3>Domaines</h3>
              <div class="tag-list">
                <span 
                  v-for="domain in artwork.domains" 
                  :key="domain.id" 
                  class="tag"
                  @click="navigateWithFilter('domain', domain.id)"
                >
                  {{ domain.name }}
                </span>
              </div>
            </div>

            <div class="tag-group" v-if="artwork.techniques && artwork.techniques.length">
              <h3>Techniques</h3>
              <div class="tag-list">
                <span 
                  v-for="technique in artwork.techniques" 
                  :key="technique.id" 
                  class="tag"
                  @click="navigateWithFilter('technique', technique.id)"
                >
                  {{ technique.name }}
                </span>
              </div>
            </div>

            <div class="tag-group" v-if="artwork.periods && artwork.periods.length">
              <h3>Périodes</h3>
              <div class="tag-list">
                <span 
                  v-for="period in artwork.periods" 
                  :key="period.id" 
                  class="tag"
                  @click="navigateWithFilter('period', period.id)"
                >
                  {{ period.name }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="artwork-footer" v-if="relatedArtworks && relatedArtworks.length">
        <h2>Œuvres similaires</h2>
        <div class="related-artworks">
          <div 
            v-for="related in relatedArtworks" 
            :key="related.id" 
            class="related-artwork"
            @click="navigateTo(`/artworks/${related.id}`)"
          >
            <div class="related-artwork-image">
              <img :src="related.imageUrl || '/placeholder-image.jpg'" :alt="related.title" />
            </div>
            <div class="related-artwork-info">
              <h3>{{ related.title || 'Sans titre' }}</h3>
              <p v-if="related.artists && related.artists.length">
                {{ related.artists.map(a => `${a.firstName || ''} ${a.lastName || ''}`.trim()).join(', ') }}
              </p>
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

// Types
interface ArtworkDetail {
  id: string;
  title: string;
  reference: string;
  inventoryNumber: string;
  imageUrl: string;
  description: string;
  dimensions: string;
  creationDate: string;
  creationPlace: string;
  museum: {
    id: string;
    name: string;
  };
  artists: {
    id: string;
    firstName: string;
    lastName: string;
    role?: string;
  }[];
  domains: {
    id: string;
    name: string;
  }[];
  techniques: {
    id: string;
    name: string;
  }[];
  periods: {
    id: string;
    name: string;
  }[];
}

interface RelatedArtwork {
  id: string;
  title: string;
  imageUrl: string;
  artists: {
    id: string;
    firstName: string;
    lastName: string;
  }[];
}

export default defineComponent({
  name: 'ArtworkDetailView',
  setup() {
    const route = useRoute();
    const router = useRouter();
    
    const artwork = ref<ArtworkDetail | null>(null);
    const relatedArtworks = ref<RelatedArtwork[]>([]);
    const loading = ref(true);
    const error = ref(false);
    
    // Charger les détails de l'œuvre quand l'ID change
    watch(() => route.params.id, (newId) => {
      if (newId) {
        fetchArtwork();
      }
    }, { immediate: true });
    
    const fetchArtwork = async () => {
      const artworkId = route.params.id as string;
      
      if (!artworkId) {
        error.value = true;
        loading.value = false;
        return;
      }
      
      loading.value = true;
      error.value = false;
      
      try {
        // Dans une implémentation réelle, on ferait un appel API ici
        // Exemple: const response = await artworkService.getArtworkById(artworkId);
        
        // Simulation d'une réponse d'API pour le moment
        await new Promise(resolve => setTimeout(resolve, 800));
        
        // Données factices pour le prototype
        if (artworkId === '1') {
          artwork.value = {
            id: '1',
            title: 'La Joconde',
            reference: 'INV779',
            inventoryNumber: 'INV779',
            imageUrl: 'https://via.placeholder.com/800x600',
            description: 'Portrait de Lisa Gherardini, épouse de Francesco del Giocondo, réalisé par Léonard de Vinci entre 1503 et 1519.',
            dimensions: '77 cm × 53 cm',
            creationDate: '1503-1519',
            creationPlace: 'Florence, Italie',
            museum: {
              id: '1',
              name: 'Musée du Louvre'
            },
            artists: [
              {
                id: '1',
                firstName: 'Leonardo',
                lastName: 'da Vinci',
                role: 'Peintre'
              }
            ],
            domains: [
              {
                id: '1',
                name: 'Peinture'
              }
            ],
            techniques: [
              {
                id: '1',
                name: 'Huile sur panneau de bois'
              }
            ],
            periods: [
              {
                id: '1',
                name: 'Renaissance'
              }
            ]
          };
          
          // Œuvres similaires fictives
          relatedArtworks.value = [
            {
              id: '2',
              title: 'La Liberté guidant le peuple',
              imageUrl: 'https://via.placeholder.com/300x200',
              artists: [{ id: '2', firstName: 'Eugène', lastName: 'Delacroix' }]
            },
            {
              id: '3',
              title: 'Le Radeau de la Méduse',
              imageUrl: 'https://via.placeholder.com/300x200',
              artists: [{ id: '3', firstName: 'Théodore', lastName: 'Géricault' }]
            },
            {
              id: '4',
              title: 'La Naissance de Vénus',
              imageUrl: 'https://via.placeholder.com/300x200',
              artists: [{ id: '4', firstName: 'Sandro', lastName: 'Botticelli' }]
            }
          ];
        } else {
          // Si l'ID n'est pas reconnu, renvoyer null (œuvre non trouvée)
          artwork.value = null;
          relatedArtworks.value = [];
        }
      } catch (err) {
        console.error('Erreur lors du chargement de l\'œuvre:', err);
        error.value = true;
      } finally {
        loading.value = false;
      }
    };
    
    const navigateTo = (path: string) => {
      router.push(path);
    };
    
    const navigateWithFilter = (filterType: string, filterId: string) => {
      router.push({
        path: '/artworks',
        query: { [filterType]: filterId }
      });
    };
    
    return {
      artwork,
      relatedArtworks,
      loading,
      error,
      fetchArtwork,
      navigateTo,
      navigateWithFilter
    };
  }
});
</script>

<style scoped lang="scss">
.artwork-detail {
  .loading, .error, .not-found {
    text-align: center;
    padding: 40px;
    background-color: white;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
    margin-bottom: 24px;

    button, .back-link {
      display: inline-block;
      margin-top: 16px;
      padding: 10px 20px;
      background-color: var(--secondary-color);
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: bold;
      text-decoration: none;
      
      &:hover {
        background-color: darken(#42b983, 10%);
      }
    }
  }

  .artwork-content {
    .artwork-header {
      margin-bottom: 20px;
      
      .back-link {
        display: inline-block;
        padding: 8px 16px;
        color: var(--primary-color);
        text-decoration: none;
        font-weight: bold;
        transition: color 0.3s;
        
        &:hover {
          color: var(--secondary-color);
        }
      }
    }
    
    .artwork-main {
      display: grid;
      grid-template-columns: 1fr;
      gap: 32px;
      margin-bottom: 40px;
      
      @media (min-width: 768px) {
        grid-template-columns: minmax(300px, 1fr) 1fr;
      }
      
      .artwork-image {
        background-color: white;
        border-radius: 8px;
        overflow: hidden;
        box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
        
        img {
          width: 100%;
          height: auto;
          display: block;
        }
      }
      
      .artwork-info {
        h1 {
          margin-bottom: 24px;
          color: var(--primary-color);
        }
        
        .metadata {
          display: grid;
          grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
          gap: 16px;
          margin-bottom: 24px;
          
          .metadata-item {
            h3 {
              font-size: 0.9rem;
              color: #666;
              margin-bottom: 4px;
            }
            
            p {
              font-size: 1.1rem;
              
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
        
        .description {
          margin-bottom: 24px;
          
          h3 {
            font-size: 1.2rem;
            margin-bottom: 8px;
            color: var(--primary-color);
          }
          
          p {
            line-height: 1.6;
          }
        }
        
        .tags {
          .tag-group {
            margin-bottom: 16px;
            
            h3 {
              font-size: 1rem;
              margin-bottom: 8px;
              color: var(--primary-color);
            }
            
            .tag-list {
              display: flex;
              flex-wrap: wrap;
              gap: 8px;
              
              .tag {
                display: inline-block;
                padding: 6px 12px;
                background-color: #f0f0f0;
                border-radius: 16px;
                font-size: 0.9rem;
                cursor: pointer;
                transition: background-color 0.3s;
                
                &:hover {
                  background-color: #e0e0e0;
                }
              }
            }
          }
        }
      }
    }
    
    .artwork-footer {
      margin-top: 40px;
      
      h2 {
        margin-bottom: 20px;
        color: var(--primary-color);
      }
      
      .related-artworks {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
        gap: 20px;
        
        .related-artwork {
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
          
          .related-artwork-image {
            img {
              width: 100%;
              height: 150px;
              object-fit: cover;
            }
          }
          
          .related-artwork-info {
            padding: 12px;
            
            h3 {
              font-size: 1rem;
              margin-bottom: 4px;
              color: var(--primary-color);
            }
            
            p {
              font-size: 0.8rem;
              color: #666;
            }
          }
        }
      }
    }
  }
}
</style>
