<template>
  <div class="oeuvres-par-id">
    <div class="liste">
      <div v-for="(entree, position) in entrees" :key="entree.id" class="element" data-testid="oeuvre-element">
        <ArtworkCard
          v-if="entree.etat === 'ok' && entree.oeuvre"
          :id="entree.id"
          :title="entree.oeuvre.title"
          :imageUrl="entree.oeuvre.imageUrl"
          :hasImage="entree.oeuvre.hasImage === true"
          :date="entree.oeuvre.creationDate"
          :artists="entree.oeuvre.artists"
          @click="ouvrir(entree.id)"
        />
        <div v-else class="carte-etat" :data-testid="`oeuvre-${entree.etat}`">
          <template v-if="entree.etat === 'retiree'">
            <p>Œuvre retirée de la base</p>
            <button v-if="retirable" type="button" @click="$emit('retirer', entree.id)">{{ libelleRetrait }}</button>
          </template>
          <template v-else-if="entree.etat === 'panne'">
            <p>Cette œuvre n'a pas pu être chargée.</p>
            <button type="button" @click="charger(entree.id)">Réessayer</button>
            <button v-if="retirable" type="button" @click="$emit('retirer', entree.id)">{{ libelleRetrait }}</button>
          </template>
          <p v-else>Chargement...</p>
        </div>
        <div v-if="$slots.actions" class="actions">
          <slot name="actions" :id="entree.id" :index="(page - 1) * parPage + position" :etat="entree.etat" />
        </div>
      </div>
    </div>

    <PaginationBar :page="page" :totalPages="totalPages" @change="page = $event" />
  </div>
</template>

<script lang="ts">
import { defineComponent, computed, reactive, ref, watch, PropType } from 'vue';
import { useRouter } from 'vue-router';
import ApiService from '@/services/api';
import ArtworkCard from '@/components/ArtworkCard.vue';
import PaginationBar from '@/components/PaginationBar.vue';
import { Artwork } from '@/types/artwork';

type Etat = 'chargement' | 'ok' | 'retiree' | 'panne';

interface Entree {
  id: string;
  etat: Etat;
  oeuvre?: Artwork;
}

// Une liste d'œuvres données par leur identifiant (favoris, collections) : le contenu est relu à l'affichage, une page à
// la fois. Une œuvre qui n'existe plus dans la base (404) est signalée sans casser le reste de la liste.
export default defineComponent({
  name: 'OeuvresParId',
  components: { ArtworkCard, PaginationBar },
  props: {
    ids: { type: Array as PropType<string[]>, required: true },
    /** Propose de supprimer de la liste une œuvre retirée de la base ou qui ne se charge pas */
    retirable: { type: Boolean, default: true },
    libelleRetrait: { type: String, default: 'Supprimer de la liste' },
    parPage: { type: Number, default: 12 }
  },
  emits: ['retirer'],
  setup(props) {
    const router = useRouter();
    const page = ref(1);
    const connues = reactive<Record<string, Entree>>({});

    const totalPages = computed(() => Math.max(1, Math.ceil(props.ids.length / props.parPage)));
    const idsDeLaPage = computed(() => props.ids.slice((page.value - 1) * props.parPage, page.value * props.parPage));
    const entrees = computed<Entree[]>(() => idsDeLaPage.value.map(id => connues[id] ?? { id, etat: 'chargement' }));

    const charger = async (id: string) => {
      connues[id] = { id, etat: 'chargement' };
      try {
        const detail = await ApiService.getArtwork(id);
        const artists = detail.artists.map(a => ({ id: a.id, firstName: a.firstName ?? '', lastName: a.lastName }));
        connues[id] = {
          id,
          etat: 'ok',
          oeuvre: {
            ...detail,
            title: detail.title ?? '',
            imageUrl: detail.imageUrl ?? '',
            creationDate: detail.creationDate ?? '',
            inventoryNumber: detail.inventoryNumber ?? '',
            artists
          }
        };
      } catch (erreur) {
        const statut = (erreur as { response?: { status?: number } })?.response?.status;
        connues[id] = { id, etat: statut === 404 ? 'retiree' : 'panne' };
      }
    };

    // Retirer la dernière œuvre d'une page ramène à la page précédente
    watch(totalPages, (pages) => { if (page.value > pages) page.value = pages; });
    watch(idsDeLaPage, (ids) => ids.filter(id => !connues[id]).forEach(charger), { immediate: true });

    const ouvrir = (id: string) => router.push({ name: 'artwork-detail', params: { id } });

    return { page, totalPages, entrees, charger, ouvrir };
  }
});
</script>

<style scoped lang="scss">
.oeuvres-par-id {
  .liste {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 12px;
    margin-bottom: 20px;
  }

  .element {
    display: flex;
    flex-direction: column;
    gap: 6px;
  }

  .actions {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
  }

  .carte-etat {
    display: flex;
    flex-direction: column;
    gap: 8px;
    align-items: flex-start;
    justify-content: center;
    min-height: 120px;
    padding: 16px;
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
  }
}
</style>
