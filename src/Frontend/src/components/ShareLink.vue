<template>
  <div class="share-link" data-testid="share-link">
    <button v-if="canShare" type="button" class="share-button" data-testid="share-native" @click="share">Partager</button>
    <button type="button" class="share-button" data-testid="share-copy" @click="copy">Copier le lien</button>

    <p class="share-status" data-testid="share-status" role="status" aria-live="polite">{{ status }}</p>

    <label v-if="manual" class="share-manual">
      Copiez l'adresse :
      <input type="text" readonly :value="url()" data-testid="share-manual" @focus="($event.target as HTMLInputElement).select()" />
    </label>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref } from 'vue';

// Partager une page sans réseau social intégré (ils tracent les visiteurs) : le partage du système quand le navigateur
// le propose (messagerie, courriel, réseaux au choix de l'utilisateur), sinon la copie de l'adresse.
export default defineComponent({
  name: 'ShareLink',
  props: {
    /** Titre proposé au partage */
    title: { type: String, required: true },
    /** Adresse à partager ; celle de la page par défaut */
    shareUrl: { type: String, default: '' }
  },
  setup(props) {
    const status = ref('');
    const manual = ref(false);
    const canShare = typeof navigator !== 'undefined' && typeof navigator.share === 'function';

    const url = (): string => props.shareUrl || window.location.href;

    const share = async () => {
      status.value = '';
      try {
        await navigator.share({ title: props.title, url: url() });
      } catch (error) {
        // L'utilisateur qui ferme la fenêtre de partage n'a pas d'erreur à lire
        if ((error as DOMException)?.name === 'AbortError') return;
        await copy();
      }
    };

    const copy = async () => {
      manual.value = false;
      try {
        await navigator.clipboard.writeText(url());
        status.value = 'Lien copié dans le presse-papiers.';
      } catch {
        // Presse-papiers refusé ou absent (page non sécurisée, navigateur ancien) : l'adresse est donnée à copier à la main
        manual.value = true;
        status.value = "La copie automatique n'a pas fonctionné.";
      }
    };

    return { status, manual, canShare, share, copy, url };
  }
});
</script>

<style scoped lang="scss">
.share-link {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;

  .share-button {
    padding: 6px 14px;
    border: 1px solid var(--secondary-color);
    border-radius: 4px;
    background: white;
    cursor: pointer;

    &:hover {
      background: #eaf7f1;
    }
  }

  .share-status {
    margin: 0;
    font-size: 0.85rem;
    color: #555;
  }

  .share-manual {
    display: flex;
    gap: 8px;
    align-items: center;
    width: 100%;
    font-size: 0.85rem;

    input {
      flex: 1;
      padding: 4px 8px;
    }
  }
}
</style>
