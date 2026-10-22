import { test as base, expect } from '@playwright/test';

/**
 * `test` de la suite : tout scénario échoue si la page lève une erreur JavaScript ou écrit une erreur dans la console
 * (hors icône du site, que personne ne sert). Un appel utilisé avant son initialisation ou une requête refusée sont
 * ainsi attrapés sans scénario dédié.
 */
export const test = base.extend<{ erreursPage: void }>({
  erreursPage: [
    async ({ page }, use) => {
      const erreurs: string[] = [];
      page.on('pageerror', erreur => erreurs.push(`erreur JavaScript : ${erreur.message}`));
      page.on('console', message => {
        if (message.type() === 'error' && !message.location().url.includes('favicon')) {
          erreurs.push(`console.error : ${message.text()}`);
        }
      });

      await use();

      expect(erreurs, 'erreurs levées par la page').toEqual([]);
    },
    { auto: true }
  ]
});

export { expect };
