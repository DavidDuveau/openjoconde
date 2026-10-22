<template>
  <aside class="data-source" data-testid="source-citation">
    <h3>Source</h3>
    <p v-if="kind === 'joconde'">
      Notice <strong>{{ reference }}</strong> de la base Joconde (collections des musées de France), Ministère de la
      Culture, diffusée sur
      <a :href="popUrl" target="_blank" rel="noopener noreferrer">POP</a><template v-if="place">. Conservée : {{ place }}</template>.
      Données reprises telles que publiées, sous <a :href="LICENCE_URL" target="_blank" rel="noopener noreferrer">Licence Ouverte 2.0</a>.
      <router-link to="/about#sources">Sources et licences</router-link>
    </p>
    <p v-else>
      Coordonnées et histoire : base Muséofile (répertoire des Musées de France), Ministère de la Culture, reprises
      telles que publiées, sous <a :href="LICENCE_URL" target="_blank" rel="noopener noreferrer">Licence Ouverte 2.0</a>.
      <router-link to="/about#sources">Sources et licences</router-link>
    </p>
    <p v-if="kind === 'joconde'" class="rights-note">
      Les images et certaines informations de la notice peuvent relever de droits de tiers : voir la notice sur POP.
    </p>
  </aside>
</template>

<script lang="ts">
import { defineComponent, computed } from 'vue';
import { popNoticeUrl } from '@/utils/pop';
import { LICENCE_URL } from '@/utils/sources';

// Attribution exigée à chaque rediffusion des données du ministère de la Culture : origine, lieu de conservation,
// référence (le titre, l'auteur et la date de la notice sont sur la fiche elle-même)
export default defineComponent({
  name: 'DataSource',
  props: {
    kind: { type: String as () => 'joconde' | 'museofile', default: 'joconde' },
    reference: { type: String, default: '' },
    place: { type: String, default: '' }
  },
  setup(props) {
    const popUrl = computed(() => popNoticeUrl(props.reference) ?? 'https://pop.culture.gouv.fr/');
    return { popUrl, LICENCE_URL };
  }
});
</script>

<style scoped lang="scss">
.data-source {
  margin-top: 28px;
  padding-top: 14px;
  border-top: 1px solid #e3e6ea;
  color: #666;
  font-size: 0.85rem;
  line-height: 1.5;

  h3 {
    font-size: 0.9rem;
    margin-bottom: 6px;
    color: var(--primary-color);
  }

  p + p {
    margin-top: 6px;
  }

  a {
    color: var(--primary-color);
  }
}
</style>
