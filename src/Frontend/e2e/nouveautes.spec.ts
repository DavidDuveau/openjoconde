import { expect, test } from './fixtures';
import pkg from '../package.json';

test("« Nouveautés » est accessible depuis le pied de page et « À propos », et la dernière version est celle du produit", async ({ page }) => {
  await page.goto('/');
  await page.getByTestId('footer-nouveautes').click();

  await expect(page.getByTestId('nouveautes-heading')).toBeVisible();
  await expect(page.getByTestId('nouveautes-derniere')).toContainText(`Version ${pkg.version}`);
  await expect(page.getByTestId('nouveaute').first()).toBeVisible();

  await page.goto('/about');
  await page.getByTestId('about-nouveautes').getByRole('link').click();
  await expect(page).toHaveURL(/\/nouveautes$/);
});

test("aucune ligne de la page ne contient de lien de commit ni de balisage", async ({ page }) => {
  await page.goto('/nouveautes');

  const lignes = await page.getByTestId('nouveaute').allInnerTexts();

  expect(lignes.length).toBeGreaterThan(0);
  for (const ligne of lignes) {
    expect(ligne).not.toMatch(/https?:\/\/|\]\(|\*\*/);
  }
});
