import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router'
import HomeView from '../views/HomeView.vue'

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    name: 'home',
    component: HomeView
  },
  {
    path: '/search',
    name: 'search',
    component: () => import(/* webpackChunkName: "search" */ '../views/SearchView.vue')
  },
  {
    path: '/artwork/:id',
    name: 'artwork-detail',
    component: () => import(/* webpackChunkName: "artwork-detail" */ '../views/ArtworkDetailView.vue'),
    props: true
  },
  {
    path: '/artists',
    name: 'artists',
    component: () => import(/* webpackChunkName: "artists" */ '../views/ArtistsView.vue')
  },
  {
    path: '/artist/:id',
    name: 'artist-detail',
    component: () => import(/* webpackChunkName: "artist-detail" */ '../views/ArtistDetailView.vue'),
    props: true
  },
  {
    path: '/museums',
    name: 'museums',
    component: () => import(/* webpackChunkName: "museums" */ '../views/MuseumsView.vue')
  },
  {
    path: '/museum/:id',
    name: 'museum-detail',
    component: () => import(/* webpackChunkName: "museum-detail" */ '../views/MuseumDetailView.vue'),
    props: true
  }
]

const router = createRouter({
  history: createWebHistory(process.env.BASE_URL),
  routes
})

export default router
