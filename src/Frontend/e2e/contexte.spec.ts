import { expect, test } from './fixtures';

// Le jeu n'a ni biographie ni lien Wikipédia (jeu.ts) : le contexte ne montre alors que ce que la base sait, sans rien inventer.

test("la fiche d'une œuvre a une section Contexte avec l'artiste et ses autres œuvres", async ({ page }) => {
  await page.goto('/artworks');
  await page.getByTestId('artwork-card').first().click();
  await expect(page.getByTestId('artwork-reference')).toHaveText(/^E2E\d{5}$/);

  const contexte = page.getByTestId('artwork-context');
  await expect(contexte).toBeVisible();
  await expect(page.getByTestId('context-artist-link')).toContainText('Prénom AUTEUR');
  await expect(contexte.getByTestId('context-term').first()).toBeVisible();
  await expect(page.getByTestId('context-biography')).toHaveCount(0);
  await expect(page.getByTestId('context-source')).toHaveCount(0);
});

test("le nom de l'artiste du contexte ouvre sa page avec la liste de ses œuvres", async ({ page }) => {
  await page.goto('/artworks');
  await page.getByTestId('artwork-card').first().click();
  await expect(page.getByTestId('artwork-reference')).toHaveText(/^E2E\d{5}$/);

  await page.getByTestId('context-artist-link').click();

  await expect(page.getByTestId('artist-heading')).toContainText('AUTEUR');
  await expect(page.getByTestId('artwork-card').first()).toBeVisible();
});
