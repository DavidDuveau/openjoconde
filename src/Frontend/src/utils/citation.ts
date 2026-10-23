import { displayTitle } from '@/utils/title';
import { popNoticeUrl } from '@/utils/pop';
import { ArtworkDetail } from '@/types/artwork';

export type FormatCitation = 'iso690' | 'apa' | 'mla';

export const FORMATS: { id: FormatCitation; label: string }[] = [
  { id: 'iso690', label: 'ISO 690' },
  { id: 'apa', label: 'APA' },
  { id: 'mla', label: 'MLA' }
];

const MOIS = ['janvier', 'février', 'mars', 'avril', 'mai', 'juin', 'juillet', 'août', 'septembre', 'octobre', 'novembre', 'décembre'];
const SOURCE = 'Base Joconde, ministère de la Culture';
const MAX_AUTEURS = 3;

/** « 23 octobre 2026 », « 1er mars 2026 » */
export const dateEnLettres = (date: Date): string =>
  `${date.getDate() === 1 ? '1er' : date.getDate()} ${MOIS[date.getMonth()]} ${date.getFullYear()}`;

/** Initiales d'un prénom : « Alexandre Théodore » -> « A. T. », « Jean-François » -> « J.-F. » */
export function initiales(prenom: string): string {
  return prenom.trim().split(/\s+/).filter(Boolean)
    .map(mot => mot.split('-').filter(Boolean).map(partie => `${[...partie][0].toUpperCase()}.`).join('-'))
    .join(' ');
}

interface Auteur {
  nom: string;
  prenom: string;
}

const auteursDe = (oeuvre: ArtworkDetail): Auteur[] =>
  oeuvre.artists
    .map(a => ({ nom: (a.lastName ?? '').trim(), prenom: (a.firstName ?? '').trim() }))
    .filter(a => a.nom !== '');

const termine = (texte: string) => (/[.!?…]$/.test(texte) ? texte : `${texte}.`);

/** Les parties présentes, séparées par « ; » ou « , » selon le format, sans trou quand une partie manque */
const joindre = (parties: (string | null | undefined | false)[], separateur: string) =>
  parties.filter((p): p is string => !!p && p.trim() !== '').join(separateur);

function lieu(oeuvre: ArtworkDetail): string {
  return joindre([oeuvre.museum?.name, oeuvre.inventoryNumber ? `n° d'inventaire ${oeuvre.inventoryNumber}` : null], ', ');
}

function iso690(oeuvre: ArtworkDetail, consulte: string, notice: string): string {
  const auteurs = auteursDe(oeuvre);
  const noms = auteurs.slice(0, MAX_AUTEURS).map(a => joindre([a.nom.toUpperCase(), a.prenom], ', '));
  const auteur = noms.length
    ? `${noms.join(' ; ')}${auteurs.length > MAX_AUTEURS ? ' et al' : ''}. `
    : '';
  const titre = displayTitle(oeuvre.title, Infinity);
  const genre = oeuvre.denomination && oeuvre.denomination !== 'Œuvre sans dénomination' ? ` [${oeuvre.denomination}]` : '';
  const date = oeuvre.creationDate ? ` ${termine(oeuvre.creationDate)}` : '';
  const conservation = lieu(oeuvre) ? ` ${termine(lieu(oeuvre))}` : '';
  const source = `In : ${SOURCE} [en ligne]. Notice ${oeuvre.reference}.`;
  const adresse = notice ? ` Disponible à l'adresse : ${notice} (consulté le ${consulte}).` : ` (consulté le ${consulte}).`;
  return `${auteur}${termine(titre + genre)}${date}${conservation} ${source}${adresse}`.replace(/\s+/g, ' ').trim();
}

/** « Nom, P. », deux auteurs reliés par « et », plus de trois : le premier suivi de « et al. » */
function auteursApa(auteurs: Auteur[]): string {
  const noms = auteurs.map(a => joindre([a.nom, a.prenom ? initiales(a.prenom) : ''], ', '));
  if (noms.length > MAX_AUTEURS) return `${noms[0]}, et al.`;
  if (noms.length <= 1) return noms[0] ?? '';
  return `${noms.slice(0, -1).join(', ')} et ${noms[noms.length - 1]}`;
}

function apa(oeuvre: ArtworkDetail, consulte: string, notice: string): string {
  const auteur = auteursApa(auteursDe(oeuvre));
  const date = oeuvre.creationDate ? `(${oeuvre.creationDate})` : '(s. d.)';
  const titre = displayTitle(oeuvre.title, Infinity);
  const genre = oeuvre.denomination && oeuvre.denomination !== 'Œuvre sans dénomination' ? ` [${oeuvre.denomination}]` : '';
  const conservation = lieu(oeuvre) ? ` ${termine(lieu(oeuvre))}` : '';
  const source = ` ${SOURCE}, notice ${oeuvre.reference}.`;
  const acces = notice ? ` ${notice}` : '';
  // Sans auteur, le titre ouvre la référence, suivi de la date
  const debut = auteur ? `${termine(auteur)} ${date}. ${termine(titre + genre)}` : `${termine(titre + genre)} ${date}.`;
  return `${debut}${conservation}${source}${acces} (consulté le ${consulte})`.replace(/\s+/g, ' ').trim();
}

function mla(oeuvre: ArtworkDetail, consulte: string, notice: string): string {
  const auteurs = auteursDe(oeuvre);
  const premier = auteurs[0];
  const noms = premier ? joindre([premier.nom, premier.prenom], ', ') : '';
  const auteur = noms ? `${termine(noms + (auteurs.length > 1 ? ', et al' : ''))} ` : '';
  const titre = termine(displayTitle(oeuvre.title, Infinity));
  const date = oeuvre.creationDate ? ` ${termine(oeuvre.creationDate)}` : '';
  const conservation = lieu(oeuvre) ? ` ${termine(lieu(oeuvre))}` : '';
  const source = ` ${SOURCE}, notice ${oeuvre.reference}${notice ? `, ${notice}` : ''}.`;
  return `${auteur}${titre}${date}${conservation}${source} Consulté le ${consulte}.`.replace(/\s+/g, ' ').trim();
}

/**
 * Citation d'une œuvre dans le format choisi, avec la source (notice Joconde, adresse POP) et la date de consultation.
 * Une partie absente de la notice est omise, jamais inventée ; plus de trois auteurs : les trois premiers et « et al. ».
 */
export function citer(oeuvre: ArtworkDetail, format: FormatCitation, aujourdhui: Date = new Date()): string {
  const consulte = dateEnLettres(aujourdhui);
  const notice = popNoticeUrl(oeuvre.reference) ?? '';
  switch (format) {
    case 'apa': return apa(oeuvre, consulte, notice);
    case 'mla': return mla(oeuvre, consulte, notice);
    default: return iso690(oeuvre, consulte, notice);
  }
}
