<template>
  <section v-if="museum" class="museum-summary" data-testid="museum-summary">
    <h2>
      <router-link :to="{ name: 'museum-detail', params: { id: museum.id } }" data-testid="museum-summary-name">{{ museum.name }}</router-link>
    </h2>
    <p v-if="address" data-testid="museum-summary-address">{{ address }}</p>
    <p v-if="siteUrl">
      <a :href="siteUrl" target="_blank" rel="noopener noreferrer" data-testid="museum-summary-site">Site web du musée</a>
    </p>
  </section>
</template>

<script lang="ts">
import { computed, defineComponent, ref, watch } from 'vue';
import ApiService from '@/services/api';
import { safeWebUrl } from '@/utils/rights';
import { Museum } from '@/types/models';

export default defineComponent({
  name: 'MuseumSummary',
  props: {
    museumId: { type: String, required: true }
  },
  setup(props) {
    const museum = ref<Museum | null>(null);

    watch(() => props.museumId, async (id) => {
      museum.value = null;
      try {
        const loaded = await ApiService.getMuseum(id);
        // Le musée a pu changer pendant l'attente : la réponse d'un musée quitté ne s'affiche pas
        if (id === props.museumId) museum.value = loaded;
      } catch (error) {
        // Un résumé manquant ne gêne pas la liste des œuvres
        console.error('Erreur lors du chargement du musée:', error);
      }
    }, { immediate: true });

    const address = computed(() => {
      const city = [museum.value?.zipCode, museum.value?.city].filter(Boolean).join(' ');
      return [museum.value?.address, city].filter(Boolean).join(', ');
    });
    const siteUrl = computed(() => safeWebUrl(museum.value?.website));

    return { museum, address, siteUrl };
  }
});
</script>

<style scoped lang="scss">
.museum-summary {
  margin-bottom: 20px;
  padding: 16px;
  background-color: white;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.08);

  h2 {
    margin-bottom: 8px;
    font-size: 1.2rem;

    a {
      color: var(--primary-color);
      text-decoration: none;
    }
  }

  a {
    color: var(--secondary-color);
  }
}
</style>
