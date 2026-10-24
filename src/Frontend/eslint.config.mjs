import js from '@eslint/js';
import globals from 'globals';
import tseslint from 'typescript-eslint';
import vue from 'eslint-plugin-vue';

export default tseslint.config(
  { ignores: ['dist/', 'dist-e2e/', 'node_modules/', 'test-results/', 'playwright-report/', 'coverage/'] },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  ...vue.configs['flat/essential'],
  {
    files: ['**/*.vue'],
    languageOptions: { parserOptions: { parser: tseslint.parser } }
  },
  {
    languageOptions: { globals: { ...globals.browser, ...globals.node, __APP_VERSION__: 'readonly', __CHANGELOG__: 'readonly' } },
    rules: {
      'no-console': 'off',
      '@typescript-eslint/no-explicit-any': 'off'
    }
  }
);
