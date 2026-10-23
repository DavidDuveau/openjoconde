import { expect, test } from './fixtures';

// Les trois musées du jeu (jeu.ts) ont des coordonnées ; chaque notice du jeu a un musée.

test("la fiche d'une œuvre a une section Musée : nom, localisation, carte et itinéraire", async ({ page }) => {
  await page.goto('/artworks');
  await page.getByTestId('artwork-card').first().click();
  await expect(page.getByTestId('artwork-reference')).toHaveText(/^E2E\d{5}$/);

  const section = page.getByTestId('artwork-museum');
  await expect(section).toBeVisible();
  await expect(page.getByTestId('artwork-museum-link')).toHaveText(/^musée \d$/);
  await expect(page.getByTestId('artwork-museum-place')).toContainText(/Vannes|Albi|Toulouse|Bretagne|Occitanie|Morbihan|Tarn|Haute-Garonne/);
  await expect(section.getByTestId('visit-map')).toHaveAttribute('href', /openstreetmap\.org\/\?mlat=/);
  await expect(section.getByTestId('visit-route-osm')).toHaveAttribute('href', /openstreetmap\.org\/directions/);
  await expect(section.getByTestId('visit-hours')).toContainText('ne figurent pas dans les données publiées');
});

test("le nom du musée, depuis la fiche d'une œuvre, ouvre la page du musée", async ({ page }) => {
  await page.goto('/artworks');
  await page.getByTestId('artwork-card').first().click();
  await expect(page.getByTestId('artwork-museum')).toBeVisible();
  const nom = await page.getByTestId('artwork-museum-link').innerText();

  await page.getByTestId('artwork-museum-link').click();

  await expect(page.getByTestId('museum-heading')).toHaveText(nom);
  await expect(page.getByTestId('museum-visit-section')).toBeVisible();
});
