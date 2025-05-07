import axios from 'axios';
import { PaginatedResult, Artwork, ArtworkDetail } from '@/types/artwork';
import { Artist, Domain, Technique, Period } from '@/types/models';

// Création de l'instance axios avec la configuration de base
const api = axios.create({
  baseURL: process.env.VUE_APP_API_URL || 'https://localhost:5001/api',
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  }
});

// Intercepteur pour gérer les erreurs globalement
api.interceptors.response.use(
  (response) => response,
  (error) => {
    // Log de l'erreur en console
    console.error('API Error:', error.response);
    return Promise.reject(error);
  }
);

// Méthodes pour accéder aux endpoints d'API
api.getArtworks = async (page = 1, pageSize = 10) => {
  const response = await api.get('/artworks', { 
    params: { page, pageSize } 
  });
  return response.data;
};

api.getArtwork = async (id) => {
  const response = await api.get(`/artworks/${id}`);
  return response.data;
};

api.searchArtworks = async (params) => {
  const response = await api.get('/artworks/search', { params });
  return response.data;
};

api.getArtists = async (page = 1, pageSize = 10) => {
  const response = await api.get('/artists', { 
    params: { page, pageSize } 
  });
  return response.data;
};

api.getArtist = async (id) => {
  const response = await api.get(`/artists/${id}`);
  return response.data;
};

api.getDomains = async () => {
  const response = await api.get('/domains');
  return response.data;
};

api.getTechniques = async () => {
  const response = await api.get('/techniques');
  return response.data;
};

api.getPeriods = async () => {
  const response = await api.get('/periods');
  return response.data;
};

api.getMuseums = async (page = 1, pageSize = 10) => {
  const response = await api.get('/museums', { 
    params: { page, pageSize } 
  });
  return response.data;
};

api.getMuseum = async (id) => {
  const response = await api.get(`/museums/${id}`);
  return response.data;
};

export default api;