import { expect, test } from './fixtures';
import { NOTICES } from './jeu';
import type { Page } from '@playwright/test';

// Dans le jeu de notices (global-setup.ts), une notice sur quatre a un artiste sous droits (domaine public en 2080)
// et une notice sur deux un lien vers le site du musée.

async function ouvrirFiche(page: Page, titre: string) {
  await page.goto(`/search?searchText=${encodeURIComponent(titre)}`);
  // Un titre est le début d'un autre (« … 1 », « … 12 ») : on choisit la carte dont le titre est exactement celui-ci
  await page.getByTestId('artwork-card')
    .filter({ has: page.locator(`[data-testid="artwork-title"][title="${titre}"]`) })
    .click();
  await expect(page).toHaveURL(/\/artworks\//);
}

test('une œuvre dont l\'artiste est sous droits dit quand elle entrera dans le domaine public', async ({ page }) => {
  const notice = NOTICES.find(n => n.numero % 4 === 0 && n.titre)!;

  await ouvrirFiche(page, notice.titre);

  await expect(page.getByTestId('artwork-rights')).toHaveText(
    "Artiste sous droits d'auteur ; l'œuvre entrera dans le domaine public en 2080."
  );
});

test('une œuvre dont la notice ne parle pas de droits n\'en affiche aucun', async ({ page }) => {
  const notice = NOTICES.find(n => n.numero % 4 !== 0 && n.titre)!;

  await ouvrirFiche(page, notice.titre);

  await expect(page.getByTestId('artwork-reference')).toHaveText(notice.reference);
  await expect(page.getByTestId('artwork-rights')).toHaveCount(0);
});

test('le lien vers le site du musée s\'ouvre dans un nouvel onglet, sans transmettre la page', async ({ page }) => {
  const notice = NOTICES.find(n => n.numero % 2 === 0 && n.titre)!;

  await ouvrirFiche(page, notice.titre);

  const lien = page.getByTestId('museum-site-link');
  await expect(lien).toHaveAttribute('href', 'https://example.org/musee');
  await expect(lien).toHaveAttribute('target', '_blank');
  await expect(lien).toHaveAttribute('rel', /noopener/);
});

test('une œuvre sans lien de site n\'en propose pas', async ({ page }) => {
  const notice = NOTICES.find(n => n.numero % 2 === 1 && n.titre)!;

  await ouvrirFiche(page, notice.titre);

  await expect(page.getByTestId('artwork-reference')).toHaveText(notice.reference);
  await expect(page.getByTestId('museum-site-link')).toHaveCount(0);
});
