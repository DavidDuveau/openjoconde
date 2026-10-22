import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';

// Views
import HomeView from '@/views/HomeView.vue';

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    name: 'home',
    component: HomeView
  },
  {
    path: '/artworks',
    name: 'artworks',
    component: () => import('@/views/ArtworksView.vue')
  },
  {
    path: '/search',
    name: 'search',
    component: () => import('@/views/SearchView.vue')
  },
  {
    path: '/artworks/:id',
    name: 'artwork-detail',
    component: () => import('@/views/ArtworkDetailView.vue')
  },
  {
    path: '/artists',
    name: 'artists',
    component: () => import('@/views/ArtistsView.vue')
  },
  {
    path: '/artists/:id',
    name: 'artist-detail',
    component: () => import('@/views/ArtistDetailView.vue')
  },
  {
    path: '/museums',
    name: 'museums',
    component: () => import('@/views/MuseumsView.vue')
  },
  {
    path: '/museums/:id',
    name: 'museum-detail',
    component: () => import('@/views/MuseumDetailView.vue')
  },
  {
    path: '/about',
    name: 'about',
    component: () => import('@/views/AboutView.vue')
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/views/NotFoundView.vue')
  }
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  // Un lien vers une ancre (/about#sources) y mène ; sinon, en haut de la page
  scrollBehavior: to => (to.hash ? { el: to.hash, top: 80 } : { top: 0 })
});

export default router;
