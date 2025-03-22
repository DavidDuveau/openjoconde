# Scripts d'automatisation OpenJoconde

Ce répertoire contient les scripts utilitaires pour faciliter le développement et le déploiement du projet OpenJoconde.

## Scripts disponibles

### setup-and-run.bat

Script complet pour l'installation, la configuration et le lancement du projet OpenJoconde. Ce script :

1. Vérifie les prérequis (.NET, Node.js, PostgreSQL)
2. Crée et initialise la base de données PostgreSQL
3. Télécharge les données Joconde depuis data.culture.gouv.fr
4. Importe les données dans la base de données
5. Lance l'application (Backend et Frontend)

**Utilisation :**

```bash
setup-and-run.bat
```

Lors de l'exécution, le script vous demandera si vous souhaitez ignorer la création de la base de données (si elle existe déjà) et si vous souhaitez télécharger et importer les données Joconde.

### run-dev.bat

Script simplifié pour lancer uniquement les serveurs de développement (Backend et Frontend) sans configuration préalable.

**Utilisation :**

```bash
run-dev.bat
```

Ce script suppose que la base de données est déjà configurée et les dépendances installées.

### commit-changes.bat

Utilitaire pour ajouter et commiter les changements au dépôt Git.

**Utilisation :**

```bash
commit-changes.bat
```

### conventional-commits.bat

Utilitaire pour réaliser des commits suivant la convention Conventional Commits.

**Utilisation :**

```bash
conventional-commits.bat
```

## Notes importantes

- Assurez-vous que pgAdmin est en cours d'exécution avant d'utiliser `setup-and-run.bat` si vous souhaitez initialiser la base de données
- Le téléchargement des données Joconde peut prendre plusieurs minutes selon votre connexion internet
- L'importation des données peut être longue selon la taille du fichier XML et les performances de votre système