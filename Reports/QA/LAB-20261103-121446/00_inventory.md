# 00 — Inventario del entorno y componentes

Laboratorio: LAB-20261103-121446
Fecha: 2026-11-03 (America/Mexico_City)
Rama: `feature/saul-jungle-party-alpha`
Último commit: `fd35005` — "Preserve current gameplay, presentation and adaptive controls"

## 1. Entorno de ejecución detectado

| Herramienta | Versión detectada | Ruta / origen | Requerida por el proyecto | Estado |
|---|---|---|---|---|
| Sistema operativo | Windows 11 (build 10.0.29698) | — | Windows | OK |
| Java (PATH) | 27.0.1 | `java` | 21 | ⚠️ Mismatch en PATH |
| Java (JAVA_HOME) | OpenJDK 21.0.13 LTS | `C:\Users\equipo\AppData\Local\Programs\Microsoft OpenJDK\jdk-21.0.13.0.2+1` | 21 | OK (usar JAVA_HOME) |
| Maven | no global | wrapper `Server\mvnw.cmd` (Maven 3.9.11) | wrapper | OK |
| Node (sistema) | 26.13.0 | `C:\Program Files\nodejs` | 24.x (`>=24 <25`) | ⚠️ Incompatible con engine-strict |
| Node (DSH) | **24.21.0** | `C:\Users\equipo\.dsh\...\dependencies\node\bin\node.exe` | 24.x | OK (usado para PWA) |
| npm | 11.15.0 (sistema) / 11.20.0 (vía DSH node) | npm-cli.js | — | OK |
| Flutter | 3.47.6 (revision 5fc346839b) | `C:\Users\equipo\AppData\Local\Programs\Flutter` | 3.47.6 | OK |
| Dart | 3.13.5 | con Flutter | 3.13.5 | OK |
| Android SDK | presente (platform-tools, build-tools, platforms, ndk, emulator, system-images) | `C:\Users\equipo\AppData\Local\Android\Sdk` | — | OK |
| adb | presente | `...\Android\Sdk\platform-tools\adb.exe` | — | OK |
| Python (sistema) | 3.10.11 | `python` | 3.12 (runtime portátil cámara) | ⚠️ Distinto del runtime empaquetado |
| Python (DSH) | bundled | `C:\Users\equipo\.dsh\...\python\python.exe` | — | auxiliar |
| dotnet | 10.0.403 | `dotnet` | — | OK (checks C#) |
| git | 2.56.0 | `git` | — | OK |
| Unity Editor | **6000.6.0f1** (rev f7f8ed4d1e24) | `C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe` | 6000.6.0f1 | OK |
| Unity Editor (alt) | 6000.6.7f1 | `C:\Program Files\Unity\Hub\Editor\6000.6.7f1` | — | presente |
| Licencia Unity | **Pro Student activa**, headless concedido, disable-splash concedido | `Unity.Entitlements.Audit.log` | — | OK → batchmode build posible |
| cloudflared | `tools\cloudflared.exe` (no en PATH) | repositorio | empaquetado | presente |
| Cámara física | "Integrated Webcam" + "Integrated IR Webcam" | WMI PnP | hardware | presente (no probada aún) |

## 2. Componentes del monorepo

| Componente | Ruta | Tecnología | Rol |
|---|---|---|---|
| Juego | `Unity/` | Unity 6000.6.0f1, C#, URP 17.6.0 | Autoridad de gameplay, puntuación, escenas |
| Servidor local | `Server/` | Java 21 + Spring Boot 4.1.1 | Transporte técnico, señalización RTC, IPC loopback, admisión, host HTTPS |
| PWA | `PWA/` | React 19.3.0 + Vite 8.3.2, Node 24 | Control móvil (WebRTC DataChannel, sensores, lifecycle) |
| Companion | `Companion/` | Flutter 3.47.6 / Dart 3.13.5 | Control móvil nativo Android/iOS |
| Contratos | `Shared/` | JSON fixtures + C# DTOs | Contrato v1 interoperable |
| Cámara/visión | `Unity/Vision/`, `Unity/CameraTracker/` | Python + MediaPipe (pose_landmarker_lite) + OpenCV | Detección de pose → landmarks → IPC |
| Worker | `tools/cloudflare-worker/` | Cloudflare Worker + Durable Objects (RoomRegistry, SQLite) | Enrutamiento/registro de salas públicas |
| Instalador | `installer/GorillaEscape.iss` | Inno Setup | Instalador Windows único |
| Herramientas | `tools/` | Python | Validación, QA, integración, spikes |

## 3. Dependencias directas

### Server (pom.xml)
- spring-boot-starter-parent **4.1.1** (webmvc, websocket, test)
- dev.onvoid.webrtc:webrtc-java **0.19.0**
- com.google.zxing:core **3.5.3**
- Java **21**

### PWA (package.json)
- react **19.3.0**, react-dom **19.3.0**
- devDeps: vite **8.3.2**, @vitejs/plugin-react **6.1.2**
- engines: node `>=24 <25` (engine-strict)

### Companion (pubspec.yaml)
- mobile_scanner ^7.4.2, flutter_webrtc ^1.6.2+hotfix.4, sensors_plus ^7.1.1, flutter_rotation_sensor ^0.4.0, wakelock_plus ^1.8.1, cupertino_icons ^1.0.8
- dev: flutter_lints ^6.0.0

### Unity (Packages/manifest.json)
- com.unity.render-pipelines.universal **17.6.0**
- com.unity.cloud.gltfast **6.19.0**, com.unity.ai.inference **2.6.1**, com.unity.test-framework **1.8.0**
- com.gorillaescape.contracts (local `file:../../Shared/CSharp`)
- toolchains Linux (sdk.linux, toolchain.linux-x86_64)

## 4. Catálogo de disciplinas vs escenas (discrepancias detectadas)

| # | Disciplina (spec §15) | Escena en BuildSettings | Controlador principal | Observación |
|---|---|---|---|---|
| 1 | Gorilla Smash | GorillaSmash.unity | GorillaSmashController | OK |
| 2 | Coconut Throw | CoconutThrow.unity | CoconutThrowController | OK |
| 3 | Coconut Bowling | **BananaBowling.unity** | BowlingController | ⚠️ Nombre escena difiere |
| 4 | Jungle Slice | CoconutSlice.unity | CoconutSliceController | comparte escena con Keeper |
| 5 | Log Balance | LogBalance.unity | LogBalanceGameController | OK |
| 6 | Gorilla Boxing | GorillaBoxing.unity | BoxingController | OK |
| 7 | Jungle Baseball | JungleBaseball.unity | BaseballController | OK |
| 8 | Jungle Drums | JungleDrums.unity | RhythmDrumsController | OK |
| 9 | Whac-a-Mole | WhackAMole.unity | WhackAMoleController | OK |
| 10 | Jungle Keeper | CoconutSlice.unity | CoconutSliceController.Keeper | ⚠️ comparte escena con Slice |

Escenas en BuildSettings (11): Connection, MainMenu, GorillaSmash, CoconutThrow, BananaBowling, CoconutSlice, LogBalance, GorillaBoxing, JungleBaseball, JungleDrums, WhackAMole.
Escena de prueba presente en `Assets/Scenes` (NO en build): PhoneMotionTest.unity.

## 5. Estructura de código Unity (`Assets/_Project/Scripts`)
- Core: Bootstrap, ProductScenes, PackagedRuntimeEnvironment, ApplicationSettings, JungleSceneBindings, JungleAssetReference
- Network: JavaProbeSupervisor, IpcLifecycle, IpcProbeRunner, LobbyAuthorityBridge
- CameraTracking: VisionSupervisor, CameraInputStore, CameraRuntimePaths, CameraFusionLabRunner, FrameTimingWindow
- Input: InputFusion, PlayerLock, PhoneInputStore, CameraInput, PlayerInput, JungleInputAdapter, OfficialActionLedger
- Minigames/LogBalance: LogBalanceArtCatalog/Atmosphere/Character/Model/Settings/Spectator/Stage
- UI: MainMenuController/View, JunglePanel, JungleRetainedUI, TestsMenu
- QA: CameraPreviewClient, LiveQaModel, QaRuntimeService
- Tests: 18 EditMode + 13 PlayMode (asmdef dedicados)

## 6. Hallazgos inmediatos (a verificar en el laboratorio)
1. `installer/GorillaEscape.iss` empaqueta `..\Builds\Windows\*` (raíz); el `Setup-1.0.exe` existente (268 MB) es previo a la build nueva → riesgo real de empaquetar binario viejo (no se puede regenerar: ISCC ausente).
2. `AppId={{5E4B2810-...F421}` es un GUID **correctamente escapado** en Inno (no es defecto; descartado).
3. Node de sistema (26) incompatible con engine-strict de PWA → usar Node 24 (DSH).
4. Escena `BananaBowling` (identificador interno) vs disciplina "Coconut Bowling" (nombre visible); `Jungle Keeper` comparte escena con `CoconutSlice` vía flag `SceneNavigator.IsJungleKeeper`.
5. `PhoneMotionTest.unity` (escena de prueba) existe en Assets aunque no esté en build.
6. Identidad pública correcta: `productName: Gorilla Escape`, `companyName: Gorilla Escape Team`.
