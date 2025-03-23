# OpenJoconde - Graphe de Connaissance du Projet

Ce document présente une analyse structurée du projet OpenJoconde, offrant une vision globale de son organisation, ses composants et leurs interactions.

## Vue d'ensemble du projet

Le projet OpenJoconde est une application web visant à exploiter les données ouvertes de la base Joconde (catalogue des collections des musées de France). Il s'articule autour d'une architecture moderne en trois parties:

1. Un backend en .NET Core
2. Une base de données PostgreSQL
3. Un frontend en Vue.js avec TypeScript

## Structure du projet

```
openjoconde/
├── src/
│   ├── Backend/                # Backend .NET
│   │   ├── OpenJoconde.API/             # Couche API
│   │   ├── OpenJoconde.Core/            # Domaine métier
│   │   └── OpenJoconde.Infrastructure/  # Infrastructure
│   └── Frontend/               # Frontend Vue.js
```

## Composants du Backend

### 1. OpenJoconde.API

**Rôle**: Exposer les données via des API REST, gérer les requêtes HTTP.

**Éléments clés**:
- **Controllers**: Gestion des points d'entrée de l'API
  - `ArtworksController.cs`: Gestion des requêtes liées aux œuvres d'art
  - `ImportController.cs`: Gestion des processus d'importation de données
  - `JocondeDataController.cs`: Accès aux données Joconde
- **Extensions**: Extensions utilitaires pour la configuration des services
  - `ServiceCollectionExtensions.cs`: Configuration de l'injection de dépendances

### 2. OpenJoconde.Core

**Rôle**: Définir le domaine métier et les interfaces des services.

**Éléments clés**:
- **Interfaces**: Contrats pour les classes d'implémentation
  - Repositories: `IArtistRepository`, `IArtworkRepository`, etc.
  - Services: `IDataImportService`, `IJocondeDataService`, etc.
  - Parseurs: `IJocondeXmlParser`
- **Models**: Entités du domaine
  - `Artist.cs`, `Artwork.cs`, `Domain.cs`, `Museum.cs`, etc.
  - Relations: `ArtworkArtist.cs`
- **Parsers**: Logique d'analyse des données
  - `JocondeXmlParser.cs`: Conversion des données XML en entités du domaine

### 3. OpenJoconde.Infrastructure

**Rôle**: Implémenter l'accès aux données et les services techniques.

**Éléments clés**:
- **Data**: Implémentation des repositories
  - `ArtworkRepository.cs`, `ArtworkRelationsRepository.cs`
  - `OpenJocondeDbContext.cs`: Contexte Entity Framework Core
- **Database**: Scripts et migrations de base de données
  - `CreateDatabase.sql`: Script initial
  - `Migrations/`: Scripts de migration
- **Services**: Implémentation des services
  - Services d'importation: `DataImportService.cs`, `AdvancedDataImportService.cs`
  - Services de données: `JocondeDataService.cs`, `JocondeXmlParserService.cs`

## Composants du Frontend

**Rôle**: Fournir une interface utilisateur pour explorer les données.

**Éléments clés**:
- **Components**: Composants réutilisables
  - `ArtworkCard.vue`: Carte d'affichage d'une œuvre d'art
- **Views**: Pages de l'application
  - `HomeView.vue`, `ArtworksView.vue`, `ArtworkDetailView.vue`
  - `SearchView.vue`, `AboutView.vue`, `NotFoundView.vue`
- **Store**: Gestion de l'état global
  - `artworkStore.ts`: Store pour les œuvres d'art
- **Services**: Services d'accès aux API
  - `api.ts`: Configuration des requêtes HTTP
  - `artworkService.ts`: Service spécifique aux œuvres d'art
- **Types**: Définitions TypeScript
  - `artwork.ts`, `models.ts`: Types pour les modèles de données

## Flux de données

1. **Importation des données**:
   - Source externe (data.gouv.fr) → `DataImportService` → `JocondeXmlParser` → Repositories → Base de données

2. **Consultation des œuvres**:
   - Frontend (`artworkService.ts`) → API (`ArtworksController`) → Repositories → Base de données → Retour des données

3. **Recherche multicritères**:
   - Interface utilisateur (`SearchView.vue`) → API (filtres) → Repositories (requêtes optimisées) → Résultats

## Modèle de données

Le modèle de données est structuré autour des entités principales suivantes:

1. **Artwork**: Œuvre d'art centrale
   - Propriétés: Référence, Numéro d'inventaire, Titre, Description, Dimensions, etc.

2. **Artist**: Artiste créateur
   - Propriétés: Nom, Prénom, Dates, Nationalité, Biographie

3. **Domain**: Domaine artistique
   - Propriétés: Nom, Description

4. **Technique**: Technique artistique
   - Propriétés: Nom, Description

5. **Period**: Période/Époque
   - Propriétés: Nom, Années de début/fin, Description

6. **Museum**: Musée
   - Propriétés: Nom, Ville, Département, Coordonnées, Description

7. **Relations**:
   - ArtworkArtist: Relation entre œuvres et artistes (avec rôle)
   - Autres relations N-N entre œuvres et entités (domaine, technique, période)

## Méthodologie de développement

Le projet suit une architecture Clean Architecture, avec:

1. **Séparation des préoccupations**:
   - API (présentation)
   - Core (domaine métier)
   - Infrastructure (technique)

2. **Injection de dépendances**:
   - Interfaces définies dans Core
   - Implémentations dans Infrastructure
   - Configuration dans API

3. **Conventions de nommage**:
   - PascalCase pour classes, interfaces, propriétés
   - camelCase pour variables et paramètres
   - Préfixe "I" pour interfaces
   - Suffixes explicites pour les classes (Repository, Service)

## Objectifs du projet

1. **Éducatif et culturel**: Faciliter l'accès au patrimoine artistique français
2. **Technique**: Démontrer l'exploitation des données ouvertes
3. **Communautaire**: Encourager la contribution open source

## Particularités du projet

1. **Développement assisté par IA**: Utilisation de Claude AI (Anthropic) pour:
   - Analyse des spécifications
   - Génération de code
   - Résolution de problèmes
   - Documentation

2. **Données ouvertes**: Exploitation de la base Joconde disponible sur data.gouv.fr

3. **Architecture moderne**:
   - Backend .NET avec Clean Architecture
   - Frontend Vue.js avec TypeScript
   - Base de données PostgreSQL optimisée

## Conclusion

Le projet OpenJoconde représente une application complète et moderne pour l'exploration des données culturelles françaises. Sa structure bien organisée facilite la maintenance et l'évolution du code, tout en offrant une expérience utilisateur intuitive pour accéder à un riche patrimoine artistique.