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
  title: string;
  reference: string;
  inventoryNumber: string;
  imageUrl: string;
  description: string;
  dimensions: string;
  creationDate: string;
  creationPlace: string;
  museum: {
    id: string;
    name: string;
  };
  artists: {
    id: string;
    firstName: string;
    lastName: string;
    role?: string;
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
