<template>
  <div class="museum-detail">
    <div v-if="loading" class="loading-container">
      <div class="loader"></div>
      <p>Chargement du musée...</p>
    </div>

    <div v-else-if="error" class="message error">
      <h3>Une erreur est survenue</h3>
      <p>{{ error }}</p>
      <button class="primary-button" @click="fetchMuseum">Réessayer</button>
      <router-link to="/museums" class="secondary-button">Retour aux musées</router-link>
    </div>

    <div v-else-if="!museum" class="message">
      <h2>Musée non trouvé</h2>
      <p>Le musée que vous recherchez n'existe pas ou a été supprimé.</p>
      <router-link to="/museums" class="primary-button">Retour aux musées</router-link>
    </div>

    <div v-else>
      <div class="back-link">
        <router-link to="/museums">&larr; Retour aux musées</router-link>
      </div>

      <header class="museum-header">
        <h1>{{ museum.name }}</h1>
        <p class="museum-place" v-if="place">{{ place }}</p>
      </header>

      <dl class="museum-facts">
        <template v-if="museum.address">
          <dt>Adresse</dt>
          <dd>{{ museum.address }}</dd>
        </template>
        <template v-if="museum.region">
          <dt>Région</dt>
          <dd>{{ museum.region }}</dd>
        </template>
        <template v-if="museum.code">
          <dt>Code Muséofile</dt>
          <dd>{{ museum.code }}</dd>
        </template>
        <template v-if="museum.phone">
          <dt>Téléphone</dt>
          <dd>{{ museum.phone }}</dd>
        </template>
        <template v-if="museum.email">
          <dt>Courriel</dt>
          <dd>{{ museum.email }}</dd>
        </template>
        <template v-if="museum.website">
          <dt>Site</dt>
          <dd>
            <a :href="museum.website" target="_blank" rel="noopener noreferrer">{{ museum.website }}</a>
          </dd>
        </template>
        <template v-if="hasCoordinates">
          <dt>Position</dt>
          <dd>
            <a :href="mapUrl" target="_blank" rel="noopener noreferrer">
              {{ museum.latitude }}, {{ museum.longitude }}
            </a>
          </dd>
        </template>
      </dl>

      <p class="museum-description" v-if="museum.description">{{ museum.description }}</p>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref, computed, onMounted, watch } from 'vue';
import { useRoute } from 'vue-router';
import ApiService from '@/services/api';
import { Museum } from '@/types/models';

export default defineComponent({
  name: 'MuseumDetailView',
  setup() {
    const route = useRoute();
    const museum = ref<Museum | null>(null);
    const loading = ref<boolean>(false);
    const error = ref<string | null>(null);

    const fetchMuseum = async () => {
      const museumId = route.params.id as string;
      if (!museumId) return;

      loading.value = true;
      error.value = null;

      try {
        museum.value = await ApiService.getMuseum(museumId);
      } catch (err: any) {
        console.error('Error fetching museum:', err);
        museum.value = null;
        // Un 404 est un musée inconnu, pas une panne
        if (err?.response?.status !== 404) {
          error.value = 'Erreur lors du chargement du musée. Veuillez réessayer.';
        }
      } finally {
        loading.value = false;
      }
    };

    onMounted(fetchMuseum);
    watch(() => route.params.id, (newId) => {
      if (newId) {
        fetchMuseum();
      }
    });

    const place = computed(() =>
      [museum.value?.zipCode, museum.value?.city, museum.value?.department].filter(Boolean).join(' · ')
    );
    const hasCoordinates = computed(() =>
      museum.value?.latitude != null && museum.value?.longitude != null
    );
    const mapUrl = computed(() =>
      `https://www.openstreetmap.org/?mlat=${museum.value?.latitude}&mlon=${museum.value?.longitude}#map=15/${museum.value?.latitude}/${museum.value?.longitude}`
    );

    return { museum, loading, error, fetchMuseum, place, hasCoordinates, mapUrl };
  }
});
</script>

<style lang="scss" scoped>
.museum-detail {
  max-width: 900px;
  margin: 0 auto;
  padding: 20px;
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
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

.message {
  background-color: #f8faff;
  border: 1px solid #e6f0ff;
  border-radius: 8px;
  padding: 24px;
  margin: 20px 0;
  text-align: center;

  &.error {
    background-color: #fff8f8;
    border-color: #ffd2d2;

    h3 {
      color: #dc3545;
    }
  }

  p {
    margin: 10px 0 20px;
  }

  .primary-button,
  .secondary-button {
    display: inline-block;
    padding: 10px 15px;
    border-radius: 4px;
    text-decoration: none;
    font-weight: bold;
    margin: 5px;
    cursor: pointer;
  }

  .primary-button {
    background-color: var(--secondary-color);
    color: white;
    border: none;
  }

  .secondary-button {
    background-color: #f8f9fa;
    border: 1px solid #ddd;
    color: #333;
  }
}

.back-link {
  margin-bottom: 20px;

  a {
    color: var(--primary-color);
    text-decoration: none;
    font-weight: 500;

    &:hover {
      text-decoration: underline;
    }
  }
}

.museum-header {
  margin-bottom: 25px;

  h1 {
    color: var(--primary-color);
    margin-bottom: 8px;
  }

  .museum-place {
    color: #666;
    font-size: 1.1rem;
  }
}

.museum-facts {
  display: grid;
  grid-template-columns: max-content 1fr;
  gap: 8px 20px;
  margin-bottom: 25px;

  dt {
    font-weight: bold;
    color: #444;
  }

  dd {
    margin: 0;
    overflow-wrap: anywhere;
  }
}

.museum-description {
  line-height: 1.6;
  white-space: pre-line;
}
</style>
