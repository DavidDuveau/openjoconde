/**
 * Représente une œuvre d'art (version simplifiée pour les listes)
 */
export interface Artwork {
  id: string;
  title: string;
  reference: string;
  inventoryNumber: string;
  imageUrl: string;
  creationDate: string;
  artists: {
    id: string;
    firstName: string;
    lastName: string;
  }[];
}

/**
 * Représente les détails complets d'une œuvre d'art
 */
export interface ArtworkDetail {
  id: string;
  title: string | null;
  reference: string;
  inventoryNumber: string | null;
  denomination: string | null;
  imageUrl: string | null;
  description: string | null;
  dimensions: string | null;
  creationDate: string | null;
  creationPlace: string | null;
  conservationPlace: string | null;
  copyright: string | null;
  associatedSiteUrl: string | null;
  artistUnderCopyright: boolean | null;
  publicDomainYear: number | null;
  museum: {
    id: string;
    name: string;
  } | null;
  artists: {
    id: string;
    firstName: string | null;
    lastName: string;
    role: string | null;
  }[];
  domains: {
    id: string;
    name: string;
  }[];
  techniques: {
    id: string;
    name: string;
  }[];
  periods: {
    id: string;
    name: string;
  }[];
}

/**
 * Représente un résultat paginé pour tout type d'élément
 */
export interface PaginatedResult<T> {
  items: T[];
  totalItems: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
