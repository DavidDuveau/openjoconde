<template>
  <div id="app">
    <AppHeader />

    <main>
      <router-view />
    </main>

    <footer>
      <p>OpenJoconde - Exploration des collections des musées de France</p>
    </footer>
  </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import AppHeader from '@/components/AppHeader.vue';

export default defineComponent({
  components: {
    AppHeader
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
  height: var(--footer-height);
  padding: 0 20px;
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>
