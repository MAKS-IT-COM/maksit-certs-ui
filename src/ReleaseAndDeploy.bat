@echo off
setlocal

REM Get the directory of the current script
set "SCRIPT_DIR=%~dp0"

REM Invoke the PowerShell script (Release-NuGetPackage.ps1) in the same directory
powershell -ExecutionPolicy Bypass -File "%~dp0Release.ps1"

REM Invoke the PowerShell script (Release-NuGetPackage.ps1) in the same directory
powershell -ExecutionPolicy Bypass -File "%~dp0Deploy-Helm.ps1"

echo All scripts completed.
pause