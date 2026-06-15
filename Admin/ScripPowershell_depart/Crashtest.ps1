# 1. Définir le dossier où les dumps seront sauvegardés
# TRÈS IMPORTANT : Si ta VM est détruite après un crash, utilise un chemin réseau (ex: \\MonServeur\CrashDumps)
$DumpFolder = "C:\CrashDumps" 

# Créer le dossier s'il n'existe pas
New-Item -Path $DumpFolder -ItemType Directory -Force | Out-Null

# 2. Définir le dossier de travail pour l'outil ProcDump
$ToolFolder = "C:\ProcDump"
New-Item -Path $ToolFolder -ItemType Directory -Force | Out-Null

# 3. Télécharger ProcDump directement depuis les serveurs officiels de Microsoft
$ZipPath = "$env:TEMP\procdump.zip"
Write-Host "Téléchargement de ProcDump..."
Invoke-WebRequest -Uri "https://download.sysinternals.com/files/Procdump.zip" -OutFile $ZipPath

# 4. Extraire le fichier ZIP
Write-Host "Extraction..."
Expand-Archive -Path $ZipPath -DestinationPath $ToolFolder -Force

# 5. Armer ProcDump (Installation silencieuse)
# -ma         : Crée un dump complet de la mémoire (indispensable pour les crashs natifs)
# -i          : Installe ProcDump comme débogueur post-mortem (JIT) de Windows
# -AcceptEula : Accepte la licence Microsoft silencieusement (Obligatoire en script)
Write-Host "Installation du débogueur JIT..."
$ProcDumpExe = "$ToolFolder\procdump.exe"
& $ProcDumpExe -ma -i $DumpFolder -AcceptEula

Write-Host "ProcDump est maintenant en mode écoute. En cas de crash, le dump ira dans $DumpFolder"