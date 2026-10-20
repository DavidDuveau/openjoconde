<template>
  <nav v-if="totalPages > 1" class="pagination" aria-label="Pagination">
    <button class="pagination-button" :disabled="page === 1" @click="$emit('change', page - 1)">
      &laquo; Précédent
    </button>

    <div class="page-numbers">
      <button
        v-for="number in pageNumbers"
        :key="number"
        class="pagination-number"
        :class="{ active: number === page }"
        @click="$emit('change', number)"
      >
        {{ number }}
      </button>
    </div>

    <button class="pagination-button" :disabled="page === totalPages" @click="$emit('change', page + 1)">
      Suivant &raquo;
    </button>
  </nav>
</template>

<script lang="ts">
import { defineComponent, computed } from 'vue';

export default defineComponent({
  name: 'PaginationBar',
  props: {
    page: { type: Number, required: true },
    totalPages: { type: Number, required: true }
  },
  emits: ['change'],
  setup(props) {
    // Au plus cinq numéros autour de la page courante
    const pageNumbers = computed(() => {
      let start = Math.max(1, props.page - 2);
      const end = Math.min(props.totalPages, start + 4);
      start = Math.max(1, end - 4);
      const numbers: number[] = [];
      for (let i = start; i <= end; i++) {
        numbers.push(i);
      }
      return numbers;
    });

    return { pageNumbers };
  }
});
</script>

<style lang="scss" scoped>
.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  margin-top: 30px;

  .pagination-button {
    background-color: #f8f9fa;
    border: 1px solid #ddd;
    padding: 8px 15px;
    border-radius: 4px;
    cursor: pointer;

    &:hover:not(:disabled) {
      background-color: #e9ecef;
    }

    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
  }

  .page-numbers {
    display: flex;
    margin: 0 10px;

    .pagination-number {
      margin: 0 3px;
      min-width: 32px;
      height: 32px;
      background-color: #f8f9fa;
      border: 1px solid #ddd;
      border-radius: 4px;
      cursor: pointer;

      &:hover:not(.active) {
        background-color: #e9ecef;
      }

      &.active {
        background-color: var(--secondary-color);
        color: white;
        border-color: var(--secondary-color);
      }
    }
  }
}

@media (max-width: 768px) {
  .pagination {
    flex-wrap: wrap;
    gap: 10px;

    .page-numbers {
      width: 100%;
      justify-content: center;
      margin: 0;
      order: 3;
    }
  }
}
</style>
