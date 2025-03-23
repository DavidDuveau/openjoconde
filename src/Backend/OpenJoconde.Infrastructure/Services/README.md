# Services d'infrastructure d'OpenJoconde

Ce répertoire contient les services d'infrastructure pour l'application OpenJoconde. Chaque service implémente une interface définie dans le projet Core.

## Architecture des services

### Services pour les données Joconde

- **JocondeDataService** - Service principal pour télécharger, analyser et importer les données Joconde. Implémente `IJocondeDataService` et fournit les opérations de base pour la gestion des données.

- **JocondeXmlParserService** - Service spécialisé pour l'analyse des fichiers XML Joconde. Implémente `IJocondeXmlParser` et fournit une analyse détaillée qui extrait toutes les entités (œuvres, artistes, domaines, etc.).

### Services d'importation de données

- **AdvancedDataImportService** - Service avancé pour l'importation des données depuis le résultat du parsing vers la base de données. Implémente `IDataImportService` et gère l'importation des entités avec des optimisations de performance.

### Services de synchronisation

- **AutoSyncService** - Service en arrière-plan qui vérifie périodiquement les mises à jour de la base Joconde et lance l'importation si nécessaire.

## Conventions de nommage

1. Les services implémentent toujours une interface définie dans le projet Core.
2. Les noms des services se terminent par "Service".
3. Le nommage des méthodes suit les conventions de .NET (PascalCase).
4. Les méthodes asynchrones se terminent par "Async".

## Bonnes pratiques

- Les services doivent être stateless pour permettre l'injection de dépendances.
- Utilisez la journalisation pour suivre les opérations importantes.
- Gérez correctement les exceptions et évitez de laisser des ressources non libérées.
- Utilisez les tokens d'annulation pour les opérations longues.
- Pour les opérations d'importation volumineuses, utilisez des mécanismes de traitement par lots.
