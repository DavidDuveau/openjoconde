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
          La base Joconde est le catalogue collectif des collections des musées de France, géré par le Ministère de la Culture. Elle compte plus d'un million de notices d'objets de toute nature (archéologie, beaux-arts, ethnologie, histoire, sciences et techniques...).
        </p>
      </section>

      <section class="about-section">
        <h2>Fonctionnalités</h2>
        <ul>
          <li>Recherche par mots dans les collections, avec filtres (artiste, domaine, technique, période, musée) et tris</li>
          <li>Fiche détaillée de chaque œuvre, avec renvoi vers sa notice sur la plateforme POP</li>
          <li>Annuaire des artistes et des musées, avec leurs œuvres</li>
        </ul>
      </section>

      <section class="about-section">
        <h2>Technologies</h2>
        <p>OpenJoconde est développé avec les technologies suivantes :</p>
        <ul>
          <li><strong>Backend</strong> : .NET 10 et ASP.NET Core</li>
          <li><strong>Base de données</strong> : PostgreSQL pour le stockage relationnel optimisé</li>
          <li><strong>Frontend</strong> : Vue.js 3 avec TypeScript pour une interface utilisateur moderne et réactive</li>
        </ul>
      </section>

      <section class="about-section" id="sources" data-testid="about-sources">
        <h2>Sources et licences</h2>
        <p>
          Les données viennent du Ministère de la Culture, publiées en données ouvertes sur data.gouv.fr et diffusées par la
          plateforme ouverte du patrimoine (POP) :
        </p>
        <ul>
          <li v-for="source in sources" :key="source.nom">
            <a :href="source.url" target="_blank" rel="noopener noreferrer">{{ source.nom }}</a> : {{ source.description }}
          </li>
        </ul>
        <p>
          Ces données sont réutilisées sous
          <a :href="licenceUrl" target="_blank" rel="noopener noreferrer">Licence Ouverte 2.0</a>. Elles sont reprises telles
          que publiées, sans en altérer le sens ; chaque fiche rappelle la référence de la notice, le lieu de conservation et
          l'origine des données.
        </p>
        <p>
          Les images, et certaines informations des notices, peuvent relever de droits de tiers : leur réutilisation demande
          l'autorisation de leurs auteurs ou ayants droit. OpenJoconde n'affiche donc aucune image ; la fiche renvoie vers la
          notice sur
          <a href="https://pop.culture.gouv.fr/" target="_blank" rel="noopener noreferrer">POP</a>, qui les diffuse.
        </p>
        <p>
          OpenJoconde est un projet indépendant, sans lien officiel avec le Ministère de la Culture.
        </p>
      </section>

      <section class="about-section">
        <h2>Contribuer</h2>
        <p>
          OpenJoconde est un projet open source. Les contributions sont les bienvenues, que ce soit pour améliorer le code, la documentation ou suggérer de nouvelles fonctionnalités.
        </p>
        <p>
          Le code source du projet est disponible sur GitHub :
          <a href="https://github.com/DavidDuveau/openjoconde" target="_blank" rel="noopener noreferrer">github.com/DavidDuveau/openjoconde</a>
        </p>
      </section>

      <section class="about-section">
        <h2>Contact</h2>
        <p>
          Pour toute question ou suggestion concernant l'application, ouvrez une demande sur
          <a href="https://github.com/DavidDuveau/openjoconde/issues" target="_blank" rel="noopener noreferrer">GitHub</a>.
        </p>
      </section>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import { SOURCES, LICENCE_URL } from '@/utils/sources';

export default defineComponent({
  name: 'AboutView',
  setup() {
    return { sources: SOURCES, licenceUrl: LICENCE_URL };
  }
});
</script>

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
