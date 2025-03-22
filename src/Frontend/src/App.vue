<template>
  <div id="app">
    <header>
      <nav>
        <router-link to="/">Accueil</router-link> |
        <router-link to="/artworks">Œuvres</router-link> |
        <router-link to="/artists">Artistes</router-link> |
        <router-link to="/museums">Musées</router-link> |
        <router-link to="/about">À propos</router-link>
      </nav>
    </header>

    <main>
      <router-view />
    </main>

    <footer>
      <p>OpenJoconde - Exploration des collections des musées de France</p>
    </footer>
  </div>
</template>

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

header {
  background-color: var(--primary-color);
  color: white;
  height: var(--header-height);
  padding: 0 20px;
  display: flex;
  align-items: center;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);

  nav {
    a {
      color: white;
      text-decoration: none;
      margin-right: 15px;
      font-weight: bold;
      
      &.router-link-exact-active {
        color: var(--secondary-color);
      }
    }
  }
}

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
