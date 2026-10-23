import { defineConfig } from 'vitest/config';
import vue from '@vitejs/plugin-vue';
import { fileURLToPath, URL } from 'node:url';
import { readFileSync } from 'node:fs';

const { version } = JSON.parse(readFileSync(new URL('./package.json', import.meta.url), 'utf8')) as { version: string };

// Tests unitaires du front : composants montés dans jsdom, sans le build webpack de Vue CLI
export default defineConfig({
  plugins: [vue()],
  define: { __APP_VERSION__: JSON.stringify(version) },
  resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
  test: {
    environment: 'jsdom',
    include: ['tests/unit/**/*.spec.ts'],
    css: false,
    coverage: {
      provider: 'v8',
      include: ['src/**/*.{ts,vue}'],
      exclude: ['src/main.ts', 'src/**/*.d.ts', 'src/types/**'],
      reporter: ['text', 'lcov', 'cobertura'],
      // Plancher de couverture : un nouveau code doit être testé, sinon la CI échoue
      thresholds: { lines: 85, statements: 85, functions: 85, branches: 85 }
    }
  }
});
