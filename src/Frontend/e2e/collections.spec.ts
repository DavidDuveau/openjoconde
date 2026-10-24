import { expect, test } from './fixtures';
import type { Page } from '@playwright/test';

/** Ouvre la première œuvre de la liste et la range dans une nouvelle collection ; rend le titre de l'œuvre */
async function rangerDansUneCollection(page: Page, nom: string): Promise<string> {
  await page.goto('/artworks');
  await page.getByTestId('artwork-card').first().click();
  await expect(page.getByTestId('artwork-reference')).toHaveText(/^E2E\d{5}$/);
  const titre = await page.getByTestId('artwork-heading').innerText();

  await page.getByTestId('add-to-collection-toggle').click();
  await page.getByTestId('add-to-collection-name').fill(nom);
  await page.getByTestId('add-to-collection-submit').click();
  await expect(page.getByTestId('add-to-collection-status')).toContainText(`Collection « ${nom} » créée`);
  return titre;
}

test("une collection créée depuis une fiche se retrouve dans « Mes collections », même après rechargement", async ({ page }) => {
  const titre = await rangerDansUneCollection(page, 'Mes paysages');

  await page.getByRole('link', { name: 'Mes collections' }).click();
  await expect(page.getByTestId('collection-item')).toContainText('Mes paysages');
  await expect(page.getByTestId('collection-item')).toContainText('1 œuvre');

  await page.getByRole('link', { name: 'Mes paysages' }).click();
  await expect(page.getByTestId('artwork-title')).toHaveText(titre);

  await page.reload();
  await expect(page.getByTestId('collection-titre')).toHaveText('Mes paysages');
  await expect(page.getByTestId('artwork-title')).toHaveText(titre);
});

test("une collection se renomme, et se supprime après confirmation", async ({ page }) => {
  await rangerDansUneCollection(page, 'Brouillon');
  await page.goto('/collections');
  await page.getByRole('link', { name: 'Brouillon' }).click();

  await page.getByTestId('collection-modifier').click();
  await page.getByTestId('collection-edition-nom').fill('Définitif');
  await page.getByTestId('collection-enregistrer').click();
  await expect(page.getByTestId('collection-titre')).toHaveText('Définitif');

  await page.getByTestId('collection-supprimer').click();
  await page.getByTestId('collection-confirmer').click();

  await expect(page).toHaveURL(/\/collections$/);
  await expect(page.getByTestId('collections-vide')).toBeVisible();
});

test("l'export d'une sauvegarde se réimporte : la collection supprimée revient", async ({ page }, info) => {
  await rangerDansUneCollection(page, 'À sauvegarder');
  await page.goto('/collections');

  const [telechargement] = await Promise.all([page.waitForEvent('download'), page.getByTestId('sauvegarde-export').click()]);
  expect(telechargement.suggestedFilename()).toMatch(/^openjoconde-sauvegarde-\d{4}-\d{2}-\d{2}\.json$/);
  const chemin = info.outputPath('sauvegarde.json');
  await telechargement.saveAs(chemin);

  await page.getByRole('link', { name: 'À sauvegarder' }).click();
  await page.getByTestId('collection-supprimer').click();
  await page.getByTestId('collection-confirmer').click();
  await expect(page.getByTestId('collections-vide')).toBeVisible();

  await page.getByTestId('sauvegarde-fichier').setInputFiles(chemin);
  await page.getByTestId('sauvegarde-importer').click();

  await expect(page.getByTestId('sauvegarde-compte-rendu')).toContainText('1 collection ajoutée');
  await expect(page.getByTestId('collection-item')).toContainText('À sauvegarder');
});

test("un fichier invalide est refusé avec un message, sans rien modifier", async ({ page }, info) => {
  await rangerDansUneCollection(page, 'Intacte');
  await page.goto('/collections');
  const chemin = info.outputPath('invalide.json');
  await import('node:fs').then(fs => fs.writeFileSync(chemin, 'ceci nest pas du json'));

  await page.getByTestId('sauvegarde-fichier').setInputFiles(chemin);
  await page.getByTestId('sauvegarde-importer').click();

  await expect(page.getByTestId('sauvegarde-erreur')).toContainText("Vos données n'ont pas été modifiées");
  await expect(page.getByTestId('collection-item')).toContainText('Intacte');
});

test("le lien de partage s'ouvre en lecture seule chez quelqu'un d'autre, qui peut l'ajouter à ses collections", async ({ page, browser }) => {
  const titre = await rangerDansUneCollection(page, 'À partager');
  await page.goto('/collections');
  await page.getByRole('link', { name: 'À partager' }).click();
  await expect(page.getByTestId('collection-titre')).toHaveText('À partager');
  await page.context().grantPermissions(['clipboard-read', 'clipboard-write']);
  await page.getByTestId('share-copy').click();
  const lien = await page.evaluate(() => navigator.clipboard.readText());
  expect(lien).toMatch(/\/partage#[A-Za-z0-9_-]+$/);

  const autre = await browser.newContext();
  const page2 = await autre.newPage();
  await page2.goto(lien);

  await expect(page2.getByTestId('partage-bandeau')).toContainText('lecture seule');
  await expect(page2.getByTestId('partage-titre')).toHaveText('À partager');
  await expect(page2.getByTestId('artwork-title')).toHaveText(titre);
  await expect(page2.getByTestId('oeuvre-retirer')).toHaveCount(0);

  await page2.getByTestId('partage-ajouter').click();
  await expect(page2.getByTestId('partage-statut')).toContainText('Collection ajoutée');
  await page2.goto('/collections');
  await expect(page2.getByTestId('collection-item')).toContainText('À partager');
  await autre.close();
});

test("un lien de collection malformé donne une erreur claire et n'affiche rien", async ({ page }) => {
  await page.goto('/partage#pas-une-collection-valide');

  await expect(page.getByTestId('partage-erreur')).toContainText('Lien de collection invalide');
  await expect(page.getByTestId('partage-titre')).toHaveCount(0);
});

test("aucune collection n'est envoyée à un serveur", async ({ page }) => {
  const requetes: string[] = [];
  page.on('request', requete => { if (requete.method() !== 'GET') requetes.push(`${requete.method()} ${requete.url()}`); });

  await rangerDansUneCollection(page, 'Privée');
  await page.goto('/collections');
  await expect(page.getByTestId('collection-item')).toBeVisible();

  expect(requetes).toEqual([]);
});
