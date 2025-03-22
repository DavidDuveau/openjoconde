import axios from 'axios';

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

export default api;
