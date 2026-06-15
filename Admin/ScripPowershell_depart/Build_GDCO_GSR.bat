@echo off
title Build & Package - Carto5 GSR
cd /d "%~dp0"

echo ===========================================
echo   BUILD & PACKAGE - Carto5 GSR
echo ===========================================
echo.

powershell.exe -NoExit -ExecutionPolicy Bypass -File "%~dp0Build_Carto5_GSR.ps1"