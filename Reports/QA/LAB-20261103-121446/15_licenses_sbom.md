# 15 — Inventario SBOM y licencias (primera pasada)

Laboratorio: LAB-20261103-121446
Alcance: dependencias directas declaradas en manifiestos + recursos distribuibles identificados.
Límite: no se validaron licencias de assets 3D generados por IA (Meshy) ni de Asset Store; marcados "PENDIENTE".

## 1. Dependencias de runtime directas

| Componente | Dependencia | Versión | Licencia (canónica) | Estado |
|---|---|---|---|---|
| Server (Java) | Spring Boot (parent) | 4.1.1 | Apache-2.0 | OK |
| Server | spring-boot-starter-webmvc/websocket/test | 4.1.1 | Apache-2.0 | OK |
| Server | dev.onvoid.webrtc:webrtc-java | 0.19.0 | Apache-2.0 | OK |
| Server | com.google.zxing:core | 3.5.3 | Apache-2.0 | OK |
| Server | JUnit 5 / Mockito / Jackson (transitivas) | — | EPL-2.0 / MIT / Apache-2.0 | OK |
| PWA | react / react-dom | 19.3.0 | MIT | OK |
| PWA | vite | 8.3.2 | MIT | OK |
| PWA | @vitejs/plugin-react | 6.1.2 | MIT | OK |
| PWA | Lilita One (fuente) | — | SIL OFL 1.1 | OK (LICENSE-Lilita.txt) |
| PWA | Nunito Sans (fuente) | — | SIL OFL 1.1 | OK (LICENSE-Nunito.txt) |
| Companion | flutter / dart SDK | 3.47.6 / 3.13.5 | BSD-3-Clause | OK |
| Companion | cupertino_icons | ^1.0.8 | MIT | OK |
| Companion | mobile_scanner | ^7.4.2 | MIT | OK |
| Companion | flutter_webrtc | ^1.6.2+hotfix.4 | MIT | OK |
| Companion | sensors_plus | ^7.1.1 | BSD-3-Clause | OK |
| Companion | flutter_rotation_sensor | ^0.4.0 | MIT | OK |
| Companion | wakelock_plus | ^1.8.1 | BSD-3-Clause | OK |
| Companion | flutter_lints (dev) | ^6.0.0 | BSD-3-Clause | OK |
| Unity | com.unity.render-pipelines.universal | 17.6.0 | Unity Companion License | OK (módulo Unity) |
| Unity | com.unity.cloud.gltfast | 6.19.0 | Apache-2.0 | OK |
| Unity | com.unity.ai.inference | 2.6.1 | Unity Companion License | OK |
| Unity | com.unity.test-framework | 1.8.0 | Unity Companion License | OK |
| Visión | MediaPipe pose_landmarker_lite | — | Apache-2.0 | OK |
| Visión | OpenCV (Python) | — | Apache-2.0 | OK |
| Visión | Python runtime | 3.12 (portátil) | PSF-2.0 | OK |
| Red | cloudflared | empaquetado | Apache-2.0 (Cloudflare) | OK |
| Instalador | Inno Setup | 6 | Propietaria (gratuita uso libre) | OK (no redistribuir el compilador) |

## 2. Recursos distribuibles y procedencia

| Recurso | Ubicación | Procedencia | Licencia | Estado |
|---|---|---|---|---|
| Fuentes Lilita One / Nunito Sans | PWA/src/assets/fonts | Google Fonts | SIL OFL 1.1 | OK |
| Modelos/texturas 3D | assets/models-and-textures (Git LFS) | "Jungle Party" / Meshy | POR VERIFICAR | PENDIENTE |
| Modelos referenciados Meshy | Unity/MeshyReferences (vacío local) | Meshy AI | POR VERIFICAR | PENDIENTE |
| Gorilla mark (SVG) | Companion/assets/branding | interno | interna | OK |
| Modelo MediaPipe pose_landmarker_lite-v1.task | Unity/Vision | Google MediaPipe | Apache-2.0 | OK |

## 3. Hallazgos de licencia

1. **PENDIENTE (riesgo P1):** los modelos 3D generados con **Meshy** no tienen licencia/procedencia documentada. Meshy impone restricciones según el plan de generación; sin evidencia de plan comercial, la redistribución comercial de los modelos es un riesgo legal. Bloquear distribución comercial hasta resolver.
2. **PENDIENTE:** los modelos en `assets/models-and-textures` usan Git LFS; su licencia de origen (generador de modelos) no está documentada en el repo.
3. **Nota de identidad:** `assets/README.md` aún titula "Biblioteca visual de **Jungle Party**" (branding de prototipo). No es visible al usuario final, pero conviene renombrar por "Gorilla Escape".
4. Sin archivo `LICENSE` en la raíz del repositorio (identidad legal del proyecto ausente).
5. Sin archivo `THIRD-PARTY-NOTICES` consolidado para el paquete distribuible (obligación Apache-2.0 de Spring Boot/OpenCV/MediaPipe: incluir avisos y atribución).

## 4. Auditoría de dependencias ejecutada

- `npm audit --omit=dev --audit-level=high` (PWA): **0 vulnerabilidades**.
- Auditoría de dependencias Java/Flutter: no ejecutada en este entorno (requiere Maven/OWASP dependency-check y `flutter pub outdated`; marcadas NOT RUN).
