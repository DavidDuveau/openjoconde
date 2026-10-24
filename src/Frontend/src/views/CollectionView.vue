<template>
  <div class="collection-page">
    <div v-if="!collection" class="introuvable" data-testid="collection-introuvable">
      <p>{{ messages.inconnue }}</p>
      <router-link :to="{ name: 'collections' }">Voir mes collections</router-link>
    </div>

    <template v-else>
      <router-link :to="{ name: 'collections' }" class="back-link">&larr; Mes collections</router-link>

      <header class="entete">
        <template v-if="!edition">
          <h1 data-testid="collection-titre">{{ collection.nom }}</h1>
          <p v-if="collection.description" class="description" data-testid="collection-texte">{{ collection.description }}</p>
          <p class="compte" data-testid="collection-compte">
            {{ collection.oeuvres.length }} {{ collection.oeuvres.length > 1 ? 'œuvres' : 'œuvre' }}
          </p>
          <div class="boutons">
            <button type="button" data-testid="collection-modifier" @click="editer">Renommer</button>
            <button v-if="!confirmation" type="button" data-testid="collection-supprimer" @click="confirmation = true">Supprimer</button>
            <span v-else class="confirmation" role="alert">
              Supprimer cette collection ?
              <button type="button" data-testid="collection-confirmer" @click="supprimer">Oui, supprimer</button>
              <button type="button" data-testid="collection-annuler" @click="confirmation = false">Annuler</button>
            </span>
          </div>
        </template>

        <form v-else class="edition" data-testid="collection-edition" @submit.prevent="enregistrer">
          <label>
            Nom
            <input v-model="nom" type="text" :maxlength="longueurNom" data-testid="collection-edition-nom" />
          </label>
          <label>
            Description
            <textarea v-model="description" rows="2" :maxlength="longueurDescription" data-testid="collection-edition-description"></textarea>
          </label>
          <button type="submit" data-testid="collection-enregistrer">Enregistrer</button>
          <button type="button" @click="edition = false">Annuler</button>
        </form>

        <p v-if="erreur" class="alerte" data-testid="collection-erreur" role="alert">{{ erreur }}</p>
      </header>

      <section class="partage" data-testid="collection-partage" aria-labelledby="partage-titre">
        <h2 id="partage-titre">Partager cette collection</h2>
        <ShareLink v-if="adresse" :title="collection.nom" :shareUrl="adresse" />
        <p v-else class="trop-grande" data-testid="collection-trop-grande">
          Cette collection compte plus de {{ limitePartage }} œuvres : c'est trop pour un lien.
          <router-link :to="{ name: 'collections', hash: '#sauvegarde' }">Exportez-la</router-link> pour la transmettre.
        </p>
      </section>

      <p v-if="collection.oeuvres.length === 0" class="vide" data-testid="collection-vide">
        Cette collection est vide. Depuis la fiche d'une œuvre, utilisez « Ajouter à une collection ».
      </p>
      <OeuvresParId v-else :ids="collection.oeuvres" libelleRetrait="Retirer de la collection" @retirer="retirer">
        <template #actions="{ id, index }">
          <button type="button" data-testid="oeuvre-monter" :disabled="index === 0" :aria-label="`Monter l'œuvre en position ${index}`" @click="deplacer(id, -1)">↑ Monter</button>
          <button type="button" data-testid="oeuvre-descendre" :disabled="index === collection.oeuvres.length - 1" :aria-label="`Descendre l'œuvre en position ${index + 2}`" @click="deplacer(id, 1)">↓ Descendre</button>
          <button type="button" data-testid="oeuvre-retirer" @click="retirer(id)">Retirer</button>
        </template>
      </OeuvresParId>
    </template>
  </div>
</template>

<script lang="ts">
import { defineComponent, computed, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import OeuvresParId from '@/components/OeuvresParId.vue';
import ShareLink from '@/components/ShareLink.vue';
import {
  collections, deplacerOeuvre, LONGUEUR_DESCRIPTION, LONGUEUR_NOM, MESSAGES_COLLECTIONS, renommerCollection, retirerOeuvre,
  supprimerCollection, trouverCollection
} from '@/utils/collections';
import { adressePartage, LIMITE_PARTAGE } from '@/utils/partage';

export default defineComponent({
  name: 'CollectionView',
  components: { OeuvresParId, ShareLink },
  setup() {
    const route = useRoute();
    const router = useRouter();
    const edition = ref(false);
    const confirmation = ref(false);
    const nom = ref('');
    const description = ref('');
    const erreur = ref('');

    const collection = computed(() => {
      // Dépendre de la liste réactive pour suivre les modifications (autre onglet compris)
      void collections.collections;
      return trouverCollection(String(route.params.id ?? ''));
    });

    const adresse = computed(() => collection.value
      ? adressePartage(collection.value, window.location.origin, import.meta.env.BASE_URL ?? '/')
      : null);

    const signaler = (resultat: { ok: boolean; refus?: keyof typeof MESSAGES_COLLECTIONS }) => {
      erreur.value = resultat.ok ? '' : MESSAGES_COLLECTIONS[resultat.refus!];
      return resultat.ok;
    };

    const editer = () => {
      nom.value = collection.value?.nom ?? '';
      description.value = collection.value?.description ?? '';
      erreur.value = '';
      edition.value = true;
    };

    const enregistrer = () => {
      if (collection.value && signaler(renommerCollection(collection.value.id, nom.value, description.value))) edition.value = false;
    };

    const supprimer = () => {
      if (collection.value && signaler(supprimerCollection(collection.value.id))) router.push({ name: 'collections' });
      confirmation.value = false;
    };

    const retirer = (id: string) => { if (collection.value) signaler(retirerOeuvre(collection.value.id, id)); };
    const deplacer = (id: string, sens: -1 | 1) => { if (collection.value) signaler(deplacerOeuvre(collection.value.id, id, sens)); };

    return {
      collection, messages: MESSAGES_COLLECTIONS, edition, confirmation, nom, description, erreur, adresse,
      limitePartage: LIMITE_PARTAGE, longueurNom: LONGUEUR_NOM, longueurDescription: LONGUEUR_DESCRIPTION,
      editer, enregistrer, supprimer, retirer, deplacer
    };
  }
});
</script>

<style scoped lang="scss">
.collection-page {
  max-width: 1000px;
  margin: 0 auto;
  padding: 20px;

  .introuvable {
    text-align: center;
    padding: 40px;
  }

  .back-link {
    display: inline-block;
    margin-bottom: 12px;
  }

  .entete {
    margin-bottom: 16px;

    h1 {
      margin: 0 0 6px;
    }

    .description {
      color: #555;
      white-space: pre-line;
    }

    .compte {
      font-weight: 600;
    }

    .boutons,
    .edition {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      align-items: center;
    }

    .edition {
      flex-direction: column;
      align-items: stretch;
      max-width: 480px;

      label {
        display: flex;
        flex-direction: column;
        gap: 4px;
      }
    }
  }

  .alerte {
    color: #a33;
  }

  .partage {
    margin: 16px 0;

    h2 {
      font-size: 1.05rem;
      color: var(--primary-color);
    }
  }

  button {
    padding: 6px 14px;
    border: 1px solid var(--secondary-color);
    border-radius: 4px;
    background: white;
    cursor: pointer;

    &:disabled {
      opacity: 0.5;
      cursor: default;
    }
  }
}
</style>
