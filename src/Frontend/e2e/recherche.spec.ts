import { expect, test, APIRequestContext } from '@playwright/test';

const API = 'http://localhost:5101/api';

interface Resume { id: string; title: string | null }
interface Fiche { id: string; title: string | null; reference: string; techniques: { id: string; name: string }[]; domains: { id: string; name: string }[] }

async function oeuvres(request: APIRequestContext, query = ''): Promise<{ items: Resume[]; totalItems: number }> {
  const response = await request.get(`${API}/artworks?pageSize=100${query}`);
  expect(response.ok()).toBeTruthy();
  return response.json();
}

async function fiche(request: APIRequestContext, id: string): Promise<Fiche> {
  const response = await request.get(`${API}/artworks/${id}`);
  expect(response.ok()).toBeTruthy();
  return response.json();
}

/** Une œuvre qui porte au moins une technique, pour viser un filtre qui renvoie quelque chose */
async function ficheAvecTechnique(request: APIRequestContext): Promise<Fiche> {
  for (const { id } of (await oeuvres(request)).items) {
    const f = await fiche(request, id);
    if (f.techniques.length > 0) return f;
  }
  throw new Error('Aucune œuvre avec technique dans les données de test');
}

test('la recherche par mot du titre rend les œuvres correspondantes', async ({ page, request }) => {
  const titrees = (await oeuvres(request)).items.filter(o => o.title && /\p{L}{4,}/u.test(o.title));
  const mot = titrees[0].title!.match(/\p{L}{4,}/u)![0];
  const attendu = (await oeuvres(request, `&search=${encodeURIComponent(mot)}`)).totalItems;
  expect(attendu).toBeGreaterThan(0);

  await page.goto(`/search?searchText=${encodeURIComponent(mot)}`);

  await expect(page.locator('.results-count strong')).toHaveText(String(attendu));
  await expect(page.locator('.artwork-card').first()).toBeVisible();
});

test('un filtre de technique restreint les résultats, et le retirer les rend tous', async ({ page, request }) => {
  const f = await ficheAvecTechnique(request);
  const technique = f.techniques[0];
  const tous = (await oeuvres(request)).totalItems;
  const filtrees = (await oeuvres(request, `&techniqueId=${technique.id}`)).totalItems;
  expect(filtrees).toBeGreaterThan(0);

  await page.goto('/search');
  await expect(page.locator('.results-count strong')).toHaveText(String(tous));

  await page.getByPlaceholder('Rechercher une technique').fill(technique.name.slice(0, 6));
  await page.locator('.options li', { hasText: technique.name }).first().click();

  await expect(page).toHaveURL(new RegExp(`techniqueId=${technique.id}`));
  await expect(page.locator('.results-count strong')).toHaveText(String(filtrees));

  await page.getByRole('button', { name: /Retirer le filtre/ }).click();

  await expect(page).not.toHaveURL(/techniqueId/);
  await expect(page.locator('.results-count strong')).toHaveText(String(tous));
});

test('le filtre reste affiché au rechargement de la page', async ({ page, request }) => {
  const f = await ficheAvecTechnique(request);
  const technique = f.techniques[0];

  await page.goto(`/search?techniqueId=${technique.id}&techniqueName=${encodeURIComponent(technique.name)}`);

  await expect(page.locator('.selected-label')).toHaveText(technique.name);
});

test('le tri Z-A inverse l\'ordre des titres', async ({ page, request }) => {
  const attendu = (await oeuvres(request, '&sortBy=title_desc')).items.find(o => o.title)!.title!;

  await page.goto('/search');
  await page.locator('.results-sort select').selectOption('title_desc');

  await expect(page).toHaveURL(/sortBy=title_desc/);
  await expect(page.locator('.artwork-card h3').first()).toHaveAttribute('title', attendu);
});

test('la fiche d\'une œuvre s\'ouvre depuis la liste', async ({ page, request }) => {
  const f = await fiche(request, (await oeuvres(request)).items[0].id);

  await page.goto(`/artworks/${f.id}`);

  await expect(page.getByRole('heading', { level: 3, name: 'Référence' })).toBeVisible();
  await expect(page.getByText(f.reference).first()).toBeVisible();
});
