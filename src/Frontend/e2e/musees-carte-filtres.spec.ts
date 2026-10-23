import { expect, test } from './fixtures';

// Trois musées situés (jeu.ts) : « musée 0 » à Vannes (Bretagne), « musée 1 » à Albi et « musée 2 » à Toulouse (Occitanie).

test('la liste des musées se restreint par région, puis par département', async ({ page }) => {
  await page.goto('/museums');
  await expect(page.getByTestId('museum-count')).toHaveText('3 musées');

  await page.getByTestId('region-select').selectOption('Occitanie');
  await expect(page.getByTestId('museum-count')).toHaveText('2 musées');

  await page.getByTestId('department-select').selectOption('Tarn');
  await expect(page.getByTestId('museum-count')).toHaveText('1 musée');
  await expect(page.getByTestId('museum-card')).toContainText('musée 1');
});

test('la page des musées montre la carte, et un point mène à la fiche du musée', async ({ page }) => {
  await page.goto('/museums');
  await expect(page.getByTestId('map-point')).toHaveCount(3);

  await page.locator('[data-testid="map-point"][data-name="musée 0"]').click();

  await expect(page).toHaveURL(/\/museums\//);
  await expect(page.getByTestId('museum-heading')).toHaveText('musée 0');
});

test('la fiche du musée mène à la recherche de sa collection, déjà filtrée sur le musée', async ({ page }) => {
  await page.goto('/museums');
  await page.getByTestId('museum-card').first().click();
  const nom = await page.getByTestId('museum-heading').innerText();

  await page.getByTestId('museum-explore').click();

  await expect(page).toHaveURL(/\/search\?.*museumId=/);
  await expect(page.getByTestId('artwork-card').first()).toBeVisible();
  expect(nom).toMatch(/^musée \d$/);
});

test("sans données Muséofile, le filtre par type de collection n'est pas proposé et la liste reste complète", async ({ page }) => {
  await page.goto('/museums');

  await expect(page.getByTestId('museum-count')).toHaveText('3 musées');
  await expect(page.getByTestId('region-select')).toBeVisible();
  await expect(page.getByTestId('collection-select')).toHaveCount(0);
});
