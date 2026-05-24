@echo off
setlocal
cd /d "%~dp0"

echo.
echo CertsUI API key E2E — scenarios in src\e2e-tests\scenarios\ (logged to console^).
echo Optional args are passed to the ps1, e.g.  -Scenario Health
echo.

where pwsh >nul 2>&1
if errorlevel 1 (
  echo PowerShell 7+ ^(pwsh^) is required but was not found in PATH.
  echo Install the latest PowerShell 7 from https://github.com/PowerShell/PowerShell/releases
  exit /b 1
)

pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0Test-CertsUiApiKeyE2E.ps1" %*
exit /b %ERRORLEVEL%
