<template>
  <div id="app">
    <AppHeader />

    <main>
      <router-view />
    </main>

    <footer>
      <p>OpenJoconde - Exploration des collections des musées de France</p>
      <p class="footer-source" data-testid="footer-source">
        Données : Ministère de la Culture (bases Joconde et Muséofile), Licence Ouverte 2.0 ·
        <router-link to="/about#sources">Sources et licences</router-link>
      </p>
      <p class="footer-version">
        <span data-testid="app-version">Version {{ version }}</span> ·
        <router-link to="/nouveautes" data-testid="footer-nouveautes">Nouveautés</router-link>
      </p>
    </footer>
  </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import AppHeader from '@/components/AppHeader.vue';

export default defineComponent({
  components: {
    AppHeader
  },
  setup() {
    return { version: __APP_VERSION__ };
  }
});
</script>

<style lang="scss">
:root {
  --primary-color: #2c3e50;
  --secondary-color: #42b983;
  --text-color: #333;
  --background-color: #f8f9fa;
  --header-height: 60px;
  --footer-height: 60px;
}

* {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

body {
  font-family: 'Avenir', Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  color: var(--text-color);
  background-color: var(--background-color);
  line-height: 1.6;
}

#app {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

// Les styles de l'en-tête sont maintenant dans le composant AppHeader

main {
  flex: 1;
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
  width: 100%;
}

footer {
  background-color: var(--primary-color);
  color: white;
  min-height: var(--footer-height);
  padding: 10px 20px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 4px;
  text-align: center;

  .footer-version {
    font-size: 0.7rem;
    opacity: 0.6;
  }

  .footer-source {
    font-size: 0.8rem;
    opacity: 0.85;

    a {
      color: white;
    }
  }
}
</style>
