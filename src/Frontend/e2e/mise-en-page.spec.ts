import { expect, test } from './fixtures';

test("la barre de navigation occupe toute la largeur sur une seule ligne, avec une recherche utilisable", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto('/');

  const entete = await page.getByTestId('app-header').boundingBox();
  const liens = page.locator('.main-nav li');
  const hauteurs = await liens.evaluateAll(els => els.map(e => Math.round(e.getBoundingClientRect().top)));
  const recherche = await page.locator('.app-header .search-input').boundingBox();

  expect(entete!.width).toBe(1440);
  expect(new Set(hauteurs).size, 'tous les liens sur la même ligne').toBe(1);
  expect(recherche!.width).toBeGreaterThan(250);
});

test("à 1024 px, la barre de navigation tient sans faire défiler la page", async ({ page }) => {
  await page.setViewportSize({ width: 1024, height: 768 });
  await page.goto('/');

  const defilement = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);

  expect(defilement).toBeLessThanOrEqual(0);
});

test("les cartes de thématiques de l'accueil sont centrées", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto('/');

  const cartes = page.locator('[data-testid="home-thematiques"] [data-testid="thematique-card"]');
  await expect(cartes.first()).toBeVisible();
  const zone = await page.getByTestId('home-thematiques').boundingBox();
  const premiere = await cartes.first().boundingBox();
  const derniere = await cartes.last().boundingBox();

  const margeGauche = premiere!.x - zone!.x;
  const margeDroite = zone!.x + zone!.width - (derniere!.x + derniere!.width);
  expect(Math.abs(margeGauche - margeDroite)).toBeLessThan(2);
});
