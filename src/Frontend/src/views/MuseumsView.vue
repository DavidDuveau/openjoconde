<template>
  <div class="museums-page">
    <h1>Musées</h1>

    <div class="search-filter">
      <input
        type="text"
        v-model="searchQuery"
        data-testid="museum-search"
        placeholder="Rechercher un musée, une ville, un département..."
        @input="debounceSearch"
      />
    </div>

    <div v-if="loading" class="loading-container">
      <div class="loader"></div>
      <p>Chargement des musées...</p>
    </div>

    <div v-else-if="error" class="error-container">
      <h3>Une erreur est survenue</h3>
      <p>{{ error }}</p>
      <button class="primary-button" @click="fetchMuseums">Réessayer</button>
    </div>

    <div v-else-if="museums.length === 0" class="no-results">
      <h3>Aucun musée trouvé</h3>
      <p>Modifiez votre recherche ou essayez avec un autre terme.</p>
    </div>

    <div v-else>
      <p class="result-count" data-testid="museum-count">{{ totalItems }} musée{{ totalItems !== 1 ? 's' : '' }}</p>

      <div class="museums-grid">
        <div
          v-for="museum in museums"
          :key="museum.id"
          class="museum-card"
          data-testid="museum-card"
          @click="navigateToMuseum(museum.id)"
        >
          <h3>{{ museum.name }}</h3>
          <p class="museum-place" v-if="museum.city || museum.department">
            {{ [museum.city, museum.department].filter(Boolean).join(' · ') }}
          </p>
          <p class="museum-region" v-if="museum.region">{{ museum.region }}</p>
        </div>
      </div>

      <PaginationBar :page="currentPage" :total-pages="totalPages" @change="goToPage" />
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import ApiService from '@/services/api';
import PaginationBar from '@/components/PaginationBar.vue';
import { Museum } from '@/types/models';

export default defineComponent({
  name: 'MuseumsView',
  components: { PaginationBar },
  setup() {
    const router = useRouter();
    const museums = ref<Museum[]>([]);
    const loading = ref<boolean>(false);
    const error = ref<string | null>(null);
    const currentPage = ref<number>(1);
    const pageSize = ref<number>(12);
    const totalItems = ref<number>(0);
    const totalPages = ref<number>(1);
    const searchQuery = ref<string>('');
    let searchTimeout: number | null = null;

    const fetchMuseums = async () => {
      loading.value = true;
      error.value = null;

      try {
        const result = await ApiService.getMuseums(currentPage.value, pageSize.value, searchQuery.value.trim());
        museums.value = result.items;
        totalItems.value = result.totalItems;
        totalPages.value = result.totalPages;
      } catch (err) {
        console.error('Error fetching museums:', err);
        error.value = 'Erreur lors du chargement des musées. Veuillez réessayer.';
        museums.value = [];
      } finally {
        loading.value = false;
      }
    };

    onMounted(fetchMuseums);

    // Recherche différée pour ne pas interroger l'API à chaque touche
    const debounceSearch = () => {
      if (searchTimeout) {
        clearTimeout(searchTimeout);
      }
      searchTimeout = setTimeout(() => {
        currentPage.value = 1;
        fetchMuseums();
      }, 300) as unknown as number;
    };

    const goToPage = (page: number) => {
      currentPage.value = page;
      fetchMuseums();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    };

    const navigateToMuseum = (id: string) => {
      router.push(`/museums/${id}`);
    };

    return {
      museums,
      loading,
      error,
      currentPage,
      totalItems,
      totalPages,
      searchQuery,
      fetchMuseums,
      debounceSearch,
      goToPage,
      navigateToMuseum
    };
  }
});
</script>

<style lang="scss" scoped>
.museums-page {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;

  h1 {
    text-align: center;
    margin-bottom: 30px;
    color: var(--primary-color);
  }
}

.search-filter {
  margin: 0 auto 20px;
  max-width: 500px;

  input {
    width: 100%;
    padding: 10px 15px;
    border: 1px solid #ddd;
    border-radius: 4px;
    font-size: 1rem;

    &:focus {
      outline: none;
      border-color: var(--secondary-color);
      box-shadow: 0 0 0 2px rgba(66, 185, 131, 0.2);
    }
  }
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

.error-container,
.no-results {
  border-radius: 8px;
  padding: 24px;
  margin: 20px 0;
  text-align: center;
}

.error-container {
  background-color: #fff8f8;
  border: 1px solid #ffd2d2;

  h3 {
    color: #dc3545;
    margin-bottom: 10px;
  }

  p {
    margin-bottom: 15px;
  }
}

.no-results {
  background-color: #f8faff;
  border: 1px solid #e6f0ff;
}

.primary-button {
  background-color: var(--secondary-color);
  color: white;
  border: none;
  padding: 8px 15px;
  border-radius: 4px;
  cursor: pointer;
  font-weight: bold;
}

.result-count {
  color: #666;
  margin-bottom: 15px;
}

.museums-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 20px;
}

.museum-card {
  background-color: #fff;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  padding: 20px;
  cursor: pointer;
  transition: transform 0.2s, box-shadow 0.2s;

  &:hover {
    transform: translateY(-3px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  }

  h3 {
    margin-bottom: 8px;
    color: var(--primary-color);
  }

  .museum-place,
  .museum-region {
    color: #666;
    font-size: 0.9rem;
    margin-bottom: 4px;
  }
}

@media (max-width: 768px) {
  .museums-grid {
    grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  }
}
</style>
