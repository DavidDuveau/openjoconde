import { Page } from '@playwright/test';
import { expect, test } from './fixtures';
import { NOMBRE, NOTICES, parMotDuTitre, titresTries } from './jeu';

// Les valeurs attendues viennent du jeu de notices (jeu.ts), jamais de l'API que ces scénarios vérifient.

const PAGE = 12;

/** Titres bruts (attribut `title`) des cartes affichées, dans l'ordre ; vide pour une œuvre sans titre */
async function titresAffiches(page: Page): Promise<string[]> {
  return page.getByTestId('artwork-title').evaluateAll(titres => titres.map(titre => titre.getAttribute('title') ?? ''));
}

async function attendreTitres(page: Page, attendus: string[]) {
  await expect(async () => expect(await titresAffiches(page)).toEqual(attendus)).toPass();
}

async function choisirTechnique(page: Page, nom: string) {
  await page.getByPlaceholder('Rechercher un matériau ou une technique').fill(nom);
  await page.getByTestId('filter-option').filter({ hasText: new RegExp(`^${nom}$`) }).click();
}

test('la recherche par mot du titre rend exactement les œuvres correspondantes', async ({ page }) => {
  const attendues = parMotDuTitre('portrait');
  expect(attendues.length).toBeGreaterThan(0);
  expect(attendues.length).toBeLessThan(NOMBRE);

  await page.goto('/search?searchText=portrait');

  await expect(page.getByTestId('results-total')).toHaveText(String(attendues.length));
  await attendreTitres(page, attendues.map(n => n.titre).sort((a, b) => a.localeCompare(b, 'fr')));
});

test('la recherche ignore les accents et la casse', async ({ page }) => {
  const attendues = parMotDuTitre('ETUDE');
  expect(attendues.length).toBeGreaterThan(0);

  await page.goto('/search?searchText=ETUDE');

  await expect(page.getByTestId('results-total')).toHaveText(String(attendues.length));
});

test('une recherche sans résultat le dit', async ({ page }) => {
  await page.goto('/search?searchText=zzzinexistant');

  await expect(page.getByTestId('no-results')).toContainText('Aucune œuvre ne correspond');
  await expect(page.getByTestId('artwork-card')).toHaveCount(0);
});

test('un filtre de technique restreint les résultats, et le retirer les rend tous', async ({ page }) => {
  const attendues = NOTICES.filter(n => n.technique === 'technique 1').length;

  await page.goto('/search');
  await expect(page.getByTestId('results-total')).toHaveText(String(NOMBRE));

  await choisirTechnique(page, 'technique 1');

  await expect(page).toHaveURL(/techniqueId=/);
  await expect(page.getByTestId('results-total')).toHaveText(String(attendues));

  await page.getByRole('button', { name: /Retirer le filtre/ }).click();

  await expect(page).not.toHaveURL(/techniqueId/);
  await expect(page.getByTestId('results-total')).toHaveText(String(NOMBRE));
});

test('au rechargement, le filtre reste affiché et reste appliqué', async ({ page }) => {
  const attendues = NOTICES.filter(n => n.technique === 'technique 2').length;
  await page.goto('/search');
  await choisirTechnique(page, 'technique 2');
  await expect(page.getByTestId('results-total')).toHaveText(String(attendues));

  await page.reload();

  await expect(page.getByTestId('filter-label')).toHaveText('technique 2');
  await expect(page.getByTestId('results-total')).toHaveText(String(attendues));
  await expect(page.getByTestId('artwork-card')).toHaveCount(attendues);
});

test('le tri A-Z puis Z-A suit l\'ordre alphabétique français, accents compris', async ({ page }) => {
  await page.goto('/search');
  await attendreTitres(page, titresTries('asc').slice(0, PAGE));

  await page.getByTestId('sort').selectOption('title_desc');

  await expect(page).toHaveURL(/sortBy=title_desc/);
  await attendreTitres(page, titresTries('desc').slice(0, PAGE));
});

test('la deuxième page rend la suite de la liste', async ({ page }) => {
  await page.goto('/search');
  await expect(page.getByTestId('pagination')).toBeVisible();

  await page.getByTestId('pagination').getByRole('button', { name: '2', exact: true }).click();

  await expect(page).toHaveURL(/page=2/);
  await attendreTitres(page, titresTries('asc').slice(PAGE, 2 * PAGE));
});

test('la fiche ouverte est celle de l\'œuvre cliquée', async ({ page }) => {
  await page.goto('/search');
  const titre = (await titresAffiches(page))[1];
  const notice = NOTICES.find(n => n.titre === titre)!;
  expect(notice, `notice du titre « ${titre} »`).toBeTruthy();

  await page.getByTestId('artwork-card').nth(1).click();

  await expect(page).toHaveURL(/\/artworks\//);
  await expect(page.getByTestId('artwork-heading')).toHaveText(titre);
  await expect(page.getByTestId('artwork-reference')).toHaveText(notice.reference);
});
