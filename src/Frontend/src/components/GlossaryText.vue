<template>
  <span class="glossary-text"><template v-for="(morceau, i) in morceaux" :key="i"><abbr
    v-if="morceau.definition"
    class="glossary-term"
    data-testid="glossary-term"
    tabindex="0"
    :title="morceau.definition"
  >{{ morceau.texte }}</abbr><template v-else>{{ morceau.texte }}</template></template></span>
</template>

<script lang="ts">
import { computed, defineComponent } from 'vue';
import { decouper } from '@/utils/glossaire';

// Un texte dont les termes techniques s'expliquent au survol ou au focus clavier (infobulle native)
export default defineComponent({
  name: 'GlossaryText',
  props: {
    text: { type: String, required: true }
  },
  setup(props) {
    return { morceaux: computed(() => decouper(props.text)) };
  }
});
</script>

<style scoped lang="scss">
.glossary-term {
  text-decoration: underline dotted;
  text-underline-offset: 3px;
  cursor: help;
}
</style>
