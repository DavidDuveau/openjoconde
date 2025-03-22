@echo off
echo =====================================================
echo   Lancement de l'environnement de développement
echo   OpenJoconde
echo =====================================================
echo.

echo === Configuration ===
set BACKEND_PORT=5001
set FRONTEND_PORT=8080
set DB_SERVER=localhost
set DB_NAME=openjoconde
set DB_USER=postgres
set DB_PASSWORD=postgres

echo.
echo === Lancement du Backend (.NET) ===
cd src\Backend\OpenJoconde.API
start cmd /k "title OpenJoconde Backend && echo Démarrage du serveur Backend sur le port %BACKEND_PORT%... && dotnet watch run --urls=https://localhost:%BACKEND_PORT%"

echo.
echo === Lancement du Frontend (Vue.js) ===
cd ..\..\Frontend
start cmd /k "title OpenJoconde Frontend && echo Démarrage du serveur Frontend sur le port %FRONTEND_PORT%... && npm run serve"

echo.
echo === Environnement de développement démarré ===
echo.
echo Backend: https://localhost:%BACKEND_PORT%
echo Frontend: http://localhost:%FRONTEND_PORT%
echo Swagger: https://localhost:%BACKEND_PORT%/swagger
echo.
echo Pour arrêter les serveurs, fermez les fenêtres de commande.
echo.
pause