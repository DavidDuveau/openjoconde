# Migration v1.2 - Ajout des timestamps

## Date : 2025-05-25

## Description
Cette migration ajoute les colonnes `CreatedAt` et `UpdatedAt` aux tables suivantes :
- Artwork (seulement CreatedAt, UpdatedAt existait déjà)
- Artist
- Domain
- Technique
- Period
- Museum

## Raison
Les erreurs de compilation dans le service `StreamingJocondeJsonParserService` nécessitaient ces propriétés dans les modèles. Ces timestamps sont utiles pour :
- Tracer l'historique des modifications
- Synchroniser les données
- Auditer les changements

## Instructions d'exécution
1. Se connecter à SQL Server Management Studio
2. Ouvrir la base de données OpenJoconde
3. Exécuter le script `AddTimestamps_v1.2.sql`

## Rollback
Si nécessaire, les colonnes peuvent être supprimées avec :
```sql
ALTER TABLE [dbo].[Artwork] DROP COLUMN [CreatedAt];
ALTER TABLE [dbo].[Artist] DROP COLUMN [CreatedAt], [UpdatedAt];
ALTER TABLE [dbo].[Domain] DROP COLUMN [CreatedAt], [UpdatedAt];
ALTER TABLE [dbo].[Technique] DROP COLUMN [CreatedAt], [UpdatedAt];
ALTER TABLE [dbo].[Period] DROP COLUMN [CreatedAt], [UpdatedAt];
ALTER TABLE [dbo].[Museum] DROP COLUMN [CreatedAt], [UpdatedAt];
```

## Impact
- Les nouvelles insertions auront automatiquement les timestamps
- Les données existantes ont été mises à jour avec la date de migration
- Aucun impact sur les fonctionnalités existantes
