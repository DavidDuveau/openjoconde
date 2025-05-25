# Script de démarrage pour OpenJoconde
# Ce script lance le backend et le frontend en parallèle

Write-Host "=== Démarrage d'OpenJoconde ===" -ForegroundColor Green

# Vérifier les prérequis
Write-Host "`nVérification des prérequis..." -ForegroundColor Yellow

# Vérifier .NET
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK trouvé: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET SDK non trouvé. Veuillez installer .NET 9" -ForegroundColor Red
    exit 1
}

# Vérifier Node.js
try {
    $nodeVersion = node --version
    Write-Host "✓ Node.js trouvé: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Node.js non trouvé. Veuillez installer Node.js 18+" -ForegroundColor Red
    exit 1
}

# Chemins
$backendPath = Join-Path $PSScriptRoot "src\Backend\OpenJoconde.API"
$frontendPath = Join-Path $PSScriptRoot "src\Frontend"

# Fonction pour démarrer un processus dans une nouvelle fenêtre
function Start-ProjectComponent {
    param (
        [string]$Name,
        [string]$Path,
        [string]$Command,
        [string]$Arguments
    )
    
    Write-Host "`nDémarrage de $Name..." -ForegroundColor Yellow
    
    if (-not (Test-Path $Path)) {
        Write-Host "✗ Chemin non trouvé: $Path" -ForegroundColor Red
        return $false
    }
    
    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = $Command
    $processInfo.Arguments = $Arguments
    $processInfo.WorkingDirectory = $Path
    $processInfo.UseShellExecute = $true
    $processInfo.CreateNoWindow = $false
    
    try {
        [System.Diagnostics.Process]::Start($processInfo) | Out-Null
        Write-Host "✓ $Name démarré" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "✗ Erreur au démarrage de $Name : $_" -ForegroundColor Red
        return $false
    }
}

# Restaurer les packages backend si nécessaire
if (-not (Test-Path (Join-Path $backendPath "bin"))) {
    Write-Host "`nRestauration des packages .NET..." -ForegroundColor Yellow
    Push-Location $backendPath
    dotnet restore
    Pop-Location
}

# Installer les dépendances frontend si nécessaire
if (-not (Test-Path (Join-Path $frontendPath "node_modules"))) {
    Write-Host "`nInstallation des dépendances npm..." -ForegroundColor Yellow
    Push-Location $frontendPath
    npm install
    Pop-Location
}

# Démarrer le backend
$backendStarted = Start-ProjectComponent -Name "Backend API" `
    -Path $backendPath `
    -Command "cmd.exe" `
    -Arguments "/k dotnet run"

# Attendre un peu pour que le backend démarre
if ($backendStarted) {
    Write-Host "`nAttente du démarrage du backend..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
}

# Démarrer le frontend
$frontendStarted = Start-ProjectComponent -Name "Frontend Vue.js" `
    -Path $frontendPath `
    -Command "cmd.exe" `
    -Arguments "/k npm run serve"

# Résumé
Write-Host "`n=== Résumé ===" -ForegroundColor Green
if ($backendStarted) {
    Write-Host "Backend API: https://localhost:5001" -ForegroundColor Cyan
    Write-Host "Swagger UI: https://localhost:5001/swagger" -ForegroundColor Cyan
}
if ($frontendStarted) {
    Write-Host "Frontend: http://localhost:8080" -ForegroundColor Cyan
}

Write-Host "`nPour arrêter les services, fermez les fenêtres de commande." -ForegroundColor Yellow
Write-Host "Appuyez sur une touche pour fermer ce script..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
