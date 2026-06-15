@echo off
title Lancement du Gestionnaire Carto5
cd /d "%~dp0"

echo ===========================================
echo   LANCEMENT DU GESTIONNAIRE PROJET JANUS
echo ===========================================
echo.

powershell.exe -NoExit -ExecutionPolicy Bypass -File "%~dp0Gestionnaire_Carto5_V1.ps1"

pause