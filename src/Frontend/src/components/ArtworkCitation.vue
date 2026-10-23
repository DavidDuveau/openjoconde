<template>
  <section class="artwork-citation" data-testid="artwork-citation" aria-labelledby="artwork-citation-title">
    <h2 id="artwork-citation-title">Citer cette œuvre</h2>

    <div class="formats" role="radiogroup" aria-label="Format de la citation">
      <label v-for="f in formats" :key="f.id">
        <input type="radio" name="format-citation" :value="f.id" v-model="format" data-testid="citation-format" />
        {{ f.label }}
      </label>
    </div>

    <blockquote class="citation" data-testid="citation-text">{{ citation }}</blockquote>

    <div class="actions">
      <button type="button" class="copy-button" data-testid="citation-copy" @click="copy">Copier la citation</button>
      <span class="status" data-testid="citation-status" role="status" aria-live="polite">{{ status }}</span>
    </div>
  </section>
</template>

<script lang="ts">
import { computed, defineComponent, PropType, ref } from 'vue';
import { FORMATS, FormatCitation, citer } from '@/utils/citation';
import { ArtworkDetail } from '@/types/artwork';

// La citation de l'œuvre, prête à coller dans un travail : trois formats, la source (notice et adresse POP) et la date de
// consultation. Elle est calculée dans le navigateur à partir de la notice.
export default defineComponent({
  name: 'ArtworkCitation',
  props: {
    artwork: { type: Object as PropType<ArtworkDetail>, required: true },
    /** Date de consultation, injectable pour les tests */
    today: { type: Object as PropType<Date>, default: () => new Date() }
  },
  setup(props) {
    const format = ref<FormatCitation>('iso690');
    const status = ref('');

    const citation = computed(() => citer(props.artwork, format.value, props.today));

    const copy = async () => {
      try {
        await navigator.clipboard.writeText(citation.value);
        status.value = 'Citation copiée.';
      } catch {
        // Presse-papiers refusé ou absent : la citation est affichée, elle se sélectionne à la main
        status.value = 'La copie automatique n\'a pas fonctionné : sélectionnez la citation ci-dessus.';
      }
    };

    return { formats: FORMATS, format, citation, status, copy };
  }
});
</script>

<style scoped lang="scss">
.artwork-citation {
  margin: 32px 0;
  padding: 20px;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);

  h2 {
    margin-bottom: 8px;
    color: var(--primary-color);
  }

  .formats {
    display: flex;
    gap: 16px;
    margin-bottom: 8px;
  }

  .citation {
    margin: 8px 0;
    padding: 10px 14px;
    border-left: 3px solid var(--secondary-color);
    background: #f8f9fa;
    line-height: 1.5;
    user-select: all;
  }

  .actions {
    display: flex;
    gap: 12px;
    align-items: center;
  }

  .copy-button {
    padding: 6px 14px;
    border: 1px solid var(--secondary-color);
    border-radius: 4px;
    background: white;
    cursor: pointer;
  }

  .status {
    font-size: 0.85rem;
    color: #555;
  }
}
</style>
