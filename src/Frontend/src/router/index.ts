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
  history: createWebHistory(process.env.BASE_URL),
  routes
});

export default router;
