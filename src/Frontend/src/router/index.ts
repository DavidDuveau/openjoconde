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
    path: '/periods',
    name: 'periods',
    component: () => import('@/views/ReferenceListView.vue'),
    props: { kind: 'periods' }
  },
  {
    path: '/techniques',
    name: 'techniques',
    component: () => import('@/views/ReferenceListView.vue'),
    props: { kind: 'techniques' }
  },
  {
    path: '/thematiques',
    name: 'thematiques',
    component: () => import('@/views/ThematiquesView.vue')
  },
  {
    path: '/thematiques/:slug',
    name: 'thematique',
    component: () => import('@/views/ThematiqueView.vue')
  },
  {
    path: '/favoris',
    name: 'favoris',
    component: () => import('@/views/FavorisView.vue')
  },
  {
    path: '/collections',
    name: 'collections',
    component: () => import('@/views/CollectionsView.vue')
  },
  {
    path: '/collections/:id',
    name: 'collection',
    component: () => import('@/views/CollectionView.vue')
  },
  {
    path: '/partage',
    name: 'collection-partagee',
    component: () => import('@/views/CollectionPartageeView.vue')
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
  // Un lien vers une ancre (/about#sources) y mène ; sinon, en haut de la page. Le fragment d'une collection partagée
  // n'est pas une ancre : il porte la collection, et n'est jamais utilisé comme sélecteur.
  scrollBehavior: to => (to.hash && to.name !== 'collection-partagee' ? { el: to.hash, top: 80 } : { top: 0 })
});

export default router;
