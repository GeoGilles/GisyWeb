# ==============================================================================
# PROJET JANUS - GESTIONNAIRE Carto5 (VERSION FINALE CONSOLIDÉE)
# ==============================================================================
# 1. GESTION DES ACCENTS ET ENCODAGE
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Set-Location $PSScriptRoot

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$form = New-Object System.Windows.Forms.Form
$form.Text = "Projet Janus - Gestionnaire Carto5"
$form.Size = New-Object System.Drawing.Size(420,500)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false

# --- CONTROLES ---
$labelAcro = New-Object System.Windows.Forms.Label
$labelAcro.Location = New-Object System.Drawing.Point(20,20)
$labelAcro.Size = New-Object System.Drawing.Size(360,20)
$labelAcro.Text = "Nom COMPLET du projet (OBLIGATOIRE : Carto5_XXX) :"
$form.Controls.Add($labelAcro)

$txtAcro = New-Object System.Windows.Forms.TextBox
$txtAcro.Location = New-Object System.Drawing.Point(20,40)
$txtAcro.Size = New-Object System.Drawing.Size(360,20)
$form.Controls.Add($txtAcro)

# --- FONCTIONS DE SECURITE (TES VALIDATIONS) ---
function Test-FormatNom {
    $nom = $txtAcro.Text.Trim().ToUpper()
    if (-not ($nom -match "^Carto5_[A-Z0-9]+$")) {
        $msg = "ERREUR DE FORMAT !`n`nLe nom doit IMPERATIVEMENT commencer par 'Carto5_' suivi de votre acronyme.`n`nExemple valide : Carto5_M012"
        [void][System.Windows.Forms.MessageBox]::Show($msg, "Rejet de securite", 0, [System.Windows.Forms.MessageBoxIcon]::Stop)
        return $null
    }
    return $nom
}

function Test-DossierSecurite ($nomProjet) {
    $dossierCourant = (Get-Item .).Name
    if ($dossierCourant -ne $nomProjet) {
        $msgErreur = "VERROUILLAGE ACTIF !`n`nLe script doit etre execute dans un dossier nomme EXACTEMENT : $nomProjet.`nDossier actuel : $dossierCourant."
        [void][System.Windows.Forms.MessageBox]::Show($msgErreur, "Mauvais Repertoire", 0, [System.Windows.Forms.MessageBoxIcon]::Stop)
        return $false
    }
    return $true
}

# --- BOUTONS ---

# BOUTON 1 : INITIALISATION (L'UNIQUE "BIG BANG")
$btn1 = New-Object System.Windows.Forms.Button
$btn1.Location = New-Object System.Drawing.Point(20,80)
$btn1.Size = New-Object System.Drawing.Size(360,45)
$btn1.Text = "1. KIT DE DEPART (Creation initiale SEULEMENT)"
$btn1.BackColor = [System.Drawing.Color]::LightCoral
$btn1.Add_Click({
    $nomProjet = Test-FormatNom
    if (-not $nomProjet) { return }
    if (-not (Test-DossierSecurite $nomProjet)) { return }

    # SECURITÉ A : Vérification locale
    if (Test-Path ".git") {
        [void][System.Windows.Forms.MessageBox]::Show("DANGER : Deja initialise ! Action bloquee.", "Stop", 0, [System.Windows.Forms.MessageBoxIcon]::Stop)
        return
    }

    # SECURITÉ B : Vérification Azure (Le serveur doit être vide)
    $urlAzure = "https://transports.visualstudio.com/Carte_Carto5/_git/$nomProjet"
    $check = git ls-remote $urlAzure 2>&1
    if ($check -match "refs/") {
        [void][System.Windows.Forms.MessageBox]::Show("STOP : Azure contient deja des donnees. Utilisez le Bouton 2.", "Securite Serveur", 0, [System.Windows.Forms.MessageBoxIcon]::Stop)
        return
    }

    try {
        git init -b main
        git remote add origin $urlAzure
        git remote add source-framework "https://transports.visualstudio.com/Carte_Carto5/_git/Carte_Carto5"
        git fetch source-framework
        
        # Aspiration de la source Framework
        git reset --hard source-framework/Carto5_Main
        
        # Renommage de la solution .sln
        if (Test-Path "Carto5_MapLibre.sln") {
            git mv "Carto5_MapLibre.sln" "$nomProjet.sln"
            git commit -m "Initialisation : Renommage de la solution pour $nomProjet"
        }
        
        # Creation des branches standards
        git branch -f framework_depart main
        git branch -f integration main
        git branch -f acceptation main
        git branch -f unitaire main
        
        # Envoi initial (Le -f ecrase un eventuel README vide)
        git push -f -u origin main integration acceptation unitaire framework_depart
        git checkout unitaire
        
        [void][System.Windows.Forms.MessageBox]::Show("Projet cree ! Branche active : unitaire.", "Succes", 0, [System.Windows.Forms.MessageBoxIcon]::Information)
        
        # SECURITÉ C : Desactivation définitive du bouton
        $btn1.Enabled = $false
        $btn1.BackColor = [System.Drawing.Color]::LightGray
        $btn1.Text = "1. KIT DE DEPART (VERROUILLE)"
        
    } catch { [void][System.Windows.Forms.MessageBox]::Show("Erreur : $_", "Erreur", 0, [System.Windows.Forms.MessageBoxIcon]::Error) }
})
$form.Controls.Add($btn1)

# BOUTON 2 : RECREER L'ENVIRONNEMENT LOCAL (INTELLIGENT)
$btn2 = New-Object System.Windows.Forms.Button
$btn2.Location = New-Object System.Drawing.Point(20,135)
$btn2.Size = New-Object System.Drawing.Size(360,45)
$btn2.Text = "2. Recreer local (Telecharger depuis Azure)"
$btn2.BackColor = [System.Drawing.Color]::LightYellow
$btn2.Add_Click({
    $nomProjet = Test-FormatNom
    if (-not $nomProjet) { return }
    if (-not (Test-DossierSecurite $nomProjet)) { return }
    if (Test-Path ".git") { [void][System.Windows.Forms.MessageBox]::Show("Deja connecte !", "Info", 0, [System.Windows.Forms.MessageBoxIcon]::Information); return }

    try {
        git init
        git remote add origin "https://transports.visualstudio.com/Carte_Carto5/_git/$nomProjet"
        git fetch origin
        
        if ([string]::IsNullOrWhiteSpace((git branch -r))) {
            Remove-Item -Recurse -Force ".git"
            [void][System.Windows.Forms.MessageBox]::Show("Azure est VIDE. Utilisez le Bouton 1.", "Vide", 0, [System.Windows.Forms.MessageBoxIcon]::Warning); return
        }
        
        # On attache les branches standards
        git branch --track main origin/main 2>$null
        git branch --track integration origin/integration 2>$null
        git branch --track acceptation origin/acceptation 2>$null
        git branch --track unitaire origin/unitaire 2>$null
        git branch --track framework_depart origin/framework_depart 2>$null
        
        # Focus sur UNITAIRE pour le developpeur
        git checkout -f unitaire
        
        git remote add source-framework "https://transports.visualstudio.com/Carte_Carto5/_git/Carte_Carto5"
        git fetch source-framework
        [void][System.Windows.Forms.MessageBox]::Show("Local restaure sur 'unitaire' !", "Succes", 0, [System.Windows.Forms.MessageBoxIcon]::Information)
    } catch { [void][System.Windows.Forms.MessageBox]::Show("Erreur.", "Erreur", 0, [System.Windows.Forms.MessageBoxIcon]::Error) }
})
$form.Controls.Add($btn2)

# BOUTON 3 : VERIFICATION (LE RADAR)
$btn3 = New-Object System.Windows.Forms.Button
$btn3.Location = New-Object System.Drawing.Point(20,190)
$btn3.Size = New-Object System.Drawing.Size(360,45)
$btn3.Text = "3. Verifier les mises a jour du Framework"
$btn3.Add_Click({
    $nomProjet = Test-FormatNom
    if (-not $nomProjet -or -not (Test-DossierSecurite $nomProjet)) { return }
    try {
        git fetch source-framework --quiet
        $retard = (git rev-list --count "main..source-framework/Carto5_Main")
        if ([int]$retard -gt 0) { [void][System.Windows.Forms.MessageBox]::Show("Framework : $retard mise(s) a jour.", "Radar", 0, [System.Windows.Forms.MessageBoxIcon]::Warning) }
        else { [void][System.Windows.Forms.MessageBox]::Show("A jour !", "Info", 0, [System.Windows.Forms.MessageBoxIcon]::Information) }
    } catch { [void][System.Windows.Forms.MessageBox]::Show("Erreur de connexion.", "Erreur", 0, [System.Windows.Forms.MessageBoxIcon]::Error) }
})
$form.Controls.Add($btn3)

# BOUTON 4 : IMPORTATION (FUSION)
$btn4 = New-Object System.Windows.Forms.Button
$btn4.Location = New-Object System.Drawing.Point(20,245)
$btn4.Size = New-Object System.Drawing.Size(360,45)
$btn4.Text = "4. Importer la mise a jour (Fusion temporaire)"
$btn4.BackColor = [System.Drawing.Color]::LightGreen
$btn4.Add_Click({
    $nomProjet = Test-FormatNom
    if (-not $nomProjet -or -not (Test-DossierSecurite $nomProjet)) { return }
    try {
        $date = Get-Date -Format "yyyy_MM_dd"
        git fetch source-framework
        git checkout -B "maj_framework_$date" source-framework/Carto5_Main
        git checkout main
        git merge "maj_framework_$date" --no-commit
        [void][System.Windows.Forms.MessageBox]::Show("Fusion preparee sur 'main'. Verifiez dans Visual Studio.", "Fusion", 0, [System.Windows.Forms.MessageBoxIcon]::Information)
    } catch { [void][System.Windows.Forms.MessageBox]::Show("Erreur.", "Erreur", 0, [System.Windows.Forms.MessageBoxIcon]::Error) }
})
$form.Controls.Add($btn4)

# --- SECTION 5 : ORCHESTRATEUR DE PROPAGATION (JANUS) ---
$lblSync = New-Object System.Windows.Forms.Label
$lblSync.Location = New-Object System.Drawing.Point(20,300)
$lblSync.Size = New-Object System.Drawing.Size(360,20)
$lblSync.Text = "5. PROPAGATION DE LA VALIDATION (Source: 'unitaire') :"
$form.Controls.Add($lblSync)

# Cases à cocher pour les cibles de propagation
$chkInteg = New-Object System.Windows.Forms.CheckBox
$chkInteg.Text = "Integration"; $chkInteg.Location = New-Object System.Drawing.Point(30,320); $chkInteg.Checked = $true
$form.Controls.Add($chkInteg)

$chkAccep = New-Object System.Windows.Forms.CheckBox
$chkAccep.Text = "Acceptation"; $chkAccep.Location = New-Object System.Drawing.Point(130,320); $chkAccep.Checked = $true
$form.Controls.Add($chkAccep)

$chkMain = New-Object System.Windows.Forms.CheckBox
$chkMain.Text = "MAIN (PROD)"; $chkMain.Location = New-Object System.Drawing.Point(240,320); $chkMain.ForeColor = [System.Drawing.Color]::Red
$form.Controls.Add($chkMain)

$btn5 = New-Object System.Windows.Forms.Button
$btn5.Location = New-Object System.Drawing.Point(20,350)
$btn5.Size = New-Object System.Drawing.Size(360,60)
$btn5.Text = "LANCER LA SYNCHRO GLOBALE (Azure)"
$btn5.BackColor = [System.Drawing.Color]::LightSkyBlue
$btn5.Font = New-Object System.Drawing.Font("Arial", 10, [System.Drawing.FontStyle]::Bold)

$btn5.Add_Click({
    # A. Sécurité de base
    $nomProjet = Test-FormatNom
    if (-not $nomProjet) { return }

    # B. On force le retour sur UNITAIRE pour démarrer la source de vérité
    Write-Host "--- Préparation de la source (unitaire) ---"
    git checkout -f unitaire
    
    # C. Commit automatique de ton travail actuel pour ne rien perdre
    git add .
    $status = git status --porcelain
    if ($status) {
        git commit -m "Synchro Janus : Sauvegarde automatique avant propagation ($(Get-Date))"
    }

    # D. Push de sécurité : On envoie ton travail 'unitaire' sur Azure en premier
    git push origin unitaire

    # E. La Boucle de Propagation
    $targets = @()
    if ($chkInteg.Checked) { $targets += "integration" }
    if ($chkAccep.Checked) { $targets += "acceptation" }
    if ($chkMain.Checked)  { $targets += "main" }

    if ($targets.Count -eq 0) {
        [void][System.Windows.Forms.MessageBox]::Show("Veuillez cocher au moins une cible.", "Info")
        return
    }

    $succes = @()
    try {
        foreach ($target in $targets) {
            Write-Host ">>> PROPAGATION VERS : $target"
            
            git checkout -f $target           # On saute sur la branche cible
            git pull origin $target           # On récupère le dernier état d'Azure
            git merge unitaire --no-edit      # On injecte la validation d'unitaire
            
            $res = git push origin $target    # On renvoie la branche mise à jour sur Azure
            if ($LASTEXITCODE -eq 0) { $succes += $target }
        }

        # Retour à la case départ
        git checkout unitaire
        
        $msgFinal = "SYNCHRONISATION TERMINÉE !`n`nLes branches suivantes sont à jour sur Azure :`n" + ($succes -join "`n")
        [void][System.Windows.Forms.MessageBox]::Show($msgFinal, "Janus Succès", 0, [System.Windows.Forms.MessageBoxIcon]::Information)

    } catch {
        [void][System.Windows.Forms.MessageBox]::Show("CONFLIT DÉTECTÉ ! La synchro s'est arrêtée. Vérifiez manuellement dans Visual Studio.", "Erreur", 0, [System.Windows.Forms.MessageBoxIcon]::Error)
        git checkout unitaire
    }
})
$form.Controls.Add($btn5)

$form.ShowDialog() | Out-Null