@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start-GorillaEscape.ps1"
set "EXIT=%ERRORLEVEL%"
if not "%EXIT%"=="0" pause
exit /b %EXIT%
