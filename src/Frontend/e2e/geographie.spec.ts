import { expect, test } from './fixtures';
import { MUSEES, NOTICES } from './jeu';

// Trois musées situés (jeu.ts) : « musée 0 » en Bretagne (Morbihan), « musée 1 » en Occitanie (Tarn), « musée 2 » en
// Occitanie (Haute-Garonne), chacun avec les notices de son numéro modulo 3.

const duMusee = (nom: string) => NOTICES.filter(n => n.musee === nom);

test('la région choisie ne garde que les œuvres de ses musées, et restreint les départements', async ({ page }) => {
  await page.goto('/search');

  await page.getByTestId('region-select').selectOption('Occitanie');

  const attendues = duMusee('musée 1').length + duMusee('musée 2').length;
  await expect(page.getByTestId('results-total')).toHaveText(String(attendues));
  await expect(page).toHaveURL(/region=Occitanie/);
  await expect(page.getByTestId('department-select').locator('option')).toHaveText(
    ['Tous les départements', 'Haute-Garonne (1)', 'Tarn (1)']
  );
});

test('le département choisi ne garde que les œuvres de ses musées', async ({ page }) => {
  await page.goto('/search');

  await page.getByTestId('department-select').selectOption('Tarn');

  await expect(page.getByTestId('results-total')).toHaveText(String(duMusee('musée 1').length));
});

test('la région et le département se retrouvent au rechargement', async ({ page }) => {
  await page.goto('/search?region=Occitanie&department=Tarn');

  await expect(page.getByTestId('results-total')).toHaveText(String(duMusee('musée 1').length));
  await expect(page.getByTestId('region-select')).toHaveValue('Occitanie');
  await expect(page.getByTestId('department-select')).toHaveValue('Tarn');
});

test('choisir un musée affiche son résumé, lié à sa fiche, au-dessus de ses œuvres', async ({ page }) => {
  await page.goto('/search');

  await page.getByPlaceholder('Rechercher un musée').fill('musée 2');
  await page.getByTestId('filter-option').filter({ hasText: 'musée 2' }).click();

  await expect(page.getByTestId('results-total')).toHaveText(String(duMusee('musée 2').length));
  await expect(page.getByTestId('museum-summary-name')).toHaveText('musée 2');
  await page.getByTestId('museum-summary-name').click();
  await expect(page).toHaveURL(/\/museums\//);
  await expect(page.getByTestId('museum-heading')).toHaveText('musée 2');
});

test.describe('près de moi', () => {
  const toulouse = MUSEES[2];
  test.use({ geolocation: { latitude: toulouse.latitude, longitude: toulouse.longitude }, permissions: ['geolocation'] });

  test('les œuvres du musée le plus proche viennent en premier, et la position se retire', async ({ page }) => {
    const siennes = duMusee('musée 2');
    await page.goto('/search');

    await page.getByTestId('near-me').click();

    await expect(page).toHaveURL(/sortBy=distance/);
    await expect(page.getByTestId('sort')).toHaveValue('distance');
    await expect(page.getByTestId('artwork-card')).toHaveCount(12);
    const titres = await page.getByTestId('artwork-title').allInnerTexts();
    const attendus = new Set(siennes.map(n => n.titre));
    expect(titres.slice(0, siennes.length).every(t => attendus.has(t))).toBe(true);

    await page.getByTestId('position-clear').click();
    await expect(page).not.toHaveURL(/sortBy=distance/);
    await expect(page.getByTestId('position-active')).toHaveCount(0);
  });
});
