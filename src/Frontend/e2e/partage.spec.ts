import { expect, test } from './fixtures';

test("la fiche d'une œuvre se partage : le lien copié est l'adresse de la fiche", async ({ page, context }) => {
  await context.grantPermissions(['clipboard-read', 'clipboard-write']);
  await page.goto('/artworks');
  await page.getByTestId('artwork-card').first().click();
  await expect(page.getByTestId('artwork-reference')).toHaveText(/^E2E\d{5}$/);

  await page.getByTestId('share-copy').click();

  await expect(page.getByTestId('share-status')).toHaveText('Lien copié dans le presse-papiers.');
  const copie = await page.evaluate(() => navigator.clipboard.readText());
  expect(copie).toBe(page.url());
  expect(copie).toMatch(/\/artworks\/[0-9a-f-]{36}$/);
});

test("sans presse-papiers, l'adresse est donnée à copier à la main", async ({ page }) => {
  await page.addInitScript(() => Object.defineProperty(navigator, 'clipboard', { value: undefined }));
  await page.goto('/artworks');
  await page.getByTestId('artwork-card').first().click();
  await expect(page.getByTestId('artwork-reference')).toHaveText(/^E2E\d{5}$/);

  await page.getByTestId('share-copy').click();

  await expect(page.getByTestId('share-manual')).toHaveValue(page.url());
});
