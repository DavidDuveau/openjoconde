import { expect, test } from './fixtures';

// Le jeu (jeu.ts) a des notices « Portrait de femme » mais aucun domaine « peinture », « dessin »… : les étapes des
// portraits par domaine sont donc vides ; ce sont les scénarios unitaires qui vérifient les étapes pleines.

test("l'accueil met en avant des thématiques, et mène à la page de toutes les thématiques", async ({ page }) => {
  await page.goto('/');
  await expect(page.getByTestId('home-thematiques').getByTestId('thematique-card').first()).toBeVisible();

  await page.getByTestId('home-thematiques-toutes').click();

  await expect(page).toHaveURL(/\/thematiques$/);
  await expect(page.getByTestId('thematique-card')).toHaveCount(6);
  await expect(page.getByTestId('thematique-saison')).toContainText('Du 1er décembre au 6 janvier');
});

test("le menu mène aux thématiques, et une vignette ouvre le parcours : thème, importance, critères et étapes", async ({ page }) => {
  await page.goto('/');
  await page.getByRole('link', { name: 'Thématiques', exact: true }).first().click();

  await page.getByTestId('thematique-card').filter({ hasText: 'Portraits' }).click();

  await expect(page.getByTestId('thematique-heading')).toHaveText('Portraits');
  await expect(page.getByTestId('thematique-explication')).not.toBeEmpty();
  await expect(page.getByTestId('thematique-importance')).not.toBeEmpty();
  await expect(page.getByTestId('thematique-criteres')).toContainText('portrait');
  await expect(page.getByTestId('thematique-section')).toHaveCount(5);
  await expect(page.getByTestId('section-chargement')).toHaveCount(0);
});

test('une adresse de thématique inconnue le dit', async ({ page }) => {
  await page.goto('/thematiques/inconnue');

  await expect(page.getByTestId('thematique-introuvable')).toBeVisible();
});

test("une étape sans notice correspondante le dit au lieu d'élargir la recherche", async ({ page }) => {
  await page.goto('/thematiques/art-religieux');
  await expect(page.getByTestId('section-chargement')).toHaveCount(0);

  // Aucune notice du jeu ne parle de la Vierge : l'étape le dit plutôt que d'élargir la recherche
  await expect(page.getByTestId('section-vide')).toHaveCount(4);
  await expect(page.getByTestId('section-voir-tout')).toHaveCount(0);
});
