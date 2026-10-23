import { ArtworkDetail } from '@/types/artwork';
import { popNoticeUrl } from '@/utils/pop';
import { LICENCE_URL } from '@/utils/sources';
import { dateEnLettres } from '@/utils/citation';

const SOURCE = 'Base Joconde, ministère de la Culture';
const LICENCE = 'Licence Ouverte 2.0';

const nomComplet = (a: ArtworkDetail['artists'][number]) => [a.firstName, a.lastName].filter(Boolean).join(' ');
const noms = (liste: { name: string }[]) => liste.map(e => e.name);

/** Les données d'une œuvre telles que la fiche les montre, avec la source et la licence : de quoi les réutiliser en citant l'origine */
export function donneesOeuvre(oeuvre: ArtworkDetail, aujourdhui: Date = new Date()) {
  return {
    reference: oeuvre.reference,
    titre: oeuvre.title,
    denomination: oeuvre.denomination,
    artistes: oeuvre.artists.map(a => ({ nom: nomComplet(a), role: a.role })),
    dateDeCreation: oeuvre.creationDate,
    lieuDeCreation: oeuvre.creationPlace,
    dimensions: oeuvre.dimensions,
    numeroInventaire: oeuvre.inventoryNumber,
    musee: oeuvre.museum?.name ?? null,
    lieuDeConservation: oeuvre.conservationPlace,
    domaines: noms(oeuvre.domains),
    techniques: noms(oeuvre.techniques),
    periodes: noms(oeuvre.periods),
    description: oeuvre.description,
    droits: {
      mention: oeuvre.copyright,
      artisteSousDroits: oeuvre.artistUnderCopyright,
      anneeDomainePublic: oeuvre.publicDomainYear
    },
    source: {
      base: SOURCE,
      notice: popNoticeUrl(oeuvre.reference),
      licence: LICENCE,
      adresseLicence: LICENCE_URL,
      consulteLe: dateEnLettres(aujourdhui)
    }
  };
}

/** L'œuvre en JSON, indenté */
export const versJson = (oeuvre: ArtworkDetail, aujourdhui?: Date): string =>
  JSON.stringify(donneesOeuvre(oeuvre, aujourdhui), null, 2) + '\n';

/**
 * Une cellule CSV : entre guillemets, guillemets doublés. Une valeur qui commence par =, +, -, @ ou une tabulation serait
 * lue comme une formule par un tableur (injection de formule) : elle est précédée d'une apostrophe, qui la rend du texte.
 */
export function celluleCsv(valeur: string | number | boolean | null | undefined): string {
  let texte = valeur === null || valeur === undefined ? '' : String(valeur);
  if (/^[=+\-@\t\r]/.test(texte)) texte = `'${texte}`;
  return `"${texte.replace(/"/g, '""')}"`;
}

const SEPARATEUR = ';';
const BOM = '﻿';

/**
 * L'œuvre en CSV : une ligne d'en-tête, une ligne de données, séparateur « ; » (le tableur français le lit sans réglage),
 * UTF-8 avec marque d'ordre des octets pour que les accents s'ouvrent bien dans Excel. Les listes sont jointes par « | ».
 */
export function versCsv(oeuvre: ArtworkDetail, aujourdhui?: Date): string {
  const d = donneesOeuvre(oeuvre, aujourdhui);
  const colonnes: [string, string | number | boolean | null | undefined][] = [
    ['reference', d.reference],
    ['titre', d.titre],
    ['denomination', d.denomination],
    ['artistes', d.artistes.map(a => a.nom).join(' | ')],
    ['date_de_creation', d.dateDeCreation],
    ['lieu_de_creation', d.lieuDeCreation],
    ['dimensions', d.dimensions],
    ['numero_inventaire', d.numeroInventaire],
    ['musee', d.musee],
    ['lieu_de_conservation', d.lieuDeConservation],
    ['domaines', d.domaines.join(' | ')],
    ['techniques', d.techniques.join(' | ')],
    ['periodes', d.periodes.join(' | ')],
    ['description', d.description],
    ['droits_mention', d.droits.mention],
    ['artiste_sous_droits', d.droits.artisteSousDroits],
    ['annee_domaine_public', d.droits.anneeDomainePublic],
    ['source', d.source.base],
    ['notice', d.source.notice],
    ['licence', d.source.licence],
    ['consulte_le', d.source.consulteLe]
  ];
  return `${BOM}${colonnes.map(([nom]) => celluleCsv(nom)).join(SEPARATEUR)}\r\n${colonnes.map(([, v]) => celluleCsv(v)).join(SEPARATEUR)}\r\n`;
}

/** Nom de fichier sûr : la référence sans caractère qui ferait sortir du dossier de téléchargement */
export function nomDeFichier(oeuvre: ArtworkDetail, extension: 'json' | 'csv'): string {
  const base = (oeuvre.reference || 'oeuvre').replace(/[^A-Za-z0-9_-]+/g, '_').replace(/^_+|_+$/g, '') || 'oeuvre';
  return `joconde-${base}.${extension}`;
}

/** Déclenche le téléchargement d'un contenu texte par le navigateur */
export function telecharger(nom: string, contenu: string, type: string): void {
  const url = URL.createObjectURL(new Blob([contenu], { type: `${type};charset=utf-8` }));
  const lien = document.createElement('a');
  lien.href = url;
  lien.download = nom;
  document.body.appendChild(lien);
  lien.click();
  lien.remove();
  URL.revokeObjectURL(url);
}
