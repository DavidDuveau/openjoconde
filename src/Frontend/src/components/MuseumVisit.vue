<template>
  <div class="museum-visit" data-testid="museum-visit">
    <p v-if="showContact && adresse" class="visit-address" data-testid="visit-address">{{ adresse }}</p>

    <p v-if="carte" class="visit-links">
      <a :href="carte" target="_blank" rel="noopener noreferrer" data-testid="visit-map">Voir sur la carte</a>
      <template v-if="osm">
        · Itinéraire :
        <a :href="osm" target="_blank" rel="noopener noreferrer" data-testid="visit-route-osm">OpenStreetMap</a>,
        <a :href="google" target="_blank" rel="noopener noreferrer" data-testid="visit-route-google">Google Maps</a>
      </template>
    </p>

    <p v-if="showContact && siteUrl" class="visit-site">
      Site : <a :href="siteUrl" target="_blank" rel="noopener noreferrer" data-testid="visit-site">{{ siteUrl }}</a>
    </p>
    <p v-if="showContact && museum.phone" class="visit-phone" data-testid="visit-phone">Téléphone : {{ museum.phone }}</p>

    <p class="visit-hours" data-testid="visit-hours">
      Horaires, tarifs, accessibilité et services : ces informations ne figurent pas dans les données publiées
      <template v-if="siteUrl">; consultez le site du musée avant de vous déplacer.</template>
      <template v-else>; renseignez-vous auprès du musée avant de vous déplacer.</template>
    </p>
  </div>
</template>

<script lang="ts">
import { computed, defineComponent, PropType } from 'vue';
import { lienCarte, lienItineraire } from '@/utils/itineraire';
import { safeWebUrl } from '@/utils/rights';
import { Museum } from '@/types/models';

// De quoi préparer une visite : où est le musée, comment y aller, comment le joindre. Les cartes sont celles de sites
// tiers, ouvertes dans un autre onglet : rien n'est chargé depuis eux avant le clic.
export default defineComponent({
  name: 'MuseumVisit',
  props: {
    museum: { type: Object as PropType<Partial<Museum>>, required: true },
    /** Adresse, site et téléphone : à masquer quand la page les montre déjà */
    showContact: { type: Boolean, default: true }
  },
  setup(props) {
    const adresse = computed(() => {
      const m = props.museum;
      return [m.address, [m.zipCode, m.city].filter(Boolean).join(' ')].filter(Boolean).join(', ');
    });
    const carte = computed(() => lienCarte(props.museum));
    const osm = computed(() => lienItineraire(props.museum, 'osm'));
    const google = computed(() => lienItineraire(props.museum, 'google') ?? '');
    const siteUrl = computed(() => safeWebUrl(props.museum.website));
    return { adresse, carte, osm, google, siteUrl };
  }
});
</script>

<style scoped lang="scss">
.museum-visit {
  p {
    margin: 4px 0;
  }

  .visit-hours {
    margin-top: 8px;
    font-size: 0.85rem;
    color: #777;
  }
}
</style>
