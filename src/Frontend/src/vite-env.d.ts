/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Adresse de l'API (défaut : https://localhost:5001/api) */
  readonly VUE_APP_API_URL?: string;
  /** Début de l'adresse d'une notice POP (défaut : https://www.pop.culture.gouv.fr/notice/joconde/) */
  readonly VUE_APP_POP_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
