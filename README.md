# Projet OpenJoconde

Application d'exploitation des données ouvertes des musées français de la base Joconde.

## Description

Le projet OpenJoconde vise à créer une application web permettant d'explorer les riches collections des musées français référencées dans la base Joconde. L'objectif est de rendre plus accessibles et exploitables les données publiques des œuvres d'art et objets patrimoniaux conservés dans les musées de France.

L'application se compose de trois composants principaux :

1. **Backend (.NET)** : Service qui récupère, traite et stocke les données depuis data.gouv.fr, et expose une API REST pour le frontend.

2. **Base de données (PostgreSQL)** : Stockage relationnel optimisé pour les requêtes de recherche sur les différents attributs des œuvres.

3. **Frontend (Vue.js TypeScript)** : Interface utilisateur intuitive permettant de visualiser et explorer les collections.

## À propos de ce projet

Ce projet OpenJoconde est doublement expérimental :
1. Il vise à créer une application exploitant les données ouvertes des musées français (base Joconde)
2. Il s'agit également d'une expérience de développement entièrement piloté par intelligence artificielle

L'intégralité du code, de la documentation et de la structure du projet est développée par Claude (Anthropic), faisant de ce projet un cas d'étude sur les capacités actuelles des grands modèles de langage dans le développement logiciel complet.

Les fichiers de documentation interne (roadmap et charte de développement) sont utilisés comme référence et guides pour le développement par IA.

## Prérequis

- [.NET SDK 7.0](https://dotnet.microsoft.com/download/dotnet/7.0) ou supérieur
- [Node.js](https://nodejs.org/) (v16 ou supérieur)
- [npm](https://www.npmjs.com/) (v8 ou supérieur)
- [PostgreSQL](https://www.postgresql.org/) (v14 ou supérieur)

## Installation

### Cloner le dépôt

```bash
git clone https://github.com/DavidDuveau/openjoconde.git
cd openjoconde
```

### Configurer la base de données

1. Créer une base de données PostgreSQL nommée `openjoconde`
2. Exécuter le script de création dans `src/Backend/OpenJoconde.Infrastructure/Database/CreateDatabase.sql`
3. Exécuter le script de migration initiale dans `src/Backend/OpenJoconde.Infrastructure/Database/Migrations/Initial_Migration.sql`
4. Exécuter le script de mise à jour v1.1 dans `src/Backend/OpenJoconde.Infrastructure/Database/Migrations/Updates/UpdateSchema_v1.1.sql`
5. (Optionnel) Ajouter des données de test avec `src/Backend/OpenJoconde.Infrastructure/Database/Migrations/Updates/InsertTestData_v1.1.sql`
6. Mettre à jour la chaîne de connexion dans `src/Backend/OpenJoconde.API/appsettings.json`

### Backend (.NET)

```bash
cd src/Backend/OpenJoconde.API
dotnet restore
dotnet build
dotnet run
```

Le serveur backend sera accessible à l'adresse : `https://localhost:5001`

### Frontend (Vue.js)

```bash
cd src/Frontend
npm install
npm run serve
```

L'application frontend sera accessible à l'adresse : `http://localhost:8080`

## Structure du projet

```
openjoconde/
├── src/                        # Code source
│   ├── Backend/                # Backend .NET
│   │   ├── OpenJoconde.API/             # Couche API
│   │   ├── OpenJoconde.Core/            # Domaine métier
│   │   ├── OpenJoconde.Infrastructure/  # Infrastructure (accès aux données)
│   │   │   ├── Data/                    # Accès aux données
│   │   │   ├── Services/                # Services d'infrastructure
│   │   │   └── Database/                # Scripts de base de données
│   │   │       ├── Migrations/          # Scripts de migration
│   │   │       │   ├── Updates/         # Scripts de mise à jour
│   │   │       │   └── Initial_Migration.sql   # Migration initiale
│   │   │       └── CreateDatabase.sql   # Script initial
│   │   └── OpenJoconde.Tests/           # Tests
│   └── Frontend/               # Frontend Vue.js
│       ├── public/             # Fichiers statiques
│       └── src/                # Sources TypeScript et Vue
│           ├── assets/         # Ressources (images, etc.)
│           ├── components/     # Composants réutilisables
│           ├── views/          # Pages de l'application
│           ├── store/          # Store Pinia (état global)
│           ├── services/       # Services (API, etc.)
│           └── types/          # Types TypeScript
└── README.md                   # Documentation principale
```

## Fonctionnalités principales

- Importation et mise à jour automatique des données depuis data.gouv.fr
- Interface de recherche multicritères (artiste, époque, technique, etc.)
- Visualisation détaillée des œuvres d'art
- Navigation par musée, artiste, période ou technique
- API REST pour accéder aux données
- Statistiques d'utilisation et analyse des données

## Objectifs du projet

1. **Éducatif et culturel** : Faciliter l'accès au patrimoine artistique français pour le grand public et les chercheurs
2. **Technique** : Démontrer l'exploitation efficace des données ouvertes avec des technologies modernes
3. **Communautaire** : Encourager l'utilisation et la contribution au code open source

## Sources de données
- [Base Joconde sur data.gouv.fr](https://www.data.gouv.fr/fr/datasets/joconde-catalogue-collectif-des-collections-des-musees-de-france/)
- [Documentation de la base Joconde](https://www.culture.gouv.fr/Espace-documentation/Bases-de-donnees/Fiches-bases-de-donnees/Joconde-catalogue-collectif-des-collections-des-musees-de-France)

## État d'avancement du projet

### Phases terminées

1. **Phase 1 : Analyse et conception** ✅
   - Analyse complète des données Joconde
   - Conception de l'architecture (base de données, backend, frontend)
   - Définition des cas d'utilisation principaux

2. **Phase 2 : Mise en place de l'infrastructure** ✅
   - Structure du projet .NET avec Clean Architecture
   - Configuration de la base de données PostgreSQL
   - Scripts de création des tables et indexes
   - Initialisation du projet frontend Vue.js avec TypeScript

### Phase en cours

1. **Phase 3 : Développement du backend** ✅ (100%)
   - Service de téléchargement des données ✅
   - Parseur XML et JSON Joconde ✅
   - Service de peuplement de la base de données ✅
   - API REST (implémentation complète) ✅
   - Documentation OpenAPI/Swagger ✅
   - Optimisation de la base de données ✅
   - Mise à jour du schéma v1.1 avec fonctionnalités avancées ✅
   - Correction des bugs de compilation dans AutoSyncService et DataSyncLog ✅

2. **Phase 4 : Développement du frontend** 🔄 (40%)
   - Architecture et composants de base ✅
   - Interface de recherche (en cours) 🔄
   - Visualisation des œuvres (en cours) 🔄
   - Navigation thématique (en cours) 🔄

### Prochaines étapes

1. **Finalisation du Frontend**
   - Finalisation de l'interface de recherche avancée
   - Amélioration de la visualisation détaillée des œuvres
   - Implémentation complète de la navigation thématique

2. **Tests du Backend**
   - Mise en place des tests unitaires
   - Tests d'intégration des API
   - Tests de performance

3. **Phase 5 : Tests et optimisation** ⏳
   - Tests unitaires et d'intégration
   - Optimisation des performances
   - Mise en cache et optimisations

4. **Phase 6 : Déploiement et livraison** ⏳
   - Préparation des environnements de production
   - Documentation finale
   - Formation et transfert de connaissances

## Dernières mises à jour (25/03/2025)

### Support du format JSON
- Migration du format XML vers JSON pour l'importation des données
- Implémentation d'un parseur JSON pour la base Joconde
- Mise à jour de l'URL source vers l'API JSON officielle
- Maintien du support XML pour la rétrocompatibilité

### Améliorations de la base de données (v1.1)
- Ajout de tables pour la gestion des métadonnées et synchronisation
- Support pour les images multiples par œuvre
- Système de tags/mots-clés
- Optimisation des requêtes textuelles avec des index GIN
- Schéma amélioré pour la collecte de statistiques d'utilisation

### Corrections techniques dans le backend
- Ajout des repositories manquants (DomainRepository, TechniqueRepository, PeriodRepository)
- Mise à jour des services d'importation de données pour corriger les problèmes de nullabilité
- Mise à jour de la classe ImportReport avec les propriétés manquantes pour le suivi des entités (artistes, musées, domaines, etc.)
- Adaptation des services pour supporter le format JSON
- Implémentation du service de synchronisation automatique (AutoSyncService)
- Ajout des méthodes manquantes dans l'interface IDataImportService et leur implémentation
- Correction des problèmes de compilation dans les contrôleurs
- Amélioration de la robustesse du code avec utilisation systématique des types nullables (nullable reference types)
- Préparation à la mise à jour vers .NET 8 (actuellement en .NET 7)
- Correction des bugs dans la classe `DataSyncLog` avec ajout des propriétés manquantes
- Correction de l'erreur de compilation dans `AutoSyncService` avec ajout de la référence à Microsoft.Extensions.Configuration.Binder

### Problèmes techniques identifiés
- La version de .NET 7.0 utilisée n'est plus prise en charge et ne reçoit plus de mises à jour de sécurité
- Vulnérabilités identifiées dans les packages :
  - Npgsql 7.0.6 présente une vulnérabilité de gravité élevée (GHSA-x9vc-6hfv-hg8c)
  - System.Text.Json 7.0.0 présente une vulnérabilité de gravité élevée (GHSA-hh2w-p6rv-4g7w)

### Prochains développements
- Migration vers .NET 8 pour améliorer la sécurité et les performances
- Mise à jour des packages Npgsql et System.Text.Json pour corriger les vulnérabilités de sécurité
- Mise à jour des modèles C# pour refléter les nouvelles tables
- Extension des API REST pour exploiter les nouvelles fonctionnalités
- Interface frontend pour la visualisation des images multiples
- Tableau de bord d'administration pour suivre les synchronisations

## Licence

Ce projet est disponible sous licence MIT. Voir le fichier LICENSE pour plus de détails.