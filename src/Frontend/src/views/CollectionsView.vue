<template>
  <div class="collections-page">
    <h1 data-testid="collections-heading">Mes collections</h1>

    <p class="note" data-testid="collections-note">
      Vos collections restent sur cet appareil, dans votre navigateur : aucun compte n'est nécessaire et rien n'est envoyé
      à un serveur. Elles sont perdues si vous videz les données de navigation : exportez-les pour les sauvegarder.
    </p>

    <p v-if="etat.probleme" class="alerte" data-testid="collections-probleme" role="alert">{{ messages.stockage }}</p>

    <form class="creation" data-testid="collection-form" @submit.prevent="creer">
      <h2>Nouvelle collection</h2>
      <label>
        Nom
        <input v-model="nom" type="text" :maxlength="longueurNom" data-testid="collection-nom" />
      </label>
      <label>
        Description (facultative)
        <textarea v-model="description" rows="2" :maxlength="longueurDescription" data-testid="collection-description"></textarea>
      </label>
      <button type="submit" data-testid="collection-creer">Créer la collection</button>
      <p v-if="erreur" class="alerte" data-testid="collection-erreur" role="alert">{{ erreur }}</p>
    </form>

    <p v-if="etat.collections.length === 0" class="vide" data-testid="collections-vide">
      Vous n'avez pas encore de collection. Créez-en une ici, ou depuis la fiche d'une œuvre.
    </p>
    <ul v-else class="liste" data-testid="collections-liste">
      <li v-for="c in etat.collections" :key="c.id" data-testid="collection-item">
        <router-link :to="{ name: 'collection', params: { id: c.id } }" class="nom">{{ c.nom }}</router-link>
        <span class="compte">{{ c.oeuvres.length }} {{ c.oeuvres.length > 1 ? 'œuvres' : 'œuvre' }}</span>
        <span v-if="c.description" class="description">{{ c.description }}</span>
      </li>
    </ul>

    <section id="sauvegarde" class="sauvegarde" data-testid="sauvegarde" aria-labelledby="sauvegarde-titre">
      <h2 id="sauvegarde-titre">Sauvegarder ou changer d'appareil</h2>
      <p>
        L'export crée un fichier de vos favoris et de vos collections ; l'import le relit sur cet appareil ou sur un autre.
      </p>
      <button type="button" data-testid="sauvegarde-export" @click="exporter">Exporter mes favoris et mes collections</button>

      <div class="import">
        <label>
          Importer un fichier de sauvegarde
          <input type="file" accept="application/json,.json" data-testid="sauvegarde-fichier" @change="choisirFichier" />
        </label>

        <fieldset v-if="fichier !== null" class="mode">
          <legend>Que faire des données actuelles ?</legend>
          <label><input v-model="mode" type="radio" value="fusionner" data-testid="mode-fusionner" /> Fusionner avec mes données</label>
          <label><input v-model="mode" type="radio" value="remplacer" data-testid="mode-remplacer" /> Remplacer mes données</label>
          <button type="button" data-testid="sauvegarde-importer" @click="importer">Importer</button>
        </fieldset>

        <p v-if="erreurImport" class="alerte" data-testid="sauvegarde-erreur" role="alert">{{ erreurImport }}</p>
        <p v-if="compteRendu" class="compte-rendu" data-testid="sauvegarde-compte-rendu" role="status">{{ compteRendu }}</p>
      </div>
    </section>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref } from 'vue';
import {
  collections, creerCollection, LONGUEUR_DESCRIPTION, LONGUEUR_NOM, MESSAGES_COLLECTIONS
} from '@/utils/collections';
import {
  CompteRendu, ErreurSauvegarde, importerSauvegarde, ModeImport, nomFichierSauvegarde, sauvegardeJson, TAILLE_MAXIMALE
} from '@/utils/sauvegarde';
import { telecharger } from '@/utils/exportOeuvre';

const pluriel = (n: number, un: string, plusieurs: string) => `${n} ${n > 1 ? plusieurs : un}`;

const decrire = (c: CompteRendu): string =>
  `Import terminé : ${pluriel(c.favorisAjoutes, 'favori ajouté', 'favoris ajoutés')}, `
  + `${pluriel(c.collectionsAjoutees, 'collection ajoutée', 'collections ajoutées')} ; `
  + `${pluriel(c.favorisIgnores, 'favori ignoré', 'favoris ignorés')} et `
  + `${pluriel(c.collectionsIgnorees, 'collection ignorée', 'collections ignorées')} (déjà présents, invalides ou au-delà des limites).`;

const lireFichier = (fichier: File): Promise<string> =>
  new Promise((resolve, reject) => {
    const lecteur = new FileReader();
    lecteur.onload = () => resolve(String(lecteur.result));
    lecteur.onerror = () => reject(new ErreurSauvegarde('Ce fichier ne peut pas être lu.'));
    lecteur.readAsText(fichier);
  });

export default defineComponent({
  name: 'CollectionsView',
  setup() {
    const nom = ref('');
    const description = ref('');
    const erreur = ref('');
    const fichier = ref<string | null>(null);
    const mode = ref<ModeImport>('fusionner');
    const erreurImport = ref('');
    const compteRendu = ref('');

    const creer = () => {
      erreur.value = '';
      const resultat = creerCollection(nom.value, description.value);
      if (!resultat.ok) {
        erreur.value = MESSAGES_COLLECTIONS[resultat.refus];
        return;
      }
      nom.value = '';
      description.value = '';
    };

    const exporter = () => telecharger(nomFichierSauvegarde(), sauvegardeJson(), 'application/json');

    const choisirFichier = async (evenement: Event) => {
      fichier.value = null;
      erreurImport.value = '';
      compteRendu.value = '';
      const choisi = (evenement.target as HTMLInputElement).files?.[0];
      if (!choisi) return;
      if (choisi.size > TAILLE_MAXIMALE) {
        erreurImport.value = 'Ce fichier est trop gros pour être une sauvegarde OpenJoconde.';
        return;
      }
      try {
        fichier.value = await lireFichier(choisi);
      } catch (e) {
        erreurImport.value = (e as Error).message;
      }
    };

    const importer = () => {
      erreurImport.value = '';
      compteRendu.value = '';
      try {
        compteRendu.value = decrire(importerSauvegarde(fichier.value ?? '', mode.value));
        fichier.value = null;
      } catch (e) {
        if (!(e instanceof ErreurSauvegarde)) throw e;
        erreurImport.value = `${e.message} Vos données n'ont pas été modifiées.`;
      }
    };

    return {
      etat: collections, messages: MESSAGES_COLLECTIONS, nom, description, erreur, fichier, mode, erreurImport, compteRendu,
      longueurNom: LONGUEUR_NOM, longueurDescription: LONGUEUR_DESCRIPTION, creer, exporter, choisirFichier, importer
    };
  }
});
</script>

<style scoped lang="scss">
.collections-page {
  max-width: 800px;
  margin: 0 auto;
  padding: 20px;

  .note {
    padding: 12px;
    background: #eaf7f1;
    border-radius: 8px;
  }

  .alerte {
    color: #a33;
  }

  .creation,
  .sauvegarde {
    margin: 20px 0;
    padding: 16px;
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);

    h2 {
      margin-top: 0;
      color: var(--primary-color);
      font-size: 1.1rem;
    }

    label {
      display: flex;
      flex-direction: column;
      gap: 4px;
      margin-bottom: 10px;
    }

    button {
      padding: 6px 14px;
      border: 1px solid var(--secondary-color);
      border-radius: 4px;
      background: white;
      cursor: pointer;
    }
  }

  .liste {
    list-style: none;
    padding: 0;

    li {
      display: flex;
      flex-wrap: wrap;
      gap: 4px 12px;
      align-items: baseline;
      padding: 10px 12px;
      margin-bottom: 8px;
      background: white;
      border-radius: 8px;
    }

    .nom {
      font-weight: 600;
    }

    .compte {
      color: #666;
    }

    .description {
      width: 100%;
      color: #555;
      font-size: 0.9rem;
    }
  }

  .import {
    margin-top: 16px;

    .mode label {
      flex-direction: row;
    }
  }
}
</style>
