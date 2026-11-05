# Certificación Técnica: Clientes Flutter Android y PWA Web

**Estado:** **PASS** (Compilación, firmas, pruebas unitarias y análisis estático superados).

---

## 1. Cliente Flutter Android (`Companion`)

- **Versión:** Flutter 3.47.6, Dart 3.13.5.
- **Análisis Estático:** `flutter analyze` finalizado con **0 advertencias y 0 errores**.
- **Pruebas Unitarias:** `flutter test` ejecutado exitosamente con **0 fallos**.
- **Binario Oficial:** `Companion/build/app/outputs/flutter-apk/app-release.apk` (102,966,626 bytes, SHA-256: `60d482c228ca2732e99af6e656afb7ee5fb672d12775cf04fc28048b888a62e9`).
- **Permisos Android:** Solicitud transparente de sensores de acelerómetro, giroscopio y vibración táctil.

---

## 2. Cliente PWA Web (`PWA`)

- **Entorno:** Node 24.0.0 (Aislamiento DSH respetando `engine-strict=true`).
- **Ciclo de Construcción:** `npm ci`, `npm test` y `npm run build` ejecutados exitosamente (`ci=0 test=0 build=0`).
- **Empaquetado Estático:** Los artefactos generados en `PWA/dist/` se integran automáticamente dentro del JAR del servidor Java (`static/`).
- **Compatibilidad Móvil:** Soporte para `DeviceMotionEvent` y `DeviceOrientationEvent` con fallback táctil si el navegador carece de giroscopio.
