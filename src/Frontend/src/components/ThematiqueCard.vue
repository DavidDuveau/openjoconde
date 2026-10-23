<template>
  <router-link
    :to="{ name: 'thematique', params: { slug: thematique.slug } }"
    class="thematique-card"
    data-testid="thematique-card"
    :style="{ background: `linear-gradient(135deg, ${thematique.couleurs[0]}, ${thematique.couleurs[1]})` }"
  >
    <span class="glyphe" aria-hidden="true">{{ thematique.glyphe }}</span>
    <h3 data-testid="thematique-title">{{ thematique.titre }}</h3>
    <p>{{ thematique.resume }}</p>
    <p v-if="thematique.saison" class="saison" data-testid="thematique-saison">
      <strong v-if="enCours" data-testid="thematique-en-cours">En ce moment · </strong>{{ dates }}
    </p>
  </router-link>
</template>

<script lang="ts">
import { computed, defineComponent, PropType } from 'vue';
import { libelleSaison, saisonEnCours, Thematique } from '@/utils/thematiques';

// La vignette d'une collection thématique : un fonds de couleur et un symbole, pas une image des œuvres (droits)
export default defineComponent({
  name: 'ThematiqueCard',
  props: {
    thematique: { type: Object as PropType<Thematique>, required: true },
    /** Date du jour, injectable pour les tests */
    maintenant: { type: Object as PropType<Date>, default: () => new Date() }
  },
  setup(props) {
    const enCours = computed(() => !!props.thematique.saison && saisonEnCours(props.thematique.saison, props.maintenant));
    const dates = computed(() => props.thematique.saison ? libelleSaison(props.thematique.saison) : '');
    return { enCours, dates };
  }
});
</script>

<style scoped lang="scss">
.thematique-card {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-height: 180px;
  padding: 20px;
  border-radius: 10px;
  color: white;
  text-decoration: none;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.15);
  transition: transform 0.15s;

  &:hover,
  &:focus {
    transform: translateY(-3px);
  }

  .glyphe {
    font-size: 2rem;
  }

  h3 {
    margin: 0;
  }

  p {
    margin: 0;
    font-size: 0.9rem;
  }

  .saison {
    margin-top: auto;
    font-size: 0.8rem;
  }
}
</style>
