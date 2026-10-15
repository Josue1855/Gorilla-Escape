# Main Menu real y Centro de Pruebas Phase0

T062/#111 · DEC-029 ACCEPTED · baseline remoto `de54ff069b214015d83d722bc037258aeece7903` verificado 2026-10-14. Se conserva el trabajo local previo de QA Surface y se adapta sin iniciar gameplay. T019 OPEN/Validation; T020/F05/E0 OPEN; F41 Backlog/Phase4; Final Exit BLOCKED.

## Producto y arquitectura

Bootstrap conserva targetFrameRate/configuración; al cargar su escena el entrypoint crea la estructura persistente `MainMenu`. No hay escena QA desechable. MainMenuController posee navegación y delega servicios a QaRuntimeService; MainMenuView presenta composición original de jungla/isla con shapes, tipografía engine y logo provisional identificado. JUGAR deshabilitado «En desarrollo»; PRUEBAS abre Tests; SALIR solicita cierre mediante supervisores oficiales. Ningún gameplay/score/fallback T060.

TestsMenuController define comandos; TestsMenuView sólo presenta snapshots/solicita esos comandos. QaRuntimeService adapta JavaProbeSupervisor, VisionSupervisor, PhoneInputStore, CameraInputStore e InputFusion existentes, sin autoridad de sesión nueva. Java/Vision se inician async con límites existentes: sus fallos no bloquean navegación. Modos de diagnóstico históricos siguen opt-in; `GORILLA_MAIN_MENU=1` fuerza el flujo producto. Editor permite las pruebas automatizadas sin autoarrancar procesos.

`GORILLA_QA_MENU` default visible; `0` oculta PRUEBAS y prohíbe navegación al centro. `GORILLA_QA_AUTOSTART_TESTS=1` es exclusivamente una ayuda explícita de automatización. El arnés de performance usa además `GORILLA_QA_AUTOMATED_MEASUREMENT=1` para consumir eventos de UI sólo durante la ventana de frame timing; el cierre de ventana sigue disponible. Verifica la ruta realmente activa al terminar la medición y rechaza corridas que mezclan OFF/ON. La medición visual de fin de frame sólo vive mientras Tests está abierto. Volver detiene medición, elimina referencia al sample visual, destruye texturas QR/cámara y cierra el consumidor preview. No desconecta jugadores. Servicios globales siguen hasta SALIR. Cierre await del lifecycle Java/Vision restaura controles cámara y elimina procesos propios.

## Dashboard

Izquierda: cámara aspect-fit, landmarks de ambos brazos (hombros/codos/muñecas/manos), conexiones skeleton, ROI por FusionSettings, IDs efímeros/confidence/ambiguous, PlayerLock. LOST por falta/frescura; no certifica identidad. Preview y landmarks latest independientes, sin promesa de alineación de frame. Debajo: indicador técnico acceleration XY aceptada ±20m/s², inactivo sin XY fresco/250ms. Apply/end-of-frame sólo se marca tras Repaint real; no es scanout ni resultado deportivo.

Derecha: QR productivo, TTL, clasificación Prepared/Unprepared declarada por operador, cuatro slots con Network/Input/Player Ready y epoch; sensores completos con disponibilidad explícita, sequence/age; contadores conexiones/reconnect y success%; FPS capture/inference/preview/Unity y tiempo por frame. Scroll mantiene alcance de controles en ventanas pequeñas; escala uniforme preserva aspect ratio. START/STOP TEST y RESET QA SESSION sólo delimitan/resetean agregados QA. STOP exporta summary allowlist, sin imágenes/secret/SDP/identidades/historial sensor.

TechnicalSession/JoinQrPoc conservan session/admission/expiry/join: solicitud de regeneración por IPC autenticado QA v1, QR packed matrix bounded. HTTPS LAN real en modo físico, ningún localhost/fake permitido. Estados de autoridad AVAILABLE, SCANNED/JOINED, EXPIRED, SESSION_FULL/CONFIG_REQUIRED. Nuevo QR revoca sólo la admisión pendiente; TTL30s y un uso. UI no ofrece auto-reconnect nuevo ni modifica Player Ready.

La PWA reutiliza SensorCapture.validUnique.actualMeasuredHz, emissionRate y ACK RTT. HEARTBEAT opcional QA v1 acepta sólo cinco agregados finitos acotados o null. Java retiene cuatro slots latest y envía uno rotativo por PONG para dejar espacio a PhoneInput bajo límite4096. Unity expira presentación a2500ms y verifica epoch; missing = NOT MEASURED, cero real se conserva. Readiness Network/Input/Player del lobby no se redefine. Hz no certifican física; RTT no es E2E.

## Preview y privacidad

Único OpenCV VideoCapture existente → CapturePump → slot de frame inmutable → encoder separado → JPEG máximo320×240, calidad55, máximo5FPS → TCP ligado exclusivamente127.0.0.1/puerto efímero → slot único Unity64KiB → Texture2D. No segunda captura ni imágenes en CameraInput/Java/RTC/PWA/móvil. Publicación no bloquea por codificación; se descartan frames antiguos. Comando privado versionado `QA_PREVIEW 1 revision active` por stdin del worker crea el listener/encoder sólo en Tests; salida revisionada evita endpoints viejos. VOLVER cierra listener/threads/slots sin detener CapturePump. Además, encoder sólo retiene/codifica con consumidor conectado; desconexión vacía slot/JPEG. Sin disco/log/image/video/screenshot; el arnés de performance inspecciona aperturas y guarda sólo agregados. Summary manual/STOP exporta allowlist de métricas, jamás QR ni histories.

## Paquete autónomo y preflight físico

El JRE jlink incluye `jdk.httpserver`, requerido por TrustOnboarding para servir la preparación CA local en modo PHYSICAL. El preflight detectó su ausencia en el candidato anterior (LAB no instancia ese servicio); se conserva el fallo y se corrige el ensamblador. No se agrega JDK al paquete ni una autoridad de sesión nueva. La CA se genera por instalación fuera del paquete/Git. El preflight local verifica TLS/HTTPS y QR renderizado, sin afirmar conexión iPhone.

## Validación y artefactos

La build Linux Mono/Unity6000.3.23f1 compila con0 errores/0 warnings. PWA60/60 + build; Java82 casos únicos PASS (verify portable80+2 omitidos, suite HTTPS LAN8/8 cubre los dos omitidos); preview2/2 y cámara component23/23; PlayMode final24/24, sin omitidos. EditMode145/145 y regresión focal de navegación/admisión4/4 (retirar QR al morir backend) en [recibo de regresión](evidence/t062-main-menu-2026-10-14/regression.json). Management/Foundation y diff-check PASS.

Navegación desde el ejecutable: JUGAR inactivo, dos ciclos PRUEBAS/VOLVER, preview listener sólo en Tests, START/STOP/export agregados y SALIR;0 procesos residuales. Preflight modo PHYSICAL: HTTPS LAN200 con validación TLS normal y QR productivo renderizado, sin teléfono. RTC LAB: cuatro clientes Chrome sintéticos, un reconnect,43 PhoneInput aceptados, respuesta aplicada/renderizada y agregados recibidos;0 token logs. [Navegación](evidence/t062-main-menu-2026-10-14/navigation-r6.json), [preflight PHYSICAL](evidence/t062-main-menu-2026-10-14/delivery-physical-backend.json), [RTC LAB](evidence/t062-main-menu-2026-10-14/delivery-rtc-lab.json).

### OFF / ON — una pareja descriptiva

Cada brazo solicita5s warmup +61s frame timing; RSS/CPU54 muestras a1Hz sobre el intervalo observable después de readiness/warmup. Cámara física V4L2, perfil candidate640×480/30, buffers2, la misma build con Java y WAN aislada/cache developer oculta. OFF tiene0 aperturas Tests; ON tiene1 apertura/0 salidas hasta fin de medición. No había otra captura de QA concurrente.

| Métrica | OFF | ON |
|---|---:|---:|
| Capture FPS | 30.2243 | 30.2237 |
| Inference FPS | 30.2038 | 30.1742 |
| Preview FPS | Desactivado | 4.9969 |
| Unity effective FPS (intervalos Update) | 96.89 | 116.88 |
| Unity frame time P95 ms | 16.95 | 15.83 |
| Vision RSS P95 MiB | 258.65 | 259.46 |
| Unity RSS P95 MiB | 260.82 | 260.16 |
| Vision CPU P95 % de un core | 174.21 | 173.39 |
| Unity CPU P95 % de un core | 24.74 | 47.53 |
| Preview slot overwrites | 0 | 1505 |
| Preview / runtime errors | 0 | 0 |
| Escrituras imagen/video / raw observation logs | 0 / 0 | 0 / 0 |
| Procesos residuales Unity/Vision/Java | 0 / 0 / 0 | 0 / 0 / 0 |

Capture>=30 se conserva; overwrites son descarte esperado latest-only de preview, no pérdida de captura. La UI de Tests aumenta CPU Unity; el par no prueba impacto cero, mejora universal ni calidad humana. Los percentiles de frame time usan los últimos4096 intervalos; effectiveFPS/N cubren toda la ventana. Los intervalos QA se toman con Time.realtimeSinceStartupAsDouble monotónico. El Snapshot común heredó una etiqueta Stopwatch; el código final corrige exclusivamente esa etiqueta, sin cambiar captura, samples, cálculo, navegación o rendering del par. Los hashes productores originales se conservan en los JSON y se distingue la build final por recibo de paquete.

[OFF](evidence/t062-main-menu-2026-10-14/steady-preview-off.json), [ON](evidence/t062-main-menu-2026-10-14/steady-preview-on.json). Se conservan y excluyen corridas exploratorias con cambios de modo, fallos previos de configuración y un solapamiento de dueños de cámara; no se transforman en PASS.

### Build y operación

Paquete local: `/home/josuegalvan/Documentos/GorillaEscape-Phase0-MainMenuQA-2026-10-14`. Ejecutar `./run-qa.sh` desde esa carpeta: verifica hashes y abre Main Menu. Incluye Player/JRE21 con jdk.httpserver/Python/Vision/PWA/servidor y materiales de procedencia. No Editor/SDK requerido al receptor. El recibo local de entrega identifica fuentes, hashes, PR/merge y comprobaciones posteriores.

Un gate físico iPhone/cuatro dispositivos, offset cross-device y respuesta óptica permanece **NOT RUN**; PC limpia T020 **NOT RUN**. El tooling habilita que el PO los ejecute. T019 OPEN/Validation; T020/F05/E0 OPEN; F41 Backlog/Phase4; Phase1 NOT STARTED.

[Runbook exacto iPhone15](PHASE0_IPHONE15_LIVE_QA_RUNBOOK.md). F41 reutilizará y completará la presentación académica en Phase4; esta fundación no completa sus AC.


## Integración

[PR #112](https://github.com/Josue1855/Gorilla-Escape/pull/112) → develop, asociado a T062/#111. [Recibo de binarios y código productor](evidence/t062-main-menu-2026-10-14/package.json). Build final verificada también con Library limpia en copia aislada: no se cerró el Editor interactivo que poseía el checkout. Preflight y RTC repetidos sobre ese paquete final PASS, sin screenshots. El recibo local `delivery-receipt.json` del paquete registra merge/estado final y regresión posterior; no altera los gates físicos pendientes.


CI detectó un fallo intermitente del finder genérico en el test de un raster QR generado. La unidad ahora declara PURE_BARCODE (raster perfecto conocido), recorre64 admisiones nuevas y conserva la verificación one-use; no omite ni reintenta fallos. Assertions de contenido no imprimen tokens. No se cambia el QR productivo ni se acredita lectura con cámara/iPhone; ese gate sigue físico pendiente. El run fallido y la corrección se registran en regression.json.
