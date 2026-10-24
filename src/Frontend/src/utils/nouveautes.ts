export interface Nouveaute {
  /** Thème du commit (`export`, `compte`…), quand il en a un */
  portee: string;
  texte: string;
  /** Référence de la story (US-COMPTE-03), quand le texte la cite */
  reference: string;
}

export interface SectionNouveautes {
  titre: string;
  elements: Nouveaute[];
}

export interface VersionNouveautes {
  version: string;
  /** Date ISO (AAAA-MM-JJ), vide si le changelog n'en donne pas */
  date: string;
  sections: SectionNouveautes[];
}

/** Titres anglais de release-please (changelogs d'avant TECH-53), présentés comme les titres français */
const TITRES: Record<string, string> = {
  Features: 'Nouveautés',
  'Bug Fixes': 'Corrections',
  'Performance Improvements': 'Performances',
  Reverts: 'Retraits',
  'Documentation': 'Documentation'
};

const ENTETE_VERSION = /^##\s+\[?(\d+\.\d+\.\d+[^\]\s)]*)\]?(?:\([^)]*\))?\s*(?:\((\d{4}-\d{2}-\d{2})\))?\s*$/;
const ENTETE_SECTION = /^###\s+(.+?)\s*$/;
const ELEMENT = /^\*\s+(.*)$/;
/** Liens de fin de ligne ajoutés par release-please : ` ([abc1234](url))`, ` ([#12](url))`, `, closes [#3](url)` */
const LIENS_FINAUX = /(?:\s*\(\[[^\]]*\]\([^)]*\)\))+\s*$|,?\s*closes\s+\[[^\]]*\]\([^)]*\)\s*$/;
const REFERENCE = /\s*\((US-[A-Z]+-\d+(?:,\s*US-[A-Z]+-\d+)*)\)\s*$/;
const PORTEE = /^\*\*([^*:]+):\*\*\s*/;

function lireElement(brut: string): Nouveaute {
  // Les liens de fin de ligne se suivent (commit, puis ticket fermé) : on les retire jusqu'à n'en plus trouver
  let texte = brut.trim();
  for (let avant = ''; avant !== texte;) {
    avant = texte;
    texte = texte.replace(LIENS_FINAUX, '').replace(/,\s*$/, '').trim();
  }
  let portee = '';
  const p = PORTEE.exec(texte);
  if (p) {
    portee = p[1].trim();
    texte = texte.slice(p[0].length);
  }
  let reference = '';
  const r = REFERENCE.exec(texte);
  if (r) {
    reference = r[1];
    texte = texte.slice(0, r.index).trim();
  }
  return { portee, texte, reference };
}

/**
 * Découpe le `CHANGELOG.md` de release-please en versions, sections et lignes lisibles : sans liens vers les commits,
 * sans doublon d'une même ligne dans une version (le commit de la branche et celui de la fusion en donnaient deux avant
 * TECH-53). Une version sans aucune ligne est conservée avec ses sections vides écartées.
 */
export function lireChangelog(texte: string): VersionNouveautes[] {
  const versions: VersionNouveautes[] = [];
  let version: VersionNouveautes | null = null;
  let section: SectionNouveautes | null = null;
  const vus = new Set<string>();

  for (const ligne of texte.split(/\r?\n/)) {
    const v = ENTETE_VERSION.exec(ligne);
    if (v) {
      version = { version: v[1], date: v[2] ?? '', sections: [] };
      versions.push(version);
      section = null;
      vus.clear();
      continue;
    }
    if (!version) continue;
    const s = ENTETE_SECTION.exec(ligne);
    if (s) {
      const titre = TITRES[s[1]] ?? s[1];
      section = version.sections.find(x => x.titre === titre) ?? null;
      if (!section) {
        section = { titre, elements: [] };
        version.sections.push(section);
      }
      continue;
    }
    const e = ELEMENT.exec(ligne);
    if (e && section) {
      const element = lireElement(e[1]);
      const cle = `${section.titre}|${element.portee}|${element.texte}|${element.reference}`;
      if (element.texte && !vus.has(cle)) {
        vus.add(cle);
        section.elements.push(element);
      }
    }
  }
  for (const v of versions) v.sections = v.sections.filter(x => x.elements.length > 0);
  return versions;
}

/** « 24 octobre 2026 » ; la date brute si elle n'est pas lisible */
export function dateLisible(iso: string): string {
  const date = /^\d{4}-\d{2}-\d{2}$/.test(iso) ? new Date(`${iso}T12:00:00Z`) : null;
  if (!date || Number.isNaN(date.getTime())) return iso;
  return date.toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' });
}
