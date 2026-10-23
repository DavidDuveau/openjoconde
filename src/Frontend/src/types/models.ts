export interface Artwork {
  id: string;
  reference: string;
  inventoryNumber: string;
  denomination: string;
  title: string;
  description: string;
  dimensions: string;
  creationDate: string;
  creationPlace: string;
  conservationPlace: string;
  copyright: string;
  imageUrl: string;
  updatedAt: string;
  artists: ArtworkArtist[];
  domains: Domain[];
  techniques: Technique[];
  periods: Period[];
}

export interface Artist {
  id: string;
  lastName: string;
  firstName: string;
  birthDate: string;
  deathDate: string;
  nationality: string;
  biography: string;
  /** Nombre d'œuvres ; les œuvres se lisent par la recherche d'œuvres filtrée par artiste */
  artworksCount: number;
}

export interface ArtworkArtist {
  artworkId: string;
  artistId: string;
  role: string;
  artwork?: Artwork;
  artist?: Artist;
}

export interface Domain {
  id: string;
  name: string;
  description: string;
}

export interface Technique {
  id: string;
  name: string;
  description: string;
}

export interface Period {
  id: string;
  name: string;
  startYear?: number;
  endYear?: number;
  description: string;
}

export interface NamedItem {
  id: string;
  name: string;
}

export interface Museum {
  id: string;
  name: string;
  city: string;
  department: string;
  address: string;
  zipCode: string;
  phone: string;
  email: string;
  website: string;
  description: string;
  region?: string;
  code?: string;
  latitude?: number;
  longitude?: number;
}

/** Une région et ses départements, avec leur nombre de musées */
export interface RegionGeography {
  name: string;
  museums: number;
  departments: { name: string; museums: number }[];
}

export interface SearchParams {
  searchText?: string;
  artistId?: string;
  domainId?: string;
  techniqueId?: string;
  periodId?: string;
  museumId?: string;
  // Région et département du musée qui conserve l'œuvre
  region?: string;
  department?: string;
  // Position de l'utilisateur (« Près de moi »), pour le tri par distance
  latitude?: number;
  longitude?: number;
  museumName?: string;
  // Libellés des filtres choisis, gardés dans l'adresse pour les réafficher au rechargement
  artistName?: string;
  domainName?: string;
  techniqueName?: string;
  periodName?: string;
  page: number;
  pageSize: number;
  sortBy?: string; // title (défaut), title_desc, date, date_desc, relevance, artist ou distance
}

export interface PaginatedResult<T> {
  items: T[];
  totalItems: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
