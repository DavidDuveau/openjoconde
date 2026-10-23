/** Un morceau de texte ; `definition` est renseignée quand ce morceau est un terme du glossaire */
export interface Morceau {
  texte: string;
  definition?: string;
}

/**
 * Termes techniques ou spécialisés des notices, avec leur définition. Les clés sont écrites sans accent ni majuscule :
 * la recherche d'un terme les ignore aussi.
 */
export const GLOSSAIRE: Record<string, string> = {
  'huile sur toile': "Peinture dont les pigments sont liés par de l'huile, appliquée sur une toile tendue.",
  'huile sur panneau': "Peinture à l'huile appliquée sur un support de bois.",
  'huile': "Peinture dont les pigments sont liés par de l'huile (lin, noix, œillette), qui sèche lentement.",
  'tempera': "Peinture dont les pigments sont liés par un mélange à base d'eau et d'œuf ou de colle.",
  'aquarelle': "Peinture à l'eau, transparente, appliquée en lavis légers sur du papier.",
  'gouache': "Peinture à l'eau rendue opaque par l'ajout de craie ou de blanc ; elle donne des aplats mats.",
  'pastel': "Bâtonnet de pigments agglomérés, utilisé sec ; désigne aussi l'œuvre ainsi réalisée.",
  'fusain': "Bâtonnet de bois carbonisé qui permet un dessin noir, facile à estomper.",
  'sanguine': "Craie rouge à base d'oxyde de fer, utilisée pour le dessin.",
  'lavis': "Dessin ou peinture à l'encre ou à l'aquarelle très diluée, en teintes transparentes.",
  'fresque': "Peinture murale exécutée sur un enduit de chaux encore frais.",
  'gravure': "Image obtenue en creusant ou en incisant une matière (cuivre, bois, pierre) puis en l'imprimant.",
  'eau-forte': "Gravure sur métal creusée par un acide ; la plaque encrée est ensuite imprimée.",
  'lithographie': "Impression à partir d'un dessin tracé au crayon gras sur une pierre calcaire.",
  'xylographie': "Gravure sur bois : on épargne les traits à imprimer.",
  'burin': "Outil d'acier pour graver le métal ; désigne aussi la gravure faite avec cet outil.",
  'estampe': "Image imprimée à partir d'une matrice gravée, tirée à plusieurs exemplaires.",
  'bronze': "Alliage de cuivre et d'étain, coulé dans un moule pour les sculptures et objets.",
  'terre cuite': "Argile modelée puis cuite au four, non émaillée.",
  'faience': "Céramique à pâte poreuse recouverte d'un émail opaque.",
  'porcelaine': "Céramique blanche, dure et translucide, cuite à très haute température.",
  'gres': "Céramique très dure et imperméable, cuite à haute température.",
  'email': "Verre coloré fondu sur un support, en général du métal.",
  'marqueterie': "Décor fait de petites pièces de bois ou d'autres matières, assemblées en motif.",
  'tapisserie': "Textile tissé dont le décor est formé par la trame, souvent destiné à orner un mur.",
  'ronde-bosse': "Sculpture que l'on peut contempler sous tous les angles, par opposition au relief.",
  'bas-relief': "Sculpture dont les motifs ne se détachent que faiblement du fond.",
  'haut-relief': "Sculpture dont les motifs se détachent presque entièrement du fond.",
  'diptyque': "Œuvre composée de deux panneaux liés, parfois repliables.",
  'triptyque': "Œuvre composée de trois panneaux, dont les deux latéraux peuvent se replier sur celui du centre.",
  'esquisse': "Première version rapide d'une œuvre, avant l'exécution définitive.",
  'etude': "Travail préparatoire consacré à un détail ou à un aspect d'une œuvre.",
  'cartouche': "Cadre ornemental, souvent portant une inscription.",
  'camaieu': "Peinture exécutée dans les nuances d'une seule couleur.",
  'grisaille': "Peinture en nuances de gris, qui imite le relief d'une sculpture.",
  'verre': "Matière transparente obtenue par la fusion de sable et de fondants.",
  'vitrail': "Composition de verres colorés assemblés par des plombs, pour fermer une baie.",
  'ivoire': "Matière dure et blanche tirée des défenses d'éléphant, travaillée en sculpture et en objets.",
  'dessin': "Œuvre tracée à la plume, au crayon, au fusain ou à d'autres moyens sur du papier.",
  'peinture': "Œuvre réalisée en appliquant des pigments sur un support.",
  'sculpture': "Œuvre en volume, taillée, modelée ou coulée."
};

const sansAccent = (texte: string) => texte.normalize('NFD').replace(/\p{Mn}/gu, '').toLowerCase();

const echapper = (texte: string) => texte.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

// Les termes les plus longs d'abord : « huile sur toile » l'emporte sur « huile »
const MOTIF = new RegExp(
  `(?<![\\p{L}\\p{N}])(${Object.keys(GLOSSAIRE).sort((a, b) => b.length - a.length).map(echapper).join('|')})(?![\\p{L}\\p{N}])`,
  'gu'
);

/**
 * Découpe un texte en morceaux : les termes du glossaire portent leur définition, le reste est du texte brut.
 * La recherche ignore la casse et les accents ; le texte d'origine est conservé tel quel.
 */
export function decouper(texte: string): Morceau[] {
  // Retirer les accents ne change pas la longueur des lettres de base : les positions restent celles du texte d'origine
  const plat = texte.replace(/[À-ſ]/g, c => sansAccent(c)).toLowerCase();
  const morceaux: Morceau[] = [];
  let debut = 0;
  for (const trouve of plat.matchAll(MOTIF)) {
    const index = trouve.index ?? 0;
    if (index > debut) morceaux.push({ texte: texte.slice(debut, index) });
    morceaux.push({ texte: texte.slice(index, index + trouve[0].length), definition: GLOSSAIRE[trouve[1]] });
    debut = index + trouve[0].length;
  }
  if (debut < texte.length) morceaux.push({ texte: texte.slice(debut) });
  return morceaux;
}
