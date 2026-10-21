# OpenJoconde

Application web pour explorer la base Joconde, le catalogue collectif des collections des musées de France, publiée en données ouvertes par le ministère de la Culture.

Le backend télécharge et importe les données Joconde dans PostgreSQL, puis les expose via une API REST. Le frontend permet de parcourir et rechercher les œuvres.

## Stack

- **Backend** : .NET 10, ASP.NET Core, Entity Framework Core 10 (Npgsql, noms de tables et de colonnes en snake_case)
- **Frontend** : Vue 3, TypeScript, Pinia, Vue Router, Vue CLI 5
- **Base de données** : PostgreSQL 15 ou supérieur (gratuit). La recherche plein texte utilise les fonctions intégrées de PostgreSQL, avec les extensions `citext` et `unaccent` fournies de série

## Source des données

- [Collections des musées de France : base Joconde sur data.gouv.fr](https://www.data.gouv.fr/datasets/collections-des-musees-de-france-base-joconde)
- Format utilisé : fichier CSV (environ 1,2 Go, plus d'un million de notices, séparateur `|`). L'ancien export JSON de l'API Opendatasoft n'existe plus ; l'import d'un fichier JSON reste supporté, le format est déduit de l'extension du fichier.

L'URL de la source se règle dans `JocondeData:SourceUrl` (synchronisations manuelle et automatique) de `src/Backend/OpenJoconde.API/appsettings.json`. Le parsing du fichier complet prend environ une minute et occupe environ 1,4 Go de mémoire.

La synchronisation est **incrémentale**. Le fichier est republié environ chaque semaine, et l'export ne donne pas toujours de date de mise à jour exploitable (`Date_de_mise_a_jour` est vide pour un quart des notices). Elle procède donc par empreinte :

- si la version (ETag) de la source est celle de la dernière synchronisation réussie, rien n'est téléchargé (statut `UpToDate`) ;
- sinon le fichier est lu en entier, et l'empreinte de chaque notice est comparée à celle de la base : seules les œuvres nouvelles ou modifiées (et les artistes, domaines, techniques, périodes et musées qu'elles utilisent) sont écrites ;
- les œuvres que la source ne contient plus sont supprimées, avec leurs liens. Par sécurité, rien n'est supprimé si la source semble tronquée : plus de 10 % de la base disparaîtrait, ou des notices ont été rejetées à la lecture. La synchronisation est alors marquée `CompletedWithErrors`.

Une base importée avant l'arrivée des empreintes est réécrite une fois en entier, puis suit le régime incrémental.

## Prérequis

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) 18 ou supérieur
- PostgreSQL 15 ou supérieur
- L'outil EF Core, épinglé dans `dotnet-tools.json` (`dotnet tool restore`)

## Installation

### Base de données

1. Créer un rôle et une base (dans `psql`, en administrateur). Le collationnement ICU français donne un tri correct des titres :

   ```sql
   CREATE ROLE openjoconde LOGIN PASSWORD '<mot de passe>';
   CREATE DATABASE openjoconde OWNER openjoconde TEMPLATE template0 ENCODING 'UTF8' LOCALE_PROVIDER icu ICU_LOCALE 'fr-FR';
   ```

2. Adapter si besoin la chaîne de connexion `DefaultConnection` dans `src/Backend/OpenJoconde.API/appsettings.json`. Elle ne contient pas de mot de passe : Npgsql le lit dans le fichier `pgpass.conf` (`%APPDATA%\postgresql\pgpass.conf` sous Windows, `~/.pgpass` ailleurs), avec une ligne `localhost:5432:openjoconde:openjoconde:<mot de passe>`. En production, utiliser la variable d'environnement `ConnectionStrings__DefaultConnection`.
3. Créer le schéma avec la migration EF Core :

   ```bash
   dotnet tool restore
   cd src/Backend/OpenJoconde.API
   dotnet ef database update --project ../OpenJoconde.Infrastructure
   ```

   La migration `InitialCreate` crée les tables, leurs index, la configuration de recherche `french_unaccent` et les déclencheurs `updated_at` ; `RecherchePrefixeReference` ajoute les index de recherche par début de référence. Il n'y a pas de script SQL à appliquer ensuite.

   Les tests d'intégration créent chacun une base jetable. Ils se connectent à `Host=localhost;Port=5432;Username=postgres` (mot de passe lu dans `pgpass.conf`), ou à la connexion donnée par la variable d'environnement `OPENJOCONDE_TEST_POSTGRES` (serveur sans nom de base).

### Backend

```bash
cd src/Backend/OpenJoconde.API
dotnet run
```

L'API écoute sur `https://localhost:5001` et `http://localhost:5000`. Swagger est disponible sur `https://localhost:5001/swagger` en développement.

Si `JocondeData:CheckForUpdatesOnStartup` vaut `true`, un service d'arrière-plan synchronise les données au démarrage puis toutes les `JocondeSync:IntervalHours` heures, par le même chemin que `POST /api/sync/start`. Chaque synchronisation laisse une ligne dans la table `data_sync_log` ; une synchronisation interrompue par l'arrêt du serveur est marquée `Interrupted` au démarrage suivant.

### Frontend

```bash
cd src/Frontend
npm install
npm run serve
```

L'application est servie sur `http://localhost:8080`. L'URL de l'API vaut `https://localhost:5001/api` par défaut. Pour la changer, créer `src/Frontend/.env.local` :

```
VUE_APP_API_URL=https://localhost:5001/api
```

### Tout lancer

Depuis VS Code :

- **Run Task → `start-all`** : lance le backend et le frontend en parallèle.
- **F5 → Full Stack** : même chose avec le débogueur attaché au backend et au navigateur.

## API

| Route | Rôle |
|---|---|
| `GET /api/artworks`, `GET /api/artworks/{id}` | Œuvres, paginées, avec recherche (`search`) |
| `GET /api/artists`, `GET /api/museums` | Artistes et musées |
| `POST /api/import/from-file` | Importe un fichier Joconde envoyé par l'appelant (CSV ou JSON selon l'extension) |
| `GET /api/sync/status`, `POST /api/sync/start` | Statut de la dernière synchronisation ; lancement (télécharge la source configurée et l'importe ; `?force=true` pour télécharger même si la source n'a pas changé). Une seule à la fois : `409` si une autre tourne |
| `POST /api/sync/cancel/{id}`, `GET /api/sync/history` | Annulation d'une synchronisation en cours ; historique avec messages d'erreur (clé d'API) |
| `GET /api/health` | État de l'API et de la base |

Les routes d'écriture et d'import (`POST`, `PUT`, `DELETE`) exigent l'en-tête `X-Api-Key` égal à `Security:ApiKey`. Tant que cette clé n'est pas configurée, ces routes répondent `401`. Elles sont aussi limitées à 20 requêtes par minute et par adresse IP (`429` au-delà). Définir la clé hors du dépôt, par exemple :

```bash
dotnet user-secrets set "Security:ApiKey" "<clé>" --project src/Backend/OpenJoconde.API
```

La liste complète est dans Swagger.

## Structure

```
src/
├── Backend/
│   ├── OpenJoconde.API/             # Contrôleurs, configuration, point d'entrée
│   ├── OpenJoconde.Core/            # Modèles, interfaces, parsers Joconde
│   └── OpenJoconde.Infrastructure/  # DbContext, repositories, services d'import
│       ├── Data/                    # DbContext et repositories
│       ├── Migrations/              # Migrations EF Core
│       └── Services/                # Téléchargement, parsing, import, synchronisation
└── Frontend/
    └── src/
        ├── components/
        ├── router/
        ├── services/                # Client HTTP de l'API
        ├── store/                   # Stores Pinia
        ├── types/
        └── views/
```
