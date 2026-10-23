import { expect, test } from './fixtures';
import { NOTICES } from './jeu';
import pkg from '../package.json';

// Les données du ministère de la Culture se réutilisent à condition d'en citer l'origine à chaque rediffusion.

test('le pied de page cite la source et la licence, et mène à la page des sources', async ({ page }) => {
  await page.goto('/');

  const pied = page.getByTestId('footer-source');
  await expect(pied).toContainText('Ministère de la Culture');
  await expect(pied).toContainText('Licence Ouverte 2.0');

  await pied.getByRole('link', { name: 'Sources et licences' }).click();

  await expect(page).toHaveURL(/\/about#sources/);
  await expect(page.getByTestId('about-sources')).toBeInViewport();
  await expect(page.getByTestId('about-sources')).toContainText('Muséofile');
  await expect(page.getByTestId('about-sources')).toContainText('n\'affiche donc aucune image');
});

test('la fiche d\'une œuvre cite sa notice, son lieu de conservation et la licence', async ({ page }) => {
  const notice = NOTICES.find(n => n.titre)!;
  await page.goto('/search?searchText=' + encodeURIComponent(notice.titre));
  await page.getByTestId('artwork-card').first().click();

  const source = page.getByTestId('source-citation');
  await expect(source).toContainText(notice.reference);
  await expect(source).toContainText('base Joconde');
  await expect(source).toContainText(notice.musee);
  await expect(source).toContainText('Licence Ouverte 2.0');
  await expect(source.getByRole('link', { name: 'POP' })).toHaveAttribute('href', 'https://www.pop.culture.gouv.fr/notice/joconde/' + notice.reference);
});

test('la fiche d\'un musée cite Muséofile', async ({ page }) => {
  await page.goto('/museums');
  await page.getByText(NOTICES[0].musee, { exact: false }).first().click();

  await expect(page.getByTestId('source-citation')).toContainText('Muséofile');
});

test('le pied de page annonce la version du produit, la même que celle annoncée par l’API', async ({ page, request }) => {
  await page.goto('/');

  const sante = await request.get('http://localhost:5101/api/health');
  const version = (await sante.json()).version;

  expect(version).toBe(pkg.version);
  await expect(page.getByTestId('app-version')).toHaveText('Version ' + version);
});
