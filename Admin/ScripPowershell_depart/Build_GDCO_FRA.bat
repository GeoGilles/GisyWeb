@echo off
title Build & Package - Carto5
cd /d "%~dp0"

echo ===========================================
echo   BUILD & PACKAGE - Carto5
echo ===========================================
echo.
echo  1. FRA - Framework Carto5
echo  2. GSR - Gestion Signalisation Routiere
echo  3. GCH - Gestion de chaussees
echo  4. M012 - Gestion des ponceaux
echo  5. IIT - Inventaire infrastructures
echo  6. MRG - Marquage routier
echo  7. SMG - Suivi monitoring
echo  8. SIAS - Securite routiere
echo 20. XXX - Future projet
echo.
set /p choix="Choix : "

if "%choix%"=="1" powershell.exe -NoExit -ExecutionPolicy Bypass -File "%~dp0Build_Carto5_FRA.ps1"
if "%choix%"=="2" powershell.exe -NoExit -ExecutionPolicy Bypass -File "%~dp0Build_Carto5_GSR.ps1"
if "%choix%"=="3" powershell.exe -NoExit -ExecutionPolicy Bypass -File "%~dp0Build_Carto5_GCH.ps1"
if "%choix%"=="4" powershell.exe -NoExit -ExecutionPolicy Bypass -File "%~dp0Build_Carto5_M012.ps1"
if "%choix%"=="5" powershell.exe -NoExit -ExecutionPolicy Bypass -File "%~dp0Build_Carto5_IIT.ps1"
if "%choix%"=="6" powershell.exe -NoExit -ExecutionPolicy Bypass -File "%~dp0Build_Carto5_MRG.ps1"
if "%choix%"=="7" powershell.exe -NoExit -ExecutionPolicy Bypass -File "%~dp0Build_Carto5_SMG.ps1"
if "%choix%"=="8" powershell.exe -NoExit -ExecutionPolicy Bypass -File "%~dp0Build_Carto5_SIAS.ps1"
if "%choix%"=="20" powershell.exe -NoExit -ExecutionPolicy Bypass -File "%~dp0Build_Carto5_XXX.ps1"

pause