@echo off
setlocal
title Preparar Jungle Party Alpha
set "ROOT=%~dp0"
set "PYTHON_CMD="
where py >nul 2>nul && set "PYTHON_CMD=py -3.12"
if not defined PYTHON_CMD where python >nul 2>nul && set "PYTHON_CMD=python"
if not defined PYTHON_CMD (echo Instala Python 3.12 de 64 bits y vuelve a ejecutar este archivo.& pause & exit /b 1)

if not exist "%ROOT%CameraTracker\.venv\Scripts\python.exe" %PYTHON_CMD% -m venv "%ROOT%CameraTracker\.venv" || goto :error
"%ROOT%CameraTracker\.venv\Scripts\python.exe" -m pip install --upgrade pip || goto :error
"%ROOT%CameraTracker\.venv\Scripts\python.exe" -m pip install -r "%ROOT%CameraTracker\requirements.txt" || goto :error

where node >nul 2>nul || (echo Instala Node.js 20 o superior antes de iniciar MotionLink.& pause & exit /b 1)
if not exist "%ROOT%MotionRelay\cloudflared.exe" (
  echo Descargando Cloudflare Tunnel...
  powershell -NoProfile -ExecutionPolicy Bypass -Command "Invoke-WebRequest -UseBasicParsing 'https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe' -OutFile '%ROOT%MotionRelay\cloudflared.exe'" || goto :error
)
echo.
echo Preparacion terminada. Ya puedes ejecutar INICIAR_SERVICIOS.cmd.
pause
exit /b 0
:error
echo.
echo La preparacion no termino. Revisa el mensaje anterior y vuelve a intentarlo.
pause
exit /b 1
