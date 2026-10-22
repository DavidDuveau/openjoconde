// Notice d'une œuvre sur la plateforme POP (ministère de la Culture), où se voit l'image que Joconde annonce sans
// l'adresser. Le modèle d'adresse se règle par VUE_APP_POP_URL si POP la change.
const NOTICE_URL = import.meta.env.VUE_APP_POP_URL || 'https://www.pop.culture.gouv.fr/notice/joconde/';

/** Adresse de la notice POP d'une référence Joconde ; null si la référence n'est pas une référence simple */
export function popNoticeUrl(reference: string | null | undefined): string | null {
  if (!reference || !/^[A-Za-z0-9_-]+$/.test(reference)) return null;
  return NOTICE_URL + encodeURIComponent(reference);
}
