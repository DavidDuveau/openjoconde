/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Adresse de l'API (défaut : https://localhost:5001/api) */
  readonly VUE_APP_API_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
