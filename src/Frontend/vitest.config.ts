import { defineConfig } from 'vitest/config';
import vue from '@vitejs/plugin-vue';
import { fileURLToPath, URL } from 'node:url';

// Tests unitaires du front : composants montés dans jsdom, sans le build webpack de Vue CLI
export default defineConfig({
  plugins: [vue()],
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
