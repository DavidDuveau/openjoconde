<template>
  <div class="nouveautes-page">
    <h1 data-testid="nouveautes-heading">Nouveautés</h1>
    <p class="intro">
      Ce qui change pour vous à chaque version d'OpenJoconde. Cette page est tenue à jour automatiquement à chaque publication.
    </p>

    <p v-if="versions.length === 0" class="vide" data-testid="nouveautes-vide">Aucune version n'a encore été publiée.</p>

    <article v-for="(version, rang) in versions" :key="version.version" class="version" data-testid="nouveautes-version">
      <h2 :data-testid="rang === 0 ? 'nouveautes-derniere' : undefined">
        Version {{ version.version }}
        <span v-if="version.date" class="date">{{ dateLisible(version.date) }}</span>
      </h2>
      <p v-if="version.sections.length === 0" class="aucun">Pas de changement visible dans cette version.</p>
      <section v-for="section in version.sections" :key="section.titre">
        <h3>{{ section.titre }}</h3>
        <ul>
          <li v-for="(element, i) in section.elements" :key="i" data-testid="nouveaute">
            <strong v-if="element.portee" class="portee">{{ element.portee }}</strong>
            {{ element.texte }}
            <span v-if="element.reference" class="reference">{{ element.reference }}</span>
          </li>
        </ul>
      </section>
    </article>
  </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import { dateLisible, lireChangelog } from '@/utils/nouveautes';

// Le changelog de release-please est lu au build (voir vite.config.ts) : la page suit chaque release sans étape manuelle.
// Chaque ligne est affichée comme du texte, jamais comme du HTML.
export default defineComponent({
  name: 'NouveautesView',
  setup() {
    return { versions: lireChangelog(__CHANGELOG__), dateLisible };
  }
});
</script>

<style scoped lang="scss">
.nouveautes-page {
  max-width: 800px;
  margin: 0 auto;
  padding: 20px;

  .intro {
    color: #555;
    margin-bottom: 20px;
  }

  .version {
    margin-bottom: 24px;
    padding: 16px 20px;
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);

    h2 {
      margin: 0 0 8px;
      color: var(--primary-color);

      .date {
        margin-left: 8px;
        font-size: 0.9rem;
        font-weight: normal;
        color: #666;
      }
    }

    h3 {
      margin: 12px 0 4px;
      font-size: 1rem;
      color: var(--secondary-color);
    }

    ul {
      padding-left: 20px;
    }

    li {
      margin-bottom: 4px;
    }

    .portee {
      text-transform: capitalize;
    }

    .reference {
      margin-left: 4px;
      font-size: 0.8rem;
      color: #888;
    }

    .aucun {
      color: #666;
    }
  }
}
</style>
