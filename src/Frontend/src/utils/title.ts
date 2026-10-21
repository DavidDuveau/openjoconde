/**
 * Titre d'une œuvre prêt à afficher. Les titres Joconde sont souvent des légendes brutes : « - Ah ! docteur… / N°18.; Tout
 * ce qu'on voudra. ». On retire la ponctuation initiale et on coupe les très longs titres, sans inventer de titre :
 * une œuvre sans titre reste « Sans titre ».
 */
export const UNTITLED = 'Sans titre';

const LEADING_PUNCTUATION = /^[\s\-–—.,;:!?'"«»*/_]+/;

export function displayTitle(title: string | null | undefined, maxLength = 140): string {
  const cleaned = (title ?? '').replace(LEADING_PUNCTUATION, '').replace(/\s+/g, ' ').trim();
  if (!cleaned) {
    return UNTITLED;
  }
  if (cleaned.length <= maxLength) {
    return cleaned;
  }
  const cut = cleaned.slice(0, maxLength);
  const lastSpace = cut.lastIndexOf(' ');
  return `${(lastSpace > maxLength / 2 ? cut.slice(0, lastSpace) : cut).trimEnd()}…`;
}
