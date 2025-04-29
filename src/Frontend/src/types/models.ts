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
  artworks: ArtworkArtist[];
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
}

export interface SearchParams {
  searchText?: string;
  artistId?: string;
  domainId?: string;
  techniqueId?: string;
  periodId?: string;
  museumId?: string;
  page: number;
  pageSize: number;
  sortBy?: string;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
