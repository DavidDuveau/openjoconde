import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './router';

const app = createApp(App);

// Pinia store
app.use(createPinia());

// Router
app.use(router);

app.mount('#app');
