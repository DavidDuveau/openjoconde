import { expect, test } from './fixtures';

// Toutes les notices du jeu ont un artiste, une période, une technique et un musée, partagés avec d'autres (jeu.ts).

test("la fiche d'une œuvre propose des œuvres similaires, groupées par critère", async ({ page }) => {
  await page.goto('/artworks');
  await page.getByTestId('artwork-card').first().click();
  await expect(page.getByTestId('artwork-reference')).toHaveText(/^E2E\d{5}$/);

  const criteres = page.getByTestId('similar-criterion');
  await expect(criteres).toHaveCount(4);
  await expect(criteres.nth(0)).toContainText('Même artiste : Prénom AUTEUR');
  await expect(criteres.nth(1)).toContainText('Même période : période');
  await expect(criteres.nth(2)).toContainText('Même technique : technique');
  await expect(criteres.nth(3)).toContainText('Même musée : musée');
  await expect(page.getByTestId('similar-artworks').getByTestId('artwork-card').first()).toBeVisible();
});

test("ouvrir une œuvre similaire affiche sa fiche, et « précédent » revient à l'œuvre d'origine", async ({ page }) => {
  await page.goto('/artworks');
  await page.getByTestId('artwork-card').first().click();
  await expect(page.getByTestId('artwork-reference')).toHaveText(/^E2E\d{5}$/);
  const origine = await page.getByTestId('artwork-reference').innerText();

  await page.getByTestId('similar-artworks').getByTestId('artwork-card').first().click();

  await expect(page.getByTestId('artwork-reference')).not.toHaveText(origine);
  await page.goBack();
  await expect(page.getByTestId('artwork-reference')).toHaveText(origine);
});
