import axios, { AxiosInstance } from 'axios';
import { Artwork, Artist, Domain, Museum, Period, SearchParams, Technique, PaginatedResult } from '@/types/models';

class ApiService {
  private api: AxiosInstance;

  constructor() {
    this.api = axios.create({
      baseURL: process.env.VUE_APP_API_URL || 'https://localhost:5001/api',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Add response interceptor to handle pagination
    this.api.interceptors.response.use(response => {
      const totalCount = response.headers['x-total-count'];
      const page = response.headers['x-page'];
      const pageSize = response.headers['x-page-size'];

      // If pagination headers exist, return a paginated result
      if (totalCount && page && pageSize) {
        const totalCountNum = parseInt(totalCount, 10);
        const pageNum = parseInt(page, 10);
        const pageSizeNum = parseInt(pageSize, 10);
        
        return {
          items: response.data,
          totalCount: totalCountNum,
          page: pageNum,
          pageSize: pageSizeNum,
          totalPages: Math.ceil(totalCountNum / pageSizeNum),
        } as PaginatedResult<any>;
      }

      // Otherwise return the data directly
      return response.data;
    });
  }

  // Artworks
  public async getArtworks(page = 1, pageSize = 10): Promise<PaginatedResult<Artwork>> {
    return this.api.get(`/artworks?page=${page}&pageSize=${pageSize}`);
  }

  public async getArtwork(id: string): Promise<Artwork> {
    return this.api.get(`/artworks/${id}`);
  }

  public async searchArtworks(params: SearchParams): Promise<PaginatedResult<Artwork>> {
    // Convert params to query string
    const queryParams = new URLSearchParams();
    
    if (params.searchText) queryParams.append('searchText', params.searchText);
    if (params.artistId) queryParams.append('artistId', params.artistId);
    if (params.domainId) queryParams.append('domainId', params.domainId);
    if (params.techniqueId) queryParams.append('techniqueId', params.techniqueId);
    if (params.periodId) queryParams.append('periodId', params.periodId);
    if (params.museumId) queryParams.append('museumId', params.museumId);
    
    queryParams.append('page', params.page.toString());
    queryParams.append('pageSize', params.pageSize.toString());
    
    return this.api.get(`/artworks/search?${queryParams.toString()}`);
  }

  // Artists
  public async getArtists(page = 1, pageSize = 10): Promise<PaginatedResult<Artist>> {
    return this.api.get(`/artists?page=${page}&pageSize=${pageSize}`);
  }

  public async getArtist(id: string): Promise<Artist> {
    return this.api.get(`/artists/${id}`);
  }

  // Domains
  public async getDomains(): Promise<Domain[]> {
    return this.api.get('/domains');
  }

  // Techniques
  public async getTechniques(): Promise<Technique[]> {
    return this.api.get('/techniques');
  }

  // Periods
  public async getPeriods(): Promise<Period[]> {
    return this.api.get('/periods');
  }

  // Museums
  public async getMuseums(page = 1, pageSize = 10): Promise<PaginatedResult<Museum>> {
    return this.api.get(`/museums?page=${page}&pageSize=${pageSize}`);
  }

  public async getMuseum(id: string): Promise<Museum> {
    return this.api.get(`/museums/${id}`);
  }
}

export default new ApiService();
