# Inventario y trazabilidad de fuentes

Última revisión: 2026-10-16. Propósito: reproducir las afirmaciones de esta auditoría y delimitar su profundidad. Inventario estructural recursivo de Markdown/READMEs, código/configuración/tests y lectura detallada de contratos, fuentes representativas y evidencia final. No equivale a revisión línea por línea de toda la implementación ni auditoría de seguridad integral.

| ID | Fuente primaria / revisión | Uso y límite |
|---|---|---|
| S1 | [Monorepo Unity/Java/Shared/PWA](https://github.com/Josue1855/Gorilla-Escape/tree/7a750826c5565cbafdec983c5988c4cd5657d72a), develop `7a750826c5565cbafdec983c5988c4cd5657d72a` | Código, manifests, tests, árbol y documentación; base remota confirmada antes de cambios editoriales |
| S2 | [PR #121](https://github.com/Josue1855/Gorilla-Escape/pull/121), head `83e984a12ca662bf8b7f0c4660e721fa464ac46e`, merge `7a750826c5565cbafdec983c5988c4cd5657d72a`; [validación persistida](../evidence/phone-connection-final-2026-10-16/validation.json) | API confirmó merged=true; head integrado y evidencia Software/Lab, no nuevo ensayo físico |
| S3 | Encargo PO «Auditoría, documentación y gestión de Gorilla Escape: Gorilimpiadas», recibido 2026-10-16 en este chat | Fuente autorizada del alcance actual, cuatro reglas funcionales, laboratorio y restricciones. No es evidencia de código. PO respondió que desconoce URL backend |
| S4 | [PWA UI/UX](https://github.com/Quintana-117/Dise-o_pwa/tree/ffbec2260dbad3f36743cffc2309cb206f7dba9a), main `ffbec2260dbad3f36743cffc2309cb206f7dba9a` | Único commit accesible en clon superficial; código/lock/README/SW inspeccionados; no se afirma historia completa más allá del HEAD |
| S5 | Ejecución de auditoría 2026-10-16 en copia temporal S4: npm ci --ignore-scripts, npm run build, inventario dist y git status | Ambos comandos exit 0; build 27 módulos, sin SW/iconos; sin prueba de navegador. Herramientas Node/npm del entorno, no baseline backend |
| S6 | Consultas autenticadas gh REST/GraphQL 2026-10-16: repos/permissions/branches, issues/PR/milestones/labels, Projects/fields/views e introspección CreateProjectV2ViewInput | Observaciones remotas puntuales. Ninguna mutación del Project histórico ni recursos PWA creados. Conteos de este instante, no garantía futura |

## S7 — Creación posterior autorizada del backend

El PO indicó «haz el repositorio tú del backend, y agrégalo al proyecto». Se creó [Josue1855/Gorilla-Escape-Backend](https://github.com/Josue1855/Gorilla-Escape-Backend), privado, rama main, commit inicial `e76371097f4bcd881fa081211ec8d0d78e308c0b`. REST confirmó URL/visibilidad/default_branch y permisos admin/push/read; consulta del commit confirmó exclusivamente tres Markdown: README.md, docs/ARCHITECTURE.md y docs/DEVELOPMENT_PLAN.md. La asociación mediante `gh project link` se verificó con GraphQL repositories: Project #1 ahora enlaza Unity y backend; sigue con 95 items, 21 campos y 10 vistas. No se crearon issues ni se modificaron campos/vistas/estados.

La búsqueda S6 anterior que no localizaba backend permanece como evidencia histórica; S7 resuelve la URL sin inventar implementación. En ese snapshot S7, API/BD seguían PLANIFICADO y motor/ORM/versiones DESCONOCIDO. El checkout tenía sólo un commit documental. S9, al final de este inventario, registra el incremento funcional y las pruebas posteriores.

## Rutas significativas de S1

Todas las siguientes rutas remiten a S1, no al checkout original con cambios locales.

| Afirmación | Código/contrato | Prueba/evidencia |
|---|---|---|
| Versiones | [Unity ProjectVersion](https://github.com/Josue1855/Gorilla-Escape/blob/7a750826c5565cbafdec983c5988c4cd5657d72a/Unity/ProjectSettings/ProjectVersion.txt), [Server pom](https://github.com/Josue1855/Gorilla-Escape/blob/7a750826c5565cbafdec983c5988c4cd5657d72a/Server/pom.xml), [PWA package](https://github.com/Josue1855/Gorilla-Escape/blob/7a750826c5565cbafdec983c5988c4cd5657d72a/PWA/package.json), [lock](https://github.com/Josue1855/Gorilla-Escape/blob/7a750826c5565cbafdec983c5988c4cd5657d72a/PWA/package-lock.json) | Configuración declarada y lock; no inferir versiones por carpetas |
| Autoridad/foundation | [MainMenuController](https://github.com/Josue1855/Gorilla-Escape/blob/7a750826c5565cbafdec983c5988c4cd5657d72a/Unity/Assets/_Project/Scripts/UI/MainMenuController.cs) y Scripts/Core, Input, Network | MainMenuNavigationTests, FoundationSceneTests, S2 |
| IPC y lifecycle | [JavaProbeSupervisor](https://github.com/Josue1855/Gorilla-Escape/blob/7a750826c5565cbafdec983c5988c4cd5657d72a/Unity/Assets/_Project/Scripts/Network/JavaProbeSupervisor.cs), [ManagedProbe](https://github.com/Josue1855/Gorilla-Escape/blob/7a750826c5565cbafdec983c5988c4cd5657d72a/Server/src/main/java/com/gorillaescape/server/ipc/ManagedProbe.java), ProbeCodec, Shared/CSharp/Runtime/IpcFrameCodec.cs | IpcLifecycleProcessTests, IpcHardeningProcessTests, ManagedProbeProcessTest; Shared/Protocol/ipc/cases/corpus.json |
| QR y Shared | [PhoneInputWire](https://github.com/Josue1855/Gorilla-Escape/blob/7a750826c5565cbafdec983c5988c4cd5657d72a/Shared/CSharp/Runtime/PhoneInputWire.cs), Shared/Protocol/fixtures/qa-hosting.json, JoinQrPoc, TrustOnboarding | QaHostingContractTest y QaHostingContractTests; S2 |
| Transporte y seguridad LAN | MobileRuntime, GorillaProtocol, MobileController, PublicIngress, ConnectionBootstrap y CloudflaredSupervisor en Server/src/main/java/com/gorillaescape/server | BootstrapRtcLabTest, ConnectivityTest, LocalSignalingTest, GorillaProtocolTest, S2 |
| Firewall | [firewall.py](https://github.com/Josue1855/Gorilla-Escape/blob/7a750826c5565cbafdec983c5988c4cd5657d72a/tools/connectivity/firewall.py), tools/setup-firewall.sh y FirewallPreflight | tools/connectivity/test_firewall.py, FirewallPreflightTest, S2 |
| Captura y offline técnico | PWA/src/controller/session.js, PWA/src/input/mobileClient.js, input/sensors/capture.js y PWA/tools/build-service-worker.js | PWA/tests/sensors.test.js, lifecycle.test.js, offline.test.js, motion-flow.test.js; S2 |
| CameraInput y empaquetado | VisionSupervisor, Input/Fusion, tools/t020/package.py | CameraPipelineTests, CameraBundleProcessTests; docs/PHASE0_T016_CAMERA_RUNTIME.md y T020 |
| Sin gameplay | Árbol Unity/Assets/_Project: sólo Bootstrap.unity rastreada; PlayEnabled=false y ausencia de clases/escenas de los cuatro juegos | Inspección fuente, no prueba de un juego |
| Sin BD en servicio técnico | Server/pom.xml y controllers actuales | Sin dependencias JPA/driver y sin esquema/repositorio de dominio encontrado; en S7 se confirmó que el backend nuevo contiene únicamente documentación |

Rutas S4 decisivas: [App.jsx](https://github.com/Quintana-117/Dise-o_pwa/blob/ffbec2260dbad3f36743cffc2309cb206f7dba9a/src/App.jsx), [AvatarList.jsx](https://github.com/Quintana-117/Dise-o_pwa/blob/ffbec2260dbad3f36743cffc2309cb206f7dba9a/src/AvatarList.jsx), [sw.js](https://github.com/Quintana-117/Dise-o_pwa/blob/ffbec2260dbad3f36743cffc2309cb206f7dba9a/sw.js), [manifest](https://github.com/Quintana-117/Dise-o_pwa/blob/ffbec2260dbad3f36743cffc2309cb206f7dba9a/manifest.json), [package-lock](https://github.com/Quintana-117/Dise-o_pwa/blob/ffbec2260dbad3f36743cffc2309cb206f7dba9a/package-lock.json), [index](https://github.com/Quintana-117/Dise-o_pwa/blob/ffbec2260dbad3f36743cffc2309cb206f7dba9a/index.html). No se incorporó su código al monorepo.

## GitHub observado

Snapshot anterior a crear el PR documental #122: repositorio principal con 95 issues, 68 abiertas/27 cerradas; 26 PR consultados, todos merged. Se inspeccionaron títulos/estados/cuerpos de issues y metadatos de PR; el merge reciente #121 se confirmó individualmente. Consulta posterior con state=all confirmó siete milestones, todos abiertos: Fase 0 (8 abiertas/19 cerradas), Fase 1 (6/0), Fase 2 (10/0), Fase 3 (17/0), Fase 4 (8/0), Fase 5 (5/0) y Fase 6 (8/0). Labels por área/tipo, blocked y needs-device-test están presentes; no se modificaron.

Project #1: 95 items, 21 campos y 10 vistas consultadas. Product Backlog, Current Sprint, Sprint Board, My Work, PO Review, Blocked, Bugs, By Area, Roadmap y Releases. Vista 1 de referencia es lectura histórica, no plantilla que se ejecute contra el mismo ID. S4: cero issues/PR/milestones accesibles tras consulta explícita state=all; labels genéricas + accessibility. Projects visibles: Josue1855 sólo #1; Quintana-117 ninguno; S4 sin Projects enlazados.

Antes de crear S7, búsqueda adicional de metadatos de repositorios visibles de ambos propietarios con nombre gorilla/pwa/backend sólo devolvió los dos repositorios confirmados. No prueba inexistencia de un backend privado u hospedado en otra cuenta. No se inspeccionaron repositorios de prototipos descartados ni se preparó documentación de migración.

## Inventario histórico conservado

82 archivos Markdown rastreados en S1, incluyendo READMEs y plantillas; hash SHA-256 abreviado del contenido en ese commit (los enlaces abren la ruta mantenida). La nueva capa enlaza éstos como fuentes históricas, no replica sus resultados ni borra evidencia. Los avisos editoriales posteriores se distinguen en Git.

| Archivo histórico | Líneas en S1 | SHA-256 abreviado |
|---|---|---|
| [.github/pull_request_template.md](../../.github/pull_request_template.md) | 37 | `e2280a6b0e13` |
| [CONTRIBUTING.md](../../CONTRIBUTING.md) | 14 | `b52eba304de3` |
| [PWA/README.md](../../PWA/README.md) | 7 | `7b03fc4b59f4` |
| [PWA/spikes/sensors/README.md](../../PWA/spikes/sensors/README.md) | 33 | `af4aa7336593` |
| [README.md](../../README.md) | 36 | `f440b6ac0afd` |
| [Server/README.md](../../Server/README.md) | 5 | `38521e8da020` |
| [Shared/Protocol/README.md](../../Shared/Protocol/README.md) | 7 | `d42ca7a1b9a5` |
| [Shared/Protocol/camera/README.md](../../Shared/Protocol/camera/README.md) | 66 | `98d690c8d849` |
| [Shared/Protocol/ipc/README.md](../../Shared/Protocol/ipc/README.md) | 85 | `97b57e0bde1a` |
| [Shared/Protocol/mobile/README.md](../../Shared/Protocol/mobile/README.md) | 47 | `324875a84ba9` |
| [Unity/Assets/_Project/Art/README.md](../../Unity/Assets/_Project/Art/README.md) | 1 | `eb2ffda8d63e` |
| [Unity/Assets/_Project/Audio/README.md](../../Unity/Assets/_Project/Audio/README.md) | 1 | `25bf55552982` |
| [Unity/Assets/_Project/Scenes/Bootstrap/README.md](../../Unity/Assets/_Project/Scenes/Bootstrap/README.md) | 1 | `61e837d2b2e0` |
| [Unity/Assets/_Project/Scripts/CameraTracking/README.md](../../Unity/Assets/_Project/Scripts/CameraTracking/README.md) | 5 | `974e4cdae7ef` |
| [Unity/Assets/_Project/Scripts/Network/README.md](../../Unity/Assets/_Project/Scripts/Network/README.md) | 5 | `a042e29211c8` |
| [Unity/Assets/_Project/Settings/README.md](../../Unity/Assets/_Project/Settings/README.md) | 1 | `0430bcc918ca` |
| [Unity/README.md](../../Unity/README.md) | 5 | `9b5b78f91319` |
| [docs/ACADEMIC_MVP_IDGS102N_GORILLA_ESCAPE.md](../../docs/ACADEMIC_MVP_IDGS102N_GORILLA_ESCAPE.md) | 46 | `4beaee630876` |
| [docs/DECISIONS.md](../../docs/DECISIONS.md) | 543 | `e8f1dec92ab0` |
| [docs/DEVELOPMENT_PROGRESS.md](../../docs/DEVELOPMENT_PROGRESS.md) | 644 | `9b2c8836a7f8` |
| [docs/DEVELOPMENT_RULES.md](../../docs/DEVELOPMENT_RULES.md) | 275 | `268719a19087` |
| [docs/Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md](../../docs/Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md) | 3985 | `e57419476f29` |
| [docs/MONOREPO_PLAN.md](../../docs/MONOREPO_PLAN.md) | 117 | `a1bac32ff6a2` |
| [docs/PHASE0_CAMERA_FUSION_VALIDATION.md](../../docs/PHASE0_CAMERA_FUSION_VALIDATION.md) | 186 | `ee019d626292` |
| [docs/PHASE0_FINAL_AUDIT.md](../../docs/PHASE0_FINAL_AUDIT.md) | 215 | `dc312c72dfff` |
| [docs/PHASE0_FINAL_EXIT_REPORT.md](../../docs/PHASE0_FINAL_EXIT_REPORT.md) | 155 | `ce6129e8ce07` |
| [docs/PHASE0_INPUT_FUSION_CONCEPT.md](../../docs/PHASE0_INPUT_FUSION_CONCEPT.md) | 319 | `03f71116aafd` |
| [docs/PHASE0_IPC_2B_DESIGN.md](../../docs/PHASE0_IPC_2B_DESIGN.md) | 194 | `83e0e6895e35` |
| [docs/PHASE0_IPC_2B_VALIDATION.md](../../docs/PHASE0_IPC_2B_VALIDATION.md) | 69 | `1a4acff7f563` |
| [docs/PHASE0_IPC_2C_DESIGN.md](../../docs/PHASE0_IPC_2C_DESIGN.md) | 209 | `875557a79ee3` |
| [docs/PHASE0_IPC_2C_VALIDATION.md](../../docs/PHASE0_IPC_2C_VALIDATION.md) | 139 | `9c09faad9c7a` |
| [docs/PHASE0_IPHONE15_LIVE_QA_RUNBOOK.md](../../docs/PHASE0_IPHONE15_LIVE_QA_RUNBOOK.md) | 71 | `8bc3ccfe940b` |
| [docs/PHASE0_LIVE_QA_IPHONE_RUNBOOK.md](../../docs/PHASE0_LIVE_QA_IPHONE_RUNBOOK.md) | 45 | `d2bf91d7a61d` |
| [docs/PHASE0_MAIN_MENU_QA_CENTER.md](../../docs/PHASE0_MAIN_MENU_QA_CENTER.md) | 93 | `56e3bc3cfe23` |
| [docs/PHASE0_MOBILE_INPUT_RUNTIME_VALIDATION.md](../../docs/PHASE0_MOBILE_INPUT_RUNTIME_VALIDATION.md) | 75 | `6c96fcc3ec99` |
| [docs/PHASE0_MOTION_HARDENING.md](../../docs/PHASE0_MOTION_HARDENING.md) | 13 | `ca8d887fc6a8` |
| [docs/PHASE0_OVERNIGHT_INTEGRATION_REPORT_2026-10-14.md](../../docs/PHASE0_OVERNIGHT_INTEGRATION_REPORT_2026-10-14.md) | 55 | `8b4c2d76f34d` |
| [docs/PHASE0_OVERNIGHT_RUN_2026-10-13.md](../../docs/PHASE0_OVERNIGHT_RUN_2026-10-13.md) | 25 | `2f7ae9e52069` |
| [docs/PHASE0_PHONE_3A_ANDROID_EMULATOR.md](../../docs/PHASE0_PHONE_3A_ANDROID_EMULATOR.md) | 79 | `8f0243dc3379` |
| [docs/PHASE0_PHONE_3A_FEASIBILITY.md](../../docs/PHASE0_PHONE_3A_FEASIBILITY.md) | 272 | `4aca4e77292b` |
| [docs/PHASE0_PHONE_3A_LINUX_443.md](../../docs/PHASE0_PHONE_3A_LINUX_443.md) | 64 | `872a36f75639` |
| [docs/PHASE0_PHONE_3A_PHYSICAL_PLAN.md](../../docs/PHASE0_PHONE_3A_PHYSICAL_PLAN.md) | 82 | `4f8ab539518d` |
| [docs/PHASE0_PHONE_3A_PREFLIGHT.md](../../docs/PHASE0_PHONE_3A_PREFLIGHT.md) | 149 | `363c6cd9e0a9` |
| [docs/PHASE0_PHONE_3A_VALIDATION.md](../../docs/PHASE0_PHONE_3A_VALIDATION.md) | 64 | `22c84c9a598a` |
| [docs/PHASE0_PHONE_CONNECTION_FINAL.md](../../docs/PHASE0_PHONE_CONNECTION_FINAL.md) | 129 | `d47161f6e148` |
| [docs/PHASE0_PHONE_LAN_ONBOARDING_DESIGN.md](../../docs/PHASE0_PHONE_LAN_ONBOARDING_DESIGN.md) | 189 | `b004e5b3a887` |
| [docs/PHASE0_PHONE_LAN_ONBOARDING_REDESIGN.md](../../docs/PHASE0_PHONE_LAN_ONBOARDING_REDESIGN.md) | 193 | `20321aaafdf2` |
| [docs/PHASE0_PORTABLE_LAN_REDESIGN.md](../../docs/PHASE0_PORTABLE_LAN_REDESIGN.md) | 139 | `2b9b2588ee1e` |
| [docs/PHASE0_PROJECT_RECONCILIATION.md](../../docs/PHASE0_PROJECT_RECONCILIATION.md) | 152 | `35fd13d5e49e` |
| [docs/PHASE0_SECURE_MOTION_SPIKE_PLAN.md](../../docs/PHASE0_SECURE_MOTION_SPIKE_PLAN.md) | 233 | `e1e5293aed32` |
| [docs/PHASE0_SECURE_MOTION_TRANSPORT_DECISION.md](../../docs/PHASE0_SECURE_MOTION_TRANSPORT_DECISION.md) | 325 | `bc9b745b06a5` |
| [docs/PHASE0_T002_HARDWARE_INVENTORY.md](../../docs/PHASE0_T002_HARDWARE_INVENTORY.md) | 93 | `866dfe851b26` |
| [docs/PHASE0_T003_TECHNICAL_BASELINE.md](../../docs/PHASE0_T003_TECHNICAL_BASELINE.md) | 65 | `e5e459a29925` |
| [docs/PHASE0_T008_LOCAL_TRUST.md](../../docs/PHASE0_T008_LOCAL_TRUST.md) | 164 | `65efac01fb10` |
| [docs/PHASE0_T009_PWA_SHELL.md](../../docs/PHASE0_T009_PWA_SHELL.md) | 71 | `eac555787555` |
| [docs/PHASE0_T010_SENSOR_CAPTURE.md](../../docs/PHASE0_T010_SENSOR_CAPTURE.md) | 144 | `42b0360bd5e2` |
| [docs/PHASE0_T011_OFFLINE_LIFECYCLE.md](../../docs/PHASE0_T011_OFFLINE_LIFECYCLE.md) | 54 | `943596c4ab16` |
| [docs/PHASE0_T013_LOBBY_ADMISSION.md](../../docs/PHASE0_T013_LOBBY_ADMISSION.md) | 64 | `0a34ca38b425` |
| [docs/PHASE0_T015_ACTION_LIFECYCLE.md](../../docs/PHASE0_T015_ACTION_LIFECYCLE.md) | 53 | `d17deeab9234` |
| [docs/PHASE0_T016_CAMERA_RUNTIME.md](../../docs/PHASE0_T016_CAMERA_RUNTIME.md) | 252 | `bf9a53c6185f` |
| [docs/PHASE0_T017_DETERMINISTIC_VALIDATION.md](../../docs/PHASE0_T017_DETERMINISTIC_VALIDATION.md) | 23 | `c2e3910f1b2f` |
| [docs/PHASE0_T018_TELEMETRY_METRICS.md](../../docs/PHASE0_T018_TELEMETRY_METRICS.md) | 155 | `0b854c1897a6` |
| [docs/PHASE0_T019_PHYSICAL_MOBILE_QA.md](../../docs/PHASE0_T019_PHYSICAL_MOBILE_QA.md) | 76 | `a8207afbaf30` |
| [docs/PHASE0_T020_CLEAN_PC_RUNBOOK.md](../../docs/PHASE0_T020_CLEAN_PC_RUNBOOK.md) | 21 | `5f8afd12a50a` |
| [docs/PHASE0_T063_TRANSPARENT_AUTO.md](../../docs/PHASE0_T063_TRANSPARENT_AUTO.md) | 119 | `076d7a430d9f` |
| [docs/PHASE0_UNITY_FOUNDATION.md](../../docs/PHASE0_UNITY_FOUNDATION.md) | 295 | `82fd4e6abfc3` |
| [docs/PHASE0_UNITY_JAVA_DECISION.md](../../docs/PHASE0_UNITY_JAVA_DECISION.md) | 249 | `1cee4c2bec8c` |
| [docs/TEAM_START.md](../../docs/TEAM_START.md) | 129 | `83caa3ca6a8e` |
| [docs/TECHNOLOGY_RESEARCH.md](../../docs/TECHNOLOGY_RESEARCH.md) | 234 | `fd667055e007` |
| [docs/TEST_REPORT.md](../../docs/TEST_REPORT.md) | 776 | `bb0eb030b160` |
| [docs/TRANSPARENT_CONNECTIVITY.md](../../docs/TRANSPARENT_CONNECTIVITY.md) | 69 | `fb13990da051` |
| [docs/UNITY_FOUNDATION_VALIDATION.md](../../docs/UNITY_FOUNDATION_VALIDATION.md) | 120 | `a6c1a7538505` |
| [docs/WORKFLOW.md](../../docs/WORKFLOW.md) | 88 | `e7ffe63a3007` |
| [docs/github/BACKLOG_STRUCTURE.md](../../docs/github/BACKLOG_STRUCTURE.md) | 141 | `1d0e4dd3dc39` |
| [docs/github/PROJECT_SETUP.md](../../docs/github/PROJECT_SETUP.md) | 113 | `5c81ba5c13af` |
| [docs/github/TEAM_WORKFLOW.md](../../docs/github/TEAM_WORKFLOW.md) | 56 | `0e1870b3ab96` |
| [tools/spikes/camera/README.md](../../tools/spikes/camera/README.md) | 67 | `ad7357cf88ff` |
| [tools/spikes/webrtc/README.md](../../tools/spikes/webrtc/README.md) | 28 | `f27a06ef967f` |
| [tools/spikes/webrtc/RUNTIME.md](../../tools/spikes/webrtc/RUNTIME.md) | 20 | `70ad4f4fb475` |
| [tools/spikes/webtransport/DEPENDENCIES.md](../../tools/spikes/webtransport/DEPENDENCIES.md) | 15 | `966679cba31d` |
| [tools/spikes/webtransport/README.md](../../tools/spikes/webtransport/README.md) | 58 | `4c6784e2ac7e` |
| [tools/t063/README.md](../../tools/t063/README.md) | 12 | `22198be06cda` |

## S8 — Flujo obligatorio de ramas confirmado por el PO

El 2026-10-16 el PO confirmó: tareas en ramas desde develop, integración por PR a develop y promoción a main únicamente cuando la entrega esté validada. Backend: API confirmó default_branch=develop; main y develop apuntan a `e76371097f4bcd881fa081211ec8d0d78e308c0b` tras crear la rama de integración. Se preparó el [PR backend #1](https://github.com/Josue1855/Gorilla-Escape-Backend/pull/1), desde codex/docs-development-workflow hacia develop, con README/CONTRIBUTING/plan actualizados en `67ca1c4`. No se hicieron nuevos commits en main ni merge de PR. La configuración y documentación del repositorio UI/UX siguen requiriendo al mantenedor porque no hay Write.

## S9 — Backend de jugadores aprobado e implementado

El PO aprobó «Sí, apruebo SQLite para este incremento». [ADR acotado](https://github.com/Josue1855/Gorilla-Escape-Backend/blob/36351860624412b0b454a80dc19a7f63704164e0/docs/adr/ADR-001-PLAYERS-APPROVAL.md). PR [#14](https://github.com/Josue1855/Gorilla-Escape-Backend/pull/14) abierto hacia develop, rama feature/players-sqlite-increment; revisión funcional/evidencias `36351860624412b0b454a80dc19a7f63704164e0`. [CI Linux/Windows PASS](https://github.com/Josue1855/Gorilla-Escape-Backend/actions/runs/38989509375): 17 tests por plataforma, build y harness adicional Linux PASS. Spring Boot4.1.1, Java21, SQLite JDBC3.53.2.1 (core3.53.2), Flyway12.4.0, sin JPA. GET/POST /api/players y migración V1 reales. La aprobación es acotada: PR #12 de arquitectura permanece abierto, sin merge presumido.

[Dossier](https://github.com/Josue1855/Gorilla-Escape-Backend/tree/36351860624412b0b454a80dc19a7f63704164e0/docs/laboratorio) con SQL, logs y capturas auténticas; tres capturas literales de DevTools pendientes. main/develop backend siguen en la base documental e763710; implementación en rama de tarea. S7/S8 se conservan como snapshots históricos, no estado de la implementación actual. Backend privado; evaluador necesita acceso o paquete de evidencias autorizado.

## S10 — Fork PWA de laboratorio y evidencia navegador

[PR #1 del fork](https://github.com/Josue1855/Dise-o_pwa/pull/1), rama codex/pwa-database-lab → develop, revisión funcional `69084f53aeabfa2aab4dfd7edaadd64f4c98775b`. [CI PASS](https://github.com/Josue1855/Dise-o_pwa/actions/runs/38989521358): siete tests, build y auditoría npm sin vulnerabilidades reportadas. React18.3.1, Vite6.4.4, plugin4.7.0. Pantalla /lab.html → proxy loopback4173 → backend8090 → SQLite. SW generado, Cache Storage shell y snapshot/borrador IndexedDB. Chrome real: GET2, POST201/SQL3, offline/recarga, reconexión sin envío automático, POST manual201/SQL4; API inaccesible simulada y validación sin escrituras. No hay Background Sync ni POST idempotente. No acredita teléfono, LAN/WAN físicos, instalador o gameplay.

Upstream Quintana-117/Dise-o_pwa conserva main ffbec226 y permisos READ; no se cambió. develop del fork se creó sobre ese baseline y los cambios se proponen por PR sin tocar main. S4/S5 describen únicamente el upstream histórico; su falta de SW/IndexedDB ya está corregida y probada en el fork, pendiente de revisión del mantenedor.

## Sincronización documental posterior

El PR Unity #122 fue integrado por revisión externa el 2026-10-17T00:22:32Z; origin/develop observado `db408d7`. Esta actualización crea una nueva rama desde esa base y no modifica el checkout original con cambios locales. Los resultados citados pertenecen a sus commits; las validaciones documentales posteriores se registran por separado.

## S11 — Propuesta UI/UX del PO

Documento adjunto «Gorilla Escape: Gorilimpiadas — UI/UX Design System», fecha original2026-10-16, recibido en este chat. SHA-256 del texto original: `08f85ca3c67ce0c66f99eff74429f0eb84a045d9eb6bb2c953cbf45e3d63cafe`. Se incorpora como [propuesta para revisión](../01-architecture/UI_UX_DESIGN_SYSTEM.md), con correcciones explícitas a SQLite/jugadores y al perfil offline S9/S10. La [revisión documental](../04-testing/UI_UX_DESIGN_REVIEW.md) añade criterios medibles y enlaces a trabajo existente; no es aprobación visual ni nueva implementación. No se incluyen activos de fuentes descartadas.
