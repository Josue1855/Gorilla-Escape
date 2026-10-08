# PC Webcam → CameraInput → Player Lock → Temporal Alignment → Input Fusion

## Estado vigente — FINAL PHASE0 AUDIT, 2026-10-08

**PHASE 0 FINAL AUDIT: PASS — SOFTWARE/LAB.** Fase0 — Base/Spike: **COMPLETED — SOFTWARE/LAB**. Fase1 — Gorilla Smash Vertical Slice: **READY TO START**, no iniciada. Physical validation debt remains open and is required before final MVP acceptance. 3A product/physical onboarding: **DEFERRED / IN PROGRESS**. #94 **OPEN/DRAFT**; candidato local sin commit distinto del PR remoto. Sin nuevo código de producto, commit/push/merge ni fase posterior. [Audit y DoD](PHASE0_FINAL_AUDIT.md), [evidencia](evidence/phase0-final-audit-2026-10-08/gate-summary.json). Los estados previos siguientes son snapshots históricos.

> **Lectura de estado (auditoría 2026-10-08):** las fechas, estados y próximos pasos de este documento son snapshots históricos del incremento descrito. El estado consolidado vigente se registra en [PHASE0_FINAL_AUDIT.md](PHASE0_FINAL_AUDIT.md). Las decisiones de transporte anteriores están **superseded / replaced by DEC-010 Mobile Transport**; se conservan resultados y limitaciones originales.

2026-10-08 UTC. Incremento exclusivamente Software/Lab de Fase0; no gameplay ni cierre final de fase.
Autoriza el PO continuar desde session/admission/QR/protocolo/RTC/PhoneInput ya PASS, sin reiniciar 3A.
[Contrato y corpus](../Shared/Protocol/camera/README.md), [harness y dependencias](../tools/spikes/camera/README.md),
[evidencia independiente](evidence/camera-fusion-2026-10-08/).

## Arquitectura implementada

PC V4L2/OpenCV → MediaPipe Tasks CPU local → landmarks CameraInput v1 por pipes privados del child
supervisado por Unity → latest/bounded CameraInputStore → Player Lock de turno → Temporal Alignment →
InputFusionFrame. Fixture/replay usa exactamente el mismo contrato, supervisor y store.
El adaptador Python es opt-in del laboratorio Linux, no nuevo backend ni decisión de distribución definitiva.
Justificación/versiones/licencias/native inventory en el README y dependencies.json. Preparación previa local;
ningún acceso a Internet durante inferencia. Ni vídeo, fotos, cara ni biometría se publican/almacenan.

La única modificación al bloque previo es un getter de lectura de PhoneInputStore en IpcProbeRunner para
conectar la nueva infraestructura. Java, protocolo/codec móvil/IPC, PhoneInputStore, supervisor Java,
PWA, admisión, QR, RTC y evidencias anteriores se conservan. CameraInput reemplaza su scaffold con campos
raw de frame/subject/association; elimina los placeholders deportivos Throw/Smash sin detector existente.

## Semántica del lock

| Estado | Entrada/salida |
|---|---|
| UNASSIGNED | sin READY; no fusión elegible |
| ACQUIRING | READY explícito 1–4; un sujeto fiable en ROI; dos frames iniciales |
| LOCKED | continuidad del mismo subjectId; un único candidato ROI; datos frescos |
| LOST | ausencia, baja confianza, salto espacial, ambigüedad/segunda persona en ROI, cámara stale/fallida |
| RECOVERING | mismo subjectId y continuidad; dos frames fiables para volver a LOCKED |

Un sujeto distinto nunca sustituye automáticamente al reservado. READY explícito libera la asociación anterior.
Un sujeto real que retorna con ID nuevo necesita READY; no se presume identidad humana. Los fixtures controlan
IDs para probar recovery determinista. El detector multi-pose no demuestra continuidad física ni cero switches.
Hay cuatro teléfonos independientes, **un jugador cámara-activo por turno**, sin asignar la misma observación
a cuatro jugadores simultáneos. Release/Ready controla la plaza, no produce acción deportiva.

## Alineación / fusión

Configuración de laboratorio inicial: maxPhoneAge 250ms, maxCameraAge 250ms, alignmentWindow 100ms,
futureTolerance 20ms, confianza mínima .6, ROI x [.2,.8], paso espacial máximo .2, adquisición/recovery 2 frames.
Se pueden sustituir mediante FusionSettings validado; no son valores competitivos definitivos.

32 muestras de motion por jugador y 16 frames de cámara. Selección del par fresco más cercano; empate:
phone más nuevo, después camera más nueva. No relación 1:1 entre cadencias; duplicado/reorder rechazado
sin refrescar tiempo. Cambio de session/device/epoch vacía history de esa plaza. Estado desconectado actual
invalida history sin afectar otras plazas. Ausencias/partial axes quedan raw, sin cero inventado ni quaternion.

ALIGNED requiere ambas fuentes frescas, asociación LOCKED y delta dentro de ventana. Fuentes stale/lost,
confidence baja o mismatch nunca producen Eligible. Sin teléfono fresco: PHONE_DEGRADED; pérdida de lock:
ASSOCIATION_LOST; ambos ausentes: NO_CANDIDATE; plazas fuera de turno: INACTIVE. Flags de frescura y raw
quality separan las causas aun cuando coinciden. UNALIGNED conserva las señales sin confirmar una acción.

Comparación temporal: Java **receipt PC UTC** versus camera **read-complete PC UTC** (exposición desconocida).
Client timestamp se conserva pero no se usa como reloj sincronizado. Es infraestructura con proxies de recepción,
no validación de alineación física captura→captura. Se requiere mapping monotónico/clock drift/incertidumbre y
medición exposición posteriormente. Reloj ajustado/futuro no se declara movimiento comparable.

RawPhone/RawCamera y sus edades/freshness de receipt/frame se conservan aun cuando asociación/calidad rechazan
la señal usable. Una muestra reciente de baja confianza no se oculta como si no existiera.

FusionFrame conserva PlayerId/SubjectId, referencias PhoneInput/CameraInput, fresh/aligned/delta/status,
confidence/capabilities/signals/quality/timestamps. No decide Smash/Throw/potencia/spin/scoring. DEC-009 sigue
conceptual IDLE→PREPARING→ACTION_CANDIDATE→CONFIRMED→RECOVERY; no necesita máquina deportiva nueva en este gate.

## Corpus / pruebas

12 camera fixtures: sin persona, neutral, ambos brazos arriba, brazo descendente/móvil, mano visible/oculta,
low confidence, lost/recovered, segunda persona. 21 escenarios de fusión ejecutados desde corpus compartido,
con offsets/edad/estado/expectedEligible; ocho trazas conceptuales con datos ordenados.
Neutral/preparación/lento/rápido/jerk/ruido/brazo arriba/descenso prueban conservación y routing solamente.
No validan intención ni false-positive rate.

PlayMode usa un child real de fixtures, bounded stdout/stderr, EOF, cancelación durante startup, READY ausente,
frame malformado/oversized, cámara unavailable y fallo de visión. El Player conecta cuatro perfiles Chrome
reales al mismo Java hijo/IPC, decodifica QR real, recibe cuatro PhoneInputs y añade replay por el mismo pipe
camera. Prueba turnos, aislamiento, loss/recovery y desconexión/reconexión manual con epoch nueva.
Camera unavailable/failure se verifica adicionalmente en Player mientras los cuatro móviles siguen vivos.

## Hardware / límites físicos

V4L2 identifica Integrated_Webcam_FHD, /dev/video0 captura RGB/MJPEG; /dev/video2 GREY no usado.
Modos declarados MJPEG hasta 1920×1080/30FPS; YUYV 1080p/5FPS, 720p/10FPS y 640×480/30FPS.
Permiso efectivo comprobado al abrir. Backend uvcvideo/V4L2; no modificación global. WirePlumber preexistente
permanece intacto. Prueba solicita MJPEG 640×480/30; resolución/FPS **observados** se reportan en evidencia.

Smoke capture/MediaPipe/pipe/store real separado de replay. Sin persona detectada en la corrida del adaptador;
no se acredita la secuencia humana persona/salida/retorno. No desenchufar hardware no autorizado.
Hardware dropped frames desconocidos; gaps sin sujeto son tracking gaps, no contador de frames perdidos.
No vídeo guardado ni landmark físico identificable; sólo counts/timing/confidence aggregate.

## Fallos y cleanup

Startup 10s; stdin EOF normal; espera3s, fallback sólo propio child, límite5s. Stderr drenado, stdout 16384
caracteres, slot latest único; no tasks por frame ni queue de imágenes. Detector y writer síncronos; un lector
EOF con os.read. Histories/metric arrays acotados. Error de cámara no termina Java; móvil perdido no termina
cámara. No watchdog nativo/árboles arbitrarios/restart automático. Unity espera ambos owners al salir.

Intentos preservados: edit01 CS0012 por referencia de tests; edit02 39/41 (test de delta accidentalmente stale,
y desempate temporal que seleccionaba phone antiguo); e2e-replay01 reconnect inválido por harness usando
identidad pública sin resume credential. e2e-vision-failure01 mostró aislamiento PASS pero exit134 inesperado;
review separado registra defecto del EOF reader buffered y reparación os.read. No se reescribe evidencia previa.
physical-adapter01 conserva tiempos combinados lectura+procesamiento; corrida posterior separa lectura/inferencia.

## Regresión y Git

Java48/PWA18 y builds aplicables se vuelven a verificar. Unity EditMode/PlayMode/buildLinux, Player2A/2B,
gate mobile real y nuevos Player camera/fusion son la regresión de esta entrega. Se conservan los tres N1000
independientes y estabilidad601.2234171s de 2C del incremento anterior; **no se presentan como nueva ejecución**.
Justificación: JAR hash y fuentes codec/writer/deadlines/managed lifecycle/PhoneInputStore/polling intactos;
getter nuevo y camera runner opt-in no cambian transporte. Tests reales de proceso/codec 2C incluidos en
Java/PlayMode se repiten; nuevo Player2A/2B verifica arranque/shutdown de la build nueva.

Antes de editar se archivaron cambios/untracked, se registraron hashes, rama y PR94. Se conserva HEAD
0d940f77f533bb6780bc3f07169dce414ea21e54 / feature/mobile-phase0-lan-https-3a / OPEN-DRAFT.
No branch/commit/push/merge. No cambio de red, hosting, router, DNS, certificados o permisos del sistema.

## Resultados finales observados

| Gate | Resultado |
|---|---|
| CameraInput / Player Lock / Temporal Alignment / Input Fusion | PASS — SOFTWARE/LAB |
| Java | 48/48 PASS, 0 SKIP |
| Node PWA/capture/adapter | 18/18 PASS |
| Unity EditMode final | 64/64 PASS (23 anteriores + 41 del incremento) |
| Unity PlayMode final | 16/16 PASS (12 anteriores + 4 del incremento) |
| Builds Linux / PWA | PASS; Linux0 errores/0 warnings |
| Player2A/2B nuevo | 11 grupos PASS |
| Corpus camera/fusion | 12 camera cases + 21 fusion scenarios + 8 conceptual traces ejecutados |
| E2E replay final | e2e-replay04 PASS; cuatro clientes RTC reales + CameraInput child real |
| Cámara unavailable / vision failure Player | PASS; cuatro inputs móviles siguen activos; child esperado exit2 |
| Webcam física capture→MediaPipe→Unity store | PASS técnico parcial; seguimiento humano fiable/salida/retorno NOT RUN |
| Regresión móvil real / validaciones finales | Ver gate-summary.json final |
| Benchmark2C sostenido | evidencia previa conservada, no nueva corrida; justificación hash en regression-retention.json |

Corrida anterior replay03 (antes del último agregado diagnóstico raw): 342 frames producidos, 336 recibidos por store; 5 overwrites de slot latest; un frame final puede
quedar durante shutdown. 29.868FPS replay; processingP50/P95 .159/.335ms (sólo fixture, sin inferencia).
Capture/process→Unity, mismo reloj PC: process→receive N336 P50/P9510/43ms, max202ms; todas las muestras
retenidas, sin excluir startup/pausas. Son observaciones de diagnóstico, no presupuesto físico validado.
Fusion: 788 evaluaciones, 50 Eligible; delta alineado N50 P50/P953/13ms, max14ms. Turnos1–4 y loss/recovery
PASS; 322 phone-stale y 721 camera-degraded incluyen startup, plazas inactivas, fixtures lost/low-confidence,
desconexiones y cierre. No son tasas de error de transporte ni gestos únicos. No 1:1 ni acciones generadas.

Physical01 Player: resolución real640×480; 102 frames/100 recibidos; 17.611FPS observados frente a30 solicitado.
Inferencia/preprocesado (sin espera read) P50/P9539.341/65.884ms; read P50/P957.655/36.538ms.
Process→Unity N100 P50/P9510/157ms, max402ms. Outputs pose de confianza .00136–.00687, **cero frames fiables**
según .6 lab; máximo2 landmarks visibles. Tracking-present no significa humano correctamente detectado.
Captura/pipe/store/release técnico PASS, no persona/salida/retorno/calibración o fusión física PASS.
Objetivo webcam>=30FPS no acreditado. Hardware dropped frames desconocidos; no estimación inventada.
MediaPipe/OpenCV y native callbacks podrían requerir tuning posteriormente; no se optimiza este incremento.

Normal replay y physical: Unity exit0, Java exit0, vision exit0, STOPPED/cleanupComplete, sin forced stop,
residuales propios0; cámara/detector liberados en physical. unavailable/failure: vision exit2 esperado,
Unity/Java exit0, residuales0. El harness/lifecycle 2B conserva sus fallos/forced tests intencionales aparte.
No se afirma ausencia global de leaks ni precisión/latencia de gameplay.

Evidencia: e2e-replay03.json, e2e-physical01.json, e2e-unavailable01.json, e2e-vision-failure03.json,
component-tests.json, ipc-2a-2b-regression.json, mobile-regression01.json, hardware.json,
dependencies.json, regression-retention.json y candidate-hashes.json. Todos los intentos anteriores quedan.

### Candidato final, después de conservar raw metadata

EditMode64/64 PASS; build03 Linux0/0. Replay04 **PASS** con el binario final y fuentes congeladas:
329 frames producidos/326 recibidos, 29.870FPS replay; processing
P50/P95 0.137/0.349ms. 768 evaluaciones/48 Eligible;
delta N48 P50/P95 9/15ms, max15ms. Process→Unity P50/P95
10/42ms, max210ms. 342 phone-stale/708 camera-degraded
incluyen fases deliberadas y plazas inactivas; no errores nominales de transporte. Cuatro turnos/loss/recovery,
manual phone resume, cierre normal y propios residuales0. Se publica esta corrida por separado; ninguna
muestra se combina con replay03/physical01. El cambio final añade sólo acceso diagnóstico raw del frame,
no modifica detector, supervisor, IPC, móvil ni selección/eligibilidad; las demás regresiones conservan su alcance.

## Deudas / siguiente bloque

Físicos iPhone/Android, Android Emulator RTC BLOCKED, Windows, movimiento humano y fusión física, sincronización
de adquisición, confiabilidad de lock/hand tracking, FPS físicos/threshold tuning, y packaging/licencias de
redistribución siguen DEFERRED. Secure-context/onboarding producto no se da por resuelto por este lab.
Sólo queda el bloque **FINAL PHASE0 AUDIT** del cierre Software/Lab solicitado; no ejecutado aquí. La auditoría
revisará también integración Git/alcance/riesgos antes de declarar COMPLETED. Fase0/3A IN PROGRESS,
Fase1 NOT STARTED; #94 OPEN/DRAFT; no gameplay/commit/push/merge ni avance automático.
