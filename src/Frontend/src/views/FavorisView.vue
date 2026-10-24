<template>
  <div class="favoris-page">
    <h1 data-testid="favoris-heading">Mes favoris</h1>

    <p class="note" data-testid="favoris-note">
      Vos favoris restent sur cet appareil, dans votre navigateur : aucun compte n'est nécessaire et rien n'est envoyé à
      un serveur. Ils sont perdus si vous videz les données de navigation.
      <router-link :to="{ name: 'collections', hash: '#sauvegarde' }" data-testid="favoris-export">
        Exporter une sauvegarde de vos favoris et de vos collections
      </router-link>
    </p>

    <p v-if="favoris.probleme" class="alerte" data-testid="favoris-probleme" role="alert">
      {{ messages[favoris.probleme] }}
    </p>

    <p v-if="ids.length === 0" class="vide" data-testid="favoris-vide">
      Vous n'avez pas encore de favori. Sur une œuvre, utilisez l'étoile pour la retrouver ici.
      <router-link :to="{ name: 'artworks' }">Parcourir les œuvres</router-link>
    </p>

    <template v-else>
      <p class="compte" data-testid="favoris-compte">{{ ids.length }} {{ ids.length > 1 ? 'œuvres favorites' : 'œuvre favorite' }}</p>
      <OeuvresParId :ids="ids" libelleRetrait="Supprimer des favoris" @retirer="retirerFavori" />
    </template>
  </div>
</template>

<script lang="ts">
import { defineComponent, computed } from 'vue';
import OeuvresParId from '@/components/OeuvresParId.vue';
import { favoris, MESSAGES_FAVORIS, retirerFavori } from '@/utils/favoris';

export default defineComponent({
  name: 'FavorisView',
  components: { OeuvresParId },
  setup() {
    const ids = computed(() => favoris.favoris.map(f => f.id));
    return { favoris, messages: MESSAGES_FAVORIS, ids, retirerFavori };
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
}
</style>
