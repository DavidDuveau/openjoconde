# Projet OpenJoconde

Application d'exploitation des données ouvertes des musées français de la base Joconde.

## Description

Le projet OpenJoconde vise à créer une application web permettant d'explorer les riches collections des musées français référencées dans la base Joconde. L'objectif est de rendre plus accessibles et exploitables les données publiques des œuvres d'art et objets patrimoniaux conservés dans les musées de France.

L'application se compose de trois composants principaux :

1. **Backend (.NET)** : Service qui récupère, traite et stocke les données depuis data.gouv.fr, et expose une API REST pour le frontend.

2. **Base de données (PostgreSQL)** : Stockage relationnel optimisé pour les requêtes de recherche sur les différents attributs des œuvres.

3. **Frontend (Vue.js TypeScript)** : Interface utilisateur intuitive permettant de visualiser et explorer les collections.

## Contexte de développement

Ce projet est développé avec l'assistance de Claude AI (Anthropic), un modèle de langage avancé qui aide à la génération de code, la résolution de problèmes, et la conception d'architecture. Cette approche permet :

- Une analyse approfondie des spécifications du projet
- La génération rapide de code de haute qualité suivant les bonnes pratiques
- Une résolution proactive des problèmes potentiels de développement
- Une documentation claire et complète

Les fichiers de documentation interne (roadmap et charte de développement) sont utilisés comme référence pour Claude AI et ne sont pas versionnés dans le dépôt public.

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
4. Mettre à jour la chaîne de connexion dans `src/Backend/OpenJoconde.API/appsettings.json`

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
├── docs/                       # Documentation interne (non versionné)
├── src/                        # Code source
│   ├── Backend/                # Backend .NET
│   │   ├── OpenJoconde.API/             # Couche API
│   │   ├── OpenJoconde.Core/            # Domaine métier
│   │   ├── OpenJoconde.Infrastructure/  # Infrastructure (accès aux données)
│   │   │   ├── Data/                    # Accès aux données
│   │   │   ├── Services/                # Services d'infrastructure
│   │   │   └── Database/                # Scripts de base de données
│   │   │       ├── Migrations/          # Scripts de migration
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

## Objectifs du projet

1. **Éducatif et culturel** : Faciliter l'accès au patrimoine artistique français pour le grand public et les chercheurs
2. **Technique** : Démontrer l'exploitation efficace des données ouvertes avec des technologies modernes
3. **Communautaire** : Encourager l'utilisation et la contribution au code open source

## Sources de données
- [Base Joconde sur data.gouv.fr](https://www.data.gouv.fr/fr/datasets/joconde-catalogue-collectif-des-collections-des-musees-de-france/)
- [Documentation de la base Joconde](https://www.culture.gouv.fr/Espace-documentation/Bases-de-donnees/Fiches-bases-de-donnees/Joconde-catalogue-collectif-des-collections-des-musees-de-France)

## Licence

Ce projet est disponible sous licence MIT. Voir le fichier LICENSE pour plus de détails.