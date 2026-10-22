import { expect, test } from './fixtures';
import { NOTICES } from './jeu';

// Une notice sur trois annonce une image (jeu.ts) : la fiche renvoie alors vers sa notice POP.

const POP = 'https://www.pop.culture.gouv.fr/notice/joconde/';

async function ouvrirFiche(page: import('@playwright/test').Page, titre: string) {
  await page.goto(`/search?searchText=${encodeURIComponent(titre)}`);
  await page.getByTestId('artwork-card').first().click();
  await expect(page).toHaveURL(/\/artworks\//);
}

test('une notice qui annonce une image renvoie vers sa notice POP', async ({ page }) => {
  const notice = NOTICES.find(n => n.image === 'oui' && n.titre)!;

  await ouvrirFiche(page, notice.titre);

  const lien = page.getByTestId('pop-link');
  await expect(lien).toHaveText("Voir l'image sur POP");
  await expect(lien).toHaveAttribute('href', POP + notice.reference);
  await expect(lien).toHaveAttribute('target', '_blank');
  await expect(lien).toHaveAttribute('rel', /noopener/);
});

test('une notice sans image annoncée renvoie vers sa notice POP sans promettre d\'image', async ({ page }) => {
  const notice = NOTICES.find(n => n.image === 'non' && n.titre)!;

  await ouvrirFiche(page, notice.titre);

  await expect(page.getByTestId('pop-link')).toHaveText('Voir la notice sur POP');
  await expect(page.getByTestId('pop-link')).toHaveAttribute('href', POP + notice.reference);
});

test('les cartes repèrent les notices qui annoncent une image', async ({ page }) => {
  const avecImage = NOTICES.filter(n => n.image === 'oui' && n.titre);
  await page.goto('/search?searchText=' + encodeURIComponent(avecImage[0].titre));

  await expect(page.getByTestId('pop-badge')).toHaveCount(1);

  const sansImage = NOTICES.find(n => n.image === 'non' && n.titre)!;
  await page.goto('/search?searchText=' + encodeURIComponent(sansImage.titre));

  await expect(page.getByTestId('artwork-card')).toHaveCount(1);
  await expect(page.getByTestId('pop-badge')).toHaveCount(0);
});
