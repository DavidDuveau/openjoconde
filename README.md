# OpenJoconde

Application web pour explorer la base Joconde, le catalogue collectif des collections des musées de France, publiée en données ouvertes par le ministère de la Culture.

Le backend télécharge et importe les données Joconde dans SQL Server, puis les expose via une API REST. Le frontend permet de parcourir et rechercher les œuvres.

## Stack

- **Backend** : .NET 9, ASP.NET Core, Entity Framework Core 9 (SQL Server)
- **Frontend** : Vue 3, TypeScript, Pinia, Vue Router, Vue CLI 5
- **Base de données** : SQL Server 2019 ou supérieur (Express suffit)

## Source des données

- [Base Joconde sur data.culture.gouv.fr](https://data.culture.gouv.fr/explore/dataset/base-joconde-extrait/)
- Format utilisé : export JSON de l'API Opendatasoft (l'import XML reste supporté)

L'URL de la source se règle dans `JocondeData:SourceUrl` de `src/Backend/OpenJoconde.API/appsettings.json`.

## Prérequis

- [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) 18 ou supérieur
- SQL Server 2019 ou supérieur
- L'outil EF Core : `dotnet tool install --global dotnet-ef`

## Installation

### Base de données

1. Adapter la chaîne de connexion `DefaultConnection` dans `src/Backend/OpenJoconde.API/appsettings.json`. Par défaut : instance locale, base `OpenJoconde`, authentification Windows.
2. Créer le schéma avec la migration EF Core :

   ```bash
   cd src/Backend/OpenJoconde.API
   dotnet ef database update --project ../OpenJoconde.Infrastructure
   ```

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
| `POST /api/jocondedata/import` | Télécharge et importe les données Joconde |
| `POST /api/jsonimport/from-file` | Importe un fichier JSON Joconde |
| `GET /api/sync/status`, `POST /api/sync/start` | Suivi et lancement de la synchronisation |
| `GET /api/health` | État de l'API et de la base |

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
