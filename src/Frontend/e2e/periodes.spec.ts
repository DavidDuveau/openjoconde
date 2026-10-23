import { expect, test } from './fixtures';
import { NOTICES } from './jeu';

// Trois périodes (« période 0 » à « période 2 »), chacune avec les notices de son numéro modulo 3 (jeu.ts).

test('la liste des périodes les présente toutes, depuis le menu', async ({ page }) => {
  await page.goto('/');

  await page.getByRole('link', { name: 'Périodes' }).click();

  await expect(page).toHaveURL(/\/periods$/);
  await expect(page.getByTestId('reference-count')).toHaveText('3 périodes');
  await expect(page.getByTestId('reference-card')).toHaveCount(3);
});

test('la recherche réduit la liste aux périodes dont le nom correspond', async ({ page }) => {
  await page.goto('/periods');
  await expect(page.getByTestId('reference-card')).toHaveCount(3);

  await page.getByTestId('reference-search').fill('période 1');

  await expect(page.getByTestId('reference-card')).toHaveCount(1);
  await expect(page.getByTestId('reference-card')).toContainText('période 1');
  await expect(page.getByTestId('reference-count')).toHaveText('1 période');
});

test("cliquer une période affiche ses œuvres, et seulement les siennes, filtre réaffiché", async ({ page }) => {
  const siennes = NOTICES.filter(n => n.periode === 'période 2');
  await page.goto('/periods');

  await page.getByTestId('reference-card').filter({ hasText: 'période 2' }).click();

  await expect(page).toHaveURL(/\/search\?.*periodId=/);
  await expect(page.getByTestId('results-total')).toHaveText(String(siennes.length));
  await expect(page.getByTestId('filter-label')).toHaveText('période 2');
});

test("la période d'une œuvre mène à la recherche des œuvres de cette période", async ({ page }) => {
  await page.goto('/periods');
  await page.getByTestId('reference-card').filter({ hasText: 'période 0' }).click();
  await page.getByTestId('artwork-card').first().click();
  await expect(page.getByTestId('artwork-reference')).toHaveText(/^E2E\d{5}$/);

  await page.getByTestId('period-tag').click();

  await expect(page).toHaveURL(/\/search\?.*periodId=/);
  await expect(page.getByTestId('results-total')).toHaveText(String(NOTICES.filter(n => n.periode === 'période 0').length));
});
