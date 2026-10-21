@echo off
setlocal
title Jungle Party - MotionLink y Cloudflare
set "ROOT=%~dp0"
where node >nul 2>nul || (echo Node.js no esta disponible. Instala Node.js 20 o superior.& pause & exit /b 1)
if not exist "%ROOT%cloudflared.exe" (echo Falta cloudflared.exe. Ejecuta ..\PREPARAR_ALPHA.cmd primero.& pause & exit /b 1)
start "MotionLink Server" /min node "%ROOT%server.cjs"
echo Iniciando enlace publico. Copia la direccion https://...trycloudflare.com.
:tunnel
"%ROOT%cloudflared.exe" tunnel --no-autoupdate --protocol http2 --edge-ip-version 4 --url http://127.0.0.1:8788
echo El enlace se desconecto. Reintentando en 3 segundos...
timeout /t 3 /nobreak >nul
goto tunnel
