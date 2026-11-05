# Certificación Técnica: Instalador de Windows y Distribución

**Instalador Evaluado:** `Builds/Installer/GorillaEscape-Setup-1.0.exe`  
**Tamaño:** `267,975,699 bytes` (~255.5 MB)  
**SHA-256:** `3783344d75357b5b1a8ec1e03ed64172007a055c1a174482b467eedef8695d6e`  
**Herramienta:** Inno Setup Compiler 6.7.3  
**Estado:** **PASS** (Suite completa de 5 pasos verificada exitosamente).

---

## 1. Verificación E2E de Instalación (`test_installed_distribution.ps1`)

Se ejecutó la prueba automatizada de extremo a extremo obteniendo resultados limpios en cada fase:
1. **Instalación Silenciosa Limpia (`ExitCode=0`):** Despliegue sin intervención de usuario en `Builds/TestInstall`.
2. **Verificación de Layout y Runtimes Embebidos:**
   - `[OK] GorillaEscape.exe`
   - `[OK] UnityPlayer.dll` y `UnityCrashHandler64.exe`
   - `[OK] Java/runtime/bin/java.exe` y `Java/server.jar`
   - `[OK] Vision/python.exe`, `Vision/worker.py` y `Vision/pose_landmarker_lite-v1.task`
   - `[OK] Tools/cloudflared.exe`
   - `[OK] unins000.exe`
3. **Integridad de Python y MediaPipe:**
   - Comprobada importación exitosa de librerías: `certifi`, `mediapipe 1.2.0` y `cv2 5.0.0`.
   - Ejecución del worker de visión en modo replay (`corpus-v1.json`): 24 líneas de telemetría emitidas correctamente.
4. **Integridad de Cloudflared:**
   - Hash SHA-256 verificado: `86AEE4017B26625CEE8484C113558F48EFFA4CD47F7AA05FCF425604E5D2B23C`.
5. **Ejecución y Cierre Supervisado:**
   - El ejecutable instalado inició en segundo plano (PID 28228) y se detuvo limpiamente.
6. **Desinstalación Silenciosa Limpia:**
   - `unins000.exe` eliminó la totalidad de archivos y directorios sin dejar residuos.
