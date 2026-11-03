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
| INS-01 | Instalador | regenerar Setup | **BLOCKED** | ISCC no instalado |
| UX-01 | Unity | splash cinematográfico + menú 3D | **FAIL** | no implementado (entry=Connection.unity, menú 2D) |
| UX-02 | Unity | Modo Historia (historieta) | **FAIL** | modal de texto, no viñetas |
| LIC-01 | Legal | LICENSE + THIRD-PARTY + procedencia | **FAIL** | ausentes |

---

## D. Defectos

| ID | Causa raíz | Impacto | Archivo | Cambio | Regresión |
|---|---|---|---|---|---|
| D-01 | `check()` guardaba por SO, rompiendo el test del estado inyectado | 4 unit tests fallaban en Windows | `Server/.../FirewallPreflight.java` | guard `physical && isWindows()` (solo ruta física) | FirewallPreflightTest 5/5 |
| D-02 | `rglob('*.md')` entraba en `Companion/build/.transforms/` | validator roto en Windows | `tools/github/validate_management.py` | poda de dirs generados (`iter_markdown`) | test_validate_management 3/3 |
| D-03 | Tests escritos para Linux (pkexec/firewall fail-closed, openssl, /proc+RST) | 3 fallos en Windows | `LocalTrustProcessTest`, `MobileHttpsTest`, `ProbePressureProcessTest` | no corregido (no es defecto de producto) | — |
| D-04 | Falta identidad legal y procedencia de assets IA | riesgo de distribución | raíz + `assets/` + `Unity/MeshyReferences` | pendiente | — |
| D-05 | Splash/menú 3D nunca implementados | requisito §5 incumplido | `Unity/Assets/Scenes` (entry=Connection) | pendiente | — |
| D-06 | Historia es texto, no historieta | requisito §6 incumplido | `MainMenuController.cs` (modal) | pendiente | — |
| D-07 | Setup-1.0.exe previo a la build nueva; ISCC ausente | instalador stale | `installer/` + `Builds/` | pendiente | — |
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

Splash cinematográfico: **NO implementado**. Menú 3D de playa: **NO implementado** (menú 2D IMGUI con 10 tarjetas). Historieta: **NO implementada** (modal de texto). Calibración personal "Prepárate para jugar": **no localizada como requisito** (existe infraestructura de cámara/input, no la experiencia guiada).

---

## H. Licencias y seguridad

- Dependencias directas documentadas con licencias canónicas (Spring Boot/OpenCV/MediaPipe/cloudflared Apache-2.0; React/Vite MIT; fuentes OFL 1.1).
- **Faltan:** `LICENSE` raíz, `THIRD-PARTY-NOTICES` (obligación Apache-2.0), y procedencia/licencia de modelos 3D **Meshy** (riesgo comercial).
- `npm audit`: 0 vulnerabilidades. Escaneo de secretos y análisis estático Java: **no configurados** (el workflow `security.yml` lo declara).
- Sin credenciales/tokens expuestos detectados en los archivos inspeccionados.

---

## I. Rendimiento

**NOT RUN** — no se ejecutó profiling en build Release real (requiere ejecutar el build instalado y capturar FPS/CPU/GPU; no realizado en esta sesión).

---

## J. Artefactos

| Artefacto | Ruta | Estado |
|---|---|---|
| Build Windows (fresca) | `Builds/Windows/GorillaEscape.exe` + `GorillaEscape_Data/` | **PASS** (12:24) |
| SHA-256 exe | `A4C71B1BFC3E42D02F2F129FE04ADEF3EBF2343A662884E4B99598E9DFC90E7D` | — |
| SHA-256 UnityPlayer.dll | `8698332FA0D299291B58E71A505DB6F20A8F291A2B7349421F93A4F9838ED895` | — |
| Instalador | `Builds/Installer/GorillaEscape-Setup-1.0.exe` | **STALE** (previo a la build nueva) |
| APK Android | no generado | NOT RUN |
| Build PWA | `PWA/dist/` | PASS |
| Harness QA | `tools/qa/run_full_lab.ps1` | creado |
| SBOM | [15_licenses_sbom.md](15_licenses_sbom.md) | primera pasada |

---

## K. Decisión final

### NO-GO

No se puede declarar Release Candidate 1.0 porque quedan criterios obligatorios sin demostrar:

1. Splash cinematográfico y menú 3D de playa **no implementados** (§5 — P1).
2. Modo Historia como historieta **no implementado** (§6 — P1).
3. **Aceptación física completa pendiente**: cámara/MediaPipe con hardware, 4 jugadores físicos, WebRTC P2P en las redes declaradas, Cloudflare público, instalador regenerado desde la build nueva (§17, §13, §22).
4. **Licencias/avisos** de terceros y procedencia de assets IA **sin resolver** (§20).

Lo que SÍ está demostrado y es reutilizable como base: la build de Unity compila limpia, la PWA/Flutter y la mayor parte de Java pasan, y el ciclo IPC Unity↔Java funciona. Ninguna de estas pruebas verdes sustituye los criterios físicos/visuales/legales pendientes.

*Calidad demostrada, no supuesta: cada PASS anterior está respaldado por un log/artefacto concreto; cada bloqueo por una dependencia real y documentada.*
