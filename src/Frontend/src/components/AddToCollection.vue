<template>
  <div class="add-to-collection" data-testid="add-to-collection">
    <button
      type="button"
      class="toggle"
      data-testid="add-to-collection-toggle"
      :aria-expanded="ouvert"
      @click="ouvert = !ouvert"
    >
      Ajouter à une collection
    </button>

    <form v-if="ouvert" class="panneau" data-testid="add-to-collection-panel" @submit.prevent="valider">
      <label v-if="collections.collections.length">
        Collection
        <select v-model="choix" data-testid="add-to-collection-select">
          <option v-for="c in collections.collections" :key="c.id" :value="c.id">
            {{ c.nom }}{{ c.oeuvres.includes(id) ? ' (déjà ajoutée)' : '' }}
          </option>
          <option :value="NOUVELLE">Nouvelle collection…</option>
        </select>
      </label>

      <label v-if="choix === NOUVELLE">
        Nom de la nouvelle collection
        <input v-model="nom" type="text" :maxlength="longueurMax" data-testid="add-to-collection-name" />
      </label>

      <button type="submit" data-testid="add-to-collection-submit">{{ choix === NOUVELLE ? 'Créer et ajouter' : 'Ajouter' }}</button>
    </form>

    <p class="statut" data-testid="add-to-collection-status" role="status" aria-live="polite">{{ statut }}</p>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref, watch } from 'vue';
import {
  ajouterOeuvre, collections, creerCollection, LONGUEUR_NOM, MESSAGES_COLLECTIONS, trouverCollection
} from '@/utils/collections';

const NOUVELLE = '__nouvelle__';

// Range une œuvre dans une collection du navigateur : une existante, ou une nouvelle créée au passage
export default defineComponent({
  name: 'AddToCollection',
  props: {
    id: { type: String, required: true }
  },
  setup(props) {
    const ouvert = ref(false);
    const choix = ref(collections.collections[0]?.id ?? NOUVELLE);
    const nom = ref('');
    const statut = ref('');

    // La collection choisie qui disparaît (autre onglet) ramène au choix « nouvelle »
    watch(() => collections.collections.length, () => {
      if (choix.value !== NOUVELLE && !trouverCollection(choix.value)) choix.value = collections.collections[0]?.id ?? NOUVELLE;
      if (!collections.collections.length) choix.value = NOUVELLE;
    });

    const valider = () => {
      statut.value = '';
      if (choix.value === NOUVELLE) {
        const creee = creerCollection(nom.value, '', [props.id]);
        if (!creee.ok) {
          statut.value = MESSAGES_COLLECTIONS[creee.refus];
          return;
        }
        statut.value = `Collection « ${creee.valeur.nom} » créée, œuvre ajoutée.`;
        choix.value = creee.valeur.id;
        nom.value = '';
        return;
      }
      const nomCollection = trouverCollection(choix.value)?.nom ?? '';
      const ajout = ajouterOeuvre(choix.value, props.id);
      if (!ajout.ok) statut.value = MESSAGES_COLLECTIONS[ajout.refus];
      else if (!ajout.valeur) statut.value = `Cette œuvre est déjà dans la collection « ${nomCollection} ».`;
      else statut.value = `Œuvre ajoutée à la collection « ${nomCollection} ».`;
    };

    return { ouvert, choix, nom, statut, valider, collections, NOUVELLE, longueurMax: LONGUEUR_NOM };
  }
});
</script>

<style scoped lang="scss">
.add-to-collection {
  display: flex;
  flex-direction: column;
  gap: 6px;
  align-items: flex-start;

  .toggle,
  button[type='submit'] {
    padding: 6px 14px;
    border: 1px solid var(--secondary-color);
    border-radius: 4px;
    background: white;
    cursor: pointer;

    &:hover {
      background: #eaf7f1;
    }
  }

  .panneau {
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding: 12px;
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);

    label {
      display: flex;
      flex-direction: column;
      gap: 4px;
      font-size: 0.9rem;
    }
  }

  .statut {
    margin: 0;
    font-size: 0.85rem;
    color: #555;
  }
}
</style>
