# Scripts de base de données OpenJoconde

Ce répertoire contient les scripts SQL nécessaires pour créer et maintenir la base de données PostgreSQL utilisée par l'application OpenJoconde.

## Structure

- `CreateDatabase.sql` - Script principal pour créer toutes les tables, index et contraintes
- `Migrations/` - Dossier contenant les scripts de migration pour les mises à jour incrémentales
  - `Initial_Migration.sql` - Migration initiale avec les triggers et données de base
  - `Updates/` - Dossier contenant les scripts de mise à jour incrémentale
    - `UpdateSchema_v1.1.sql` - Mise à jour du schéma v1.1
    - `InsertTestData_v1.1.sql` - Données de test supplémentaires

## Comment utiliser ces scripts

### Création initiale de la base de données

1. Créer une base de données PostgreSQL nommée `openjoconde`
   ```sql
   CREATE DATABASE openjoconde;
   ```

2. Exécuter le script de création de la base de données
   ```bash
   psql -U postgres -d openjoconde -f CreateDatabase.sql
   ```

3. Exécuter le script de migration initiale pour ajouter les triggers et données de base
   ```bash
   psql -U postgres -d openjoconde -f Migrations/Initial_Migration.sql
   ```

### Mise à jour de la base de données

1. Exécuter les scripts de mise à jour du schéma
   ```bash
   psql -U postgres -d openjoconde -f Migrations/Updates/UpdateSchema_v1.1.sql
   ```

2. (Optionnel) Ajouter des données de test supplémentaires
   ```bash
   psql -U postgres -d openjoconde -f Migrations/Updates/InsertTestData_v1.1.sql
   ```

### Utilisation avec Entity Framework Core

Les scripts sont fournis comme référence et pour une utilisation manuelle si nécessaire. 
Dans la plupart des cas, Entity Framework Core gérera automatiquement la création et la migration 
de la base de données à partir des modèles définis dans `OpenJoconde.Core`.

Pour utiliser Entity Framework Core pour les migrations :

```bash
cd src/Backend/OpenJoconde.API
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Conventions de nommage

- Tables : Noms au singulier (Artwork, Artist, Museum)
- Clés primaires : Id (UUID)
- Clés étrangères : EntityId (ex: ArtistId)
- Index : idx_table_colonne

## Extensions PostgreSQL requises

- `pg_trgm` - Pour la recherche de texte partielle et les index GIN

## Mise à jour v1.1 - Améliorations

La mise à jour v1.1 ajoute les fonctionnalités suivantes :

1. Nouvelles tables pour les fonctionnalités avancées :
   - `DataSyncLog` - Suivi des opérations de synchronisation avec les sources de données
   - `UsageStatistics` - Collecte de statistiques d'utilisation pour l'analyse
   - `ArtworkImage` - Gestion des images multiples pour une œuvre
   - `JocondeMetadata` - Métadonnées sur la source des données Joconde
   - `Tag` et `ArtworkTag` - Système de tags pour catégoriser les œuvres

2. Optimisations des performances :
   - Ajout d'index GIN pour les recherches textuelles
   - Ajustement des types et longueurs de champs

3. Données de test supplémentaires pour faciliter le développement

## Notes importantes

- Tous les changements de schéma doivent passer par une migration
- Toutes les tables principales incluent les champs CreatedAt, UpdatedAt et IsDeleted
- Les triggers automatiques mettent à jour le champ UpdatedAt lors des modifications
- Les mises à jour du schéma doivent être reflétées dans les modèles C# et le DbContext
