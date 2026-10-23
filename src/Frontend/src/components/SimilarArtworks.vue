<template>
  <section class="similar-artworks" data-testid="similar-artworks" aria-labelledby="similar-title">
    <h2 id="similar-title">Œuvres similaires</h2>

    <p v-if="loading" class="similar-status" data-testid="similar-loading">Recherche d'œuvres similaires...</p>

    <div v-else-if="groups.length" class="similar-groups">
      <div v-for="group in groups" :key="group.key" class="similar-group" data-testid="similar-group">
        <h3 data-testid="similar-criterion">{{ group.label }}</h3>
        <div class="similar-list">
          <ArtworkCard
            v-for="item in group.items"
            :key="item.id"
            :id="item.id"
            :title="item.title"
            :imageUrl="item.imageUrl"
            :hasImage="item.hasImage === true"
            :date="item.creationDate"
            :artists="item.artists"
            @click="open(item.id)"
          />
        </div>
      </div>
    </div>

    <p v-else class="similar-status" data-testid="similar-empty">
      Aucune œuvre directement similaire n'a été trouvée : cette œuvre n'a pas d'artiste, de période, de technique ni de
      musée en commun avec d'autres notices.
      <router-link :to="{ name: 'artworks' }">Parcourir toutes les œuvres</router-link>
    </p>
  </section>
</template>

<script lang="ts">
import { defineComponent, ref, watch, PropType } from 'vue';
import { useRouter } from 'vue-router';
import ApiService from '@/services/api';
import ArtworkCard from '@/components/ArtworkCard.vue';
import { Artwork, ArtworkDetail } from '@/types/artwork';

interface Group {
  key: string;
  label: string;
  items: Artwork[];
}

interface Criterion {
  key: string;
  label: string;
  params: Record<string, string>;
}

/** Nombre d'œuvres montrées par critère */
const PER_CRITERION = 6;

/** Les critères de ressemblance d'une œuvre, du plus proche au plus large ; seuls ceux que la notice renseigne */
function criteriaOf(artwork: ArtworkDetail): Criterion[] {
  const criteria: Criterion[] = [];
  const artist = artwork.artists[0];
  if (artist) {
    criteria.push({
      key: 'artist',
      label: `Même artiste : ${`${artist.firstName || ''} ${artist.lastName}`.trim()}`,
      params: { artistId: artist.id }
    });
  }
  const period = artwork.periods[0];
  if (period) criteria.push({ key: 'period', label: `Même période : ${period.name}`, params: { periodId: period.id } });
  const technique = artwork.techniques[0];
  if (technique) criteria.push({ key: 'technique', label: `Même technique : ${technique.name}`, params: { techniqueId: technique.id } });
  if (artwork.museum) criteria.push({ key: 'museum', label: `Même musée : ${artwork.museum.name}`, params: { museumId: artwork.museum.id } });
  return criteria;
}

export default defineComponent({
  name: 'SimilarArtworks',
  components: { ArtworkCard },
  props: {
    artwork: { type: Object as PropType<ArtworkDetail>, required: true }
  },
  setup(props) {
    const router = useRouter();
    const groups = ref<Group[]>([]);
    const loading = ref(true);

    const load = async () => {
      loading.value = true;
      const current = props.artwork.id;
      const criteria = criteriaOf(props.artwork);

      // Un critère en panne ne prive pas des autres ; on demande une œuvre de plus pour pouvoir écarter l'œuvre courante
      const answers = await Promise.allSettled(criteria.map(c =>
        ApiService.searchArtworks({ ...c.params, pageSize: PER_CRITERION + 1, sortBy: 'title' })));

      // La fiche a pu changer pendant l'attente : la réponse d'une œuvre quittée ne s'affiche pas
      if (current !== props.artwork.id) return;

      groups.value = criteria.flatMap((c, i) => {
        const answer = answers[i];
        if (answer.status !== 'fulfilled') return [];
        const items = answer.value.items.filter(item => item.id !== current).slice(0, PER_CRITERION);
        return items.length ? [{ key: c.key, label: c.label, items }] : [];
      });
      loading.value = false;
    };

    watch(() => props.artwork.id, load, { immediate: true });

    // Un empilement dans l'historique : « précédent » revient à l'œuvre d'où l'on vient
    const open = (id: string) => router.push({ name: 'artwork-detail', params: { id } });

    return { groups, loading, open };
  }
});
</script>

<style scoped lang="scss">
.similar-artworks {
  margin-top: 40px;

  h2 {
    margin-bottom: 16px;
    color: var(--primary-color);
  }

  .similar-status {
    padding: 16px;
    background-color: white;
    border-radius: 8px;

    a {
      margin-left: 8px;
      color: var(--secondary-color);
    }
  }

  .similar-group {
    margin-bottom: 24px;

    h3 {
      margin-bottom: 12px;
      font-size: 1.1rem;
    }
  }

  .similar-list {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 16px;
  }
}
</style>
