<template>
  <span class="favorite" :class="{ compact }">
    <button
      type="button"
      class="favorite-button"
      data-testid="favorite-button"
      :aria-pressed="favori"
      :aria-label="favori ? `Retirer des favoris : ${label}` : `Ajouter aux favoris : ${label}`"
      @click.stop="basculer"
    >
      <span aria-hidden="true">{{ favori ? '★' : '☆' }}</span>
      <span v-if="!compact" class="favorite-text">{{ favori ? 'Retirer des favoris' : 'Ajouter aux favoris' }}</span>
    </button>
    <span v-if="message" class="favorite-message" data-testid="favorite-message" role="alert">{{ message }}</span>
  </span>
</template>

<script lang="ts">
import { defineComponent, computed, ref } from 'vue';
import { basculerFavori, favoris, MESSAGES_FAVORIS } from '@/utils/favoris';
import { displayTitle } from '@/utils/title';

// Un favori reste dans le navigateur : rien n'est envoyé à un serveur (voir epics/COMPTE.md)
export default defineComponent({
  name: 'FavoriteButton',
  props: {
    id: { type: String, required: true },
    title: { type: String, default: '' },
    /** Étoile seule, pour les cartes */
    compact: { type: Boolean, default: false }
  },
  setup(props) {
    const message = ref('');
    const favori = computed(() => favoris.favoris.some(f => f.id === props.id));
    const label = computed(() => displayTitle(props.title, 60));

    const basculer = () => {
      message.value = '';
      basculerFavori(props.id);
      if (favoris.probleme) message.value = MESSAGES_FAVORIS[favoris.probleme];
    };

    return { favori, label, message, basculer };
  }
});
</script>

<style scoped lang="scss">
.favorite {
  display: inline-flex;
  flex-direction: column;
  gap: 4px;

  .favorite-button {
    display: inline-flex;
    gap: 6px;
    align-items: center;
    padding: 6px 14px;
    border: 1px solid var(--secondary-color);
    border-radius: 4px;
    background: white;
    cursor: pointer;
    font-size: 1rem;

    &:hover {
      background: #eaf7f1;
    }
  }

  .favorite-message {
    font-size: 0.85rem;
    color: #a33;
  }

  &.compact .favorite-button {
    padding: 2px 8px;
    border-radius: 14px;
    font-size: 1.2rem;
    line-height: 1.4;
  }
}
</style>
