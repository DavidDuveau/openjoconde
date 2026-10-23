<template>
  <section class="france-map" data-testid="france-map" aria-labelledby="france-map-title">
    <h2 id="france-map-title">Les musées sur la carte</h2>

    <p v-if="loading" class="map-status" data-testid="map-loading">Chargement de la carte...</p>
    <p v-else-if="error" class="map-status" data-testid="map-error">
      La carte n'a pas pu être chargée.
      <button type="button" class="retry" @click="load">Réessayer</button>
    </p>

    <template v-else>
      <nav class="map-crumbs" aria-label="Niveau de la carte">
        <button type="button" data-testid="map-crumb" :disabled="niveau.type === 'france'" @click="niveau = { type: 'france' }">France</button>
        <template v-if="regionCourante">
          <span aria-hidden="true">›</span>
          <button type="button" data-testid="map-crumb" :disabled="niveau.type === 'region'" @click="niveau = { type: 'region', code: regionCourante.code }">{{ regionCourante.nom }}</button>
        </template>
        <template v-if="departementCourant">
          <span aria-hidden="true">›</span>
          <button type="button" data-testid="map-crumb" disabled>{{ departementCourant.nom }}</button>
        </template>
      </nav>

      <div class="map-frame">
        <svg :viewBox="`0 0 ${largeur} ${hauteur}`" role="group" aria-label="Carte de France des musées" @mouseleave="survol = null">
          <rect v-for="cadre in vue.cadres" :key="cadre.nom" class="map-inset"
                :x="cadre.x" :y="cadre.y" :width="cadre.largeur" :height="cadre.hauteur" />
          <text v-for="cadre in vue.cadres" :key="`t-${cadre.nom}`" class="map-inset-name" :x="cadre.x + 6" :y="cadre.y + 12">{{ cadre.nom }}</text>

          <path
            v-for="forme in vue.formes"
            :key="`${forme.niveau}-${forme.code}`"
            class="map-shape"
            :class="{ 'is-clickable': niveau.type !== 'departement' }"
            data-testid="map-shape"
            :data-code="forme.code"
            :d="forme.d"
            :role="niveau.type === 'departement' ? undefined : 'button'"
            :tabindex="niveau.type === 'departement' ? undefined : 0"
            :aria-label="niveau.type === 'departement' ? undefined : `Zoomer sur ${forme.nom}`"
            @click="zoomer(forme)"
            @keydown.enter="zoomer(forme)"
          >
            <title>{{ forme.nom }}</title>
          </path>

          <circle
            v-for="point in vue.points"
            :key="point.id"
            class="map-point"
            data-testid="map-point"
            :data-name="point.nom"
            :cx="point.x"
            :cy="point.y"
            r="5"
            role="link"
            tabindex="0"
            :aria-label="`Ouvrir la fiche de ${point.nom}`"
            @mouseenter="survol = point"
            @focus="survol = point"
            @mouseleave="survol = null"
            @blur="survol = null"
            @click.stop="ouvrir(point.id)"
            @keydown.enter="ouvrir(point.id)"
          />
        </svg>
        <div
          v-if="survol"
          class="map-tooltip"
          data-testid="map-tooltip"
          :style="{ left: `${(survol.x / largeur) * 100}%`, top: `${(survol.y / hauteur) * 100}%` }"
        >{{ survol.nom }}</div>
      </div>

      <p class="map-count" data-testid="map-count">
        {{ vue.points.length }} musée{{ vue.points.length > 1 ? 's' : '' }} sur la carte
        <span v-if="niveau.type === 'france' && nonLocalises > 0" data-testid="map-unlocated">
          · {{ nonLocalises }} non localisé{{ nonLocalises > 1 ? 's' : '' }}, à retrouver dans <router-link :to="{ name: 'museums' }">la liste des musées</router-link>
        </span>
      </p>
      <p class="map-credit">Contours : IGN Admin Express, via <a href="https://github.com/gregoiredavid/france-geojson" target="_blank" rel="noopener noreferrer">france-geojson</a> (Licence Ouverte 2.0).</p>
    </template>
  </section>
</template>

<script lang="ts">
import { computed, defineComponent, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import ApiService from '@/services/api';
import {
  FondDeCarte, Forme, HAUTEUR, LARGEUR, MuseePlace, Niveau, PointCarte, composerVue, placerMusees
} from '@/utils/carte';

export default defineComponent({
  name: 'FranceMap',
  setup() {
    const router = useRouter();
    const loading = ref(true);
    const error = ref(false);
    const fond = ref<FondDeCarte | null>(null);
    const places = ref<MuseePlace[]>([]);
    const nonLocalises = ref(0);
    const niveau = ref<Niveau>({ type: 'france' });
    const survol = ref<PointCarte | null>(null);

    const load = async () => {
      loading.value = true;
      error.value = false;
      try {
        // Le fond de carte est un gros fichier : il ne se charge qu'avec la carte, pas avec la page
        const [donnees, musees] = await Promise.all([
          import('@/assets/geo/france.json').then(m => m.default as unknown as FondDeCarte),
          ApiService.getMuseumLocations()
        ]);
        const resultat = placerMusees(donnees, musees.items);
        fond.value = donnees;
        places.value = resultat.places;
        // Sans coordonnées côté base, ou hors de tout contour : dans les deux cas, absent de la carte
        nonLocalises.value = musees.total - resultat.places.length;
      } catch (err) {
        console.error('Erreur lors du chargement de la carte:', err);
        error.value = true;
      } finally {
        loading.value = false;
      }
    };

    onMounted(load);

    const vue = computed(() => fond.value
      ? composerVue(fond.value, places.value, niveau.value)
      : { formes: [], points: [], cadres: [] });

    const regionCourante = computed(() => {
      const n = niveau.value;
      if (n.type === 'france' || !fond.value) return null;
      const code = n.type === 'region' ? n.code : fond.value.departements.find(d => d.code === n.code)?.region;
      return fond.value.regions.find(r => r.code === code) ?? null;
    });
    const departementCourant = computed(() => {
      const n = niveau.value;
      return n.type === 'departement' ? fond.value?.departements.find(d => d.code === n.code) ?? null : null;
    });

    // Région puis département : un clic descend d'un niveau ; au dernier niveau, il n'y a plus rien à ouvrir
    const zoomer = (forme: Forme) => {
      survol.value = null;
      if (niveau.value.type === 'france') niveau.value = { type: 'region', code: forme.code };
      else if (niveau.value.type === 'region') niveau.value = { type: 'departement', code: forme.code };
    };

    const ouvrir = (id: string) => router.push({ name: 'museum-detail', params: { id } });

    return {
      loading, error, load, niveau, vue, survol, nonLocalises, regionCourante, departementCourant, zoomer, ouvrir,
      largeur: LARGEUR, hauteur: HAUTEUR
    };
  }
});
</script>

<style scoped lang="scss">
.france-map {
  margin: 40px 0;

  h2 {
    margin-bottom: 16px;
    color: var(--primary-color);
  }

  .map-status {
    padding: 16px;
    background: white;
    border-radius: 8px;
  }

  .map-crumbs {
    display: flex;
    gap: 8px;
    align-items: center;
    margin-bottom: 8px;

    button {
      padding: 4px 10px;
      border: 1px solid #ddd;
      border-radius: 14px;
      background: white;
      cursor: pointer;

      &:disabled {
        cursor: default;
        font-weight: bold;
        background: #f0f0f0;
      }
    }
  }

  .map-frame {
    position: relative;
    max-width: 800px;
    margin: 0 auto;

    svg {
      display: block;
      width: 100%;
      height: auto;
    }
  }

  .map-inset {
    fill: none;
    stroke: #ccc;
    stroke-dasharray: 3 3;
  }

  .map-inset-name {
    font-size: 11px;
    fill: #666;
  }

  // Fond incolore : seuls les contours se voient ; transparent (et non none) pour que le clic touche toute la surface
  .map-shape {
    fill: transparent;
    stroke: #8a8a8a;
    stroke-width: 0.8;

    &.is-clickable {
      cursor: pointer;

      // Incolore au repos, la zone se colore au survol (ou au focus clavier)
      &:hover,
      &:focus {
        fill: rgba(66, 185, 131, 0.3);
        stroke: #222;
        stroke-width: 1.6;
        outline: none;
      }
    }
  }

  .map-point {
    fill: #d32f2f;
    stroke: white;
    stroke-width: 1.5;
    cursor: pointer;

    &:hover,
    &:focus {
      r: 7;
      outline: none;
    }
  }

  .map-tooltip {
    position: absolute;
    transform: translate(-50%, calc(-100% - 10px));
    padding: 4px 8px;
    background: #222;
    color: white;
    border-radius: 4px;
    font-size: 0.85rem;
    white-space: nowrap;
    pointer-events: none;
  }

  .map-count,
  .map-credit {
    margin-top: 8px;
    text-align: center;
  }

  .map-credit {
    font-size: 0.75rem;
    color: #777;
  }
}
</style>
