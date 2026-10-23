<template>
  <div class="thematique-page">
    <div v-if="!thematique" class="not-found" data-testid="thematique-introuvable">
      <p>Cette thématique n'existe pas.</p>
      <router-link :to="{ name: 'thematiques' }">Voir toutes les thématiques</router-link>
    </div>

    <template v-else>
      <router-link :to="{ name: 'thematiques' }" class="back-link">&larr; Toutes les thématiques</router-link>

      <header class="entete" :style="{ background: `linear-gradient(135deg, ${thematique.couleurs[0]}, ${thematique.couleurs[1]})` }">
        <span class="glyphe" aria-hidden="true">{{ thematique.glyphe }}</span>
        <h1 data-testid="thematique-heading">{{ thematique.titre }}</h1>
        <p v-if="thematique.saison" class="saison" data-testid="thematique-saison">
          <strong v-if="enCours">En ce moment · </strong>{{ dates }}
        </p>
      </header>

      <section class="introduction" data-testid="thematique-introduction">
        <h2>Le thème</h2>
        <p data-testid="thematique-explication">{{ thematique.explication }}</p>
        <h2>Son importance</h2>
        <p data-testid="thematique-importance">{{ thematique.importance }}</p>
        <h2>Comment les œuvres sont choisies</h2>
        <p data-testid="thematique-criteres">{{ thematique.criteres }}</p>
      </section>

      <section
        v-for="(etape, index) in etapes"
        :key="etape.section.titre"
        class="etape"
        data-testid="thematique-section"
      >
        <h2><span class="numero">{{ index + 1 }}</span> {{ etape.section.titre }}</h2>
        <p class="texte">{{ etape.section.texte }}</p>

        <p v-if="etape.etat === 'chargement'" class="statut" data-testid="section-chargement">Recherche des œuvres...</p>
        <p v-else-if="etape.etat === 'panne'" class="statut" data-testid="section-panne">
          Les œuvres n'ont pas pu être chargées.
          <button type="button" class="retry" @click="charger(index)">Réessayer</button>
        </p>
        <p v-else-if="etape.items.length === 0" class="statut" data-testid="section-vide">
          Aucune notice de la base ne correspond à cette étape.
        </p>
        <template v-else>
          <div class="liste">
            <ArtworkCard
              v-for="item in etape.items"
              :key="item.id"
              :id="item.id"
              :title="item.title"
              :imageUrl="item.imageUrl"
              :hasImage="item.hasImage === true"
              :date="item.creationDate"
              :artists="item.artists"
              @click="ouvrir(item.id)"
            />
          </div>
          <p class="suite">
            <router-link :to="lienRecherche(etape)" data-testid="section-voir-tout">
              Voir les {{ etape.total }} œuvre{{ etape.total > 1 ? 's' : '' }} de cette étape
            </router-link>
          </p>
        </template>
      </section>
    </template>
  </div>
</template>

<script lang="ts">
import { computed, defineComponent, reactive, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import ApiService from '@/services/api';
import ArtworkCard from '@/components/ArtworkCard.vue';
import { Artwork } from '@/types/artwork';
import { Section, libelleSaison, saisonEnCours, trouverThematique } from '@/utils/thematiques';

interface Etape {
  section: Section;
  etat: 'chargement' | 'ok' | 'panne';
  items: Artwork[];
  total: number;
  domainId?: string;
}

const PAR_ETAPE = 6;
const normaliser = (texte: string) => texte.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase();

// Une thématique est un parcours en étapes : un texte, puis les œuvres de la recherche qui définit l'étape, dans l'ordre
// du récit. Une panne sur une étape n'empêche pas les autres de s'afficher.
export default defineComponent({
  name: 'ThematiqueView',
  components: { ArtworkCard },
  setup() {
    const route = useRoute();
    const router = useRouter();
    const thematique = computed(() => trouverThematique(String(route.params.slug ?? '')));
    const etapes = reactive<Etape[]>([]);

    const enCours = computed(() => !!thematique.value?.saison && saisonEnCours(thematique.value.saison, new Date()));
    const dates = computed(() => thematique.value?.saison ? libelleSaison(thematique.value.saison) : '');

    // Le domaine se désigne par son nom dans le catalogue ; on le retrouve par son nom exact, sans tenir compte de la casse ni des accents
    const idDuDomaine = async (nom: string): Promise<string | undefined> => {
      const resultat = await ApiService.getDomains(nom, 20);
      return resultat.items.find(d => normaliser(d.name) === normaliser(nom))?.id;
    };

    const charger = async (index: number) => {
      const etape = etapes[index];
      if (!etape) return;
      etape.etat = 'chargement';
      try {
        const { recherche, domaine } = etape.section;
        if (domaine) {
          etape.domainId = await idDuDomaine(domaine);
          // Un domaine inconnu de la base n'a aucune œuvre : on n'élargit pas la recherche à tout le catalogue
          if (!etape.domainId) {
            etape.items = [];
            etape.total = 0;
            etape.etat = 'ok';
            return;
          }
        }
        const resultat = await ApiService.searchArtworks({
          search: recherche || undefined,
          domainId: etape.domainId,
          pageSize: PAR_ETAPE,
          sortBy: recherche ? 'relevance' : undefined
        });
        etape.items = resultat.items;
        etape.total = resultat.totalItems;
        etape.etat = 'ok';
      } catch (err) {
        console.error("Erreur lors du chargement d'une étape de thématique:", err);
        etape.etat = 'panne';
      }
    };

    watch(thematique, (courante) => {
      etapes.splice(0, etapes.length, ...(courante?.sections ?? []).map(section => ({
        section, etat: 'chargement' as const, items: [], total: 0
      })));
      etapes.forEach((_, index) => charger(index));
    }, { immediate: true });

    const lienRecherche = (etape: Etape) => ({
      name: 'search',
      query: {
        ...(etape.section.recherche ? { searchText: etape.section.recherche } : {}),
        ...(etape.domainId ? { domainId: etape.domainId, domainName: etape.section.domaine } : {})
      }
    });

    const ouvrir = (id: string) => router.push({ name: 'artwork-detail', params: { id } });

    return { thematique, etapes, enCours, dates, charger, lienRecherche, ouvrir };
  }
});
</script>

<style scoped lang="scss">
.thematique-page {
  max-width: 1000px;
  margin: 0 auto;
  padding: 20px;

  .back-link {
    display: inline-block;
    margin-bottom: 12px;
  }

  .not-found {
    text-align: center;
    padding: 40px;
  }

  .entete {
    padding: 28px;
    border-radius: 10px;
    color: white;
    margin-bottom: 20px;

    .glyphe {
      font-size: 2.4rem;
    }

    h1 {
      margin: 6px 0;
    }

    .saison {
      margin: 0;
    }
  }

  .introduction {
    background: white;
    padding: 20px;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
    margin-bottom: 24px;

    h2 {
      font-size: 1.1rem;
      color: var(--primary-color);
      margin: 12px 0 4px;

      &:first-child {
        margin-top: 0;
      }
    }

    p {
      line-height: 1.5;
    }
  }

  .etape {
    margin-bottom: 32px;

    h2 {
      color: var(--primary-color);
    }

    .numero {
      display: inline-block;
      width: 1.6em;
      height: 1.6em;
      line-height: 1.6em;
      text-align: center;
      border-radius: 50%;
      background: var(--secondary-color);
      color: white;
      font-size: 0.8em;
    }

    .texte {
      margin: 6px 0 12px;
      color: #555;
    }

    .liste {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 12px;
    }

    .suite {
      margin-top: 10px;
    }

    .statut {
      padding: 12px;
      background: white;
      border-radius: 8px;
    }
  }
}
</style>
