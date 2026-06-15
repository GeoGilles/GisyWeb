[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Set-Location $PSScriptRoot

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# Variables globales
$global:configPath = "$PSScriptRoot\janus_config.json"
$global:logPath = "$PSScriptRoot\janus_logs.txt"

function Write-Log {
    param([string]$Message, [string]$Type = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$Type] $Message"
    
    $color = switch($Type) {
        "SUCCESS" { "Green" }
        "ERROR"   { "Red" }
        "WARNING" { "Yellow" }
        "PROGRESS" { "Cyan" }
        default   { "White" }
    }
    Write-Host $logEntry -ForegroundColor $color
    Add-Content -Path $global:logPath -Value $logEntry -Encoding UTF8
}

function Save-Config {
    param($nomProjet, $lastUsedBranches)
    $config = @{
        NomProjet = $nomProjet
        LastUsedBranches = $lastUsedBranches
        LastUpdate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
    $config | ConvertTo-Json | Set-Content -Path $global:configPath -Encoding UTF8
}

function Load-Config {
    if (Test-Path $global:configPath) {
        $config = Get-Content $global:configPath | ConvertFrom-Json
        return $config
    }
    return $null
}

function Test-NomProjet {
    param([string]$nom)
    if ($nom -notmatch '^Carto5_\w+$') {
        [System.Windows.Forms.MessageBox]::Show(
            "Format invalide ! Le nom doit etre Carto5_XXX`n`nExemple: Carto5_GSE",
            "Erreur de format",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Error
        )
        return $false
    }
    return $true
}

function Test-DossierSecurite {
    param([string]$nomProjet)
    $currentDir = Split-Path -Leaf (Get-Location)
    if ($currentDir -ne $nomProjet) {
        $result = [System.Windows.Forms.MessageBox]::Show(
            "ATTENTION: Le dossier actuel ($currentDir) ne correspond pas au projet ($nomProjet).`n`nContinuer quand meme ?",
            "Incoherence de dossier",
            [System.Windows.Forms.MessageBoxButtons]::YesNo,
            [System.Windows.Forms.MessageBoxIcon]::Warning
        )
        return ($result -eq "Yes")
    }
    return $true
}

# Interface graphique
$form = New-Object System.Windows.Forms.Form
$form.Text = "Projet Janus - Gestionnaire Carto5 PRO v3.0"
$form.Size = New-Object System.Drawing.Size(520,680)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false
$form.BackColor = [System.Drawing.Color]::FromArgb(240, 248, 255)

# Menu Principal
$menuStrip = New-Object System.Windows.Forms.MenuStrip

$fileMenu = New-Object System.Windows.Forms.ToolStripMenuItem
$fileMenu.Text = "Fichier"
$menuStrip.Items.Add($fileMenu)

$exitItem = New-Object System.Windows.Forms.ToolStripMenuItem
$exitItem.Text = "Quitter"
$exitItem.Add_Click({ $form.Close() })
$fileMenu.DropDownItems.Add($exitItem)

$toolsMenu = New-Object System.Windows.Forms.ToolStripMenuItem
$toolsMenu.Text = "Outils"
$menuStrip.Items.Add($toolsMenu)

$logsItem = New-Object System.Windows.Forms.ToolStripMenuItem
$logsItem.Text = "Voir les logs"
$logsItem.Add_Click({
    if (Test-Path $global:logPath) {
        Invoke-Item $global:logPath
    } else {
        [System.Windows.Forms.MessageBox]::Show("Aucun log trouve.", "Info", 0, [System.Windows.Forms.MessageBoxIcon]::Information)
    }
})
$toolsMenu.DropDownItems.Add($logsItem)

$helpMenu = New-Object System.Windows.Forms.ToolStripMenuItem
$helpMenu.Text = "Aide"
$menuStrip.Items.Add($helpMenu)

$aboutItem = New-Object System.Windows.Forms.ToolStripMenuItem
$aboutItem.Text = "A propos"
$aboutItem.Add_Click({
    [System.Windows.Forms.MessageBox]::Show(
        "Projet Janus - Gestionnaire Carto5`n`nVersion PRO 3.0`n`n(c) 2024 - Outil de gestion de branches Git`n`nCe logiciel permet de gerer efficacement les propagations de code entre les branches de developpement.",
        "A propos",
        0,
        [System.Windows.Forms.MessageBoxIcon]::Information
    )
})
$helpMenu.DropDownItems.Add($aboutItem)

$form.Controls.Add($menuStrip)

# Panel Principal
$panel = New-Object System.Windows.Forms.Panel
$panel.Location = New-Object System.Drawing.Point(10, 30)
$panel.Size = New-Object System.Drawing.Size(495, 610)
$panel.AutoScroll = $true
$form.Controls.Add($panel)

# --- CONTROLES ---
$labelAcro = New-Object System.Windows.Forms.Label
$labelAcro.Location = New-Object System.Drawing.Point(10,10)
$labelAcro.Size = New-Object System.Drawing.Size(470,25)
$labelAcro.Text = "Nom COMPLET du projet (OBLIGATOIRE : Carto5_XXX) :"
$labelAcro.Font = New-Object System.Drawing.Font("Arial", 9, [System.Drawing.FontStyle]::Bold)
$panel.Controls.Add($labelAcro)

$txtAcro = New-Object System.Windows.Forms.TextBox
$txtAcro.Location = New-Object System.Drawing.Point(10,35)
$txtAcro.Size = New-Object System.Drawing.Size(470,25)
$txtAcro.Font = New-Object System.Drawing.Font("Consolas", 10)
$panel.Controls.Add($txtAcro)

# Charger la derniere configuration
$savedConfig = Load-Config
if ($savedConfig -and $savedConfig.NomProjet) {
    $txtAcro.Text = $savedConfig.NomProjet
}

# Status Label
$statusLabel = New-Object System.Windows.Forms.Label
$statusLabel.Location = New-Object System.Drawing.Point(10,580)
$statusLabel.Size = New-Object System.Drawing.Size(470,20)
$statusLabel.Text = "Pret"
$statusLabel.Font = New-Object System.Drawing.Font("Arial", 8)
$statusLabel.ForeColor = [System.Drawing.Color]::Gray
$panel.Controls.Add($statusLabel)

function Update-Status {
    param([string]$Message)
    $statusLabel.Text = $Message
    Write-Log $Message -Type "PROGRESS"
    [System.Windows.Forms.Application]::DoEvents()
}

# ==============================================================================
# BOUTON 1 : INITIALISATION
# ==============================================================================
$btn1 = New-Object System.Windows.Forms.Button
$btn1.Location = New-Object System.Drawing.Point(10,75)
$btn1.Size = New-Object System.Drawing.Size(470,50)
$btn1.Text = "1. KIT DE DEPART (Creation initiale)"
$btn1.BackColor = [System.Drawing.Color]::LightCoral
$btn1.FlatStyle = 'Flat'
$btn1.Font = New-Object System.Drawing.Font("Arial", 9, [System.Drawing.FontStyle]::Bold)
$btn1.Cursor = [System.Windows.Forms.Cursors]::Hand

$btn1.Add_Click({
    $nomProjet = $txtAcro.Text.Trim().ToUpper()
    if (-not (Test-NomProjet $nomProjet)) { return }
    if (-not (Test-DossierSecurite $nomProjet)) { return }
    
    if (Test-Path ".git") {
        [System.Windows.Forms.MessageBox]::Show("Deja initialise ! Action bloquee.", "Stop", 0, [System.Windows.Forms.MessageBoxIcon]::Stop)
        return
    }
    
	
    try {
        Update-Status "Initialisation en cours..."
        
        $urlAzure = "https://transports.visualstudio.com/Carte_Carto5/_git/$nomProjet"
        $check = git ls-remote $urlAzure 2>&1
        if ($check -match "refs/") {
            throw "Azure contient deja des donnees"
        }
        
        git init -b main
        git remote add origin $urlAzure
        git remote add source-framework "https://transports.visualstudio.com/Carte_Carto5/_git/Carte_Carto5"
        git fetch source-framework
        git reset --hard source-framework/Carto5_Main
        
        # === DEBUT CORRECTION RENOMMAGE PROJET ===
        
        # 1. Trouver le VRAI nom du fichier .sln existant
        # 1. Trouver le fichier .sln du framework (priorité à Carto5_MapLibre.sln)
		$ancienSln = Get-ChildItem -Path . -Filter "Carto5_MapLibre.sln" | Select-Object -First 1
		if (-not $ancienSln) {
			# Si Carto5_MapLibre.sln n'existe pas, prend le premier .sln trouvé
			$ancienSln = Get-ChildItem -Path . -Filter "*.sln" | Select-Object -First 1
		}
        
        if ($ancienSln) {
            $ancienNomProjet = [System.IO.Path]::GetFileNameWithoutExtension($ancienSln.Name)
            
            Write-Host "Ancien nom du projet detecte : $ancienNomProjet" -ForegroundColor Cyan
            Write-Host "Nouveau nom du projet : $nomProjet" -ForegroundColor Green
            
            # 2. Renommer le fichier .sln
            Rename-Item -Path $ancienSln.FullName -NewName "$nomProjet.sln"
            Write-Host "Fichier .sln renomme : $ancienSln -> $nomProjet.sln" -ForegroundColor Yellow
            
            # 3. Remplacer l'ancien nom dans le contenu du .sln
            $slnContent = Get-Content "$nomProjet.sln" -Raw
            $slnContent = $slnContent -replace $ancienNomProjet, $nomProjet
            Set-Content -Path "$nomProjet.sln" -Value $slnContent -Encoding UTF8
            Write-Host "Contenu du .sln corrige" -ForegroundColor Yellow
            
            # 4. Renommer les dossiers de projet (ex: GSR -> Carto5_GSE)
            Get-ChildItem -Path . -Directory | ForEach-Object {
                $dossier = $_.Name
                if ($dossier -like "*$ancienNomProjet*") {
                    $nouveauNomDossier = $dossier -replace $ancienNomProjet, $nomProjet
                    Rename-Item -Path $_.FullName -NewName $nouveauNomDossier
                    Write-Host "Dossier renomme : $dossier -> $nouveauNomDossier" -ForegroundColor Yellow
                }
            }
            
            # 5. Parcourir TOUS les fichiers de configuration et remplacer l'ancien nom
            Get-ChildItem -Path . -Recurse -Include "*.csproj", "*.csproj.user", "*.config", "*.json", "*.xml", "*.cs", "*.xaml", "*.vb", "*.resx", "*.settings" | ForEach-Object {
                $contenuFichier = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue
                if ($contenuFichier -match $ancienNomProjet) {
                    Write-Host "Correction du fichier : $($_.FullName)" -ForegroundColor DarkYellow
                    $nouveauContenu = $contenuFichier -replace $ancienNomProjet, $nomProjet
                    Set-Content -Path $_.FullName -Value $nouveauContenu -Encoding UTF8
                }
            }
            
            # 6. Ajouter et commiter TOUS les changements de renommage
            git add -A
            git commit -m "Initialisation : Renommage complet du projet $ancienNomProjet vers $nomProjet"
            Write-Host "Projet renomme de $ancienNomProjet a $nomProjet avec succes." -ForegroundColor Green
        } else {
            Write-Host "ATTENTION : Aucun fichier .sln trouve pour renommage. Verifiez le depot source." -ForegroundColor Red
        }
        
        # === FIN CORRECTION RENOMMAGE PROJET ===
        
        git branch -f framework_depart main
        git branch -f integration main
        git branch -f acceptation main
        git branch -f unitaire main
        git push -f -u origin main integration acceptation unitaire framework_depart
        git checkout unitaire
        
        Save-Config -nomProjet $nomProjet -lastUsedBranches @("integration", "acceptation")
        
        $btn1.Enabled = $false
        $btn1.BackColor = [System.Drawing.Color]::LightGray
        $btn1.Text = "1. KIT DE DEPART (VERROUILLE)"
        
        Update-Status "Projet cree avec succes"
        Write-Log "Projet $nomProjet cree et renomme avec succes" -Type "SUCCESS"
        [System.Windows.Forms.MessageBox]::Show("Projet $nomProjet cree avec succes !`n`nLe projet a ete renomme depuis le modele.", "Succes", 0, [System.Windows.Forms.MessageBoxIcon]::Information)
    }
    catch {
        Write-Log "Erreur : $_" -Type "ERROR"
        Update-Status "Erreur lors de l'initialisation"
        [System.Windows.Forms.MessageBox]::Show("Erreur : $_", "Erreur", 0, [System.Windows.Forms.MessageBoxIcon]::Error)
    }
})
$panel.Controls.Add($btn1)

# ==============================================================================
# BOUTON 2 : RECREER LOCAL
# ==============================================================================
$btn2 = New-Object System.Windows.Forms.Button
$btn2.Location = New-Object System.Drawing.Point(10,135)
$btn2.Size = New-Object System.Drawing.Size(470,50)
$btn2.Text = "2. Recreer local (Telecharger depuis Azure)"
$btn2.BackColor = [System.Drawing.Color]::LightYellow
$btn2.FlatStyle = 'Flat'
$btn2.Cursor = [System.Windows.Forms.Cursors]::Hand

$btn2.Add_Click({
    $nomProjet = $txtAcro.Text.Trim().ToUpper()
    if (-not (Test-NomProjet $nomProjet)) { return }
    if (-not (Test-DossierSecurite $nomProjet)) { return }
    
    if (Test-Path ".git") {
        [System.Windows.Forms.MessageBox]::Show("Deja connecte !", "Info", 0, [System.Windows.Forms.MessageBoxIcon]::Information)
        return
    }
    
    try {
        Update-Status "Telechargement en cours..."
        
        git init
        git remote add origin "https://transports.visualstudio.com/Carte_Carto5/_git/$nomProjet"
        git fetch origin
        
        if ([string]::IsNullOrWhiteSpace((git branch -r))) {
            Remove-Item -Recurse -Force ".git" -ErrorAction SilentlyContinue
            throw "Azure est vide"
        }
        
        git branch --track main origin/main 2>$null
        git branch --track integration origin/integration 2>$null
        git branch --track acceptation origin/acceptation 2>$null
        git branch --track unitaire origin/unitaire 2>$null
        git branch --track framework_depart origin/framework_depart 2>$null
        git checkout -f unitaire
        git remote add source-framework "https://transports.visualstudio.com/Carte_Carto5/_git/Carte_Carto5"
        git fetch source-framework
        
        Update-Status "Projet restaure avec succes"
        Write-Log "Projet $nomProjet restaure" -Type "SUCCESS"
        [System.Windows.Forms.MessageBox]::Show("Projet restaure avec succes !", "Succes", 0, [System.Windows.Forms.MessageBoxIcon]::Information)
    }
    catch {
        Write-Log "Erreur : $_" -Type "ERROR"
        Update-Status "Erreur lors du telechargement"
        [System.Windows.Forms.MessageBox]::Show("Erreur : $_", "Erreur", 0, [System.Windows.Forms.MessageBoxIcon]::Error)
    }
})
$panel.Controls.Add($btn2)

# ==============================================================================
# BOUTON 3 : VERIFICATION
# ==============================================================================
$btn3 = New-Object System.Windows.Forms.Button
$btn3.Location = New-Object System.Drawing.Point(10,195)
$btn3.Size = New-Object System.Drawing.Size(470,50)
$btn3.Text = "3. Verifier les mises a jour du Framework"
$btn3.BackColor = [System.Drawing.Color]::LightSteelBlue
$btn3.FlatStyle = 'Flat'
$btn3.Cursor = [System.Windows.Forms.Cursors]::Hand

$btn3.Add_Click({
    $nomProjet = $txtAcro.Text.Trim().ToUpper()
    if (-not (Test-NomProjet $nomProjet)) { return }
    
    Update-Status "Verification des mises a jour..."
    try {
        git fetch source-framework --quiet
        $retard = (git rev-list --count "main..source-framework/Carto5_Main")
        if ([int]$retard -gt 0) {
            [System.Windows.Forms.MessageBox]::Show("Framework : $retard mise(s) a jour disponible(s).", "Mises a jour", 0, [System.Windows.Forms.MessageBoxIcon]::Warning)
        } else {
            [System.Windows.Forms.MessageBox]::Show("Framework a jour !", "Info", 0, [System.Windows.Forms.MessageBoxIcon]::Information)
        }
        Update-Status "Verification terminee"
    } catch {
        Update-Status "Erreur de connexion"
        [System.Windows.Forms.MessageBox]::Show("Erreur de connexion.", "Erreur", 0, [System.Windows.Forms.MessageBoxIcon]::Error)
    }
})
$panel.Controls.Add($btn3)

# ==============================================================================
# BOUTON 4 : IMPORTATION
# ==============================================================================
$btn4 = New-Object System.Windows.Forms.Button
$btn4.Location = New-Object System.Drawing.Point(10,255)
$btn4.Size = New-Object System.Drawing.Size(470,50)
$btn4.Text = "4. Importer la mise a jour (Fusion)"
$btn4.BackColor = [System.Drawing.Color]::LightGreen
$btn4.FlatStyle = 'Flat'
$btn4.Cursor = [System.Windows.Forms.Cursors]::Hand

$btn4.Add_Click({
   $nomProjet = $txtAcro.Text.Trim().ToUpper()
if ([string]::IsNullOrWhiteSpace($nomProjet) -or $nomProjet -notmatch "^Carto5_[A-Z0-9]+$") {
    [void][System.Windows.Forms.MessageBox]::Show("Format invalide ! Carto5_XXX requis.", "Erreur", 0, [System.Windows.Forms.MessageBoxIcon]::Stop)
    return
}
    if (-not $nomProjet -or -not (Test-DossierSecurite $nomProjet)) { return }
    
    try {
        Update-Status "Fusion en cours..."
        $date = Get-Date -Format "yyyy_MM_dd_HHmmss"
        git fetch source-framework
        
        # On crée une branche temporaire depuis le framework source
        git checkout -B "maj_framework_$date" source-framework/Carto5_Main
        
        # On va sur framework_depart (pas main)
        git checkout framework_depart
        
        # On fusionne dans framework_depart
        git merge "maj_framework_$date" --no-edit
        
        # On pousse framework_depart sur Azure
        git push origin framework_depart
        
        Update-Status "Fusion terminee dans framework_depart"
        Write-Log "Fusion effectuee dans framework_depart" -Type "SUCCESS"
        [void][System.Windows.Forms.MessageBox]::Show("Fusion effectuee dans 'framework_depart' !`n`nBranche poussee sur Azure.", "Fusion", 0, [System.Windows.Forms.MessageBoxIcon]::Information)
        
    } catch { 
        Write-Log "Erreur : $_" -Type "ERROR"
        [void][System.Windows.Forms.MessageBox]::Show("Erreur : $_", "Erreur", 0, [System.Windows.Forms.MessageBoxIcon]::Error) 
    }
})
$panel.Controls.Add($btn4)

# ==============================================================================
# SECTION 5 : PROPAGATION
# ==============================================================================
$groupBox = New-Object System.Windows.Forms.GroupBox
$groupBox.Location = New-Object System.Drawing.Point(5,315)
$groupBox.Size = New-Object System.Drawing.Size(480,150)
$groupBox.Text = "5. PROPAGATION DE LA VALIDATION (Source: 'unitaire')"
$groupBox.Font = New-Object System.Drawing.Font("Arial", 9, [System.Drawing.FontStyle]::Bold)
$panel.Controls.Add($groupBox)

$chkInteg = New-Object System.Windows.Forms.CheckBox
$chkInteg.Text = "Integration"
$chkInteg.Location = New-Object System.Drawing.Point(20,30)
$chkInteg.Size = New-Object System.Drawing.Size(120,25)
$chkInteg.Checked = $true
$groupBox.Controls.Add($chkInteg)

$chkAccep = New-Object System.Windows.Forms.CheckBox
$chkAccep.Text = "Acceptation"
$chkAccep.Location = New-Object System.Drawing.Point(160,30)
$chkAccep.Size = New-Object System.Drawing.Size(120,25)
$chkAccep.Checked = $true
$groupBox.Controls.Add($chkAccep)

$chkMain = New-Object System.Windows.Forms.CheckBox
$chkMain.Text = "MAIN (PROD)"
$chkMain.Location = New-Object System.Drawing.Point(300,30)
$chkMain.Size = New-Object System.Drawing.Size(130,25)
$chkMain.ForeColor = [System.Drawing.Color]::Red
$chkMain.Font = New-Object System.Drawing.Font("Arial", 8, [System.Drawing.FontStyle]::Bold)
$groupBox.Controls.Add($chkMain)

$btn5 = New-Object System.Windows.Forms.Button
$btn5.Location = New-Object System.Drawing.Point(20,70)
$btn5.Size = New-Object System.Drawing.Size(440,60)
$btn5.Text = "LANCER LA SYNCHRO GLOBALE (Azure)"
$btn5.BackColor = [System.Drawing.Color]::LightSkyBlue
$btn5.FlatStyle = 'Flat'
$btn5.Font = New-Object System.Drawing.Font("Arial", 11, [System.Drawing.FontStyle]::Bold)
$btn5.Cursor = [System.Windows.Forms.Cursors]::Hand

$btn5.Add_Click({
    $nomProjet = $txtAcro.Text.Trim().ToUpper()
    if (-not (Test-NomProjet $nomProjet)) { return }
    if (-not (Test-DossierSecurite $nomProjet)) { return }
    
    $targets = @()
    if ($chkInteg.Checked) { $targets += "integration" }
    if ($chkAccep.Checked) { $targets += "acceptation" }
    if ($chkMain.Checked)  { $targets += "main" }
    
    if ($targets.Count -eq 0) {
        [System.Windows.Forms.MessageBox]::Show("Veuillez cocher au moins une cible.", "Info", 0, [System.Windows.Forms.MessageBoxIcon]::Information)
        return
    }
    
    if ($chkMain.Checked) {
        $confirm = [System.Windows.Forms.MessageBox]::Show(
            "ATTENTION : Vous allez propager vers MAIN (PRODUCTION) !`n`nContinuer ?",
            "Confirmation Production",
            4,
            [System.Windows.Forms.MessageBoxIcon]::Warning
        )
        if ($confirm -ne "Yes") { return }
    }
    
    try {
        Update-Status "Synchronisation en cours..."
        
        git checkout -f unitaire
        
        git add .
        $status = git status --porcelain
        if ($status) {
            git commit -m "Synchro Janus : Sauvegarde auto ($(Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))"
        }
        
        git push origin unitaire
        
        $succes = @()
        foreach ($target in $targets) {
            Write-Host ">>> Propagation vers : $target" -ForegroundColor Cyan
            Update-Status "Propagation vers $target..."
            git checkout -f $target
            git pull origin $target
            git merge unitaire --no-edit
            git push origin $target
            if ($LASTEXITCODE -eq 0) { $succes += $target }
        }
        
        git checkout unitaire
        
        Update-Status "Synchronisation terminee"
        Write-Log "Synchronisation reussie vers : $($succes -join ', ')" -Type "SUCCESS"
        [System.Windows.Forms.MessageBox]::Show("Propagation effectuee avec succes !`n`nBranches mises a jour : $($succes -join ', ')", "Succes", 0, [System.Windows.Forms.MessageBoxIcon]::Information)
    }
    catch {
        Write-Log "Erreur : $_" -Type "ERROR"
        Update-Status "Erreur lors de la synchronisation"
        [System.Windows.Forms.MessageBox]::Show("Erreur : $_", "Erreur", 0, [System.Windows.Forms.MessageBoxIcon]::Error)
        git checkout unitaire
    }
})
$groupBox.Controls.Add($btn5)

# ==============================================================================
# BOUTON SAUVEGARDE RAPIDE
# ==============================================================================
$btnQuickSave = New-Object System.Windows.Forms.Button
$btnQuickSave.Location = New-Object System.Drawing.Point(10,475)
$btnQuickSave.Size = New-Object System.Drawing.Size(470,40)
$btnQuickSave.Text = "Sauvegarde Rapide (Commit + Push sur unitaire)"
$btnQuickSave.BackColor = [System.Drawing.Color]::LightBlue
$btnQuickSave.FlatStyle = 'Flat'
$btnQuickSave.Cursor = [System.Windows.Forms.Cursors]::Hand

$btnQuickSave.Add_Click({
    $nomProjet = $txtAcro.Text.Trim().ToUpper()
    if (-not (Test-NomProjet $nomProjet)) { return }
    
    try {
        Update-Status "Sauvegarde en cours..."
        git checkout unitaire
        git add .
        $status = git status --porcelain
        if ($status) {
            $commitMsg = "Sauvegarde rapide - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
            git commit -m $commitMsg
            git push origin unitaire
            Update-Status "Sauvegarde terminee"
            Write-Log "Sauvegarde rapide effectuee" -Type "SUCCESS"
            [System.Windows.Forms.MessageBox]::Show("Sauvegarde effectuee : $commitMsg", "Succes", 0, [System.Windows.Forms.MessageBoxIcon]::Information)
        } else {
            Update-Status "Aucun changement"
            [System.Windows.Forms.MessageBox]::Show("Aucun changement a sauvegarder", "Info", 0, [System.Windows.Forms.MessageBoxIcon]::Information)
        }
    }
    catch {
        Write-Log "Erreur : $_" -Type "ERROR"
        Update-Status "Erreur lors de la sauvegarde"
        [System.Windows.Forms.MessageBox]::Show("Erreur : $_", "Erreur", 0, [System.Windows.Forms.MessageBoxIcon]::Error)
    }
})
$panel.Controls.Add($btnQuickSave)

# ==============================================================================
# BOUTON AFFICHAGE LOGS
# ==============================================================================
$btnShowLogs = New-Object System.Windows.Forms.Button
$btnShowLogs.Location = New-Object System.Drawing.Point(10,525)
$btnShowLogs.Size = New-Object System.Drawing.Size(470,30)
$btnShowLogs.Text = "Afficher les logs"
$btnShowLogs.BackColor = [System.Drawing.Color]::LightGray
$btnShowLogs.FlatStyle = 'Flat'
$btnShowLogs.Cursor = [System.Windows.Forms.Cursors]::Hand

$btnShowLogs.Add_Click({
    if (Test-Path $global:logPath) {
        Invoke-Item $global:logPath
    } else {
        [System.Windows.Forms.MessageBox]::Show("Aucun log trouve.", "Info", 0, [System.Windows.Forms.MessageBoxIcon]::Information)
    }
})
$panel.Controls.Add($btnShowLogs)

# ==============================================================================
# LANCEMENT
# ==============================================================================
Write-Log "Application Janus PRO demarree" -Type "SUCCESS"
Write-Log "Dossier : $PSScriptRoot" -Type "INFO"
$form.ShowDialog() | Out-Null
Write-Log "Application Janus PRO fermee" -Type "INFO"