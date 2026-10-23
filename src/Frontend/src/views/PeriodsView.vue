<template>
  <div class="periods-page">
    <h1>Périodes</h1>

    <div class="search-filter">
      <input
        type="text"
        v-model="searchQuery"
        data-testid="period-search"
        placeholder="Rechercher une période..."
        @input="debounceSearch"
      />
    </div>

    <div v-if="loading" class="loading-container">
      <div class="loader"></div>
      <p>Chargement des périodes...</p>
    </div>

    <div v-else-if="error" class="error-container">
      <h3>Une erreur est survenue</h3>
      <p>{{ error }}</p>
      <button class="primary-button" @click="fetchPeriods">Réessayer</button>
    </div>

    <div v-else-if="periods.length === 0" class="no-results">
      <h3>Aucune période trouvée</h3>
      <p>Modifiez votre recherche ou essayez avec un autre terme.</p>
    </div>

    <div v-else>
      <p class="result-count" data-testid="period-count">{{ totalItems }} période{{ totalItems !== 1 ? 's' : '' }}</p>

      <div class="periods-grid">
        <div
          v-for="period in periods"
          :key="period.id"
          class="period-card"
          data-testid="period-card"
          @click="openPeriod(period)"
        >
          <h3>{{ period.name }}</h3>
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
import { NamedItem } from '@/types/models';

export default defineComponent({
  name: 'PeriodsView',
  components: { PaginationBar },
  setup() {
    const router = useRouter();
    const periods = ref<NamedItem[]>([]);
    const loading = ref<boolean>(false);
    const error = ref<string | null>(null);
    const currentPage = ref<number>(1);
    const pageSize = 24;
    const totalItems = ref<number>(0);
    const totalPages = ref<number>(1);
    const searchQuery = ref<string>('');
    let searchTimeout: number | null = null;

    const fetchPeriods = async () => {
      loading.value = true;
      error.value = null;

      try {
        const result = await ApiService.getPeriods(searchQuery.value.trim(), pageSize, currentPage.value);
        periods.value = result.items;
        totalItems.value = result.totalItems;
        totalPages.value = result.totalPages;
      } catch (err) {
        console.error('Error fetching periods:', err);
        error.value = 'Erreur lors du chargement des périodes. Veuillez réessayer.';
        periods.value = [];
      } finally {
        loading.value = false;
      }
    };

    onMounted(fetchPeriods);

    // Recherche différée pour ne pas interroger l'API à chaque touche
    const debounceSearch = () => {
      if (searchTimeout) {
        clearTimeout(searchTimeout);
      }
      searchTimeout = setTimeout(() => {
        currentPage.value = 1;
        fetchPeriods();
      }, 300) as unknown as number;
    };

    const goToPage = (page: number) => {
      currentPage.value = page;
      fetchPeriods();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    };

    // Les œuvres d'une période : la recherche filtrée par période, dont le libellé suit dans l'adresse
    const openPeriod = (period: NamedItem) => {
      router.push({ name: 'search', query: { periodId: period.id, periodName: period.name } });
    };

    return {
      periods,
      loading,
      error,
      currentPage,
      totalItems,
      totalPages,
      searchQuery,
      fetchPeriods,
      debounceSearch,
      goToPage,
      openPeriod
    };
  }
});
</script>

<style lang="scss" scoped>
.periods-page {
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

.periods-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 16px;
}

.period-card {
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
    color: var(--primary-color);
    font-size: 1.05rem;
  }
}
</style>
