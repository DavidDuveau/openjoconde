# Nettoyage de l'architecture du backend

Ce projet contient plusieurs implémentations redondantes de services qui remplissent des fonctions similaires. Pour clarifier l'architecture et faciliter la maintenance, nous devons supprimer les fichiers suivants :

1. `BackgroundService.cs.bak` - Fichier de sauvegarde inutile
2. `AdvancedJocondeImportService.cs` - Fonctionnalités couvertes par d'autres services
3. `DataImportService.cs` - Remplacé par AdvancedDataImportService

## Actions réalisées

1. Renommage de `JocondeDataServiceAlt` en `JocondeDataService` (service principal)
2. Mise à jour des références dans `ServiceCollectionExtensions.cs` 
3. Suppression des fichiers redondants

## Architecture simplifiée des services

Après nettoyage, l'architecture des services est la suivante :

1. `JocondeDataService` - Implémente `IJocondeDataService`, gère le téléchargement, l'analyse et l'importation basique
2. `JocondeXmlParserService` - Implémente `IJocondeXmlParser`, service spécialisé pour le parsing XML
3. `AdvancedDataImportService` - Implémente `IDataImportService`, gère l'importation avancée
4. `AutoSyncService` - Service de synchronisation automatique

Cette structure est plus claire, évite la duplication et facilite la maintenance.
