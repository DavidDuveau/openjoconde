import { defineConfig } from '@playwright/test';

// Les chemins de la pile (e2e/pile.mjs) partent de ce dossier, d'où que la commande soit lancée
process.chdir(__dirname);

const API = 'http://localhost:5101';
const FRONT = 'http://localhost:8101';

export default defineConfig({
  testDir: './e2e',
  testMatch: '**/*.spec.ts',
  workers: 1,
  timeout: 30_000,
  reporter: [['list']],
  use: { baseURL: FRONT, trace: 'retain-on-failure' },
  globalSetup: './e2e/global-setup.ts',
  globalTeardown: './e2e/global-teardown.ts',
  webServer: [
    { command: 'node e2e/start-api.mjs', url: `${API}/api/health`, timeout: 300_000 },
    { command: 'node e2e/start-front.mjs', url: FRONT, timeout: 300_000 }
  ],
  projects: [{ name: 'chromium', use: { browserName: 'chromium' } }]
});
