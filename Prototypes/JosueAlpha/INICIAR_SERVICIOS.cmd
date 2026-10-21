@echo off
setlocal
set "ROOT=%~dp0"
start "Camera Tracker" "%ROOT%CameraTracker\iniciar_camara.cmd"
start "MotionLink" "%ROOT%MotionRelay\iniciar_motionlink.cmd"
echo Camera Tracker y MotionLink fueron iniciados en ventanas separadas.
echo Manten ambas ventanas abiertas mientras Unity esta en ejecucion.
timeout /t 4 /nobreak >nul
