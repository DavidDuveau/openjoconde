# OpenJoconde

Application web pour explorer la base Joconde, le catalogue collectif des collections des musées de France, publiée en données ouvertes par le ministère de la Culture.

Le backend télécharge et importe les données Joconde dans PostgreSQL, puis les expose via une API REST. Le frontend permet de parcourir et rechercher les œuvres.

## Stack

- **Backend** : .NET 10, ASP.NET Core, Entity Framework Core 10 (Npgsql, noms de tables et de colonnes en snake_case)
- **Frontend** : Vue 3, TypeScript, Pinia, Vue Router, Vite
- **Base de données** : PostgreSQL 15 ou supérieur (gratuit). La recherche plein texte utilise les fonctions intégrées de PostgreSQL, avec les extensions `citext` et `unaccent` fournies de série

## Source des données

- [Collections des musées de France : base Joconde sur data.gouv.fr](https://www.data.gouv.fr/datasets/collections-des-musees-de-france-base-joconde)
- Format utilisé : fichier CSV (environ 1,2 Go, plus d'un million de notices, séparateur `|`). L'ancien export JSON de l'API Opendatasoft n'existe plus ; l'import d'un fichier JSON reste supporté, le format est déduit de l'extension du fichier.

L'URL de la source se règle dans `Sync:SourceUrl` (synchronisations manuelle et automatique) de `src/Backend/OpenJoconde.API/appsettings.json`. Le parsing du fichier complet prend environ une minute et occupe environ 1,4 Go de mémoire.

La synchronisation est **incrémentale**. Le fichier est republié environ chaque semaine, et l'export ne donne pas toujours de date de mise à jour exploitable (`Date_de_mise_a_jour` est vide pour un quart des notices). Elle procède donc par empreinte :

- si la version (ETag) de la source est celle de la dernière synchronisation réussie, rien n'est téléchargé (statut `UpToDate`) ;
- sinon le fichier est lu en entier, et l'empreinte de chaque notice est comparée à celle de la base : seules les œuvres nouvelles ou modifiées (et les artistes, domaines, techniques, périodes et musées qu'elles utilisent) sont écrites ;
- les œuvres que la source ne contient plus sont supprimées, avec leurs liens. Par sécurité, rien n'est supprimé si la source semble tronquée : plus de 10 % de la base disparaîtrait, ou des notices ont été rejetées à la lecture. La synchronisation est alors marquée `CompletedWithErrors`.

Une base importée avant l'arrivée des empreintes est réécrite une fois en entier, puis suit le régime incrémental.

Les données du ministère de la Culture se réutilisent à condition d'en citer l'origine : le pied de page, la page « À propos » (ancre `#sources`), chaque fiche d'œuvre (référence de la notice, lieu de conservation, renvoi vers POP, Licence Ouverte 2.0) et chaque fiche de musée (Muséofile) portent cette attribution. Les images de POP, soumises à des droits de tiers, ne sont pas affichées.

Les musées sont complétés par la base [Muséofile](https://www.data.gouv.fr/datasets/musees-de-france-base-museofile) (Musées de France, Licence Ouverte 2.0) : adresse, code postal, téléphone, site Internet et histoire, rapprochés par le code Muséofile (`museum.code`). Le fichier (`Sync:MuseofileSourceUrl`, environ 3 Mo) est appliqué à chaque synchronisation, même quand Joconde n'a pas changé ; une panne de Muséofile ne fait pas échouer la synchronisation des œuvres. La ville, le département, la région et les coordonnées de Joconde sont gardés : Muséofile ne comble que les manques, et ne touche jamais à la ville, clé de rattachement des musées à l'import.

## Prérequis

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) 22.12 ou supérieur (22 LTS ; `nvm use 22` avec nvm-windows, voir `.nvmrc`)
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

   La migration `InitialCreate` crée les tables, leurs index, la configuration de recherche `french_unaccent` et les déclencheurs `updated_at` ; `RecherchePrefixeReference` ajoute les index de recherche par début de référence ; `TriTitre` ajoute la clé de tri des titres (sans la ponctuation initiale) ; `ChampsDroitsEtLien` ajoute les droits d'auteur et le lien vers le site du musée ; `TriDate` ajoute la fonction SQL `oj_creation_year` et la colonne calculée `creation_year` du tri par date. Il n'y a pas de script SQL à appliquer ensuite.

   Les tests d'intégration créent chacun une base jetable. Ils se connectent à `Host=localhost;Port=5432;Username=postgres` (mot de passe lu dans `pgpass.conf`), ou à la connexion donnée par la variable d'environnement `OPENJOCONDE_TEST_POSTGRES` (serveur sans nom de base).

### Backend

```bash
cd src/Backend/OpenJoconde.API
dotnet run
```

L'API écoute sur `https://localhost:5001` et `http://localhost:5000`. Swagger est disponible sur `https://localhost:5001/swagger` en développement.

Les réglages des synchronisations tiennent dans la seule section `Sync` de `appsettings.json` (`SourceUrl`, `MuseofileSourceUrl`, `TempDirectory`, `CheckForUpdatesOnStartup`, `IntervalHours`) ; ils sont contrôlés au démarrage, et une adresse invalide ou un intervalle absurde arrête le serveur avec un message qui nomme la clé fautive.

Si `Sync:CheckForUpdatesOnStartup` vaut `true`, un service d'arrière-plan synchronise les données au démarrage puis toutes les `Sync:IntervalHours` heures, par le même chemin que `POST /api/sync/start`. Chaque synchronisation laisse une ligne dans la table `data_sync_log` ; une synchronisation interrompue par l'arrêt du serveur est marquée `Interrupted` au démarrage suivant.

### Frontend

```bash
cd src/Frontend
npm install
npm run serve
```

L'application est servie sur `http://localhost:8080` (Vite ; `npm run build` contrôle les types avec `vue-tsc` puis construit, `npm run lint` lance ESLint). L'URL de l'API vaut `https://localhost:5001/api` par défaut. Pour la changer, créer `src/Frontend/.env.local` :

```
VUE_APP_API_URL=https://localhost:5001/api
```

### Tests du front

`npm test` (dans `src/Frontend`) lance les tests unitaires avec Vitest et jsdom : utilitaires (`title`, `rights`), composants (`ArtworkCard`, `FilterPicker`), store Pinia et client HTTP. Ils vivent dans `src/Frontend/tests/unit` et tournent en quelques secondes, sans API ni base.

### Tests de bout en bout

`npm run e2e` (dans `src/Frontend`) pilote un vrai navigateur (Playwright, Chromium) contre une pile jetable : une base `openjoconde_e2e` créée puis supprimée, l'API sur `http://localhost:5101`, le front construit et servi sur `http://localhost:8101`. Elle ne touche ni à la base ni aux ports de développement. Prérequis : PostgreSQL local (mot de passe de `postgres` dans `pgpass.conf`, ou variable `E2E_PG_PASSWORD`), `dotnet tool restore`, puis `npx playwright install chromium` une fois. La CI la rejoue à chaque fusion. Les origines du front autorisées par l'API se complètent par `Cors:Origins`.

Images : l'export Joconde annonce une image (`Presence_image`, enregistrée dans `artwork.has_image`) sans en donner l'adresse. La fiche d'une œuvre renvoie donc vers sa notice sur la plateforme POP (« Voir l'image sur POP » quand une image est annoncée), et les cartes repèrent ces notices. Le début de l'adresse POP se règle par `VUE_APP_POP_URL` (défaut `https://www.pop.culture.gouv.fr/notice/joconde/`). Une base importée avant cette colonne la remplit à la synchronisation suivante, qui réécrit toutes les notices une fois.

Le contexte des artistes (biographie, nationalité, courant) vient de [Wikidata](https://www.wikidata.org) (CC0) et de [Wikipédia](https://fr.wikipedia.org) (extraits sous CC BY-SA 4.0, cités avec le lien de l'article sur la fiche), car Joconde n'en donne pas et aucune base publique française n'en fournit. L'API s'en charge elle-même, en arrière-plan, quand l'enrichissement est activé (`Enrichment:Enabled`, variable d'environnement `Enrichment__Enabled=true` ; désactivé par défaut puisqu'il appelle des services externes) : par lots, les artistes de 3 œuvres ou plus et d'année de naissance connue, les plus fournis d'abord, avec une pause entre deux requêtes. Un rapprochement n'est retenu que si une seule personne a le même nom et les mêmes années de naissance et de décès, avec un article en français ; au moindre doute, l'artiste reste sans biographie, et n'est pas réexaminé (`artist.enrichment_attempted_at`). Une panne de Wikimedia laisse l'artiste pour plus tard. Les réglages (taille des lots, pauses, nombre minimal d'œuvres) sont dans la section `Enrichment` de `appsettings.json`. Le script `scripts/enrichir-artistes.py` fait la même chose hors ligne, en une fois, vers un fichier SQL (tests : `python -m unittest scripts/test_enrichir_artistes.py`).

Les scénarios (`src/Frontend/e2e/recherche.spec.ts`) tirent leurs valeurs attendues du jeu de 24 notices de `e2e/jeu.ts` (titres avec accents, « œ » et notices sans titre), jamais de l'API qu'ils vérifient ; l'import du jeu est contrôlé avant tout scénario. Tout scénario échoue si la page lève une erreur JavaScript ou écrit une erreur dans la console (`e2e/fixtures.ts`). Les éléments de page visés portent un attribut `data-testid`. Chaque nouvelle fonction du front ajoute ou met à jour un scénario.

### Tout lancer

Depuis VS Code :

- **Run Task → `start-all`** : lance le backend et le frontend en parallèle.
- **F5 → Full Stack** : même chose avec le débogueur attaché au backend et au navigateur.

## Versions et releases

Le produit a **une seule version** (SemVer), la même pour le back et le front : `<Version>` dans `src/Backend/Directory.Build.props` et `version` dans `src/Frontend/package.json`. Elle est dans les builds : `GET /api/health` l'annonce (`version`) et le pied de page du site l'affiche. Un test unitaire du front vérifie que les deux numéros sont identiques.

Personne ne la modifie à la main. À chaque fusion dans `main`, le workflow `release.yml` ([release-please](https://github.com/googleapis/release-please)) lit les commits au format `feat:` / `fix:` (un `!` ou `BREAKING CHANGE:` pour une rupture) et tient à jour une pull request « chore(main): release x.y.z » qui monte les deux numéros et alimente `CHANGELOG.md`. La fusionner pose le tag `vX.Y.Z` et publie la release GitHub. Tant que la version est en `0.x`, un `feat:` monte le numéro mineur.

### Écrire un changelog lisible

Le changelog n'est pas déduit du code : il reprend **le sujet des commits**. Son intention vient donc du message, et lui seul.

- **Écrire le sujet comme l'effet pour l'utilisateur**, à l'infinitif ou au nominal, avec la référence de la story : `feat(export): télécharger les données d'une œuvre en JSON et CSV (US-EXPORT-01)`. Pas « ajout du composant X ».
- **Un `type(portée):` par commit**, la portée étant le thème (`navigation`, `recherche`, `visualisation`, `export`, `compte`, `tech`). Elle apparaît en gras devant la ligne. Types visibles dans le changelog : `feat` (Nouveautés), `fix` (Corrections), `perf`, `revert`, `docs` ; `refactor`, `test`, `build`, `ci`, `chore` n'y figurent pas.
- **Le titre de la pull request n'a pas de préfixe** (`Favoris dans le navigateur (US-COMPTE-04)`) : le commit de fusion le reprend, et avec `feat:` le changement serait compté deux fois. La CI refuse un titre qui en porte un (sauf la PR de release).
- **Corriger une ligne après coup** : dans la description de la PR fusionnée, un bloc `BEGIN_COMMIT_OVERRIDE` … `END_COMMIT_OVERRIDE` contenant les lignes `feat: nouveau texte` remplace les commits de cette PR dans le prochain changelog.

Les checks de la CI ne se lancent pas sur la pull request de release : GitHub n'exécute pas de workflow déclenché par le jeton d'un autre workflow. Elle ne change que des numéros de version, et la fusion se fait après relecture.

## API

| Route | Rôle |
|---|---|
| `GET /api/artworks` | Œuvres, paginées (`page`, `pageSize` ≤ 100), avec recherche (`search`), filtres cumulables (`museumId`, `artistId`, `domainId`, `techniqueId`, `periodId`) et tri (`sortBy`, voir ci-dessous) |
| `GET /api/artworks/{id}` | Fiche complète : champs, musée, artistes avec rôle, domaines, techniques, périodes, droits d'auteur et lien vers le site du musée |
| `GET /api/artists`, `GET /api/artists/{id}`, `GET /api/museums` | Artistes (avec leur nombre d'œuvres ; leurs œuvres se lisent par `GET /api/artworks?artistId=`) et musées, paginés, avec recherche par nom (musées : aussi ville et département) |
| `GET /api/domains`, `GET /api/techniques`, `GET /api/periods` | Listes de référence paginées, avec recherche par nom (`name`) : propositions des filtres du front |
| `POST /api/import/from-file` | Importe un fichier Joconde envoyé par l'appelant (CSV ou JSON selon l'extension) |
| `GET /api/sync/status`, `POST /api/sync/start` | Statut de la dernière synchronisation ; lancement (télécharge la source configurée et l'importe ; `?force=true` pour télécharger même si la source n'a pas changé). Une seule à la fois : `409` si une autre tourne |
| `POST /api/sync/cancel/{id}`, `GET /api/sync/history` | Annulation d'une synchronisation en cours ; historique avec messages d'erreur (clé d'API) |
| `POST /api/museofile/sync` | Applique le fichier Muséofile aux musées (adresse, téléphone, site, histoire) sans synchroniser Joconde ; rend le nombre de musées lus, retrouvés, mis à jour et sans correspondance (clé d'API) |
| `GET /api/health` | État de l'API et de la base |

Tris de `GET /api/artworks` (`sortBy`) : `title` (A-Z, par défaut), `title_desc`, `date` (plus ancien d'abord ; la première année est lue dans le texte libre de la date, « 4e quart 19e siècle » donne 1875), `date_desc`, `relevance` (pertinence ; demande une recherche par mots) et `artist` (premier artiste ; demande une recherche ou un filtre). Les œuvres sans titre, sans date lisible ou sans artiste viennent en dernier. Un tri inconnu ou impossible répond `400`.

Les corps de `POST` et `PUT` sur les musées et les artistes ne portent que les champs de la fiche (`name`/`lastName`, adresse, etc.) : l'identifiant, les dates et les œuvres liées sont gérés par le serveur, et un champ vide, trop long ou absurde (coordonnées hors du globe) répond `400`.

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
│   ├── OpenJoconde.Core/            # Modèles, DTO de sortie, interfaces, parsers Joconde
│   └── OpenJoconde.Infrastructure/  # DbContext, repositories, services d'import
│       ├── Configuration/           # Options liées à la configuration (section Sync)
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
