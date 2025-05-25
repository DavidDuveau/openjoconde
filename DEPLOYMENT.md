# Guide de compilation et déploiement - OpenJoconde

## Prérequis

1. **.NET 9 SDK** - [Télécharger](https://dotnet.microsoft.com/download/dotnet/9.0)
2. **SQL Server 2019 ou supérieur** (Express Edition suffit)
3. **Node.js 18+** et **npm 9+**
4. **Visual Studio 2022** ou **VS Code** avec extensions C# et Vue

## Backend (.NET)

### 1. Configuration de la base de données

1. Créer une base de données `OpenJoconde` dans SQL Server
2. Exécuter les scripts SQL dans l'ordre :
   ```
   1. src/Backend/OpenJoconde.Infrastructure/Database/CreateDatabase.sql
   2. src/Backend/OpenJoconde.Infrastructure/Database/Migrations/Initial_Migration.sql
   3. src/Backend/OpenJoconde.Infrastructure/Database/Migrations/Updates/UpdateSchema_v1.1.sql
   4. src/Backend/OpenJoconde.Infrastructure/Database/Migrations/Updates/AddTimestamps_v1.2.sql
   ```

### 2. Configuration de l'application

1. Ouvrir `src/Backend/OpenJoconde.API/appsettings.json`
2. Modifier la chaîne de connexion si nécessaire :
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Data Source=.;Initial Catalog=OpenJoconde;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
   }
   ```

### 3. Compilation et exécution

```bash
cd src/Backend/OpenJoconde.API
dotnet restore
dotnet build
dotnet run
```

L'API sera accessible à :
- HTTP : http://localhost:5000
- HTTPS : https://localhost:5001
- Swagger : https://localhost:5001/swagger

### 4. Vérification

- Accéder à Swagger UI pour tester les endpoints
- Vérifier la connexion à la base de données via `/api/health` (si implémenté)

## Frontend (Vue.js)

### 1. Installation des dépendances

```bash
cd src/Frontend
npm install
```

### 2. Configuration

1. Créer un fichier `.env.local` dans `src/Frontend/` :
   ```
   VUE_APP_API_URL=https://localhost:5001/api
   ```

2. Modifier si l'API est sur un autre port/serveur

### 3. Développement

```bash
npm run serve
```

L'application sera accessible à : http://localhost:8080

### 4. Production

```bash
npm run build
```

Les fichiers compilés seront dans `dist/`

## Problèmes courants et solutions

### Erreur : "Certificate not trusted"
- Solution : Exécuter `dotnet dev-certs https --trust`

### Erreur : "Cannot connect to SQL Server"
- Vérifier que SQL Server est démarré
- Vérifier l'authentification Windows
- Tester la connexion avec SQL Server Management Studio

### Erreur : "CreatedAt column not found"
- Exécuter la migration `AddTimestamps_v1.2.sql`

### Erreur CORS avec le frontend
- Vérifier que CORS est configuré dans `Program.cs`
- S'assurer que l'URL du frontend est autorisée

## Déploiement en production

### Backend
1. Publier l'application :
   ```bash
   dotnet publish -c Release -o ./publish
   ```
2. Configurer IIS ou utiliser Kestrel avec un reverse proxy
3. Mettre à jour `appsettings.Production.json`

### Frontend
1. Build de production :
   ```bash
   npm run build
   ```
2. Déployer le contenu de `dist/` sur un serveur web
3. Configurer les redirections pour le routing Vue

### Base de données
1. Sauvegarder la base de développement
2. Restaurer sur le serveur de production
3. Mettre à jour la chaîne de connexion

## Structure des déploiements

```
Production/
├── Backend/
│   ├── OpenJoconde.API.exe
│   ├── appsettings.Production.json
│   └── [autres DLLs]
├── Frontend/
│   ├── index.html
│   ├── css/
│   ├── js/
│   └── img/
└── Database/
    └── [Backup files]
```

## Monitoring

- Logs backend : dans le dossier `logs/` (si configuré)
- Métriques : via Application Insights (si configuré)
- Santé de l'API : endpoint `/api/health`

## Maintenance

### Mises à jour de sécurité
```bash
# Backend
dotnet list package --outdated
dotnet add package [PackageName] --version [Version]

# Frontend
npm audit
npm audit fix
```

### Sauvegardes
- Base de données : quotidienne
- Code source : Git + backups
- Configurations : versionnées séparément

## Support

Pour toute question ou problème :
1. Consulter les logs
2. Vérifier la documentation Swagger
3. Consulter le graphe de connaissances du projet
