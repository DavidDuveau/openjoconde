/** Un moment de l'année, jour et mois, sans année : les collections saisonnières reviennent chaque année */
export interface JourDeLAnnee {
  mois: number;
  jour: number;
}

export interface Saison {
  debut: JourDeLAnnee;
  fin: JourDeLAnnee;
}

/** Une étape du parcours : un texte qui la présente, puis les œuvres de la recherche qui la définit */
export interface Section {
  titre: string;
  texte: string;
  /** Mots cherchés dans les titres et descriptions des notices (tous requis) */
  recherche?: string;
  /** Nom exact d'un domaine de la base ; la recherche est restreinte à ses œuvres */
  domaine?: string;
}

export interface Thematique {
  slug: string;
  titre: string;
  /** Une ou deux phrases, pour la vignette */
  resume: string;
  /** Décor de la vignette : le fonds d'écran n'est pas une image des œuvres (pas d'image sans droits) */
  glyphe: string;
  couleurs: [string, string];
  /** Mise en avant sur l'accueil */
  enAvant?: boolean;
  saison?: Saison;
  explication: string;
  importance: string;
  criteres: string;
  sections: Section[];
}

export const THEMATIQUES: Thematique[] = [
  {
    slug: 'portraits',
    titre: 'Portraits',
    resume: 'Visages de rois, de bourgeois, d\'anonymes et d\'artistes : le portrait dans toutes les techniques.',
    glyphe: '🖼️',
    couleurs: ['#8e5572', '#c98bb9'],
    enAvant: true,
    explication:
      'Le portrait représente une personne identifiable, ou donnée pour telle. Il s\'est décliné dans presque toutes les ' +
      'techniques, de la peinture à la photographie, et sert tour à tour à affirmer un rang, à garder le souvenir d\'un proche ou à ' +
      'rendre un hommage.',
    importance:
      'Le portrait est l\'un des grands genres de l\'histoire de l\'art : il documente les visages, les costumes et les ' +
      'codes sociaux de chaque époque, et conserve la trace de personnes que l\'histoire a parfois oubliées.',
    criteres:
      'Notices dont le titre ou la description contient le mot « portrait », regroupées par domaine (peinture, dessin, ' +
      'estampe, photographie, sculpture). La sélection suit la notice : une œuvre qui n\'emploie pas ce mot n\'y figure pas.',
    sections: [
      { titre: 'Portraits peints', texte: 'La peinture, genre noble du portrait : huiles, miniatures et pastels.', recherche: 'portrait', domaine: 'peinture' },
      { titre: 'Portraits dessinés', texte: 'Plus rapide et plus intime, le dessin saisit un visage en quelques traits.', recherche: 'portrait', domaine: 'dessin' },
      { titre: 'Portraits gravés', texte: 'La gravure diffuse les visages célèbres en grand nombre.', recherche: 'portrait', domaine: 'estampe' },
      { titre: 'Portraits photographiques', texte: 'Depuis le XIXe siècle, la photographie démocratise le portrait.', recherche: 'portrait', domaine: 'photographie' },
      { titre: 'Portraits sculptés', texte: 'Bustes et médaillons donnent du volume aux visages.', recherche: 'portrait', domaine: 'sculpture' }
    ]
  },
  {
    slug: 'art-religieux',
    titre: 'Art religieux',
    resume: 'La Vierge, le Christ et les saints : des siècles d\'images au service de la foi.',
    glyphe: '⛪',
    couleurs: ['#3b5b8c', '#8fb0dc'],
    enAvant: true,
    explication:
      'L\'art religieux rassemble les œuvres créées pour le culte ou pour illustrer des récits sacrés. En France, ce sont ' +
      'surtout des images chrétiennes : la Vierge et l\'Enfant, la vie du Christ, les saints.',
    importance:
      'Pendant des siècles, l\'Église a été l\'un des principaux commanditaires des artistes. Ces images ont fixé des ' +
      'iconographies que l\'on reconnaît encore, et transmettent des récits à un public qui ne lisait pas toujours.',
    criteres:
      'Notices dont le titre ou la description contient le mot du sujet (Vierge, Christ, saint, Crucifixion). Les noms ' +
      'de saints sont donc trouvés par le mot « saint » seulement quand la notice l\'emploie.',
    sections: [
      { titre: 'La Vierge et l\'Enfant', texte: 'Le sujet le plus répandu de l\'art chrétien : Marie, seule ou avec Jésus enfant.', recherche: 'vierge' },
      { titre: 'Le Christ', texte: 'De la naissance à la Passion, les épisodes de la vie du Christ.', recherche: 'christ' },
      { titre: 'La Crucifixion', texte: 'La mort du Christ sur la croix, scène centrale de l\'iconographie chrétienne.', recherche: 'crucifixion' },
      { titre: 'Les saints', texte: 'Apôtres, martyrs et saints locaux, reconnaissables à leurs attributs.', recherche: 'saint' }
    ]
  },
  {
    slug: 'paysages',
    titre: 'Paysages et marines',
    resume: 'Campagnes, rivages et horizons : la nature vue par les artistes.',
    glyphe: '🏞️',
    couleurs: ['#3f7d5a', '#9bd1a8'],
    enAvant: true,
    explication:
      'Le paysage fait de la nature le sujet de l\'œuvre, et non plus un simple décor. La marine, sa cousine, a pour sujet la mer, ' +
      'les ports et les navires.',
    importance:
      'Devenu un genre à part entière à l\'époque moderne, le paysage a connu un tournant au XIXe siècle, quand les artistes ' +
      'sont sortis de l\'atelier pour peindre sur le motif.',
    criteres:
      'Notices dont le titre ou la description contient « paysage » ou « marine », en peinture et en dessin pour les paysages.',
    sections: [
      { titre: 'Paysages peints', texte: 'La nature en couleurs, de la campagne classique aux effets de lumière.', recherche: 'paysage', domaine: 'peinture' },
      { titre: 'Paysages dessinés', texte: 'Carnets et feuilles d\'étude : le paysage pris sur le vif.', recherche: 'paysage', domaine: 'dessin' },
      { titre: 'Marines', texte: 'La mer, les ports et les navires.', recherche: 'marine' }
    ]
  },
  {
    slug: 'natures-mortes',
    titre: 'Natures mortes',
    resume: 'Fruits, fleurs et objets du quotidien, rassemblés pour être regardés.',
    glyphe: '🍎',
    couleurs: ['#a8662f', '#e3b27b'],
    explication:
      'La nature morte met en scène des objets inanimés : fruits, fleurs, vaisselle, instruments. Certaines, les vanités, ' +
      'rappellent aussi que la vie est brève.',
    importance:
      'Longtemps jugé mineur, le genre a permis aux peintres d\'exercer leur virtuosité sur les matières et la lumière, et ' +
      'il est devenu un terrain d\'expérience pour l\'art moderne.',
    criteres:
      'Notices dont le titre ou la description contient « nature morte », « bouquet » ou « vanité ».',
    sections: [
      { titre: 'Natures mortes', texte: 'Fruits, gibier, vaisselle : l\'objet devient sujet.', recherche: 'nature morte' },
      { titre: 'Bouquets', texte: 'Les fleurs, entre étude botanique et décor.', recherche: 'bouquet' },
      { titre: 'Vanités', texte: 'Crânes, sabliers, fleurs fanées : le temps qui passe.', recherche: 'vanité' }
    ]
  },
  {
    slug: 'prehistoire-antiquite',
    titre: 'De la préhistoire à l\'Antiquité',
    resume: 'Silex, poteries, monnaies et objets du quotidien des premiers habitants de la Gaule.',
    glyphe: '🏺',
    couleurs: ['#7a6a4f', '#cbb892'],
    explication:
      'Les musées de France conservent aussi des objets archéologiques : outils du Néolithique, objets gallo-romains, ' +
      'monnaies. Ils racontent la vie quotidienne bien avant les premières peintures de chevalet.',
    importance:
      'Ces objets sont souvent les seules traces écrites ou matérielles de peuples qui n\'ont laissé aucun récit, ' +
      'et permettent de reconstituer leurs techniques, leurs échanges et leurs croyances.',
    criteres:
      'Notices rattachées au domaine « néolithique », « gallo-romain » ou « numismatique » par le musée qui les a décrites.',
    sections: [
      { titre: 'Le Néolithique', texte: 'Premiers agriculteurs et éleveurs : haches polies, poteries, parures.', domaine: 'néolithique' },
      { titre: 'L\'époque gallo-romaine', texte: 'Quand la Gaule devient romaine : céramiques, bijoux, objets du quotidien.', domaine: 'gallo-romain' },
      { titre: 'Monnaies et médailles', texte: 'La monnaie circule les images du pouvoir et les idées.', domaine: 'numismatique' }
    ]
  },
  {
    slug: 'noel',
    titre: 'Noël et l\'Épiphanie',
    resume: 'La Nativité, les bergers et les rois mages, à retrouver chaque hiver.',
    glyphe: '⭐',
    couleurs: ['#8c2f39', '#d9848c'],
    saison: { debut: { mois: 12, jour: 1 }, fin: { mois: 1, jour: 6 } },
    explication:
      'Collection de saison : les images de la naissance du Christ et de la visite des rois mages, du début de l\'Avent à ' +
      'l\'Épiphanie.',
    importance:
      'La Nativité et l\'Adoration des mages comptent parmi les sujets les plus peints de l\'art chrétien : ils ont ' +
      'offert aux artistes l\'occasion de mêler scène sacrée, paysage et vie quotidienne.',
    criteres:
      'Notices dont le titre ou la description contient « Nativité », « adoration » et « mages », ou « Noël ».',
    sections: [
      { titre: 'La Nativité', texte: 'La naissance de Jésus dans l\'étable de Bethléem.', recherche: 'nativité' },
      { titre: 'L\'Adoration des mages', texte: 'Les trois rois venus de l\'Orient apportent leurs présents.', recherche: 'adoration mages' },
      { titre: 'Noël', texte: 'Les œuvres dont la notice évoque Noël.', recherche: 'noël' }
    ]
  }
];

export const trouverThematique = (slug: string): Thematique | undefined => THEMATIQUES.find(t => t.slug === slug);

const MOIS = ['janvier', 'février', 'mars', 'avril', 'mai', 'juin', 'juillet', 'août', 'septembre', 'octobre', 'novembre', 'décembre'];

const libelleJour = ({ mois, jour }: JourDeLAnnee) => `${jour === 1 ? '1er' : jour} ${MOIS[mois - 1]}`;

/** « Du 1er décembre au 6 janvier » */
export const libelleSaison = (saison: Saison): string => `Du ${libelleJour(saison.debut)} au ${libelleJour(saison.fin)}`;

/** Vrai si la date tombe dans la saison, y compris quand elle enjambe le Nouvel An (décembre à janvier) */
export function saisonEnCours(saison: Saison, date: Date): boolean {
  const valeur = (mois: number, jour: number) => mois * 100 + jour;
  const maintenant = valeur(date.getMonth() + 1, date.getDate());
  const debut = valeur(saison.debut.mois, saison.debut.jour);
  const fin = valeur(saison.fin.mois, saison.fin.jour);
  return debut <= fin ? maintenant >= debut && maintenant <= fin : maintenant >= debut || maintenant <= fin;
}
