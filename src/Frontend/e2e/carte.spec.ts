import { expect, test } from './fixtures';

// Trois musées situés (jeu.ts) : « musée 0 » à Vannes (Morbihan, Bretagne), « musée 1 » à Albi (Tarn, Occitanie),
// « musée 2 » à Toulouse (Haute-Garonne, Occitanie). Codes : Bretagne 53, Occitanie 76, Morbihan 56, Tarn 81.

// Un contour se clique par son événement : le centre d'une région n'est pas toujours dans la région (Bretagne, Corse)
const cliquer = (page: import('@playwright/test').Page, code: string) =>
  page.locator(`[data-testid="map-shape"][data-code="${code}"]`).dispatchEvent('click');

test("l'accueil affiche la carte de France : 18 régions, outre-mer compris, et un point par musée", async ({ page }) => {
  await page.goto('/');

  await expect(page.getByTestId('map-shape')).toHaveCount(18);
  await expect(page.getByTestId('map-point')).toHaveCount(3);
  await expect(page.getByTestId('map-count')).toContainText('3 musées sur la carte');
  await expect(page.getByTestId('map-unlocated')).toHaveCount(0);
});

test('le nom du musée apparaît au survol de son point', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByTestId('map-point')).toHaveCount(3);

  await page.locator('[data-testid="map-point"][data-name="musée 1"]').hover();

  await expect(page.getByTestId('map-tooltip')).toHaveText('musée 1');
});

test('un clic sur une région zoome sur ses départements, un clic sur un département sur ses musées', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByTestId('map-point')).toHaveCount(3);

  await cliquer(page, '76');

  await expect(page.getByTestId('map-crumb')).toHaveText(['France', 'Occitanie']);
  await expect(page.getByTestId('map-shape')).toHaveCount(13);
  await expect(page.getByTestId('map-point')).toHaveCount(2);

  await cliquer(page, '81');

  await expect(page.getByTestId('map-crumb')).toHaveText(['France', 'Occitanie', 'Tarn']);
  await expect(page.getByTestId('map-shape')).toHaveCount(1);
  await expect(page.getByTestId('map-point')).toHaveCount(1);
  await expect(page.getByTestId('map-point')).toHaveAttribute('data-name', 'musée 1');
});

test('le fil d\'Ariane remonte de département en région, puis à la France', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByTestId('map-point')).toHaveCount(3);
  await cliquer(page, '53');
  await cliquer(page, '56');
  await expect(page.getByTestId('map-point')).toHaveCount(1);

  await page.getByTestId('map-crumb').nth(1).click();
  await expect(page.getByTestId('map-shape')).toHaveCount(4);

  await page.getByTestId('map-crumb').nth(0).click();
  await expect(page.getByTestId('map-shape')).toHaveCount(18);
  await expect(page.getByTestId('map-point')).toHaveCount(3);
});

test('un clic sur le point d\'un musée ouvre sa fiche', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByTestId('map-point')).toHaveCount(3);
  await cliquer(page, '76');
  await cliquer(page, '81');

  await page.getByTestId('map-point').click();

  await expect(page).toHaveURL(/\/museums\//);
  await expect(page.getByTestId('museum-heading')).toHaveText('musée 1');
});
