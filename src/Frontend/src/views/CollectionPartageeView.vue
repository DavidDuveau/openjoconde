<template>
  <div class="partagee-page">
    <div v-if="erreur" class="erreur" data-testid="partage-erreur" role="alert">
      <h1>Lien de collection invalide</h1>
      <p>{{ erreur }}</p>
      <router-link :to="{ name: 'home' }">Retour à l'accueil</router-link>
    </div>

    <template v-else-if="partagee">
      <header class="entete">
        <p class="bandeau" data-testid="partage-bandeau">Collection partagée, en lecture seule</p>
        <h1 data-testid="partage-titre">{{ partagee.nom }}</h1>
        <p v-if="partagee.description" class="description" data-testid="partage-texte">{{ partagee.description }}</p>
        <p class="compte" data-testid="partage-compte">
          {{ partagee.oeuvres.length }} {{ partagee.oeuvres.length > 1 ? 'œuvres' : 'œuvre' }}
        </p>
        <button type="button" data-testid="partage-ajouter" @click="ajouter">Ajouter à mes collections</button>
        <p class="statut" data-testid="partage-statut" role="status" aria-live="polite">
          {{ statut }}
          <router-link v-if="creee" :to="{ name: 'collection', params: { id: creee } }" data-testid="partage-voir">Voir la collection</router-link>
        </p>
      </header>

      <p v-if="partagee.oeuvres.length === 0" class="vide">Cette collection est vide.</p>
      <OeuvresParId v-else :ids="partagee.oeuvres" :retirable="false" />
    </template>
  </div>
</template>

<script lang="ts">
import { defineComponent, computed, ref } from 'vue';
import { useRoute } from 'vue-router';
import OeuvresParId from '@/components/OeuvresParId.vue';
import { creerCollection, MESSAGES_COLLECTIONS } from '@/utils/collections';
import { CollectionPartagee, decoderPartage, ErreurPartage } from '@/utils/partage';

// Lecture d'une collection portée par le lien : tout est dans le fragment, qu'aucun serveur ne reçoit. Ce qu'il contient
// est validé puis affiché comme du texte, jamais interprété.
export default defineComponent({
  name: 'CollectionPartageeView',
  components: { OeuvresParId },
  setup() {
    const route = useRoute();
    const statut = ref('');
    const creee = ref('');

    const lecture = computed<{ partagee: CollectionPartagee | null; erreur: string }>(() => {
      try {
        return { partagee: decoderPartage(route.hash), erreur: '' };
      } catch (e) {
        if (!(e instanceof ErreurPartage)) throw e;
        return { partagee: null, erreur: e.message };
      }
    });
    const partagee = computed(() => lecture.value.partagee);
    const erreur = computed(() => lecture.value.erreur);

    const ajouter = () => {
      if (!partagee.value) return;
      const resultat = creerCollection(partagee.value.nom, partagee.value.description, partagee.value.oeuvres);
      if (!resultat.ok) {
        statut.value = MESSAGES_COLLECTIONS[resultat.refus];
        creee.value = '';
        return;
      }
      statut.value = 'Collection ajoutée à vos collections.';
      creee.value = resultat.valeur.id;
    };

    return { partagee, erreur, statut, creee, ajouter };
  }
});
</script>

<style scoped lang="scss">
.partagee-page {
  max-width: 1000px;
  margin: 0 auto;
  padding: 20px;

  .erreur {
    padding: 40px;
    text-align: center;
  }

  .bandeau {
    display: inline-block;
    padding: 2px 10px;
    border-radius: 10px;
    background: #eaf7f1;
    font-size: 0.85rem;
  }

  .description {
    color: #555;
    white-space: pre-line;
  }

  .compte {
    font-weight: 600;
  }

  button {
    padding: 6px 14px;
    border: 1px solid var(--secondary-color);
    border-radius: 4px;
    background: white;
    cursor: pointer;
  }

  .statut {
    min-height: 1.4em;
    font-size: 0.9rem;
  }
}
</style>
