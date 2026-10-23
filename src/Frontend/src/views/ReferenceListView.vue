<template>
  <div class="reference-page">
    <h1>{{ config.title }}</h1>

    <div class="search-filter">
      <input
        type="text"
        v-model="searchQuery"
        data-testid="reference-search"
        :placeholder="config.placeholder"
        @input="debounceSearch"
      />
    </div>

    <div v-if="loading" class="loading-container">
      <div class="loader"></div>
      <p>{{ config.loading }}</p>
    </div>

    <div v-else-if="error" class="error-container">
      <h3>Une erreur est survenue</h3>
      <p>{{ error }}</p>
      <button class="primary-button" @click="fetchItems">Réessayer</button>
    </div>

    <div v-else-if="items.length === 0" class="no-results">
      <h3>{{ config.none }}</h3>
      <p>Modifiez votre recherche ou essayez avec un autre terme.</p>
    </div>

    <div v-else>
      <p class="result-count" data-testid="reference-count">{{ totalItems }} {{ totalItems !== 1 ? config.plural : config.singular }}</p>

      <div class="reference-grid">
        <div
          v-for="item in items"
          :key="item.id"
          class="reference-card"
          data-testid="reference-card"
          @click="openItem(item)"
        >
          <h3>{{ item.name }}</h3>
        </div>
      </div>

      <PaginationBar :page="currentPage" :total-pages="totalPages" @change="goToPage" />
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, PropType, computed, ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import ApiService from '@/services/api';
import PaginationBar from '@/components/PaginationBar.vue';
import { NamedItem, PaginatedResult } from '@/types/models';

export type ReferenceKind = 'periods' | 'techniques';

interface ReferenceConfig {
  title: string;
  placeholder: string;
  loading: string;
  none: string;
  singular: string;
  plural: string;
  failure: string;
  fetch: (name: string, pageSize: number, page: number) => Promise<PaginatedResult<NamedItem>>;
  /** Paramètres de la recherche d'œuvres filtrée par cet élément : identifiant et libellé (réaffiché dans le filtre) */
  query: (item: NamedItem) => Record<string, string>;
}

// Une liste de référence par genre : les périodes et les techniques se parcourent de la même façon
const CONFIGS: Record<ReferenceKind, ReferenceConfig> = {
  periods: {
    title: 'Périodes',
    placeholder: 'Rechercher une période...',
    loading: 'Chargement des périodes...',
    none: 'Aucune période trouvée',
    singular: 'période',
    plural: 'périodes',
    failure: 'Erreur lors du chargement des périodes. Veuillez réessayer.',
    fetch: (name, pageSize, page) => ApiService.getPeriods(name, pageSize, page),
    query: item => ({ periodId: item.id, periodName: item.name })
  },
  techniques: {
    title: 'Techniques',
    placeholder: 'Rechercher une technique...',
    loading: 'Chargement des techniques...',
    none: 'Aucune technique trouvée',
    singular: 'technique',
    plural: 'techniques',
    failure: 'Erreur lors du chargement des techniques. Veuillez réessayer.',
    fetch: (name, pageSize, page) => ApiService.getTechniques(name, pageSize, page),
    query: item => ({ techniqueId: item.id, techniqueName: item.name })
  }
};

export default defineComponent({
  name: 'ReferenceListView',
  components: { PaginationBar },
  props: {
    kind: { type: String as PropType<ReferenceKind>, required: true }
  },
  setup(props) {
    const router = useRouter();
    const config = computed(() => CONFIGS[props.kind]);
    const items = ref<NamedItem[]>([]);
    const loading = ref<boolean>(false);
    const error = ref<string | null>(null);
    const currentPage = ref<number>(1);
    const pageSize = 24;
    const totalItems = ref<number>(0);
    const totalPages = ref<number>(1);
    const searchQuery = ref<string>('');
    let searchTimeout: number | null = null;

    const fetchItems = async () => {
      loading.value = true;
      error.value = null;

      try {
        const result = await config.value.fetch(searchQuery.value.trim(), pageSize, currentPage.value);
        items.value = result.items;
        totalItems.value = result.totalItems;
        totalPages.value = result.totalPages;
      } catch (err) {
        console.error('Error fetching reference list:', err);
        error.value = config.value.failure;
        items.value = [];
      } finally {
        loading.value = false;
      }
    };

    onMounted(fetchItems);

    // Recherche différée pour ne pas interroger l'API à chaque touche
    const debounceSearch = () => {
      if (searchTimeout) {
        clearTimeout(searchTimeout);
      }
      searchTimeout = setTimeout(() => {
        currentPage.value = 1;
        fetchItems();
      }, 300) as unknown as number;
    };

    const goToPage = (page: number) => {
      currentPage.value = page;
      fetchItems();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    };

    const openItem = (item: NamedItem) => {
      router.push({ name: 'search', query: config.value.query(item) });
    };

    return {
      config,
      items,
      loading,
      error,
      currentPage,
      totalItems,
      totalPages,
      searchQuery,
      fetchItems,
      debounceSearch,
      goToPage,
      openItem
    };
  }
});
</script>

<style lang="scss" scoped>
.reference-page {
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

.reference-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 16px;
}

.reference-card {
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
