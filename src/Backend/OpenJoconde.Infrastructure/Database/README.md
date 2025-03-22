# Scripts de base de données OpenJoconde

Ce répertoire contient les scripts SQL nécessaires pour créer et maintenir la base de données PostgreSQL utilisée par l'application OpenJoconde.

## Structure

- `CreateDatabase.sql` - Script principal pour créer toutes les tables, index et contraintes
- `Migrations/` - Dossier contenant les scripts de migration pour les mises à jour incrémentales
  - `Initial_Migration.sql` - Migration initiale avec les triggers et données de base

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

## Notes importantes

- Tous les changements de schéma doivent passer par une migration
- Toutes les tables principales incluent les champs CreatedAt, UpdatedAt et IsDeleted
- Les triggers automatiques mettent à jour le champ UpdatedAt lors des modifications
