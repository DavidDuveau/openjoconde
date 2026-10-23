import { expect, test } from './fixtures';
import { NOTICES } from './jeu';

// Trois musées (« musée 0 » à « musée 2 »), chacun avec les notices de son numéro modulo 3 (jeu.ts).

test('la liste des musées les présente tous', async ({ page }) => {
  await page.goto('/museums');

  await expect(page.getByTestId('museum-count')).toHaveText('3 musées');
  await expect(page.getByTestId('museum-card')).toHaveCount(3);
});

test('la recherche réduit la liste aux musées dont le nom correspond', async ({ page }) => {
  await page.goto('/museums');
  await expect(page.getByTestId('museum-card')).toHaveCount(3);

  await page.getByTestId('museum-search').fill('musée 1');

  await expect(page.getByTestId('museum-card')).toHaveCount(1);
  await expect(page.getByTestId('museum-card')).toContainText('musée 1');
  await expect(page.getByTestId('museum-count')).toHaveText('1 musée');
});

test('la fiche d\'un musée liste ses œuvres, et seulement les siennes', async ({ page }) => {
  const siennes = NOTICES.filter(n => n.musee === 'musée 2');
  await page.goto('/museums');

  await page.getByTestId('museum-card').filter({ hasText: 'musée 2' }).click();

  await expect(page).toHaveURL(/\/museums\//);
  await expect(page.getByTestId('museum-heading')).toHaveText('musée 2');
  await expect(page.getByTestId('museum-artworks-count')).toHaveText(`(${siennes.length})`);
  await expect(page.getByTestId('museum-artwork')).toHaveCount(siennes.length);
});

test('cliquer une œuvre de la fiche d\'un musée ouvre sa fiche, qui renvoie vers ce musée', async ({ page }) => {
  await page.goto('/museums');
  await page.getByTestId('museum-card').filter({ hasText: 'musée 0' }).click();
  await expect(page.getByTestId('museum-artwork').first()).toBeVisible();

  await page.getByTestId('museum-artwork').first().getByRole('link').click();

  await expect(page).toHaveURL(/\/artworks\//);
  await expect(page.getByTestId('artwork-reference')).toHaveText(/^E2E\d{5}$/);
  await page.getByTestId('artwork-museum-link').click();
  await expect(page.getByTestId('museum-heading')).toHaveText('musée 0');
});
