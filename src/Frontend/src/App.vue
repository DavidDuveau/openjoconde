<template>
  <header>
    <nav>
      <router-link to="/">Accueil</router-link> |
      <router-link to="/search">Recherche</router-link> |
      <router-link to="/museums">Musées</router-link> |
      <router-link to="/artists">Artistes</router-link>
    </nav>
  </header>
  <main>
    <router-view />
  </main>
  <footer>
    <p>OpenJoconde - Exploration des collections des musées de France</p>
  </footer>
</template>

<style lang="scss">
#app {
  font-family: 'Avenir', Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
  max-width: 1200px;
  margin: 0 auto;
}

nav {
  padding: 30px;

  a {
    font-weight: bold;
    color: #2c3e50;
    text-decoration: none;
    margin: 0 10px;

    &.router-link-exact-active {
      color: #42b983;
    }
  }
}

main {
  min-height: 70vh;
}

footer {
  margin-top: 40px;
  padding: 20px;
  border-top: 1px solid #eee;
  color: #666;
}
</style>
