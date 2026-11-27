# T018 / #32 — telemetría, clocks y métricas

> **Estado integrado — 2026-10-14:** #32 CLOSED/completed/Project Done tras PR #107 / DEC-028. Literal AC1/AC2 PARTIAL; physical clocks/Hz/visible E2E NOT RUN en #33. Las instrucciones anteriores de no cerrar son checkpoints históricos superseded sólo para T018 exit. [Estado final](PHASE0_FINAL_EXIT_REPORT.md).

2026-10-13, America/Chihuahua. Base `develop` **bb90c04d050f35feae8ae1a99968aa9d91ed2e4a** (PR #102, T016 CLOSED/completed/Done). Rama `codex/t018-telemetry-metrics`. **T018 OPEN / Validation / needs-device-test / performance. Ningún AC original marcado como PASS completo.** Este incremento permite medir y reproducir el checkpoint local; no cambia los AC, no realiza pruebas con personas ni sustituye dispositivos físicos por Chrome/emulador.

## Auditoría y reutilización

[Auditoría de fuentes y vigencia](evidence/t018-telemetry-2026-10-13/audit.json). Se reutiliza:

- IPC: framing, PING/PONG correlacionados por instance/connection/sequence, supervisor, deadlines, contadores y ventana acotada. No reimplementación del transporte.
- RTC: transporte seleccionado y ACK de recibo técnico; presupuesto, pending y latest-only existentes. No protocolo de reloj físico nuevo sin fuente de validación.
- PhoneInput: session/player/device/connectionEpoch/sequence, clientTimestamp y serverReceiveTimestamp/unityReceiveTimestamp; captura callback monotónica, unidades/ejes/nulls, ventanas RateWindow4096, emisiones descartadas y sobrescrituras. Contratos y política intactos.
- CameraInput: frameSequence por lanzamiento, timestamps PC UTC, pipe privado, slot latest-only, store y fusion existentes. Python Metrics4096 y los tramos monotónicos ya implementados.
- T016: [r7 técnico sin personas](evidence/t016-camera-runtime-2026-10-09/po-original-ac/physical-offline-r7.json), runtime/modelo/versiones y perfil aceptados, restauración y privacidad. Captura30.223864FPS, inferencia28.919145FPS;1845 capturas medidas,1766 inferencias medidas; process→pipe7/15ms N1909 incluye warmup. Esto acredita su corrida original, no la comparación nueva.

**Stale para aceptación actual:** adquisición≈20FPS de perfiles antiguos; gates de packaging/candidato previos a r7; r6 cleanupFAIL (conservado, no convertido a PASS). Las cifras de RTC/Phone/IPC del [audit previo](evidence/phase0-final-audit-2026-10-08/independent-metrics.json) se retienen como historia LAB, pero cambiaron productores/runtime compartido después de esas mediciones (audit incluye hashes/diffs). PlayMode nuevo valida función, no renueva los P50/P95 históricos. T010 preflight físico y emulador no acreditan Hz físicos finales. Un buffer acotado nunca fue prueba suficiente de AC3.

## Clock domains, fuentes, unidades y correlación

| Dominio | Timestamp source / unidad | Monotónico o wall | Correlación / límites |
|---|---|---|---|
| Browser por navegación | performance.now(), callbackMonotonicMs y ACK-start/receive; ms | Monotónico local, precisión/coarsening del navegador | Diferencias dentro del mismo contexto; no restar valores de otro dispositivo/contexto |
| Evento DOM | event.timeStamp; ms | Origen/semántica hardware no acreditados | Sólo dedup; no afirmar exposición/IMU sampling físico |
| Teléfono UTC | Date.now()/receiptUtcMs/clientTimestamp; Unix ms | Wall; puede ajustarse | Envelope correlacionado por session/player/device/epoch/sequence; offset físico contra PC UNKNOWN |
| Java PC UTC | System.currentTimeMillis(), serverReceiveTimestamp; Unix ms | Wall de la PC | Hora de recibo técnico, no movimiento físico |
| Unity PC UTC | DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), pipe/store/evaluation; Unix ms | Wall de la misma PC | PhoneInput usa recibo Java PC para alineación; CameraInput sequence por worker; no offset de teléfono inferido |
| Python worker UTC | time.time_ns() truncado a Unix ms; read-complete/processTimestamp | Wall de la PC | Read-complete es proxy de captura; processTimestamp se toma tras inferencia, antes de serialize/emit |
| Python worker monotónico | time.monotonic_ns(); ns, diferencias convertidas a ms | Monotónico del host | Captura, handover, inferencia y read→output; sin usar wall para FPS |
| Kernel V4L2 | OpenCV CAP_PROP_POS_MSEC convertido a ns | Proxy kernel, validado por orden/rango contra monotonic_ns | Not optical ground truth; exposición y tiempo sensor desconocidos; unavailable y staleSkip se cuentan |
| Unity frame/IPC | Stopwatch.GetTimestamp/Frequency o Stopwatch.Elapsed; ticks→ms | Monotónico local | Update→Update (cadencia Unity, no FPS de presentación GPU/display) y PING→PONG respectivamente; no asumir que el origen numérico sea intercambiable con Python |

Los traces/contratos existentes correlacionan input de software. La nueva prueba física conserva **sólo agregados** y hashes, nunca un historial de landmarks ni observaciones personales por frame. No añade Result/Score de gameplay inexistente en Phase0.

### Sync y error/uncertainty

- Mismo host: Python/Java/Unity consultan el reloj UTC del mismo SO. No hay negociación de reloj entre procesos. Cuantización entera conjunta conservadora **<2ms sólo de cuantización**; error total UNKNOWN por hora del evento, lecturas, ajuste/slew y captura física. No confundir resolución con exactitud.
- El observador común A/B toma monotonic_ns → time_ns → monotonic_ns a1Hz; estima offset con midpoint y media anchura del bracket. Publica N/rango observado y máximo half-bracket. Es un modelo descriptivo de esta ventana; no prueba sincronización UTC absoluta, exposición, ni una cota futura de drift. No se restan raw Stopwatch ticks y Python ns.
- Teléfono↔PC: no fuente sincronizada física ni jig/medición motion→result disponible en esta sesión. Offset, drift, asimetría de red y error/incertidumbre: **NOT RUN / UNKNOWN**. No se hace sincronización ficticia por ACK. Una estimación futura debe distinguir offset de processing time y retardo asimétrico; RTT/2 por sí solo no es una cota acreditada de error.
- Red/IPC: ACK RTT y PING RTT incluyen recorrido de ida/vuelta y procesamiento; no permiten separar unidireccionalidad sin clocks sincronizados. Java→Unity UTC incluye batching/coalescing/pipe/scheduling. Camera process→pipe incluye serialize/write/lectura/scheduling; read→output incluye handover/inferencia/emisión. Ninguno incluye movimiento→exposición/IMU→resultado visible. **True motion→result end-to-end: NOT RUN.**

## Instrumentación faltante implementada

`GORILLA_TELEMETRY_MODE=minimal|enabled` conserva enabled como default. Mínimo conserva throughput/counters y el pipeline original; desactiva ventanas descriptivas/quality/pixel-mean y transit history. Enabled usa las ventanas existentes. El observer frame es opt-in `GORILLA_T018_MEASURE=1`, igual en ambos brazos; ring de4096 doubles, sin IO/sort por frame, serialización una vez al terminar. Percentiles nearest rank al finalizar; FPS P50/P95 calculados sobre recíprocos **ordenados independientemente** de frame time (FPS P95 no es1000/frame-timeP95). Reporta N lifetime, retained, overwrite, errors y FPS efectivo N/suma intervalos.

Python cuenta errores de telemetría no finita y descarta esos valores, con TimingN separado del conteo de eventos. Reporta capacidad por serie, retained y evictions; el historial legado también declara tamaño/capacidad. No se interpreta un overwrite de ventana como pérdida de input ni una ausencia de landmarks como frame perdido. Los contadores de slots/cámara/Unity son independientes.

## Protocolo A/B reproducible y alcance AC3

[Driver](../tools/t018/compare.py) reutiliza [harness T016](../tools/t016/offline_player.py). Build Linux fresco desde fuentes de esta rama; Vision deriva de r7, con worker/pipeline actuales y manifest SHA actualizado, sin modificar staging r7. Manifest y runtime legacy de T016 no se presentan como certificación de distribución T018; uso técnico local.

Orden A–B–B–A, calentamiento5s y medición Unity61s en cada brazo. Webcam real `/dev/video0`, MJPG640×480@30 solicitado, numposes2, perfil aceptado candidate/manual10ms/priority1/buffers2; mismo Player/Vision, ventana640×360, WAN aislada, developer cache oculto, HOME privado. Sin personas participantes ni cambios de iluminación solicitados; no valida carga de tracking humano ni todos los usos de producto. CPU/temperatura/carga del host no se controlan experimentalmente; orden inverso reduce sesgo de orden, no lo elimina.

El sampler RSS existente `/proc VmRSS` se reutiliza a1Hz sobre Player y worker propios, en ambos brazos; CPU NOT RUN (no sampler previo). Native syscall audit idéntico en ambas ramas; sólo contadores de escritura de imágenes/video y logs físicos. No video ni imagen guardados. Cierre normal propio y controles restaurados obligatorios. El gate PASS de cada JSON es **gate del harness/privacidad/cleanup**, nunca PASS de AC3 ni T018.

```bash
python3 tools/t018/compare.py --build /tmp/gorilla-t018-unity/Builds/FoundationLinux \
  --output /tmp/t018-nueva-evidencia --duration 61 --warmup 5
```

Directorios existentes nunca se sobrescriben. El build debe contener Vision preparada con manifest válido y fuentes coincidentes; los hashes medidos están en provenance. Las ventanas de Unity son61s solicitados con sólo intervalos completos internos; el worker empieza su warmup antes de CAMERA_READY y termina al EOF tras cierre: sus duraciones exactas (incluyen pequeño desfase/cleanup) se publican por separado, no se finge identidad exacta de ventanas.

La v4§35 exige “no afecta perceptiblemente” pero **no fija threshold numérico**. Se publican diferencias absolutas y porcentuales por par, sin promedio de percentiles ni nuevo umbral. La conclusión permanece limitada a las ventanas medidas y no acredita imperceptibilidad general/subjetiva. AC3 queda PARCIAL aunque la comparación se complete.

## Acreditación y estados conservados

| AC original | Resultado |
|---|---|
| AC1: Trazas correlacionadas con reloj/unidad/error de sincronización definidos | PARCIAL: contratos/clock model local documentados; sync/error físico entre dispositivos y motion→result NOT RUN |
| AC2: P50/P95, Hz efectivos, FPS y pérdidas con tamaño muestral | PARCIAL: PC/webcam y LAB histórico con N/rates/counters; teléfono físico/sensores y latencia física NOT RUN; hardware frame loss UNKNOWN |
| AC3: Buffer de telemetría no afecta perceptiblemente framerate; sin video | PARCIAL: A/B técnico numérico y privacidad; no threshold de imperceptible definido ni generalización a carga humana/telefono |

**No cerrar T018.** T017/#30 OPEN / Validation / needs-device-test, AC sin cambios; F04/#28 OPEN; T019/#33 BLOCKED por T017 y T018, reevaluado sin iniciar; T020/#34 BLOCKED por T019; F05/#31 OPEN; E0/#1 OPEN; Phase0 IN PROGRESS; Phase1 NOT STARTED. [Estado remoto previo](evidence/t018-telemetry-2026-10-13/remote-state-before.json). T019 sigue bloqueado aun si un día T018 se cierra: T017 exige intrusiones/oclusiones/recuperación física aún NOT RUN y las pruebas con personas están prohibidas.

No nueva decisión de arquitectura/adopción/AC: DECISIONS.md permanece intacto. Gstack solicitado no encontrado en instalaciones locales; se aplica auditoría/debug/QA/revisión manual proporcional y flujo normal branch→PR→checks→merge, sin bypass/protecciones ni cierre automático. Fallos de setup y corrida exploratoria conservados en [attempts](evidence/t018-telemetry-2026-10-13/attempts.json).

## Cifras previas conservadas con alcance explícito

| Evidencia histórica | N / Hz | P50 / P95 | Clasificación vigente |
|---|---|---|---|
| IPC tres benchmarks |1000 RTT por corrida,100 warmup;49.999668/49.999973/50.000272Hz |1.0951/1.6423;1.0853/1.6477;1.0777/1.6583ms |LAB histórico;0errors/timeouts,1100/1100 válidos. No movimiento→resultado |
| PhoneInput Java→Unity |636 inputs únicos |11/23ms |UTC mismaPC, synthetic/replay; batching/scheduling; histórico |
| Generación cliente→Unity |636 inputs únicos |13/25ms |Software mismaPC, histórico; no sensor ni resultado visible |
| Cliente→Java UTC |636 inputs únicos |1/3ms |MismaPC LAB; no física sincronizada |
| RTC/ACK ejemplo cliente1@30 solicitado |60 motion enviados en2000.6ms;62 ACK recibidos, errores0 |2.7/3.9ms |RTT local navegador; N de workload60, N de la serie RTT no archivado explícitamente (puede incluir heartbeat). Hz de envío observado≈29.991 por60/duración, no Hz físicos |
| iPhone T010 preflight |480 callbacks válidos/canal,53.388319motion/53.394270orientationHz |intervalo17/18ms ambos |Auxiliar anterior, measurement=null; no acreditación física final T018 |

La presión Java histórica recibió961, reenvió643 y sobrescribió318; rechazó202. Son denominadores de presión/coalescing distintos del agregado636 activo, no packet loss nominal. Missing receipts nominal observables0 y errors0 no acreditan pérdida de hardware. La telemetría nueva conserva la misma separación; no suma contadores de ventanas/lifetime distintas.

## Resultados definitivos A–B–B–A

[Provenance/hashes](evidence/t018-telemetry-2026-10-13/abba-final/provenance.json) · [Comparación por par](evidence/t018-telemetry-2026-10-13/abba-final/comparison.json). Ambos pares comparten las mismas condiciones solicitadas. Todos los gates técnicos de harness PASS; no son AC PASS.

|Corrida|Unity N / retained|Unity FPS P50/P95|Unity FPS efectivo|Frame time P50/P95 ms|Captura FPS (N)|Inferencia FPS (N)|
|---|---|---|---|---|---|---|
|[1 minimal](evidence/t018-telemetry-2026-10-13/abba-final/1-minimal.json)|7179/4096|83.879247/1976.284584|117.728402|11.871300/16.789800|30.224877 (1846)|30.025306 (1834)|
|[2 enabled](evidence/t018-telemetry-2026-10-13/abba-final/2-enabled.json)|6750/4096|79.877308/1177.578898|110.692595|12.469700/16.746700|30.224679 (1846)|28.244336 (1725)|
|[3 enabled](evidence/t018-telemetry-2026-10-13/abba-final/3-enabled.json)|7225/4096|88.236332/1645.007401|118.482209|11.330800/16.563800|30.223942 (1846)|29.611282 (1809)|
|[4 minimal](evidence/t018-telemetry-2026-10-13/abba-final/4-minimal.json)|7169/4096|88.697303/1066.439160|117.560105|11.241300/16.055400|30.224199 (1846)|28.134608 (1718)|

Los percentiles Unity cubren **los últimos4096 intervalos**; FPS efectivo/N cubren toda la ventana. El tail retenido dura menos que61s cuando hay más de4096 frames. FPS P95 alto refleja intervalos Update muy cortos/bursty; no se filtra, redondea a refresh, convierte a display FPS ni suaviza. No equivale a workload de gameplay/tracking humano.

|Par (B−A)|FPS P50 Δ / %|FPS P95 Δ / %|FPS efectivo Δ / %|Frame P50 ms Δ / %|Frame P95 ms Δ / %|Captura FPS Δ / %|Inferencia FPS Δ / %|
|---|---|---|---|---|---|---|---|
|1|-4.001939 / -4.7711%|-798.705686 / -40.4145%|-7.035807 / -5.9763%|+0.598400 / +5.0407%|-0.043100 / -0.2567%|-0.000198 / -0.0007%|-1.780970 / -5.9316%|
|2|-0.460971 / -0.5197%|+578.568241 / +54.2523%|+0.922104 / +0.7844%|+0.089500 / +0.7962%|+0.508400 / +3.1665%|-0.000258 / -0.0009%|+1.476674 / +5.2486%|

No se promedian percentiles de corridas independientes. Los datos observados no establecen un threshold ni un PASS de “imperceptible”.

|Corrida|Captura→inferencia overwrite / stale skip / pending discard|Pipe→Unity overwrite / accepted|Worker telemetry retained (+legado) / window evictions / errors|Unity frame-window evictions / errors|Transit N / retained / P50/P95 ms / invalid|
|---|---|---|---|---|---|
|1 minimal|12/0/0|0/1977|0 (+0)/0/0|3083/0|0/0/-1/-1/0|
|2 enabled|126/0/1|0/1862|22907 (+5589)/0/0|2654/0|1863/1863/7/15/0|
|3 enabled|40/0/0|0/1949|23663 (+5850)/0/0|3129/0|1950/1950/7/15/0|
|4 minimal|128/0/1|0/1860|0 (+0)/0/0|3073/0|0/0/-1/-1/0|

Slots/accepted/transit/legacy son lifetime de sesión (incluyen warmup), worker latency statistics y throughput principales excluyen warmup. No sumarlos como un denominador común. HardwareDroppedFrames=null en todas las corridas: **UNKNOWN**, no0. Minimal no recolecta transit (N0,P50/P95−1), no equivale a latencia/error cero. Buffer Unity frames4096 doubles=32768bytes de payload numérico; worker4096 por serie y legado20480 slots máximos, transit4096; tamaños incluyen slots/números, no se inventa tamaño exacto de objetos Python/CLR. No crecimiento de cola por input añadido.

|Corrida|Read→output P50/P95 ms (N)|Inferencia P50/P95 ms (N)|Frame age proxy P50/P95 ms (N)|Unity RSS P50/P95 MiB (N)|Worker RSS P50/P95 MiB (N)|Clock bridge N / max half-bracket ms / offset-range ms|
|---|---|---|---|---|---|---|
|1 |NOT COLLECTED (minimal)|NOT COLLECTED (minimal)|NOT COLLECTED (minimal)|224.434/229.570 (61)|257.434/257.449 (61)|61/0.004136/0.003174|
|2 |33.218236/66.741849 (1725, retained1725)|27.974138/50.920388 (1725, retained1725)|34.214546/58.773733 (1724, retained1724)|225.684/230.793 (61)|258.211/258.238 (61)|61/0.004140/0.003174|
|3 |27.675724/53.477786 (1809, retained1809)|24.986849/43.085722 (1809, retained1809)|33.630397/50.468135 (1808, retained1808)|224.805/230.000 (61)|257.492/258.371 (61)|61/0.002125/0.000977|
|4 |NOT COLLECTED (minimal)|NOT COLLECTED (minimal)|NOT COLLECTED (minimal)|226.348/231.734 (61)|257.445/257.551 (61)|61/0.002619/0.001221|

RSS es por proceso; memoria compartida no atribuida. CPU NOT RUN. El clock bridge reporta dispersión/lectura local, no una cota de sincronización entre dispositivos. Las estadísticas completas capture interval/read/handover/delivery, N y retained están en cada JSON B. Las latencias anteriores son **tramos**, no verdadero end-to-end.

Privacidad/cleanup: todas las corridas Player/workerexit0, controlsRestored=true, captura/detector liberados, hilo unido, sin forced ni residual propio; successfulImageVideoWrites0 y physicalObservationLogs0. Evidencia sólo agregada. Los intentos exploratorios r2 se conservan aparte, no se mezclan con las corridas definitivas.

## Tests y publicación

[Resumen verificaciones](evidence/t018-telemetry-2026-10-13/validation.json): componentes23/23; Unity Edit101/101, Play20/20; LinuxbuildSucceeded0errors0warnings; PWA59/59+build; Java78 tests/0errors/0failures/2SKIP existentes (76 ejecutadosPASS), completoTemurin25.0.4.1 compilando release21; CIJDK21 se exige por separado. Management/Foundation/diffPASS. Primera licenciaUnityFAIL y dos KEYTOOL_REQUIRED con runtimeJava reducido preservados; retry sin cambio de producto. Revisión manual de ventanas/recíprocos/errores/ownership/privacy/diff. Ningún test de personas ni dispositivo telefónico físico nuevo.

Publicación mediante PR hacia develop, sin closing keyword para#32. El merge de instrumentación parcial no cierra T018 ni sus dependientes; estado efectivo de PR/checks/merge registrado en GitHub.

### RSS: diferencias B−A

|Par|Unity RSS P50 ΔMiB / %|Unity RSS P95 ΔMiB / %|Worker RSS P50 ΔMiB / %|Worker RSS P95 ΔMiB / %|
|---|---|---|---|---|
|1|+1.250 / +0.5570%|+1.223 / +0.5326%|+0.777 / +0.3020%|+0.789 / +0.3065%|
|2|-1.543 / -0.6817%|-1.734 / -0.7484%|+0.047 / +0.0182%|+0.820 / +0.3185%|

[Datos RSS derivados por par](evidence/t018-telemetry-2026-10-13/abba-final/rss-comparison.json), N61 lecturas de cada proceso/corrida. No atribuir diferencias de RSS exclusivamente al buffer.

## Current authorized Phase0 exit — DEC-028 / 2026-10-13

Supersedes earlier instructions in this historical report to keep T018 open **only for T018 exit**, under explicit PO authorization. PR #103 MERGED confirmed remotely: head `136ee366b782c6f40a74114be00f881ea0307279`, merge `3129081eeb9cf92c3326038da076c67addc4b0fd`; required management/foundation checks SUCCESS. Existing bounded telemetry, four A–B–B–A runs, errors0 and privacy evidence reused; no runtime changes.

| AC | Literal status retained | Phase0 exit under DEC-028 |
|---|---|---|
| AC1 | PARTIAL: physical phone offset/drift/error UNKNOWN / NOT RUN | PASS FOR T018 EXIT |
| AC2 | PARTIAL: phone physical Hz and motion→visible-result NOT RUN | PASS FOR T018 EXIT |
| AC3 | Universal imperceptibility not demonstrated; no numerical threshold in original spec | PASS FOR T018 EXIT: bounded, no video, telemetry errors0, repeated A/B, published deltas, no consistent reproducible degradation attributable to telemetry |

Physical synchronization, sensor rates, true motion→visible-result and cross-device accreditation move to T019/#33; no E2E PASS claimed. Existing measured deltas retain sign variability; no zero-impact claim. T018 closure and Project Done permitted only after DEC merge/post-merge verification. Historical states above remain dated history. E0/F05 remain OPEN; Phase1 NOT STARTED.
