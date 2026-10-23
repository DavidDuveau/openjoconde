<template>
  <section v-if="artwork.museum" class="artwork-museum" data-testid="artwork-museum" aria-labelledby="artwork-museum-title">
    <h2 id="artwork-museum-title">Le musée</h2>

    <h3>
      <router-link :to="{ name: 'museum-detail', params: { id: artwork.museum.id } }" data-testid="artwork-museum-link">{{ artwork.museum.name }}</router-link>
    </h3>
    <p v-if="localisation" class="museum-place" data-testid="artwork-museum-place">{{ localisation }}</p>
    <p v-if="artwork.conservationPlace" class="conservation" data-testid="artwork-conservation">
      Lieu de conservation de l'œuvre : {{ artwork.conservationPlace }}
    </p>

    <MuseumVisit v-if="museum" :museum="museum" />

    <p class="museum-more">
      <router-link :to="{ name: 'museum-detail', params: { id: artwork.museum.id } }">Voir le musée et toute sa collection</router-link>
    </p>
  </section>
</template>

<script lang="ts">
import { computed, defineComponent, PropType, ref, watch } from 'vue';
import ApiService from '@/services/api';
import MuseumVisit from '@/components/MuseumVisit.vue';
import { Museum } from '@/types/models';
import { ArtworkDetail } from '@/types/artwork';

// Le musée de l'œuvre : son nom mène à sa page, ses coordonnées et de quoi s'y rendre en dessous. Si la fiche du musée
// ne se charge pas, le nom et le lien restent.
export default defineComponent({
  name: 'ArtworkMuseum',
  components: { MuseumVisit },
  props: {
    artwork: { type: Object as PropType<ArtworkDetail>, required: true }
  },
  setup(props) {
    const museum = ref<Museum | null>(null);

    const charger = async () => {
      museum.value = null;
      const attendu = props.artwork.museum?.id;
      if (!attendu) return;
      try {
        const fiche = await ApiService.getMuseum(attendu);
        // Une réponse tardive d'une œuvre déjà quittée ne s'affiche pas
        if (props.artwork.museum?.id === attendu) museum.value = fiche;
      } catch (err) {
        console.error('Erreur lors du chargement du musée de l\'œuvre:', err);
      }
    };
    watch(() => props.artwork.museum?.id, charger, { immediate: true });

    const localisation = computed(() =>
      [museum.value?.city, museum.value?.department, museum.value?.region].filter(Boolean).join(' · '));

    return { museum, localisation };
  }
});
</script>

<style scoped lang="scss">
.artwork-museum {
  margin: 32px 0;
  padding: 20px;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);

  h2 {
    margin-bottom: 8px;
    color: var(--primary-color);
  }

  h3 {
    margin: 0 0 4px;
  }

  .museum-place,
  .conservation {
    margin: 2px 0;
    color: #555;
  }

  .museum-more {
    margin-top: 12px;
  }
}
</style>
