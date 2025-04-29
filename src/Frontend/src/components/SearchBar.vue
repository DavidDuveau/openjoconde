<template>
  <div class="search-bar">
    <form @submit.prevent="handleSearch" class="search-form">
      <div class="search-input-container">
        <input 
          type="text" 
          v-model="searchTerm" 
          :placeholder="placeholder"
          class="search-input"
          :class="{ 'has-error': showError }"
          @input="showError = false"
        />
        <button 
          type="button" 
          class="clear-button" 
          v-if="searchTerm" 
          @click="clearSearch"
          aria-label="Effacer la recherche"
        >
          <span>&times;</span>
        </button>
      </div>
      <button 
        type="submit" 
        class="search-button"
        :disabled="loading"
      >
        <span v-if="loading" class="loader"></span>
        <span v-else class="search-icon">
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="11" cy="11" r="8"></circle>
            <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
          </svg>
        </span>
        <span class="search-text">{{ buttonText }}</span>
      </button>
    </form>
    <div v-if="showError" class="error-message">
      Veuillez saisir un terme de recherche
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref, watch } from 'vue';
import { useRouter } from 'vue-router';

export default defineComponent({
  name: 'SearchBar',
  props: {
    placeholder: {
      type: String,
      default: 'Rechercher une œuvre, un artiste...'
    },
    buttonText: {
      type: String,
      default: 'Rechercher'
    },
    initialSearchTerm: {
      type: String,
      default: ''
    },
    loading: {
      type: Boolean,
      default: false
    },
    debounceTime: {
      type: Number,
      default: 300
    },
    minLength: {
      type: Number,
      default: 2
    }
  },
  emits: ['search', 'clear'],
  setup(props, { emit }) {
    const router = useRouter();
    const searchTerm = ref(props.initialSearchTerm);
    const showError = ref(false);
    let debounceTimeout: number | null = null;

    watch(() => props.initialSearchTerm, (newValue) => {
      searchTerm.value = newValue;
    });

    const handleSearch = () => {
      if (!searchTerm.value || searchTerm.value.trim().length < props.minLength) {
        showError.value = true;
        return;
      }

      emit('search', searchTerm.value.trim());
      
      // Naviguer vers la page de recherche si nous ne sommes pas déjà dessus
      if (router.currentRoute.value.name !== 'search') {
        router.push({
          name: 'search',
          query: { searchText: searchTerm.value.trim() }
        });
      }
    };

    const clearSearch = () => {
      searchTerm.value = '';
      showError.value = false;
      emit('clear');
    };

    const debouncedSearch = () => {
      if (debounceTimeout) {
        clearTimeout(debounceTimeout);
      }

      if (!searchTerm.value || searchTerm.value.trim().length < props.minLength) {
        return;
      }

      debounceTimeout = window.setTimeout(() => {
        emit('search', searchTerm.value.trim());
      }, props.debounceTime);
    };

    return {
      searchTerm,
      showError,
      handleSearch,
      clearSearch,
      debouncedSearch
    };
  }
});
</script>

<style scoped lang="scss">
.search-bar {
  width: 100%;
  position: relative;
  max-width: 600px;
}

.search-form {
  display: flex;
  gap: 8px;
}

.search-input-container {
  position: relative;
  flex-grow: 1;
}

.search-input {
  width: 100%;
  padding: 10px 30px 10px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.95rem;
  transition: border-color 0.2s;
  
  &:focus {
    outline: none;
    border-color: var(--secondary-color);
    box-shadow: 0 0 0 2px rgba(66, 185, 131, 0.2);
  }
  
  &.has-error {
    border-color: #dc3545;
  }
}

.clear-button {
  position: absolute;
  right: 8px;
  top: 50%;
  transform: translateY(-50%);
  border: none;
  background: transparent;
  color: #666;
  font-size: 1.2rem;
  line-height: 1;
  padding: 0;
  cursor: pointer;
  
  &:hover {
    color: #333;
  }
}

.search-button {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 15px;
  background-color: var(--secondary-color);
  color: white;
  border: none;
  border-radius: 4px;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s;
  min-width: 100px;
  
  &:hover {
    background-color: darken(#42b983, 5%);
  }
  
  &:disabled {
    background-color: #b9b9b9;
    cursor: not-allowed;
  }
}

.search-icon {
  margin-right: 6px;
  display: flex;
  align-items: center;
}

.error-message {
  color: #dc3545;
  font-size: 0.8rem;
  margin-top: 4px;
  position: absolute;
  left: 0;
}

.loader {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-radius: 50%;
  border-top-color: white;
  animation: spin 0.8s linear infinite;
  margin-right: 6px;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

@media (max-width: 768px) {
  .search-form {
    flex-direction: column;
  }
  
  .search-button {
    width: 100%;
    margin-top: 8px;
  }
  
  .error-message {
    position: static;
    margin-top: 8px;
  }
}
</style>
