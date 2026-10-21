@echo off
setlocal
title Jungle Party - Camera Tracker
set "ROOT=%~dp0"
if not exist "%ROOT%.venv\Scripts\python.exe" (
  echo Falta preparar Camera Tracker. Ejecuta ..\PREPARAR_ALPHA.cmd primero.
  pause
  exit /b 1
)
"%ROOT%.venv\Scripts\python.exe" "%ROOT%camera_tracker.py"
if errorlevel 1 pause
