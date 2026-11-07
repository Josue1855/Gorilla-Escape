# Gorilla Escape 1.0 — Manifiesto Técnico de la Release Candidate (RC1)

**Identificador de Certificación:** `RELEASE-CERTIFICATION-20261103-190500`  
**Fecha de Generación:** 3 de noviembre de 2026, 19:05:00 CST  
**Objetivo de Publicación:** 16 de noviembre de 2026  
**Rama de Git:** `feature/saul-jungle-party-alpha`  
**Commit Base:** `4a3ee0e4d1e802807a516e055b34b3bb28a3b77a`  
**Estado del Repositorio:** Cambios verificados en Splash, Rig, Fixture y Full Lab (sin archivos no rastreados en build).

---

## 1. Identificadores y Hashes Criptográficos de los Binarios

| Componente / Artefacto | Ruta en Repositorio | Versión / Metadata | Tamaño (Bytes) | Hash Criptográfico SHA-256 |
| :--- | :--- | :--- | :--- | :--- |
| **Instalador Oficial Windows** | `Builds/Installer/GorillaEscape-Setup-1.0.exe` | Inno Setup 6.7.3 (Single Exe) | 267,975,699 | `3783344d75357b5b1a8ec1e03ed64172007a055c1a174482b467eedef8695d6e` |
| **Ejecutable Windows Standalone** | `Builds/Windows/GorillaEscape.exe` | Unity 6000.6.0f1 x64 Stub | 667,136 | `a4c71b1bfc3e42d02f2f129fe04adef3ebf2343a662884e4b99598e9dfc90e7d` |
| **Ensamblado C# del Videojuego** | `Builds/Windows/GorillaEscape_Data/Managed/Assembly-CSharp.dll` | Release Final (Splash/Rig fixes) | 719,872 | `52d76c82d160a3750708d13662d82fb7cf205334e48bf6e40df19a7bc64d2d7e` |
| **APK Oficial Android (Companion)** | `Companion/build/app/outputs/flutter-apk/app-release.apk` | Flutter 3.47.6 (Release signed) | 102,966,626 | `60d482c228ca2732e99af6e656afb7ee5fb672d12775cf04fc28048b888a62e9` |
| **Servidor Java IPC / WebRTC** | `Builds/Windows/Java/server.jar` | Spring Boot 4.1.1, Java 21 | 31,747,370 | `dc6eb65aff30543fd7f4feec6c71668e81562ed744c22c04e171a8510040dffa` |
| **Túnel Cloudflared Portátil** | `Builds/Windows/Tools/cloudflared.exe` | Cloudflared 2026.10.0 x64 | 55,365,048 | `86aee4017b26625cee8484c113558f48effa4cd47f7aa05fcf425604e5d2b23c` |
| **Runtime Python Portátil (Visión)** | `Builds/Windows/Vision/python.exe` | Python 3.12 Portátil + MediaPipe 1.2 | 105,744 | `96a7db1ef4d0b16f3ea83a4c0eb3ea34e56999238e8ec43fef8bdf619e09d1b0` |
| **Modelo MediaPipe Pose Landmarker** | `Builds/Windows/Vision/pose_landmarker_lite-v1.task` | MediaPipe Lite Pose Task | 9,376,014 | `d175658e37c376174a8cf325d97f5a828e18e8061f1c7138aa0956b9c9f6d3bf` |
| **Despliegue PWA Web** | `PWA/dist/` (embebido en `server.jar`) | Vite 5 + Node 24 | Bundled | `a147e452be6ffba63c5a31a9de4f129a0082b4cc601243171891040ea794e339` |

---

## 2. Entorno y Runtimes de Compilación y Validación

- **Sistema Operativo Anfitrión:** Windows 11 Professional (Build 10.0.29698 x64).
- **Motor Gráfico:** Unity 6000.6.0f1 (Unity 6 Tech Preview).
- **Pipeline de Renderizado:** Universal Render Pipeline (URP Lit / Standard Lit Shaders).
- **Entorno Java:** Java Development Kit 21 (`java version 27.0.1.0.3` / HotSpot 64-Bit).
- **Framework Backend:** Spring Boot 4.1.1, Netty, dev.onvoid.webrtc 0.19.0, ZXing 3.5.3.
- **Entorno Flutter:** Flutter 3.47.6, Dart 3.13.5, Gradle 8.4.
- **Entorno Node / PWA:** Node 24.0.0 (DSH Isolation), npm 10.8.0.
- **Entorno Visión:** Python 3.12.0 x64, MediaPipe 1.2.0, OpenCV 5.0.0 (cv2).
- **Empaquetador Windows:** Inno Setup Compiler 6.7.3.
