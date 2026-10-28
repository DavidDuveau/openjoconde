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

for (const largeur of [1024, 800]) {
  test(`à ${largeur} px, la navigation n'est ni coupée ni cause de défilement de la page`, async ({ page }) => {
    await page.setViewportSize({ width: largeur, height: 768 });
    await page.goto('/');

    const mesures = await page.evaluate(() => {
      const nav = document.querySelector('.main-nav')!;
      const dernier = nav.querySelector('li:last-child')!.getBoundingClientRect();
      const recherche = document.querySelector('.app-header .search-section')!.getBoundingClientRect();
      return {
        defilementPage: document.documentElement.scrollWidth - window.innerWidth,
        navCoupee: nav.scrollWidth - nav.clientWidth,
        dernierSousRecherche: dernier.right > recherche.left && dernier.top < recherche.bottom
      };
    });

    expect(mesures.defilementPage).toBeLessThanOrEqual(0);
    expect(mesures.navCoupee).toBeLessThanOrEqual(0);
    expect(mesures.dernierSousRecherche).toBe(false);
  });
}

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
