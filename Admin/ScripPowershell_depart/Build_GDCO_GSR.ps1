# =========================================================
# SCRIPT DE BUILD & PACKAGE : Carto5 GSR (Version Portable)
# =========================================================

param(
    [string]$Version = "1.0.0"
)

Add-Type -AssemblyName System.Windows.Forms

# Interface pour choisir le dossier de sortie
$folderBrowser = New-Object System.Windows.Forms.FolderBrowserDialog
$folderBrowser.Description = "Choisissez le dossier de destination pour le fichier ZIP"
$folderBrowser.ShowNewFolderButton = $true

$defaultPath = "C:\Carto5_PCVD"
if (-not (Test-Path $defaultPath)) {
    New-Item -ItemType Directory -Path $defaultPath -Force | Out-Null
}
$folderBrowser.SelectedPath = $defaultPath

$result = $folderBrowser.ShowDialog()

if ($result -ne "OK") {
    Write-Host "Operation annulee." -ForegroundColor Yellow
    pause
    exit 0
}

$dossierSortie = $folderBrowser.SelectedPath
$DossierStaging = "$dossierSortie\Staging\Carto5_GSR"
$NomZip = "Carto5_GSR_Portable"
$FichierZipFinal = "$dossierSortie\$NomZip`_v$Version.zip"

# Chemins du projet
$CheminProjet = "$PSScriptRoot\App\Carto5_MapLibre.csproj"


$CheminAppSettings = "$PSScriptRoot\App\appsettings.json"
$CheminAppSettingsOffline = "$PSScriptRoot\App\appsettings.Offline.json"
$CheminAppSettingsUnitaire = "$PSScriptRoot\App\appsettings.Unitaire.json"

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  BUILD & PACKAGE - Carto5 GSR v$Version" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  Dossier sortie : $dossierSortie" -ForegroundColor Gray
Write-Host ""

# Vérification préalable
if (-not (Test-Path $CheminProjet)) {
    Write-Host "ERREUR : Fichier projet introuvable : $CheminProjet" -ForegroundColor Red
    pause
    exit 1
}

# 1. NETTOYAGE
Write-Host "[1/5] Nettoyage du dossier Staging..." -ForegroundColor Yellow
if (Test-Path $DossierStaging) { 
    Remove-Item -Path $DossierStaging -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  -> Ancien dossier supprime" -ForegroundColor Gray
}
New-Item -ItemType Directory -Path $DossierStaging -Force | Out-Null
New-Item -ItemType Directory -Path "$DossierStaging\Data" -Force | Out-Null
New-Item -ItemType Directory -Path "$DossierStaging\Logs" -Force | Out-Null
Write-Host "  -> Structure creee : \, \Data, \Logs" -ForegroundColor Green

# Patcher modePortable pour le build portable
$programCs = "$PSScriptRoot\App\Program.cs"
if (Test-Path $programCs) {
    $content = Get-Content $programCs -Raw -Encoding UTF8
    $content = $content -replace 'bool modePortable = false', 'bool modePortable = true'
    Set-Content -Path $programCs -Value $content -Encoding UTF8
    Write-Host "  -> Mode portable active pour le build" -ForegroundColor Green
}



# 2. COMPILATION (.NET Publish)
Write-Host "[2/5] Compilation du projet..." -ForegroundColor Yellow
try {
    dotnet publish $CheminProjet -c Release -r win-x64 --self-contained true -o $DossierStaging
    
    if ($LASTEXITCODE -ne 0) {
        throw "Echec de la compilation"
    }
    
    Write-Host "  -> Compilation reussie" -ForegroundColor Green
} catch {
    Write-Host "ERREUR de compilation : $_" -ForegroundColor Red
    pause
    exit 1
}


# 3. ASSEMBLAGE
Write-Host "[3/5] Copie des configurations et donnees..." -ForegroundColor Yellow

# Ajouter les flags de déploiement portable
if ($content -notmatch '"EstDeploiementPortable"') {
    $content = $content -replace '"ModeReseau"\s*:\s*false', "`"ModeReseau`": false,`n    `"EstDeploiementPortable`": true,`n    `"EstModeCamionAutorise`": true,`n    `"ExtractionServeurActive`": false,`n    `"ExtractionTerrainActive`": true"
}

# Créer la structure Clients pour le mode portable
Write-Host "  -> Creation de la structure Clients..." -ForegroundColor Gray
$clientsPath = "$DossierStaging\Clients"
$dossiers = @(
    "$clientsPath",
    "$clientsPath\Donnees_Diffusion",
    "$clientsPath\Cartotheque",
    "$clientsPath\Carto5Data_GSR",
    "$clientsPath\Carto5Data_GSR\UTILISATEUR"
)
foreach ($dossier in $dossiers) {
    if (-not (Test-Path $dossier)) {
        New-Item -ItemType Directory -Path $dossier -Force | Out-Null
    }
}
Write-Host "  -> Structure Clients prete (Donnees_Diffusion, Cartotheque, Carto5Data_GSR)" -ForegroundColor Green


# Copier le appsettings Unitaire comme base
if (Test-Path $CheminAppSettingsUnitaire) {
    Copy-Item -Path $CheminAppSettingsUnitaire -Destination "$DossierStaging\appsettings.json" -Force
    Write-Host "  -> appsettings.json (mode UNITAIRE) copie" -ForegroundColor Green
} else {
    Copy-Item -Path $CheminAppSettings -Destination "$DossierStaging\appsettings.json" -Force
    Write-Host "  -> appsettings.json (standard) copie" -ForegroundColor Yellow
}

# Forcer le mode HORS-LIGNE et corriger les chemins
$appSettingsPath = "$DossierStaging\appsettings.json"
if (Test-Path $appSettingsPath) {
    $content = Get-Content $appSettingsPath -Raw -Encoding UTF8
    # Désactiver le mode réseau
    $content = $content -replace '"ModeReseau"\s*:\s*true', '"ModeReseau": false'
    $content = $content -replace '"ModeReseau"\s*:\s*false', '"ModeReseau": false'
    # Vider les domaines autorisés
    $content = $content -replace '"AuthentificationDomainesAutorises"\s*:\s*"[^"]*"', '"AuthentificationDomainesAutorises": ""'
    $content = $content -replace '"EmpruntIdentiteSecurite"\s*:\s*"[^"]*"', '"EmpruntIdentiteSecurite": "false"'
    $content = $content -replace '"MockAutorisationsActive"\s*:\s*"false"', '"MockAutorisationsActive": "true"'
    $content = $content -replace '"CodeSysteme"\s*:\s*"GLO"', '"CodeSysteme": "GSR"'
    # Corriger les chemins pour le mode portable (doubles backslashes pour JSON)
    $dossierStagingJson = $DossierStaging.Replace("\", "\\")
    $content = $content -replace '"Racine"\s*:\s*"[^"]*"', "`"Racine`": `"$dossierStagingJson`""
    $content = $content -replace '"DataRoot"\s*:\s*"[^"]*"', "`"DataRoot`": `"$dossierStagingJson`""
    $content = $content -replace '"ExternalClientsRoot"\s*:\s*"[^"]*"', "`"ExternalClientsRoot`": `"$dossierStagingJson\\Clients`""
    $content = $content -replace '"GabaritSpatAdmin"\s*:\s*"[^"]*"', "`"GabaritSpatAdmin`": `"wwwroot\\lib\\pedata\\gdaldata\\proj.db`""
    $content = $content -replace '"PeData"\s*:\s*"[^"]*"', "`"PeData`": `"wwwroot\\lib\\pedata`""
    
	    # Ajouter ExternalClientsRootReseau s'il n'existe pas
    if ($content -notmatch '"ExternalClientsRootReseau"') {
        $content = $content -replace '"ExternalClientsRoot"\s*:\s*"[^"]*"', "`"ExternalClientsRoot`": `"$dossierStagingJson\\\\Clients`",`n    `"ExternalClientsRootReseau`": `"\\\\\\\\mtq.min.intra\\\\min\\\\donneesSYSTM\\\\SIAS\\\\UNIT\\\\CARTO5_MASTER\\\\Clients`""
    }
		
	
	# Ajouter CartoModules s'il n'existe pas
    if ($content -notmatch '"CartoModules"') {
        $cartoModules = @"
  "CartoModules": {
    "FRA": { "DisplayName": "Framework Carto5", "IsActive": true, "ThemeColor": "#0d6efd" },
    "SIAS": { "DisplayName": "Sécurité routière", "IsActive": true, "ThemeColor": "#0056b3" },
    "GSR": { "DisplayName": "Signalisation routière", "IsActive": true, "ThemeColor": "#dc3545" },
    "GCH": { "DisplayName": "Gestion de chaussées", "IsActive": true, "ThemeColor": "#198754" },
    "M012": { "DisplayName": "Gestion des ponceaux", "IsActive": true, "ThemeColor": "#ffc107" },
    "IIT": { "DisplayName": "Inventaire infrastructures", "IsActive": true, "ThemeColor": "#6f42c1" },
    "MRG": { "DisplayName": "Marquage routier", "IsActive": true, "ThemeColor": "#0dcaf0" },
    "SMG": { "DisplayName": "Suivi monitoring", "IsActive": true, "ThemeColor": "#20c997" },
    "VHR": { "DisplayName": "Véhicule hors route", "IsActive": true, "ThemeColor": "#fd7e14" },
    "VIU": { "DisplayName": "Visionneuse photos", "IsActive": true, "ThemeColor": "#6c757d" }
  },
"@
        $content = $content -replace '("TrMco":\s*{)', "$cartoModules`n  `$1"
        Write-Host "  -> CartoModules ajoutes" -ForegroundColor Green
    }

    Set-Content -Path $appSettingsPath -Value $content -Encoding UTF8
    Write-Host "  -> Mode hors-ligne force et chemins corriges" -ForegroundColor Green
}

# Copier la base satellite GSR
$CheminDbGSR = "$PSScriptRoot\App\Admin\Carto5Data\GSR_SAT_Admin.db"
if (Test-Path $CheminDbGSR) {
    Copy-Item -Path $CheminDbGSR -Destination "$DossierStaging\Data\GSR_SAT_Admin.db" -Force
    Write-Host "  -> Base GSR copiee dans \Data" -ForegroundColor Green
} else {
    Write-Host "  -> ATTENTION : Base GSR introuvable" -ForegroundColor DarkYellow
}

# Copier la base de données administration FRA
$CheminDbFRA = "$PSScriptRoot\App\Admin\Carto5Data\Carto5_FRA_Admin.db"
if (Test-Path $CheminDbFRA) {
    Copy-Item -Path $CheminDbFRA -Destination "$DossierStaging\Data\Carto5_FRA_Admin.db" -Force
    Write-Host "  -> Base FRA copiee dans \Data" -ForegroundColor Green
} else {
    Write-Host "  -> ATTENTION : Base FRA introuvable" -ForegroundColor DarkYellow
}

# 4. CRÉATION DU LANCEUR .BAT
Write-Host "[4/5] Creation du lanceur .BAT..." -ForegroundColor Yellow

$BatContent = @"
@echo off
title Carto5 GSR - Carto5
echo =============================================
echo   LANCEMENT DE Carto5 GSR - Carto5
echo =============================================
echo.
set Carto5_DATA_ROOT=%~dp0
set Carto5_DATA_ROOT_CAMION=%~dp0
set Carto5_EXTERNAL_CLIENTS=%~dp0Clients
set ASPNETCORE_ENVIRONMENT=Camion
echo Demarrage du serveur web...
echo.
cd /d "%~dp0"
start "" "Carto5_MapLibre.exe"
timeout /t 5 /nobreak >nul
start http://127.0.0.1:5000
echo Application lancee !
pause
"@

Set-Content -Path "$DossierStaging\Demarrer_Carto5_GSR.bat" -Value $BatContent -Encoding UTF8
Write-Host "  -> Demarrer_Carto5_GSR.bat cree" -ForegroundColor Green

# Restaurer modePortable après build
$content = Get-Content $programCs -Raw -Encoding UTF8
$content = $content -replace 'bool modePortable = true', 'bool modePortable = false'
Set-Content -Path $programCs -Value $content -Encoding UTF8


# 5. CRÉATION DU ZIP
Write-Host "[5/5] Compression de l'archive finale..." -ForegroundColor Yellow

# Tuer les processus qui pourraient verrouiller les fichiers
Get-Process -Name "Carto5_MapLibre" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

if (Test-Path $FichierZipFinal) { 
    Remove-Item $FichierZipFinal -Force 
}





Compress-Archive -Path "$DossierStaging\*" -DestinationPath $FichierZipFinal -Force

# Vérification finale
if (Test-Path $FichierZipFinal) {
    $tailleZip = (Get-Item $FichierZipFinal).Length / 1MB
    Write-Host ""
    Write-Host "=============================================" -ForegroundColor Green
    Write-Host "  BUILD TERMINE AVEC SUCCES !" -ForegroundColor Green
    Write-Host "=============================================" -ForegroundColor Green
    Write-Host "  Fichier : $FichierZipFinal" -ForegroundColor White
    Write-Host "  Taille  : $([math]::Round($tailleZip, 2)) MB" -ForegroundColor White
    Write-Host "  Version : $Version" -ForegroundColor White
    Write-Host "=============================================" -ForegroundColor Green
    
    Invoke-Item $dossierSortie
} else {
    Write-Host "ERREUR : Le ZIP n'a pas ete cree." -ForegroundColor Red
    pause
    exit 1
}

pause