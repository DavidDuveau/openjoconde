<template>
  <section class="artwork-export" data-testid="artwork-export" aria-labelledby="artwork-export-title">
    <h2 id="artwork-export-title">Exporter les données</h2>
    <p class="note">
      Les données de cette notice, avec la source et la licence ({{ licence }}) : à citer en les réutilisant.
    </p>
    <div class="actions">
      <button type="button" class="export-button" data-testid="export-json" @click="exporter('json')">Télécharger en JSON</button>
      <button type="button" class="export-button" data-testid="export-csv" @click="exporter('csv')">Télécharger en CSV</button>
    </div>
  </section>
</template>

<script lang="ts">
import { defineComponent, PropType } from 'vue';
import { nomDeFichier, telecharger, versCsv, versJson } from '@/utils/exportOeuvre';
import { ArtworkDetail } from '@/types/artwork';

// L'export d'une œuvre, calculé dans le navigateur à partir de la fiche affichée : rien n'est demandé de plus à l'API.
export default defineComponent({
  name: 'ArtworkExport',
  props: {
    artwork: { type: Object as PropType<ArtworkDetail>, required: true },
    /** Date de consultation inscrite dans l'export, injectable pour les tests */
    today: { type: Object as PropType<Date>, default: () => new Date() }
  },
  setup(props) {
    const exporter = (format: 'json' | 'csv') => {
      const contenu = format === 'json' ? versJson(props.artwork, props.today) : versCsv(props.artwork, props.today);
      telecharger(nomDeFichier(props.artwork, format), contenu, format === 'json' ? 'application/json' : 'text/csv');
    };

    return { exporter, licence: 'Licence Ouverte 2.0' };
  }
});
</script>

<style scoped lang="scss">
.artwork-export {
  margin: 32px 0;
  padding: 20px;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);

  h2 {
    margin-bottom: 8px;
    color: var(--primary-color);
  }

  .note {
    margin: 0 0 10px;
    color: #555;
  }

  .actions {
    display: flex;
    gap: 12px;
    flex-wrap: wrap;
  }

  .export-button {
    padding: 6px 14px;
    border: 1px solid var(--secondary-color);
    border-radius: 4px;
    background: white;
    cursor: pointer;

    &:hover {
      background: #eaf7f1;
    }
  }
}
</style>
