import { expect, test } from './fixtures';

test("un favori ajouté depuis la fiche se retrouve dans « Mes favoris », même après rechargement", async ({ page }) => {
  await page.goto('/artworks');
  await page.getByTestId('artwork-card').first().click();
  await expect(page.getByTestId('artwork-reference')).toHaveText(/^E2E\d{5}$/);
  const titre = await page.getByTestId('artwork-heading').innerText();

  const bouton = page.locator('.artwork-header').getByTestId('favorite-button');
  await expect(bouton).toHaveAttribute('aria-pressed', 'false');
  await bouton.click();
  await expect(bouton).toHaveAttribute('aria-pressed', 'true');

  await page.getByRole('link', { name: 'Favoris', exact: true }).click();
  await expect(page.getByTestId('favoris-compte')).toHaveText('1 œuvre favorite');
  await expect(page.getByTestId('artwork-title')).toHaveText(titre);

  await page.reload();
  await expect(page.getByTestId('favoris-compte')).toHaveText('1 œuvre favorite');
});

test("l'étoile d'une carte ajoute un favori sans ouvrir la fiche, et « Mes favoris » permet de le retirer", async ({ page }) => {
  await page.goto('/artworks');
  await page.getByTestId('artwork-card').first().getByTestId('favorite-button').click();
  await expect(page).toHaveURL(/\/artworks$/);

  await page.goto('/favoris');
  await expect(page.getByTestId('favoris-compte')).toHaveText('1 œuvre favorite');
  await expect(page.getByTestId('favoris-note')).toContainText('restent sur cet appareil');

  await page.getByTestId('favorite-button').click();

  await expect(page.getByTestId('favoris-vide')).toBeVisible();
});

test("sans stockage, l'ajout d'un favori est expliqué et rien ne se casse", async ({ page }) => {
  await page.addInitScript(() => {
    Storage.prototype.setItem = () => { throw new DOMException('plein', 'QuotaExceededError'); };
  });
  await page.goto('/artworks');

  await page.getByTestId('artwork-card').first().getByTestId('favorite-button').click();

  await expect(page.getByTestId('favorite-message').first()).toContainText('ne peuvent pas être gardés sur cet appareil');
  await expect(page.getByTestId('artwork-card').first()).toBeVisible();
});

test("aucun favori n'est envoyé à un serveur", async ({ page }) => {
  const requetes: string[] = [];
  page.on('request', requete => { if (requete.method() !== 'GET') requetes.push(`${requete.method()} ${requete.url()}`); });
  await page.goto('/artworks');

  await page.getByTestId('artwork-card').first().getByTestId('favorite-button').click();
  await page.goto('/favoris');
  await expect(page.getByTestId('favoris-compte')).toBeVisible();

  expect(requetes).toEqual([]);
});
