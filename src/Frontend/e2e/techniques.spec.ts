import { expect, test } from './fixtures';
import { NOTICES } from './jeu';

// Quatre techniques (« technique 0 » à « technique 3 »), chacune avec les notices de son numéro modulo 4 (jeu.ts).

test('la liste des techniques les présente toutes, depuis le menu', async ({ page }) => {
  await page.goto('/');

  await page.getByRole('link', { name: 'Techniques', exact: true }).click();

  await expect(page).toHaveURL(/\/techniques$/);
  await expect(page.getByTestId('reference-count')).toHaveText('4 techniques');
  await expect(page.getByTestId('reference-card')).toHaveCount(4);
});

test('la recherche réduit la liste aux techniques dont le nom correspond', async ({ page }) => {
  await page.goto('/techniques');
  await expect(page.getByTestId('reference-card')).toHaveCount(4);

  await page.getByTestId('reference-search').fill('technique 2');

  await expect(page.getByTestId('reference-card')).toHaveCount(1);
  await expect(page.getByTestId('reference-card')).toContainText('technique 2');
  await expect(page.getByTestId('reference-count')).toHaveText('1 technique');
});

test('cliquer une technique affiche ses œuvres, et seulement les siennes, filtre réaffiché', async ({ page }) => {
  const siennes = NOTICES.filter(n => n.technique === 'technique 3');
  await page.goto('/techniques');

  await page.getByTestId('reference-card').filter({ hasText: 'technique 3' }).click();

  await expect(page).toHaveURL(/\/search\?.*techniqueId=/);
  await expect(page.getByTestId('results-total')).toHaveText(String(siennes.length));
  await expect(page.getByTestId('filter-label')).toHaveText('technique 3');
});

test("la technique d'une œuvre mène à la recherche des œuvres de cette technique", async ({ page }) => {
  await page.goto('/techniques');
  await page.getByTestId('reference-card').filter({ hasText: 'technique 0' }).click();
  await page.getByTestId('artwork-card').first().click();
  await expect(page.getByTestId('artwork-reference')).toHaveText(/^E2E\d{5}$/);

  await page.getByTestId('technique-tag').click();

  await expect(page).toHaveURL(/\/search\?.*techniqueId=/);
  await expect(page.getByTestId('results-total')).toHaveText(String(NOTICES.filter(n => n.technique === 'technique 0').length));
});
