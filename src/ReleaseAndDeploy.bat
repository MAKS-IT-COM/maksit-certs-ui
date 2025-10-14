@echo off
setlocal

REM Get the directory of the current script
set "SCRIPT_DIR=%~dp0"

REM Run the first batch file
call "%SCRIPT_DIR%Release-DockerImage.bat"

REM Run the second batch file
call "%SCRIPT_DIR%Deploy-Helm.bat"

echo All scripts completed.
pause