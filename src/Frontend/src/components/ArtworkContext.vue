<template>
  <section v-if="visible" class="artwork-context" data-testid="artwork-context" aria-labelledby="artwork-context-title">
    <h2 id="artwork-context-title">Contexte</h2>

    <article v-if="artist" class="context-artist" data-testid="context-artist">
      <h3>
        L'artiste :
        <router-link :to="{ name: 'artist-detail', params: { id: artist.id } }" data-testid="context-artist-link">{{ nom }}</router-link>
      </h3>
      <p class="context-facts" v-if="faits.length" data-testid="context-facts">{{ faits.join(' · ') }}</p>
      <p v-if="artist.biography" class="context-biography" data-testid="context-biography">{{ artist.biography }}</p>
      <p v-if="wikipedia" class="context-source" data-testid="context-source">
        Biographie : <a :href="wikipedia" target="_blank" rel="noopener noreferrer">Wikipédia</a>
        (<a href="https://creativecommons.org/licenses/by-sa/4.0/deed.fr" target="_blank" rel="noopener noreferrer">CC BY-SA 4.0</a>).
      </p>
      <p class="context-more">
        <router-link :to="{ name: 'artist-detail', params: { id: artist.id } }">
          Biographie complète et {{ artist.artworksCount }} œuvre{{ artist.artworksCount > 1 ? 's' : '' }} dans la base
        </router-link>
      </p>
    </article>

    <dl class="context-terms" v-if="termes.length">
      <template v-for="terme in termes" :key="terme.label">
        <dt>{{ terme.label }}</dt>
        <dd data-testid="context-term"><GlossaryText :text="terme.valeur" /></dd>
      </template>
    </dl>
  </section>
</template>

<script lang="ts">
import { computed, defineComponent, PropType, ref, watch } from 'vue';
import ApiService from '@/services/api';
import GlossaryText from '@/components/GlossaryText.vue';
import { safeWebUrl } from '@/utils/rights';
import { Artist } from '@/types/models';
import { ArtworkDetail } from '@/types/artwork';

// Ce qui éclaire l'œuvre : son artiste (biographie sourcée, courant, nationalité) et ses termes techniques expliqués.
// Une panne de la fiche artiste ne retire rien d'autre : le contexte se contente de ne pas l'afficher.
export default defineComponent({
  name: 'ArtworkContext',
  components: { GlossaryText },
  props: {
    artwork: { type: Object as PropType<ArtworkDetail>, required: true }
  },
  setup(props) {
    const artist = ref<Artist | null>(null);

    const charger = async () => {
      artist.value = null;
      const premier = props.artwork.artists[0];
      if (!premier) return;
      try {
        const fiche = await ApiService.getArtist(premier.id);
        // Une réponse tardive d'une œuvre précédente ne doit pas s'afficher
        if (props.artwork.artists[0]?.id === premier.id) artist.value = fiche;
      } catch (err) {
        console.error("Erreur lors du chargement du contexte de l'artiste:", err);
      }
    };
    watch(() => props.artwork.id, charger, { immediate: true });

    const nom = computed(() => artist.value ? `${artist.value.firstName ?? ''} ${artist.value.lastName}`.trim() : '');
    const annees = computed(() => {
      const a = artist.value;
      if (!a?.birthDate) return '';
      return a.deathDate ? `${a.birthDate} - ${a.deathDate}` : `né(e) en ${a.birthDate}`;
    });
    const faits = computed(() => [annees.value, artist.value?.nationality, artist.value?.movement].filter(Boolean) as string[]);
    const wikipedia = computed(() => safeWebUrl(artist.value?.wikipediaUrl ?? null));

    const termes = computed(() => {
      const a = props.artwork;
      const noms = (liste: { name: string }[]) => liste.map(e => e.name).join(', ');
      return [
        { label: 'Dénomination', valeur: a.denomination ?? '' },
        { label: 'Techniques', valeur: noms(a.techniques) },
        { label: 'Périodes', valeur: noms(a.periods) },
        { label: 'Domaines', valeur: noms(a.domains) }
      ].filter(t => t.valeur);
    });

    const visible = computed(() => !!artist.value || termes.value.length > 0);

    return { artist, nom, faits, wikipedia, termes, visible };
  }
});
</script>

<style scoped lang="scss">
.artwork-context {
  margin: 32px 0;
  padding: 20px;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);

  h2 {
    margin-bottom: 12px;
    color: var(--primary-color);
  }

  .context-artist {
    margin-bottom: 16px;

    h3 {
      margin-bottom: 4px;
    }

    .context-facts {
      color: #555;
      margin-bottom: 8px;
    }

    .context-biography {
      line-height: 1.5;
      margin-bottom: 4px;
    }

    .context-source {
      font-size: 0.8rem;
      color: #777;
      margin-bottom: 8px;
    }
  }

  .context-terms {
    display: grid;
    grid-template-columns: max-content 1fr;
    gap: 6px 16px;

    dt {
      font-weight: bold;
    }

    dd {
      margin: 0;
    }
  }
}
</style>
