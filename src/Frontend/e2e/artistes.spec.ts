import { expect, test } from './fixtures';
import { NOTICES, titresTries } from './jeu';

// Cinq artistes (AUTEUR0 à AUTEUR4), chacun avec les notices de son numéro modulo 5 (jeu.ts).

test('la liste des artistes dit combien d\'œuvres chacun a', async ({ page }) => {
  const attendues = NOTICES.filter(n => n.auteur === 'AUTEUR1').length;

  await page.goto('/artists');

  const carte = page.getByTestId('artist-card').filter({ hasText: 'AUTEUR1' });
  await expect(carte.getByTestId('artist-count')).toContainText(`${attendues} œuvre`);
});

test('la fiche d\'un artiste liste ses œuvres, et seulement les siennes', async ({ page }) => {
  const siennes = NOTICES.filter(n => n.auteur === 'AUTEUR1');
  await page.goto('/artists');

  await page.getByTestId('artist-card').filter({ hasText: 'AUTEUR1' }).click();

  await expect(page).toHaveURL(/\/artists\//);
  await expect(page.getByTestId('artist-heading')).toContainText('AUTEUR1');
  await expect(page.getByTestId('no-artworks')).toHaveCount(0);
  await expect(async () => {
    const titres = await page.getByTestId('artwork-title').evaluateAll(els => els.map(el => el.getAttribute('title') ?? ''));
    expect(titres).toEqual(titresTries('asc', siennes));
  }).toPass();
});

test('cliquer une œuvre de la fiche d\'un artiste ouvre sa fiche', async ({ page }) => {
  await page.goto('/artists');
  await page.getByTestId('artist-card').filter({ hasText: 'AUTEUR2' }).click();
  await expect(page.getByTestId('artwork-card').first()).toBeVisible();

  await page.getByTestId('artwork-card').first().click();

  await expect(page).toHaveURL(/\/artworks\//);
  await expect(page.getByTestId('artwork-reference')).toHaveText(/^E2E\d{5}$/);
});
