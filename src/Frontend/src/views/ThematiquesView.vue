<template>
  <div class="thematiques-page">
    <h1>Thématiques</h1>
    <p class="intro">
      Des parcours pour explorer les collections par sujet. Chaque thématique précise ses critères de sélection : elle se
      construit sur les mots des notices, pas sur un choix d'œuvres.
    </p>

    <section v-if="enCours.length" class="en-cours" data-testid="thematiques-en-cours" aria-labelledby="saison-title">
      <h2 id="saison-title">En ce moment</h2>
      <div class="grille">
        <ThematiqueCard v-for="t in enCours" :key="t.slug" :thematique="t" :maintenant="maintenant" />
      </div>
    </section>

    <section aria-labelledby="toutes-title">
      <h2 id="toutes-title">Toutes les thématiques</h2>
      <div class="grille">
        <ThematiqueCard v-for="t in autres" :key="t.slug" :thematique="t" :maintenant="maintenant" />
      </div>
    </section>
  </div>
</template>

<script lang="ts">
import { computed, defineComponent } from 'vue';
import ThematiqueCard from '@/components/ThematiqueCard.vue';
import { THEMATIQUES, saisonEnCours } from '@/utils/thematiques';

export default defineComponent({
  name: 'ThematiquesView',
  components: { ThematiqueCard },
  setup() {
    const maintenant = new Date();
    const actuelle = (saison: (typeof THEMATIQUES)[number]['saison']) => !!saison && saisonEnCours(saison, maintenant);

    // Une collection de saison est mise en évidence tant qu'elle est ouverte ; le reste de l'année, elle rejoint les autres
    const enCours = computed(() => THEMATIQUES.filter(t => actuelle(t.saison)));
    const autres = computed(() => THEMATIQUES.filter(t => !actuelle(t.saison)));
    return { maintenant, enCours, autres };
  }
});
</script>

<style scoped lang="scss">
.thematiques-page {
  max-width: 1100px;
  margin: 0 auto;
  padding: 20px;

  h1 {
    text-align: center;
    color: var(--primary-color);
    margin-bottom: 12px;
  }

  .intro {
    text-align: center;
    max-width: 700px;
    margin: 0 auto 30px;
    color: #555;
  }

  h2 {
    margin: 24px 0 12px;
    color: var(--primary-color);
  }

  .grille {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
    gap: 16px;
  }
}
</style>
