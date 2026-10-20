# OpenJoconde

Application web pour explorer la base Joconde, le catalogue collectif des collections des musées de France, publiée en données ouvertes par le ministère de la Culture.

Le backend télécharge et importe les données Joconde dans SQL Server, puis les expose via une API REST. Le frontend permet de parcourir et rechercher les œuvres.

## Stack

- **Backend** : .NET 10, ASP.NET Core, Entity Framework Core 10 (SQL Server)
- **Frontend** : Vue 3, TypeScript, Pinia, Vue Router, Vue CLI 5
- **Base de données** : SQL Server 2019 ou supérieur (Express suffit)

## Source des données

- [Collections des musées de France : base Joconde sur data.gouv.fr](https://www.data.gouv.fr/datasets/collections-des-musees-de-france-base-joconde)
- Format utilisé : fichier CSV (environ 1,2 Go, plus d'un million de notices, séparateur `|`). L'ancien export JSON de l'API Opendatasoft n'existe plus ; l'import d'un fichier JSON reste supporté, le format est déduit de l'extension du fichier.

L'URL de la source se règle dans `JocondeData:SourceUrl` (synchronisation manuelle) et `JocondeSync:DataSourceUrl` (synchronisation automatique) de `src/Backend/OpenJoconde.API/appsettings.json`. Le parsing du fichier complet prend environ une minute et occupe environ 1,4 Go de mémoire.

## Prérequis

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) 18 ou supérieur
- SQL Server 2019 ou supérieur
- L'outil EF Core : `dotnet tool install --global dotnet-ef`

## Installation

### Base de données

1. Adapter la chaîne de connexion `DefaultConnection` dans `src/Backend/OpenJoconde.API/appsettings.json`. Par défaut : instance locale, base `OpenJoconde`, authentification Windows.
2. Créer le schéma avec la migration EF Core :

   ```bash
   dotnet tool restore
   cd src/Backend/OpenJoconde.API
   dotnet ef database update --project ../OpenJoconde.Infrastructure
   ```

   Une base déjà peuplée avant la migration `AddArtworkMuseum` garde ses œuvres sans musée tant qu'on ne relance pas l'import (synchronisation) : le rattachement est écrit à l'import.

3. Appliquer les scripts SQL Server complémentaires de `src/Backend/OpenJoconde.Infrastructure/Database/Migrations/`, dans l'ordre :
   - `SqlServer_Initial_Migration.sql` (triggers `UpdatedAt`)
   - `Updates/SqlServer_UpdateSchema_v1.1.sql`
   - `Updates/AddTimestamps_v1.2.sql`
   - `Updates/SqlServer_UpdateSchema_v1.2.sql`
   - `Updates/SqlServer_UpdateSchema_v1.3.sql` (élargissement des colonnes)

### Backend

```bash
cd src/Backend/OpenJoconde.API
dotnet run
```

L'API écoute sur `https://localhost:5001` et `http://localhost:5000`. Swagger est disponible sur `https://localhost:5001/swagger` en développement.

Si `JocondeData:CheckForUpdatesOnStartup` vaut `true`, un service d'arrière-plan synchronise les données au démarrage puis toutes les `JocondeSync:IntervalHours` heures.

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
| `GET /api/sync/status`, `POST /api/sync/start` | Suivi et lancement de la synchronisation (télécharge la source configurée et l'importe) |
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
│       ├── Database/Migrations/     # Scripts SQL Server complémentaires
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
