@echo off
echo =====================================================
echo   Configuration complete et lancement d'OpenJoconde
echo =====================================================
echo.

echo === Configuration ===
set BACKEND_PORT=5001
set FRONTEND_PORT=8080
set DB_SERVER=localhost
set DB_NAME=openjoconde
set JOCONDE_URL=https://data.culture.gouv.fr/explore/dataset/joconde-musees-de-france-base-joconde/download/?format=xml

echo ===
echo Configuration de la connexion PostgreSQL
echo ===
set /p DB_USER="Nom d'utilisateur PostgreSQL (defaut: postgres): "
if "%DB_USER%"=="" set DB_USER=postgres

set /p DB_PASSWORD="Mot de passe PostgreSQL (ne sera pas affiche): "
if "%DB_PASSWORD%"=="" set DB_PASSWORD=postgres

echo.
echo === Verification des prerequis ===
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo ERREUR: .NET SDK n'est pas installe ou n'est pas dans le PATH.
    echo Veuillez installer .NET SDK 7.0 ou superieur depuis https://dotnet.microsoft.com/download
    goto :error
)

where node >nul 2>&1
if %errorlevel% neq 0 (
    echo ERREUR: Node.js n'est pas installe ou n'est pas dans le PATH.
    echo Veuillez installer Node.js v16 ou superieur depuis https://nodejs.org/
    goto :error
)

where psql >nul 2>&1
if %errorlevel% neq 0 (
    echo AVERTISSEMENT: psql n'est pas dans le PATH.
    echo Les scripts SQL devront etre executes manuellement via pgAdmin.
    set PSQL_AVAILABLE=false
) else (
    set PSQL_AVAILABLE=true
)

echo.
echo === Creation et initialisation de la base de donnees ===
echo pgAdmin doit etre lance ou le service PostgreSQL doit etre actif pour cette etape.
echo Si la base existe deja, cette etape peut etre ignoree.
echo.
echo Connexion avec : %DB_USER%@%DB_SERVER%
echo Base de donnees : %DB_NAME%
echo.
set /p SKIP_DB="Ignorer la creation de la base de donnees? (O/n): "
if /i "%SKIP_DB%"=="n" (
    echo.
    echo Creation de la base de donnees %DB_NAME%...
    if "%PSQL_AVAILABLE%"=="true" (
        psql -h %DB_SERVER% -U %DB_USER% -c "CREATE DATABASE %DB_NAME%;"
        echo Execution du script de creation des tables...
        psql -h %DB_SERVER% -U %DB_USER% -d %DB_NAME% -f "src\Backend\OpenJoconde.Infrastructure\Database\CreateDatabase.sql"
        echo Execution du script de migration initiale...
        psql -h %DB_SERVER% -U %DB_USER% -d %DB_NAME% -f "src\Backend\OpenJoconde.Infrastructure\Database\Migrations\Initial_Migration.sql"
    ) else (
        echo Veuillez executer les scripts SQL suivants via pgAdmin:
        echo 1. CREATE DATABASE %DB_NAME%;
        echo 2. src\Backend\OpenJoconde.Infrastructure\Database\CreateDatabase.sql
        echo 3. src\Backend\OpenJoconde.Infrastructure\Database\Migrations\Initial_Migration.sql
        pause
    )
)

echo.
echo === Configuration de la chaine de connexion ===
if exist src\Backend\OpenJoconde.API\appsettings.json (
    echo Mise a jour du fichier appsettings.json avec les informations de connexion...
    set CONNECTION_STRING="Server=%DB_SERVER%;Database=%DB_NAME%;User Id=%DB_USER%;Password=%DB_PASSWORD%;"
    powershell -Command "(Get-Content -Path 'src\Backend\OpenJoconde.API\appsettings.json') -replace '\"DefaultConnection\"\s*:\s*\"[^\"]*\"', '\"DefaultConnection\": %CONNECTION_STRING%' | Set-Content -Path 'src\Backend\OpenJoconde.API\appsettings.json'"
) else (
    echo Creation du fichier appsettings.json avec les informations de connexion...
    set CONNECTION_STRING="Server=%DB_SERVER%;Database=%DB_NAME%;User Id=%DB_USER%;Password=%DB_PASSWORD%;"
    echo { > "src\Backend\OpenJoconde.API\appsettings.json"
    echo   "ConnectionStrings": { >> "src\Backend\OpenJoconde.API\appsettings.json"
    echo     "DefaultConnection": %CONNECTION_STRING% >> "src\Backend\OpenJoconde.API\appsettings.json"
    echo   }, >> "src\Backend\OpenJoconde.API\appsettings.json"
    echo   "Logging": { >> "src\Backend\OpenJoconde.API\appsettings.json"
    echo     "LogLevel": { >> "src\Backend\OpenJoconde.API\appsettings.json"
    echo       "Default": "Information", >> "src\Backend\OpenJoconde.API\appsettings.json"
    echo       "Microsoft": "Warning", >> "src\Backend\OpenJoconde.API\appsettings.json"
    echo       "Microsoft.Hosting.Lifetime": "Information" >> "src\Backend\OpenJoconde.API\appsettings.json"
    echo     } >> "src\Backend\OpenJoconde.API\appsettings.json"
    echo   }, >> "src\Backend\OpenJoconde.API\appsettings.json"
    echo   "AllowedHosts": "*" >> "src\Backend\OpenJoconde.API\appsettings.json"
    echo } >> "src\Backend\OpenJoconde.API\appsettings.json"
)

echo.
echo === Restauration des packages backend (.NET) ===
cd src\Backend\OpenJoconde.API
dotnet restore
if %errorlevel% neq 0 goto :error
echo.

echo === Installation des dependances frontend (Node.js) ===
cd ..\..\Frontend
call npm install
if %errorlevel% neq 0 goto :error
cd ..\..

echo === Telechargement des donnees Joconde ===
set /p DOWNLOAD_DATA="Telecharger et importer les donnees Joconde? (O/n): "
if /i not "%DOWNLOAD_DATA%"=="n" (
    echo Creation du repertoire de donnees temporaire...
    mkdir temp 2>nul
    cd temp
    
    echo Telechargement du fichier XML Joconde depuis data.culture.gouv.fr...
    echo (Ceci peut prendre plusieurs minutes selon votre connexion)
    powershell -Command "& {Invoke-WebRequest -Uri '%JOCONDE_URL%' -OutFile 'joconde.xml'}"
    if %errorlevel% neq 0 (
        echo ERREUR: Impossible de telecharger les donnees Joconde.
        cd ..
        goto :continue_anyway
    )
    
    echo Fichier XML telecharge avec succes!
    echo Lancement de l'importation des donnees...
    
    cd ..\src\Backend\OpenJoconde.API
    echo Execution de l'importation des donnees via l'API...
    dotnet run --no-build --import-data "..\..\..\temp\joconde.xml"
    
    cd ..\..\..
    echo Nettoyage des fichiers temporaires...
    rmdir /s /q temp
)

:continue_anyway
echo.
echo === Lancement de l'application ===
echo.
echo Demarrage du Backend (.NET) et du Frontend (Vue.js)...
echo.

echo === Lancement du Backend (.NET) ===
cd src\Backend\OpenJoconde.API
start cmd /k "title OpenJoconde Backend && echo Demarrage du serveur Backend sur le port %BACKEND_PORT%... && dotnet watch run --urls=https://localhost:%BACKEND_PORT%"

echo.
echo === Lancement du Frontend (Vue.js) ===
cd ..\..\Frontend
start cmd /k "title OpenJoconde Frontend && echo Demarrage du serveur Frontend sur le port %FRONTEND_PORT%... && npm run serve"

echo.
echo === Environnement OpenJoconde demarre avec succes ===
echo.
echo Backend: https://localhost:%BACKEND_PORT%
echo Frontend: http://localhost:%FRONTEND_PORT%
echo Swagger: https://localhost:%BACKEND_PORT%/swagger
echo.
echo Pour arreter les serveurs, fermez les fenetres de commande.
echo.
goto :end

:error
echo.
echo Une erreur s'est produite lors de la configuration. Verifiez les messages ci-dessus.
exit /b 1

:end
pause