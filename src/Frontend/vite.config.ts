import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { fileURLToPath, URL } from 'node:url';

// Le serveur de développement reste sur le port 8080 (origine déjà autorisée par l'API). Les variables
// VUE_APP_* de l'ancienne chaîne Vue CLI restent lues : .env.local et la CI n'ont pas à changer.
export default defineConfig({
  plugins: [vue()],
  envPrefix: ['VITE_', 'VUE_APP_'],
  resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
  server: { port: 8080, strictPort: true }
});
