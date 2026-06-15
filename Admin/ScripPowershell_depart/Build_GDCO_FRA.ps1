# =========================================================
# SCRIPT DE BUILD & PACKAGE : Carto5 FRA (Version Portable)
# =========================================================
param(
    [string]$Version = "1.0.0"
)

Add-Type -AssemblyName System.Windows.Forms

# ── Interface de sélection du dossier de sortie ──
$folderBrowser = New-Object System.Windows.Forms.FolderBrowserDialog
$folderBrowser.Description = "Choisissez le dossier de destination pour le fichier ZIP"
$folderBrowser.ShowNewFolderButton = $true
$defaultPath = "C:\Carto5_PCVD"
if (-not (Test-Path $defaultPath)) { New-Item -ItemType Directory -Path $defaultPath -Force | Out-Null }
$folderBrowser.SelectedPath = $defaultPath
$result = $folderBrowser.ShowDialog()
if ($result -ne "OK") { Write-Host "Operation annulee." -ForegroundColor Yellow; pause; exit 0 }

$dossierSortie   = $folderBrowser.SelectedPath
$NomProjet       = "FRA"
$DossierStaging  = "$dossierSortie\Staging\Carto5_$NomProjet"
$NomZip          = "Carto5_$($NomProjet)_Portable"
$FichierZipFinal = "$dossierSortie\$NomZip`_v$Version.zip"



# ── Chemins du projet ──
$CheminProjet              = "$PSScriptRoot\App\Carto5_MapLibre.csproj"
$CheminAppSettings         = "$PSScriptRoot\App\appsettings.json"
$CheminAppSettingsUnitaire = "$PSScriptRoot\App\appsettings.Unitaire.json"
$CheminDbFRA               = "$PSScriptRoot\App\Admin\Carto5Data\Carto5_FRA_Admin.db"
$CheminDbSAT               = "$PSScriptRoot\App\Admin\Carto5Data\FRA_SAT_Admin.db"
$programCs                 = "$PSScriptRoot\App\Program.cs"

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  BUILD & PACKAGE - Carto5 FRA v$Version" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  Dossier sortie : $dossierSortie" -ForegroundColor Gray
Write-Host ""

# ── Vérification préalable ──
if (-not (Test-Path $CheminProjet)) {
    Write-Host "ERREUR : Fichier projet introuvable : $CheminProjet" -ForegroundColor Red
    pause; exit 1
}

# ═══════════════════════════════════════════════
# ÉTAPE 1 : NETTOYAGE
# ═══════════════════════════════════════════════
Write-Host "[1/6] Nettoyage du dossier Staging..." -ForegroundColor Yellow
if (Test-Path $DossierStaging) { 
    Remove-Item -Path $DossierStaging -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $DossierStaging -Force | Out-Null
New-Item -ItemType Directory -Path "$DossierStaging\Data" -Force | Out-Null
New-Item -ItemType Directory -Path "$DossierStaging\Logs" -Force | Out-Null
Write-Host "  -> Structure : \, \Data, \Logs" -ForegroundColor Green

# ═══════════════════════════════════════════════
# ÉTAPE 2 : PATCH DU MODE PORTABLE
# ═══════════════════════════════════════════════
Write-Host "[2/6] Activation du mode portable..." -ForegroundColor Yellow
if (Test-Path $programCs) {
    $content = Get-Content $programCs -Raw -Encoding UTF8
    $content = $content -replace 'bool modePortable = false', 'bool modePortable = true'
    Set-Content -Path $programCs -Value $content -Encoding UTF8
    Write-Host "  -> modePortable = true" -ForegroundColor Green
}

# ═══════════════════════════════════════════════
# ÉTAPE 3 : COMPILATION
# ═══════════════════════════════════════════════
Write-Host "[3/6] Compilation du projet..." -ForegroundColor Yellow
try {
    # 1. Nettoyage
    Write-Host "  -> Nettoyage des dossiers bin/obj..." -ForegroundColor Gray
    $cheminObj = "$PSScriptRoot\App\obj"
    $cheminBin = "$PSScriptRoot\App\bin"
    if (Test-Path $cheminObj) { Remove-Item -Path $cheminObj -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path $cheminBin) { Remove-Item -Path $cheminBin -Recurse -Force -ErrorAction SilentlyContinue }

    # 2. Restauration avec l'indicateur PublishReadyToRun et le contournement des serveurs
    Write-Host "  -> Restauration..." -ForegroundColor Gray
    dotnet restore $CheminProjet -r win-x64 -p:PublishReadyToRun=true --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) { throw "Echec de la restauration" }

    # 3. Publication
    Write-Host "  -> Publication..." -ForegroundColor Gray
    dotnet publish $CheminProjet -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true -o $DossierStaging --no-restore
    
    if ($LASTEXITCODE -ne 0) { throw "Echec de la publication" }
    Write-Host "  -> Compilation reussie" -ForegroundColor Green
} catch {
    Write-Host "ERREUR de compilation : $_" -ForegroundColor Red
    
    # Restauration du mode portable
    $content = Get-Content $programCs -Raw -Encoding UTF8
    $content = $content -replace 'bool modePortable = true', 'bool modePortable = false'
    Set-Content -Path $programCs -Value $content -Encoding UTF8
    
    pause; exit 1
}

# ═══════════════════════════════════════════════
# ÉTAPE 4 : ASSEMBLAGE
# ═══════════════════════════════════════════════
Write-Host "[4/6] Assemblage..." -ForegroundColor Yellow

# 4.1 Structure Clients
$clientsPath = "$DossierStaging\Clients"
@(
    "$clientsPath",
    "$clientsPath\Donnees_Diffusion",
    "$clientsPath\Cartotheque",
    "$clientsPath\Carto5Data_FRA",
    "$clientsPath\Carto5Data_FRA\UTILISATEUR"
) | ForEach-Object {
    if (-not (Test-Path $_)) { New-Item -ItemType Directory -Path $_ -Force | Out-Null }
}
Write-Host "  -> Structure Clients prete" -ForegroundColor Green

# 4.2 Copie du appsettings
if (Test-Path $CheminAppSettingsUnitaire) {
    Copy-Item -Path $CheminAppSettingsUnitaire -Destination "$DossierStaging\appsettings.json" -Force
    Write-Host "  -> appsettings.json (UNITAIRE) copie" -ForegroundColor Green
} else {
    Copy-Item -Path $CheminAppSettings -Destination "$DossierStaging\appsettings.json" -Force
    Write-Host "  -> appsettings.json (standard) copie" -ForegroundColor Yellow
}

# 4.3 Patch du appsettings
$appSettingsPath = "$DossierStaging\appsettings.json"
if (Test-Path $appSettingsPath) {
    $content = Get-Content $appSettingsPath -Raw -Encoding UTF8
    $dossierStagingJson = $DossierStaging.Replace("\", "\\")

    # Sécurité
    $content = $content -replace '"ModeReseau"\s*:\s*true', '"ModeReseau": false'
    $content = $content -replace '"AuthentificationDomainesAutorises"\s*:\s*"[^"]*"', '"AuthentificationDomainesAutorises": ""'
    $content = $content -replace '"EmpruntIdentiteSecurite"\s*:\s*"[^"]*"', '"EmpruntIdentiteSecurite": "false"'
    $content = $content -replace '"MockAutorisationsActive"\s*:\s*"false"', '"MockAutorisationsActive": "true"'
    $content = $content -replace '"CodeSysteme"\s*:\s*"GLO"', '"CodeSysteme": "FRA"'

    # Chemins (portable = local, réseau = MTQ)
    $content = $content -replace '"Racine"\s*:\s*"[^"]*"', "`"Racine`": `"$dossierStagingJson`""
    $content = $content -replace '"DataRoot"\s*:\s*"[^"]*"', "`"DataRoot`": `"$dossierStagingJson`""
    $content = $content -replace '"ExternalClientsRoot"\s*:\s*"[^"]*"', "`"ExternalClientsRoot`": `"$dossierStagingJson\\Clients`""
    $content = $content -replace '"ExternalClientsRootReseau"\s*:\s*"[^"]*"', "`"ExternalClientsRootReseau`": `"\\\\\\\\mtq.min.intra\\\\min\\\\donneesSYSTM\\\\SIAS\\\\UNIT\\\\CARTO5_MASTER\\\\Clients`""
    $content = $content -replace '"GabaritSpatAdmin"\s*:\s*"[^"]*"', "`"GabaritSpatAdmin`": `"wwwroot\\lib\\pedata\\gdaldata\\proj.db`""
    $content = $content -replace '"PeData"\s*:\s*"[^"]*"', "`"PeData`": `"wwwroot\\lib\\pedata`""

        # Forcer les flags à true pour le package portable (écrase les valeurs existantes)
    $content = $content -replace '"EstDeploiementPortable"\s*:\s*[^,}\n\r]*', '"EstDeploiementPortable": true'
    $content = $content -replace '"EstModeCamionAutorise"\s*:\s*[^,}\n\r]*', '"EstModeCamionAutorise": true'
    $content = $content -replace '"ExtractionTerrainActive"\s*:\s*[^,}\n\r]*', '"ExtractionTerrainActive": true'
    $content = $content -replace '"ExtractionServeurActive"\s*:\s*[^,}\n\r]*', '"ExtractionServeurActive": false'
    Write-Host "  -> Flags de déploiement forcés à true" -ForegroundColor Green

    # CartoModules
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
    Write-Host "  -> appsettings.json patché" -ForegroundColor Green
}

# 4.4 Copie des bases
if (Test-Path $CheminDbFRA) {
    Copy-Item -Path $CheminDbFRA -Destination "$DossierStaging\Data\Carto5_FRA_Admin.db" -Force
    Write-Host "  -> Base FRA copiee" -ForegroundColor Green
} else { Write-Host "  -> ATTENTION : Base FRA introuvable" -ForegroundColor DarkYellow }

if (Test-Path $CheminDbSAT) {
    Copy-Item -Path $CheminDbSAT -Destination "$DossierStaging\Data\FRA_SAT_Admin.db" -Force
    Write-Host "  -> Base SAT copiee" -ForegroundColor Green
} else { Write-Host "  -> ATTENTION : Base SAT introuvable" -ForegroundColor DarkYellow }

# ═══════════════════════════════════════════════
# ÉTAPE 5 : LANCEUR .BAT
# ═══════════════════════════════════════════════
Write-Host "[5/6] Creation du lanceur .BAT..." -ForegroundColor Yellow
$BatContent = @"
@echo off
title Carto5 FRA - Carto5
echo =============================================
echo   LANCEMENT DE Carto5 FRA - Carto5
echo =============================================
echo.
set Carto5_DATA_ROOT=%~dp0
set Carto5_DATA_ROOT_CAMION=%~dp0
set Carto5_EXTERNAL_CLIENTS=%~dp0Clients
set ASPNETCORE_ENVIRONMENT=Camion
echo Demarrage du serveur web...
cd /d "%~dp0"
start "" "Carto5_MapLibre.exe"
timeout /t 5 /nobreak >nul
start http://127.0.0.1:5000
echo Application lancee !
pause
"@
Set-Content -Path "$DossierStaging\Demarrer_Carto5_FRA.bat" -Value $BatContent -Encoding UTF8
Write-Host "  -> Demarrer_Carto5_FRA.bat cree" -ForegroundColor Green

# Restauration du mode portable
$content = Get-Content $programCs -Raw -Encoding UTF8
$content = $content -replace 'bool modePortable = true', 'bool modePortable = false'
Set-Content -Path $programCs -Value $content -Encoding UTF8
Write-Host "  -> modePortable restaure a false" -ForegroundColor Green

# ═══════════════════════════════════════════════
# ÉTAPE 6 : COMPRESSION
# ═══════════════════════════════════════════════
Write-Host "[6/6] Compression de l'archive finale..." -ForegroundColor Yellow
Get-Process -Name "Carto5_MapLibre" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
if (Test-Path $FichierZipFinal) { Remove-Item $FichierZipFinal -Force }

$sevenZip = "C:\Program Files\7-Zip\7z.exe"
if (Test-Path $sevenZip) {
    & $sevenZip a -tzip "$FichierZipFinal" "$DossierStaging\*" -mx3 -mmt=on
    Write-Host "  -> Compression 7-Zip terminee" -ForegroundColor Green
} else {
    Compress-Archive -Path "$DossierStaging\*" -DestinationPath $FichierZipFinal -Force
    Write-Host "  -> Compression PowerShell terminee" -ForegroundColor Green
}

# ── Vérification finale ──
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
    pause; exit 1
}
pause