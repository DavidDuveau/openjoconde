import { readFileSync } from 'node:fs';
import { expect, test } from './fixtures';

test("la fiche d'une œuvre s'exporte en JSON, avec sa source et sa licence", async ({ page }) => {
  await page.goto('/artworks');
  await page.getByTestId('artwork-card').first().click();
  await expect(page.getByTestId('artwork-reference')).toHaveText(/^E2E\d{5}$/);
  const reference = (await page.getByTestId('artwork-reference').innerText()).trim();

  const [telechargement] = await Promise.all([page.waitForEvent('download'), page.getByTestId('export-json').click()]);

  expect(telechargement.suggestedFilename()).toBe(`joconde-${reference}.json`);
  const donnees = JSON.parse(readFileSync((await telechargement.path())!, 'utf-8'));
  expect(donnees.reference).toBe(reference);
  expect(donnees.musee).toMatch(/^musée \d$/);
  expect(donnees.source).toMatchObject({ base: 'Base Joconde, ministère de la Culture', licence: 'Licence Ouverte 2.0' });
});

test("la fiche d'une œuvre s'exporte en CSV lisible par un tableur français", async ({ page }) => {
  await page.goto('/artworks');
  await page.getByTestId('artwork-card').first().click();
  await expect(page.getByTestId('artwork-reference')).toHaveText(/^E2E\d{5}$/);

  const [telechargement] = await Promise.all([page.waitForEvent('download'), page.getByTestId('export-csv').click()]);

  expect(telechargement.suggestedFilename()).toMatch(/^joconde-E2E\d{5}\.csv$/);
  const contenu = readFileSync((await telechargement.path())!, 'utf-8');
  expect(contenu.startsWith('\uFEFF"reference";"titre";')).toBe(true);
  expect(contenu.trim().split('\r\n')).toHaveLength(2);
});
