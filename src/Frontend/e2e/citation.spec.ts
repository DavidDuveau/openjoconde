import { expect, test } from './fixtures';

test("la fiche d'une œuvre se cite en trois formats, et la citation copiée est celle affichée", async ({ page, context }) => {
  await context.grantPermissions(['clipboard-read', 'clipboard-write']);
  await page.goto('/artworks');
  await page.getByTestId('artwork-card').first().click();
  await expect(page.getByTestId('artwork-reference')).toHaveText(/^E2E\d{5}$/);

  const citation = page.getByTestId('citation-text');
  await expect(citation).toContainText('Base Joconde, ministère de la Culture');
  await expect(citation).toContainText(/Prénom|AUTEUR/);
  await expect(citation).toContainText(/consulté le \d{1,2}(er)? \p{L}+ \d{4}/u);
  await expect(citation).toContainText('Notice E2E');

  await page.getByTestId('citation-format').nth(1).check();
  await expect(citation).toContainText('(consulté le');
  await expect(citation).not.toContainText('Disponible à l\'adresse');

  await page.getByTestId('citation-copy').click();

  await expect(page.getByTestId('citation-status')).toHaveText('Citation copiée.');
  const copie = await page.evaluate(() => navigator.clipboard.readText());
  expect(copie).toBe((await citation.innerText()).trim());
});
