/**
 * Phrase sur les droits d'auteur d'une œuvre, d'après la notice Joconde : l'artiste est-il encore sous droits, et
 * l'année où l'œuvre entre dans le domaine public. Null quand la notice n'en dit rien.
 */
export function rightsSummary(
  underCopyright: boolean | null | undefined,
  publicDomainYear: number | null | undefined,
  currentYear: number = new Date().getFullYear()
): string | null {
  if (publicDomainYear != null) {
    return publicDomainYear <= currentYear
      ? `Dans le domaine public depuis ${publicDomainYear}.`
      : `Artiste sous droits d'auteur ; l'œuvre entrera dans le domaine public en ${publicDomainYear}.`;
  }
  if (underCopyright === true) {
    return "Artiste sous droits d'auteur.";
  }
  if (underCopyright === false) {
    return "Artiste non sous droits d'auteur, selon la notice.";
  }
  return null;
}

/** Une adresse affichable en lien : http ou https seulement, jamais javascript: ni data: */
export function safeWebUrl(url: string | null | undefined): string | null {
  return url && /^https?:\/\//i.test(url) ? url : null;
}
