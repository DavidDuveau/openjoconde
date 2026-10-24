<template>
  <div class="favoris-page">
    <h1 data-testid="favoris-heading">Mes favoris</h1>

    <p class="note" data-testid="favoris-note">
      Vos favoris restent sur cet appareil, dans votre navigateur : aucun compte n'est nécessaire et rien n'est envoyé à
      un serveur. Ils sont perdus si vous videz les données de navigation.
    </p>

    <p v-if="favoris.probleme" class="alerte" data-testid="favoris-probleme" role="alert">
      {{ messages[favoris.probleme] }}
    </p>

    <p v-if="total === 0" class="vide" data-testid="favoris-vide">
      Vous n'avez pas encore de favori. Sur une œuvre, utilisez l'étoile pour la retrouver ici.
      <router-link :to="{ name: 'artworks' }">Parcourir les œuvres</router-link>
    </p>

    <template v-else>
      <p class="compte" data-testid="favoris-compte">{{ total }} {{ total > 1 ? 'œuvres favorites' : 'œuvre favorite' }}</p>

      <div class="liste">
        <template v-for="entree in entrees" :key="entree.id">
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
          <div v-else class="carte-etat" :data-testid="`favori-${entree.etat}`">
            <template v-if="entree.etat === 'retiree'">
              <p>Œuvre retirée de la base</p>
              <button type="button" @click="retirer(entree.id)">Supprimer des favoris</button>
            </template>
            <template v-else-if="entree.etat === 'panne'">
              <p>Cette œuvre n'a pas pu être chargée.</p>
              <button type="button" @click="charger(entree.id)">Réessayer</button>
              <button type="button" @click="retirer(entree.id)">Supprimer des favoris</button>
            </template>
            <p v-else>Chargement...</p>
          </div>
        </template>
      </div>

      <PaginationBar :page="page" :totalPages="totalPages" @change="page = $event" />
    </template>
  </div>
</template>

<script lang="ts">
import { defineComponent, computed, reactive, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import ApiService from '@/services/api';
import ArtworkCard from '@/components/ArtworkCard.vue';
import PaginationBar from '@/components/PaginationBar.vue';
import { favoris, MESSAGES_FAVORIS, retirerFavori } from '@/utils/favoris';
import { Artwork } from '@/types/artwork';

const PAR_PAGE = 12;

type Etat = 'chargement' | 'ok' | 'retiree' | 'panne';

interface Entree {
  id: string;
  etat: Etat;
  oeuvre?: Artwork;
}

// Les favoris ne gardent que l'identifiant : le contenu des œuvres est relu à l'affichage, une page à la fois.
// Une œuvre qui n'existe plus dans la base (404) est signalée sans casser le reste de la liste.
export default defineComponent({
  name: 'FavorisView',
  components: { ArtworkCard, PaginationBar },
  setup() {
    const router = useRouter();
    const page = ref(1);
    const connues = reactive<Record<string, Entree>>({});

    const total = computed(() => favoris.favoris.length);
    const totalPages = computed(() => Math.max(1, Math.ceil(total.value / PAR_PAGE)));
    const idsDeLaPage = computed(() => favoris.favoris.slice((page.value - 1) * PAR_PAGE, page.value * PAR_PAGE).map(f => f.id));
    const entrees = computed<Entree[]>(() => idsDeLaPage.value.map(id => connues[id] ?? { id, etat: 'chargement' }));

    const charger = async (id: string) => {
      connues[id] = { id, etat: 'chargement' };
      try {
        const detail = await ApiService.getArtwork(id);
        const artists = detail.artists.map(a => ({ id: a.id, firstName: a.firstName ?? '', lastName: a.lastName }));
        connues[id] = { id, etat: 'ok', oeuvre: { ...detail, title: detail.title ?? '', imageUrl: detail.imageUrl ?? '', creationDate: detail.creationDate ?? '', inventoryNumber: detail.inventoryNumber ?? '', artists } };
      } catch (erreur) {
        const statut = (erreur as { response?: { status?: number } })?.response?.status;
        connues[id] = { id, etat: statut === 404 ? 'retiree' : 'panne' };
      }
    };

    // Retirer le dernier favori d'une page ramène à la page précédente
    watch(totalPages, (pages) => { if (page.value > pages) page.value = pages; });
    watch(idsDeLaPage, (ids) => ids.filter(id => !connues[id]).forEach(charger), { immediate: true });

    const retirer = (id: string) => retirerFavori(id);
    const ouvrir = (id: string) => router.push({ name: 'artwork-detail', params: { id } });

    return { favoris, messages: MESSAGES_FAVORIS, page, total, totalPages, entrees, charger, retirer, ouvrir };
  }
});
</script>

<style scoped lang="scss">
.favoris-page {
  max-width: 1000px;
  margin: 0 auto;
  padding: 20px;

  .note {
    padding: 12px;
    background: #eaf7f1;
    border-radius: 8px;
    color: #333;
  }

  .alerte {
    color: #a33;
  }

  .vide {
    padding: 24px;
    text-align: center;
  }

  .compte {
    font-weight: 600;
  }

  .liste {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 12px;
    margin-bottom: 20px;
  }

  .carte-etat {
    display: flex;
    flex-direction: column;
    gap: 8px;
    align-items: flex-start;
    justify-content: center;
    padding: 16px;
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
  }
}
</style>
