import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { fileURLToPath, URL } from 'node:url';
import { readFileSync } from 'node:fs';

// Version du produit : celle de package.json, la même que le back (mise à jour par release-please)
const { version } = JSON.parse(readFileSync(new URL('./package.json', import.meta.url), 'utf8')) as { version: string };

// Changelog de release-please, lu au build pour la page « Nouveautés »
const changelog = readFileSync(new URL('../../CHANGELOG.md', import.meta.url), 'utf8');

// Le serveur de développement reste sur le port 8080 (origine déjà autorisée par l'API). Les variables
// VUE_APP_* de l'ancienne chaîne Vue CLI restent lues : .env.local et la CI n'ont pas à changer.
export default defineConfig({
  plugins: [vue()],
  envPrefix: ['VITE_', 'VUE_APP_'],
  define: { __APP_VERSION__: JSON.stringify(version), __CHANGELOG__: JSON.stringify(changelog) },
  resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
  server: { port: 8080, strictPort: true }
});
