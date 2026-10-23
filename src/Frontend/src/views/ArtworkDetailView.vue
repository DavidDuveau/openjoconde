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
        <ShareLink :title="displayTitle(artwork.title, Infinity)" />
      </div>

      <div class="artwork-main">
        <div class="artwork-image" v-if="artwork.imageUrl">
          <img :src="artwork.imageUrl" :alt="artwork.title || artwork.reference" />
        </div>

        <div class="artwork-info">
          <h1 data-testid="artwork-heading">{{ displayTitle(artwork.title, Infinity) }}</h1>
          
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

            <div class="metadata-item" v-if="artwork.denomination">
              <h3>Dénomination</h3>
              <p>{{ artwork.denomination }}</p>
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

            <div class="metadata-item" v-if="artwork.creationPlace">
              <h3>Lieu de création</h3>
              <p>{{ artwork.creationPlace }}</p>
            </div>

            <div class="metadata-item" v-if="artwork.reference">
              <h3>Référence</h3>
              <p data-testid="artwork-reference">{{ artwork.reference }}</p>
            </div>

            <div class="metadata-item" v-if="artwork.museum || artwork.conservationPlace">
              <h3>Conservation</h3>
              <p>
                <router-link v-if="artwork.museum" :to="`/museums/${artwork.museum.id}`">
                  {{ artwork.museum.name }}
                </router-link>
                <span v-if="artwork.museum && artwork.conservationPlace"> · </span>
                <span v-if="artwork.conservationPlace">{{ artwork.conservationPlace }}</span>
              </p>
            </div>

            <div class="metadata-item" v-if="artwork.copyright">
              <h3>Droits</h3>
              <p>{{ artwork.copyright }}</p>
            </div>

            <div class="metadata-item" v-if="rightsText">
              <h3>Droits d'auteur</h3>
              <p data-testid="artwork-rights">{{ rightsText }}</p>
            </div>

            <div class="metadata-item" v-if="popUrl">
              <h3>Plateforme POP</h3>
              <p>
                <a :href="popUrl" target="_blank" rel="noopener noreferrer" data-testid="pop-link">{{ artwork.hasImage ? 'Voir l\'image sur POP' : 'Voir la notice sur POP' }}</a>
              </p>
            </div>

            <div class="metadata-item" v-if="siteUrl">
              <h3>Site associé</h3>
              <p>
                <a :href="siteUrl" target="_blank" rel="noopener noreferrer" data-testid="museum-site-link">Voir sur le site du musée</a>
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
                >
                  {{ domain.name }}
                </span>
              </div>
            </div>

            <div class="tag-group" v-if="artwork.techniques && artwork.techniques.length">
              <h3>Techniques</h3>
              <div class="tag-list">
                <router-link
                  v-for="technique in artwork.techniques"
                  :key="technique.id"
                  class="tag"
                  data-testid="technique-tag"
                  :to="{ name: 'search', query: { techniqueId: technique.id, techniqueName: technique.name } }"
                >
                  {{ technique.name }}
                </router-link>
              </div>
            </div>

            <div class="tag-group" v-if="artwork.periods && artwork.periods.length">
              <h3>Périodes</h3>
              <div class="tag-list">
                <router-link
                  v-for="period in artwork.periods"
                  :key="period.id"
                  class="tag"
                  data-testid="period-tag"
                  :to="{ name: 'search', query: { periodId: period.id, periodName: period.name } }"
                >
                  {{ period.name }}
                </router-link>
              </div>
            </div>
          </div>

          <DataSource kind="joconde" :reference="artwork.reference" :place="conservationText" />
        </div>
      </div>

      <ArtworkContext :artwork="artwork" />

      <ArtworkMuseum :artwork="artwork" />

      <ArtworkCitation :artwork="artwork" />

      <ArtworkExport :artwork="artwork" />

      <SimilarArtworks :artwork="artwork" />
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref, watch, computed } from 'vue';
import { useRoute } from 'vue-router';
import ApiService from '@/services/api';
import { displayTitle } from '@/utils/title';
import { rightsSummary, safeWebUrl } from '@/utils/rights';
import { popNoticeUrl } from '@/utils/pop';
import DataSource from '@/components/DataSource.vue';
import ArtworkContext from '@/components/ArtworkContext.vue';
import ArtworkMuseum from '@/components/ArtworkMuseum.vue';
import ArtworkCitation from '@/components/ArtworkCitation.vue';
import ArtworkExport from '@/components/ArtworkExport.vue';
import ShareLink from '@/components/ShareLink.vue';
import SimilarArtworks from '@/components/SimilarArtworks.vue';
import { ArtworkDetail } from '@/types/artwork';

export default defineComponent({
  name: 'ArtworkDetailView',
  components: { DataSource, ArtworkContext, ArtworkMuseum, ArtworkCitation, ArtworkExport, ShareLink, SimilarArtworks },
  setup() {
    const route = useRoute();

    const artwork = ref<ArtworkDetail | null>(null);
    const loading = ref(true);
    const error = ref(false);

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
        artwork.value = await ApiService.getArtwork(artworkId);
      } catch (err: any) {
        console.error("Erreur lors du chargement de l'œuvre:", err);
        artwork.value = null;
        // Un 404 est une œuvre inconnue, pas une panne
        error.value = err?.response?.status !== 404;
      } finally {
        loading.value = false;
      }
    };

    // Charger l'œuvre au premier affichage et quand l'identifiant change
    watch(() => route.params.id, (newId) => {
      if (newId) {
        fetchArtwork();
      }
    }, { immediate: true });

    const rightsText = computed(() => artwork.value ? rightsSummary(artwork.value.artistUnderCopyright, artwork.value.publicDomainYear) : null);
    const siteUrl = computed(() => artwork.value ? safeWebUrl(artwork.value.associatedSiteUrl) : null);
    const conservationText = computed(() =>
      [artwork.value?.museum?.name, artwork.value?.conservationPlace].filter(Boolean).join(' · '));
    const popUrl = computed(() => artwork.value ? popNoticeUrl(artwork.value.reference) : null);

    return {
      displayTitle,
      rightsText,
      siteUrl,
      popUrl,
      conservationText,
      artwork,
      loading,
      error,
      fetchArtwork
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
      display: flex;
      flex-wrap: wrap;
      gap: 12px;
      align-items: center;
      justify-content: space-between;
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
              }

              a.tag {
                color: inherit;
                text-decoration: none;

                &:hover {
                  background-color: #e2f3ea;
                }
              }
            }
          }
        }
      }
    }
  }
}
</style>
