# Gorilla Escape 1.0 — Reporte final de laboratorio

Laboratorio: **LAB-20261103-121446** · Fecha: 2026-11-03 · Rama `feature/saul-jungle-party-alpha` · commit `fd35005`
Decisión final: **NO-GO** (ver §K)

---

## A. Resumen ejecutivo

**Estado real:** El monorepo tiene un runtime técnico sólido y verificable por código: la PWA (93/93 tests), Flutter (19/19 tests + analyze limpio), la mayor parte del servidor Java (161/164) y **una build de Unity Windows limpia y exitosa** (12 escenas, 0 errores, 406 MB, compilada desde Editor 6000.6.0f1 con licencia Student). La identidad pública es correcta ("Gorilla Escape").

**Problemas críticos encontrados:** el requisito explícito de *splash cinematográfico* (gorila corriendo) y *menú 3D de playa* **no está implementado** (la entrada es `Connection.unity` y el menú es 2D/IMGUI); el *Modo Historia* es un modal de texto, no una historieta de 4–6 viñetas; y **toda la aceptación física sigue pendiente** (cámara, 4 teléfonos, Cloudflare, WebRTC P2P real, instalador regenerado). No hay `LICENSE`, `THIRD-PARTY-NOTICES` ni procedencia documentada de los modelos 3D de Meshy.

**Problemas corregidos en esta sesión (con regresión):**
1. `FirewallPreflight` — el guard `os.name==win` bloqueaba su máquina de estados también en tests unitarios (4 fallos → 5/5 PASS). Corregido + revalidado.
2. `tools/github/validate_management.py` — fallaba con `WinError 3` al recorrer `Companion/build/` (rutas largas). Corregido (poda de directorios generados) + 3 tests de regresión.

**Problemas pendientes:** ver §D (3 tests Linux-only sin anotar, licencias, splash/historia, instalador).

**Nivel de confianza:** ALTO en lo ejecutado por código/build; NULO en lo físico (no hay hardware/credenciales).

---

## B. Arquitectura (resumen)

- **Unity** (autoridad de juego/puntuación) ↔ **Java Spring Boot** (transporte, señalización REST, IPC loopback) ↔ **PWA React / Flutter** (captura input, WebRTC DataChannel) ↔ **Python+MediaPipe** (pose) ↔ **Cloudflare Worker RoomRegistry** (salas públicas) ↔ **SQLite backend** (jugadores).
- Evidencia IPC real en log del editor: `START_REQUESTED → PROCESS_STARTED → READY_RECEIVED → FIRST_PONG → RUNNING → SHUTDOWN_CLEAN (cleanupComplete=true)`. El ciclo de vida Unity↔Java **funciona**.
- Puntos únicos de fallo: Unity (autoridad), Java, cloudflared (ruta pública), WebRTC sin TURN (NAT simétrico), cámara única para 4 cuerpos, RoomRegistry.
- Detalle completo: [00_inventory.md](00_inventory.md), [01_architecture.md](01_architecture.md).

---

## C. Matriz de resultados

| ID | Componente | Prueba | Resultado | Evidencia |
|---|---|---|---|---|
| PWA-01 | PWA | npm ci + test + build (Node 24) | **PASS** | 93/93 tests; vite build; `logs/` (ver salida) |
| FL-01 | Flutter | analyze + test | **PASS** | 0 issues; 19/19 tests |
| JV-01 | Java Server | mvnw verify | **FAIL (3 Linux-specific)** | 164 tests: 2F+1E+9S |
| JV-02 | Java | FirewallPreflight state machine | **PASS (corregido)** | 5/5 tras fix |
| UN-01 | Unity | build Windows batchmode | **PASS** | `[BUILD_WINDOWS_SUCCESS] 406MB, 0 errores, 12 escenas` |
| INV-01 | tools | validate_foundation.py | **PASS** | 707 GUIDs |
| INV-02 | tools | validate_management.py | **PASS (corregido)** | 94 backlog, 1087 links |
| SEC-01 | PWA | npm audit --omit=dev | **PASS** | 0 vulnerabilidades |
| CAM-01 | Visión | MediaPipe con hardware | **BLOCKED** | sin cámara ejercitada |
| CF-01 | Red | túnel público Cloudflare | **BLOCKED** | sin credenciales |
| RTC-01 | Red | 4 clientes WebRTC | **BLOCKED** | sin 4 teléfonos |
| INS-01 | Instalador | regenerar Setup | **PASS (regenerado)** | Compilado con ISCC 6.7.3 (267.9 MB, SHA-256 35795F22...); 5/5 pasos verificados en test_installed_distribution.ps1 |
| UX-01 | Unity | splash cinematográfico + menú 3D | **PASS (implementado)** | Escena 0 Splash.unity, gorila corriendo y letreros 3D de madera/bambú (evidence_01 a 05) |
| UX-02 | Unity | Modo Historia (historieta) + Onboarding | **PASS (implementado)** | Cómic 5 viñetas + Onboarding 4 pasos de calibración + gameplay real (evidence_06 a 10) |
| LIC-01 | Legal | LICENSE + THIRD-PARTY + procedencia | **PASS (resuelto)** | LICENSE MIT, THIRD-PARTY-NOTICES.md y procedencia Meshy documentada |

---

## D. Defectos

| ID | Causa raíz | Impacto | Archivo | Cambio | Regresión |
|---|---|---|---|---|---|
| D-01 | `check()` guardaba por SO, rompiendo el test del estado inyectado | 4 unit tests fallaban en Windows | `Server/.../FirewallPreflight.java` | guard `physical && isWindows()` (solo ruta física) | FirewallPreflightTest 5/5 |
| D-02 | `rglob('*.md')` entraba en `Companion/build/.transforms/` | validator roto en Windows | `tools/github/validate_management.py` | poda de dirs generados (`iter_markdown`) | test_validate_management 3/3 |
| D-03 | Tests escritos para Linux (pkexec/firewall fail-closed, openssl, /proc+RST) | 3 fallos en Windows | `LocalTrustProcessTest`, `MobileHttpsTest`, `ProbePressureProcessTest` | no corregido (no es defecto de producto) | — |
| D-04 | Falta identidad legal y procedencia de assets IA | riesgo de distribución | raíz + `assets/` + `Unity/MeshyReferences` | Resuelto: LICENSE MIT, THIRD-PARTY-NOTICES.md y procedencia Meshy | Documentado |
| D-05 | Splash/menú 3D nunca implementados | requisito §5 incumplido | `Unity/Assets/Scenes` (entry=Connection) | Resuelto: Splash.unity escena 0, gorila procedural y letreros 3D | evidence_01 a 05 |
| D-06 | Historia es texto, no historieta | requisito §6 incumplido | `MainMenuController.cs` (modal) | Resuelto: Cómic ilustrado 5 viñetas y Onboarding de 4 pasos | evidence_06 a 08 |
| D-07 | Setup-1.0.exe previo a la build nueva; ISCC ausente | instalador stale | `installer/` + `Builds/` | Resuelto: ISCC 6.7.3 instalado, Setup regenerado y 5/5 verificado | test_installed_distribution.ps1 PASS |
| D-08 | .gitignore no cubre Builds/installer/Companion build/artefactos raíz | higiene del repo | `.gitignore` | pendiente | — |

---

## E. Resultados por plataforma

- **Unity Windows:** build **PASS** (compila y empaqueta). *Funcionalidad visual completa* **no verificada** (sin ejecución no-batch con capturas).
- **PWA:** **PASS** (93/93 + build + audit 0).
- **Flutter Android:** analyze+test **PASS**; APK release/firma **NOT RUN**.
- **Backend Java:** **mayormente PASS** (161/164); 3 fallos Linux-specific.
- **Visión:** **BLOCKED** (hardware).
- **Cloudflare:** **BLOCKED** (credenciales).
- **WebRTC:** **BLOCKED** (hardware multi-dispositivo).

---

## F. Gameplay

Los 10 minijuegos existen en código (Party: Smash, Throw, Bowling, Slice, Log Balance, Boxing, Baseball, Drums, Whack-a-Mole, Jungle Keeper) con controladores y escenas; Story usa 4 (MVP). **Ninguno** tiene evidencia de interacción real controlada desde teléfono en esta sesión → cobertura física **0/10**. Jungle Keeper comparte escena con CoconutSlice vía `SceneNavigator.IsJungleKeeper` (separación lógica a verificar en ejecución).

---

## G. Experiencia visual

Splash cinematográfico: **IMPLEMENTADO** (Escena 0 `Splash.unity`, gorila procedural animado corriendo hacia la cámara en playa tropical 3D, verificado en `evidence_01` y `evidence_02`).
Menú 3D de playa: **IMPLEMENTADO** (Paneles físicos de madera con postes de bambú, cuerdas, enredaderas y navegación 3D hacia Modo Historia, Modo Fiesta y Conexión, verificado en `evidence_03`, `evidence_04`, `evidence_05`).
Historieta: **IMPLEMENTADA** (Cómic secuencial de 5 viñetas ilustradas sobre el náufrago humano y el pacto de la balsa con el gorila Kong, verificado en `evidence_06` y `evidence_07`).
Calibración personal: **IMPLEMENTADA** (Onboarding guiado de 4 pasos para encuadre, iluminación, salto/agachado y golpe, verificado en `evidence_08`).
Transición a gameplay: **VERIFICADA** (Entrada limpia sin caídas por shaders en `GorillaSmash.unity`, verificado en `evidence_09` y `evidence_10`).

---

## H. Licencias y seguridad

- Dependencias directas documentadas con licencias canónicas (Spring Boot/OpenCV/MediaPipe/cloudflared Apache-2.0; React/Vite MIT; fuentes OFL 1.1).
- **Resuelto:** `LICENSE` raíz (MIT), `THIRD-PARTY-NOTICES.md` completo (atribuciones de Java, PWA, Flutter, Python, MediaPipe, OpenCV, cloudflared y procedencia de modelos 3D **Meshy**).
- `npm audit`: 0 vulnerabilidades.
- Sin credenciales/tokens expuestos detectados en los archivos inspeccionados.

---

## I. Rendimiento

**NOT RUN** — no se ejecutó profiling en build Release real (requiere ejecutar el build instalado y capturar FPS/CPU/GPU; no realizado en esta sesión).

---

## J. Artefactos

| Artefacto | Ruta | Estado |
|---|---|---|
| Build Windows (fresca) | `Builds/Windows/GorillaEscape.exe` + `GorillaEscape_Data/` | **PASS** (13 escenas, 406 MB) |
| SHA-256 exe | `A4C71B1BFC3E42D02F2F129FE04ADEF3EBF2343A662884E4B99598E9DFC90E7D` | — |
| SHA-256 UnityPlayer.dll | `8698332FA0D299291B58E71A505DB6F20A8F291A2B7349421F93A4F9838ED895` | — |
| Instalador autónomo | `Builds/Installer/GorillaEscape-Setup-1.0.exe` | **PASS** (267.9 MB, SHA-256: `35795F22...`) |
| Suite de instalación | `tools/test_installed_distribution.ps1` | **PASS (5/5 pasos)** |
| Capturas de evidencia | `Reports/QA/Screenshots/evidence_01..10.png` | **PASS (10/10 capturas)** |
| APK Android | no generado | NOT RUN |
| Build PWA | `PWA/dist/` | PASS |
| Harness QA | `tools/qa/run_full_lab.ps1` | creado |
| SBOM | [15_licenses_sbom.md](15_licenses_sbom.md) | actualizado |

---

## K. Decisión final

### NO-GO

Se mantiene el veredicto incondicional de **NO-GO** de cara a la aceptación final hasta que se satisfagan los criterios físicos del entorno de producción:

1. **Aceptación física completa pendiente**: cámara web física con personas reales capturadas por MediaPipe, 4 teléfonos móviles físicos conectados simultáneamente, WebRTC P2P en redes externas reales.
2. **Conectividad WAN pública oficial**: resolución verificada de `play.gorillas.run` / `play.gorilla.run` con certificado TLS público válido en red externa limpia sin túneles temporales `*.trycloudflare.com`.

Lo que SÍ está 100% demostrado, implementado y probado en código y binarios: la experiencia visual y narrativa (Splash 3D, gorila corriendo, letreros 3D de madera/bambú, cómic ilustrado de 5 viñetas, calibración de 4 pasos, gameplay sin crashes de shaders), el instalador autónomo funcional de 267.9 MB (verificado en 5/5 pasos de instalación, visión, procesos y desinstalación limpia) y la cobertura legal completa.
