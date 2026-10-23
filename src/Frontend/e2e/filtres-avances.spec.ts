import { expect, test } from './fixtures';
import { NOMBRE } from './jeu';

// Les périodes du jeu (jeu.ts) sont des textes sans année (« période 1 ») : aucune œuvre n'a de date lisible, donc toute
// plage d'années est vide. Les plages pleines sont vérifiées côté base (FiltreParDateTests) et en test unitaire.

test("les filtres posés s'affichent en puces, se retirent un à un, puis tous ensemble", async ({ page }) => {
  await page.goto('/search?region=Occitanie&searchText=portrait');
  await expect(page.getByTestId('filter-chip')).toHaveCount(2);
  await expect(page.locator('.active-filters-title')).toHaveText('2 filtres actifs :');

  await page.getByTestId('filter-chip').filter({ hasText: 'Région : Occitanie' }).click();

  await expect(page.getByTestId('filter-chip')).toHaveCount(1);
  await expect(page).not.toHaveURL(/region=/);
  await expect(page).toHaveURL(/searchText=portrait/);

  await page.getByTestId('clear-all-filters').click();

  await expect(page.getByTestId('active-filters')).toHaveCount(0);
  await expect(page).not.toHaveURL(/searchText=/);
  await expect(page.getByTestId('results-total')).toHaveText(String(NOMBRE));
});

test("une plage d'années se pose, se retrouve dans l'adresse et se retire par sa puce", async ({ page }) => {
  await page.goto('/search');
  await expect(page.getByTestId('results-total')).toHaveText(String(NOMBRE));

  await page.getByTestId('year-from').fill('1850');
  await page.getByTestId('year-to').fill('1920');
  await page.getByTestId('year-to').press('Enter');

  await expect(page).toHaveURL(/yearFrom=1850/);
  await expect(page).toHaveURL(/yearTo=1920/);
  await expect(page.getByTestId('filter-chip')).toHaveText(/Créée entre 1850 et 1920/);
  await expect(page.getByTestId('no-results')).toBeVisible();

  await page.getByTestId('filter-chip').click();

  await expect(page.getByTestId('results-total')).toHaveText(String(NOMBRE));
  await expect(page.getByTestId('year-from')).toHaveValue('');
});

test("une plage d'années à l'envers est expliquée et n'est pas cherchée", async ({ page }) => {
  await page.goto('/search?yearFrom=1920');
  await expect(page.getByTestId('filter-chip')).toHaveCount(1);

  await page.getByTestId('year-to').fill('1850');
  await page.getByTestId('year-to').press('Enter');

  await expect(page.getByTestId('year-error')).toContainText("L'année de début doit précéder");
  await expect(page).not.toHaveURL(/yearTo=/);
});

test("sur mobile, les filtres se déplient à la demande et les filtres actifs restent visibles", async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 800 });
  await page.goto('/search?region=Occitanie');

  await expect(page.getByTestId('region-select')).toBeHidden();
  await expect(page.getByTestId('filter-chip')).toBeVisible();

  await page.getByTestId('filters-toggle').click();

  await expect(page.getByTestId('region-select')).toBeVisible();
  await expect(page.getByTestId('filters-toggle')).toHaveAttribute('aria-expanded', 'true');
});
