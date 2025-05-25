@echo off
echo === Demarrage d'OpenJoconde ===
echo.

REM Verifier .NET
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERREUR] .NET SDK non trouve. Veuillez installer .NET 9
    pause
    exit /b 1
)
echo [OK] .NET SDK trouve

REM Verifier Node.js
where node >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERREUR] Node.js non trouve. Veuillez installer Node.js 18+
    pause
    exit /b 1
)
echo [OK] Node.js trouve

REM Chemins
set BACKEND_PATH=%~dp0src\Backend\OpenJoconde.API
set FRONTEND_PATH=%~dp0src\Frontend

REM Restaurer les packages backend si necessaire
if not exist "%BACKEND_PATH%\bin" (
    echo.
    echo Restauration des packages .NET...
    cd /d "%BACKEND_PATH%"
    dotnet restore
)

REM Installer les dependances frontend si necessaire
if not exist "%FRONTEND_PATH%\node_modules" (
    echo.
    echo Installation des dependances npm...
    cd /d "%FRONTEND_PATH%"
    npm install
)

REM Demarrer le backend dans une nouvelle fenetre
echo.
echo Demarrage du Backend API...
start "OpenJoconde Backend" cmd /k "cd /d %BACKEND_PATH% && dotnet run"

REM Attendre un peu
timeout /t 5 /nobreak >nul

REM Demarrer le frontend dans une nouvelle fenetre
echo Demarrage du Frontend Vue.js...
start "OpenJoconde Frontend" cmd /k "cd /d %FRONTEND_PATH% && npm run serve"

REM Afficher les URLs
echo.
echo === Application demarree ===
echo Backend API: https://localhost:5001
echo Swagger UI: https://localhost:5001/swagger
echo Frontend: http://localhost:8080
echo.
echo Pour arreter les services, fermez les fenetres de commande.
echo.
pause
