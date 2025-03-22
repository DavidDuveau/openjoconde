<template>
  <div class="about">
    <h1>À propos d'OpenJoconde</h1>
    
    <div class="about-content">
      <section class="about-section">
        <h2>Le projet</h2>
        <p>
          OpenJoconde est une application web permettant d'explorer les collections des musées français référencées dans la base Joconde. Cette application a été développée dans le but de rendre accessible au grand public la richesse du patrimoine muséal français.
        </p>
        <p>
          La base Joconde est le catalogue collectif des collections des musées de France, géré par le Ministère de la Culture. Elle contient plus de 600 000 notices d'objets de toute nature (archéologie, beaux-arts, ethnologie, histoire, sciences et techniques...) valorisées par des notices descriptives et des images.
        </p>
      </section>

      <section class="about-section">
        <h2>Fonctionnalités</h2>
        <ul>
          <li>Recherche multicritères dans les collections</li>
          <li>Exploration par domaine, période, technique ou artiste</li>
          <li>Visualisation détaillée des œuvres avec leurs métadonnées</li>
          <li>Navigation thématique par musée</li>
          <li>Découverte d'œuvres similaires</li>
        </ul>
      </section>

      <section class="about-section">
        <h2>Technologies</h2>
        <p>OpenJoconde est développé avec les technologies suivantes :</p>
        <ul>
          <li><strong>Backend</strong> : .NET Core avec une architecture clean</li>
          <li><strong>Base de données</strong> : PostgreSQL pour le stockage relationnel optimisé</li>
          <li><strong>Frontend</strong> : Vue.js 3 avec TypeScript pour une interface utilisateur moderne et réactive</li>
        </ul>
      </section>

      <section class="about-section">
        <h2>Données ouvertes</h2>
        <p>
          Cette application utilise les données ouvertes de la base Joconde mises à disposition par le Ministère de la Culture sur la plateforme data.gouv.fr sous licence ouverte.
        </p>
        <p>
          Les données sont régulièrement mises à jour à partir de la source officielle pour garantir leur exactitude et leur fraîcheur.
        </p>
        <p>
          <a href="https://www.data.gouv.fr/fr/datasets/joconde-catalogue-collectif-des-collections-des-musees-de-france/" target="_blank" rel="noopener">
            Consulter les données sur data.gouv.fr
          </a>
        </p>
      </section>

      <section class="about-section">
        <h2>Contribuer</h2>
        <p>
          OpenJoconde est un projet open source. Les contributions sont les bienvenues, que ce soit pour améliorer le code, la documentation ou suggérer de nouvelles fonctionnalités.
        </p>
        <p>
          Le code source du projet est disponible sur GitHub :
          <!-- Dans une implémentation réelle, insérer le lien vers le repo GitHub -->
          <a href="#" target="_blank" rel="noopener">GitHub Repository</a>
        </p>
      </section>

      <section class="about-section">
        <h2>Contact</h2>
        <p>
          Pour toute question ou suggestion concernant l'application OpenJoconde, veuillez nous contacter à l'adresse :
          <!-- Adresse email fictive -->
          <a href="mailto:contact@openjoconde.org">contact@openjoconde.org</a>
        </p>
      </section>
    </div>
  </div>
</template>

<style scoped lang="scss">
.about {
  h1 {
    margin-bottom: 24px;
    color: var(--primary-color);
  }

  .about-content {
    background-color: white;
    border-radius: 8px;
    padding: 32px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);

    .about-section {
      margin-bottom: 32px;

      &:last-child {
        margin-bottom: 0;
      }

      h2 {
        font-size: 1.5rem;
        margin-bottom: 16px;
        color: var(--primary-color);
        border-bottom: 1px solid #eee;
        padding-bottom: 8px;
      }

      p {
        margin-bottom: 16px;
        line-height: 1.6;

        &:last-child {
          margin-bottom: 0;
        }
      }

      ul {
        list-style-type: disc;
        padding-left: 20px;
        margin-bottom: 16px;

        li {
          margin-bottom: 8px;
          line-height: 1.6;
        }
      }

      a {
        color: var(--secondary-color);
        text-decoration: none;

        &:hover {
          text-decoration: underline;
        }
      }
    }
  }
}
</style>
