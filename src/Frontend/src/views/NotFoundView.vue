<template>
  <div class="not-found">
    <div class="not-found-content">
      <h1>404</h1>
      <h2>Page non trouvée</h2>
      <p>La page que vous recherchez n'existe pas ou a été déplacée.</p>
      <router-link to="/" class="home-link">Retour à la page d'accueil</router-link>
    </div>
  </div>
</template>

<style scoped lang="scss">
.not-found {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 40px;
  text-align: center;

  .not-found-content {
    max-width: 600px;
    background-color: white;
    border-radius: 8px;
    padding: 40px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);

    h1 {
      font-size: 6rem;
      color: var(--primary-color);
      margin-bottom: 16px;
    }

    h2 {
      font-size: 2rem;
      color: var(--primary-color);
      margin-bottom: 16px;
    }

    p {
      font-size: 1.2rem;
      margin-bottom: 24px;
      color: #666;
    }

    .home-link {
      display: inline-block;
      padding: 12px 24px;
      background-color: var(--secondary-color);
      color: white;
      text-decoration: none;
      border-radius: 4px;
      font-weight: bold;
      transition: background-color 0.3s;

      &:hover {
        background-color: darken(#42b983, 10%);
      }
    }
  }
}
</style>
