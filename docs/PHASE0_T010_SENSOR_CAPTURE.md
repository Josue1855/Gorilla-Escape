# T010 / #21 — captura PWA real y frecuencia efectiva

Fecha: 2026-10-09. DEC-020 ACCEPTED por PO. Rama `feature/mobile-t010-sensor-capture`, base integrada `1f4b129100ab37f8621ca655a325599578359192`. #21 candidato autorizado para integración bajo DEC-021; cierre sólo después de merge y checks post-merge. Los checkpoints anteriores se conservan como historia. F02 OPEN, Fase0 IN PROGRESS, Fase1 NOT STARTED. T011 no iniciado.

## Diseño implementado

`PWA/src/input/sensors/capture.js` mantiene SensorCapture standalone, sin dependencia nueva: adquisición/eventos/validación/snapshot interno/metrics agregadas/cleanup. ControllerSession coordina permisos existentes, captura, binder y estado; React sólo presenta preparación/listo/limitado/ausencia y medición diagnóstica secundaria. Permisos se solicitan juntos desde gesto explícito, sin segunda solicitud al adjuntar captura. Los timers sólo refrescan estado y finalizan mediciones; nunca producen input.

Se conserva el comportamiento de suspensión/cleanup del capturador histórico; no se implementa la política ampliada T011. Una corrida oculta/suspendida, reiniciada o desconectada queda INTERRUPTED. Stop limpia listeners/timer; dispose cancela también monitor/medición y transporte propio. Sólo una corrida diagnóstica activa y un resultado agregado congelado, reemplazado explícitamente por la siguiente.

### Ruta legacy auditada

El import `./sensors/capture.js` **no estaba roto en el JAR**: Maven copiaba `PWA/spikes/sensors/capture.js` bajo esa ruta. Ahora el recurso proviene de `PWA/src/input/sensors/capture.js`; la copia Maven redundante se retira. El entry histórico reexporta ese módulo real. `/mobile-lab/index.html` sigue útil para regresión RTC real y comparte adquisición. El servidor del harness WT apunta al mismo archivo, sin ejecutar/reabrir WT ni modificar transporte. Copias públicas congeladas de spikes anteriores permanecen históricas, sin presentarlas como producto actual.

### Identidad y relojes

Secuencia por canal sólo avanza en evento único aceptado. `validSequence` avanza sólo si al menos un campo sensor finito existe; all-null no emite. Dedup por objeto de evento y timeStamp DOM positivo no creciente, no por valores: ceros repetidos en reposo son válidos si provienen de eventos nuevos. TimeStamp ausente/0 utiliza identidad de evento y receipt monotónico. Clocks regresivos rechazados/contados. Snapshot repetido con la misma generación/secuencias válidas no emite; nueva generación reinicia identidad de adquisición.

`performance.now()` = receipt monotónico/rate/jitter; `event.timeStamp` = metadato DOM para identidad, hardware capture time no demostrado. `Date.now()` sólo UTC de recepción para wire clientTimestamp, no rate. Dos canales pueden legítimamente compartir un milisegundo UTC; no se repite un sample para producir Hz. Último motion + última orientation conservan disponibilidad; no se afirma sincronía física. Screen angle se toma del evento más reciente.

### Unidades, ejes y ausencia

Contrato móvil v1 sin cambios: acceleration/accelerationIncludingGravity x/y/z m/s²; rotationRate alpha/beta/gamma grados/s; orientation alpha/beta/gamma grados; screenOrientation.angle grados. Convención web: ejes del dispositivo (x derecha, y arriba, z hacia fuera de pantalla), no mundo Unity/anatomía. Euler web Z-X'-Y''; alpha [0,360), beta [-180,180), gamma [-90,90). Ninguna conversión a quaternion ni inferencia de mano dominante. Fuente: [W3C](https://www.w3.org/TR/orientation-event/).

Finito o null; cero válido. Campos ausentes e inválidos diferenciados y contados por eje; availability present/partial/unavailable conserva semántica v1. No saturación física inventada ni potencia calculada. Los rangos Euler y `interval>0` corresponden al contrato web; `interval` declarado por API no sustituye la medición.

### Métricas y límites

Por canal: rawCallbacks, eventos únicos aceptados, validSamples, duplicate/out-of-order, campos ausentes/invalidFields, availability; rawAcquisition y validUnique con count/windowCount/duration/Hz/min/median/P95 interval. Frecuencia de callbacks web, no frecuencia hardware demostrada. Ventanas independientes de hasta4096 timestamps; callbacks distintos pueden compartir receipt por resolución del reloj: se cuentan todos, conservando intervalos0 reales; no se inventa un timestamp más fino. La identidad se valida antes en SensorCapture, no se deduplica por receipt. Si la ventana se llena, Hz/intervals corresponden sólo a windowCount; count total no se usa como numerador de esa ventana.

Emisión se mide por separado desde llamadas al binder/MobileClient.motion. Transport counters: motionSubmitted/motionSent/motionAck/motionErrors/overwritten + errores globales; heartbeat/control no se cuenta como Hz sensor. Deltas de ACK durante una ventana pueden incluir un frame anterior o dejar el último en vuelo: no son medida de adquisición ni end-to-end gameplay.

Cinco anillos Float64 de4096 (4 adquisición/validación +1 emisión) =160KiB numéricos; últimos dos eventos, último snapshot, un resultado agregado, claves de eje fijas; no trace física indefinida. Readiness O(1) sin ordenar ventanas en cada callback; percentiles sólo al abrir diagnóstico/finalizar medición. Un timer de captura500ms, monitor existente500ms, máximo un timeout de medición; sin timer de emisión, sin nueva cola. RTC mantiene un motion en vuelo + un último pendiente, pendingMap máximo8/frame2048/buffer8192/ACKdeadline2s existentes.

Tier = mayor umbral realmente alcanzado60/50/30/20; inferior20 degraded-below-20, ausencia unmeasured. Ejemplo49.99Hz→tier30 con actual49.99, sin tolerancia que maquille resultados. No se adoptó limitación de envío antes de medir el dispositivo físico; ajuste posterior documentado abajo. Readiness no certifica50Hz ni calibración: >=3 eventos finitos/frescos por canal + grupos completos = INPUT_READY; parcial INPUT_LIMITED; permiso sin evento sólo PREPARING y después2s NO_SENSOR_INPUT con retry real desde gesto.

## Pruebas y gate físico

Software nuevo: PWA51/51 PASS (25 previas +9 regresión del capturador histórico +17 pruebas T010), build PASS. Browser7 escenarios PASS con RTC/permisos/eventos **fixtures**, responsive320/390/768/1280, safety/foco/reduced-motion/diagnóstico sin secretos. Java56 ejecutadas,54PASS+2SKIP históricos, build SUCCESS; warnings Java preexistentes conservados. RTC→Java→IPC→Player se registra cuando termine la regresión del JAR actual. No nueva ejecución Editor/build Unity: fuentes/config idénticas a base.

Físico pendiente: iPhone15 / iOS27 reportado / Safari incluido; build exacto OS/browser UNKNOWN. Android físico NOT RUN; emulador no sustituye frecuencia física. Dos corridas independientes con página visible: reposo15.5s y movimiento suave25s. Compartir sólo diagnóstico agregado al terminar cada una; nunca lanzar/soltar/agitar fuertemente. La UI conserva un resultado congelado; copiar Run A antes de Run B. Confirmar duración real>=15s, counts, Hz independientes, intervalos, ejes ausentes, no finitos, emisión/sobrescrituras/errores y estado. Sin reinstalar CA ni simular primera preparación.

Usar confianza DEC-016 existente, HTTPS18443 separado del origen8443 que previamente mostró Inear; no borrar datos/permisos Safari. Sólo excepción temporal TCP18443 desde iPhone confirmado, autorizada por PO; sin UDP preventivo/puertos CA. Retirar exclusivamente la regla propia al cierre, comprobar política intacta/PlayerJavaexit0/residual0. Helper cuenta observaciones declaradas physical en Unity de forma agregada y descarta valores; la confirmación humana establece procedencia física, no el string source por sí solo.

### Hallazgos físicos conservados, sin reinterpretación

[Intento1](evidence/t010-sensor-capture-2026-10-09/physical-attempt-01.json): Safari mostró shell anterior T009, sin métricas; NOT MEASURED. Java comprobado servía HTML/bundle T010, actualización normal de tabs permitida, sin borrar datos ni cambiar permisos. No rediseño de Service Worker/T011 en esta tarea.

[Intento2](evidence/t010-sensor-capture-2026-10-09/physical-attempt-02-short.json): INPUT_READY real,74 eventos por canal en~2.43s, cero campos inválidos/ausentes. **No corrida >=15s; measurement=null**. Reveló subcuenta de emisión por callbacks distintos con receipt al mismo milisegundo Safari: RateWindow descartaba tiempos iguales. Causa mínima corregida: aceptar receipt no decreciente para eventos ya distintos, conservar intervalos0/precisión original y probar fixture coarsened-clock. No fabricación de muestras/tiempos. Resultados originales preservados, no recalculados como corrida final; repetición de pruebas afectadas y mediciones con build corregido.

### Ajuste de emisión tras evidencia real

[Corrida gentle corregida antes del presupuesto](evidence/t010-sensor-capture-2026-10-09/physical-attempt-05-gentle-before-budget.json):25.001s, motion60.0032Hz y orientation60.0008Hz,1500 eventos válidos por canal, emisión3000/120.0416Hz,13 errores/7motionErrors y1710 sobrescrituras. No se elimina ni reetiqueta como reposo. Java ya limita120 mensajes/s incluyendo control/heartbeat; emisión de ambos canales cerca de120 deja sin margen. Los códigos individuales de esos errores no se capturaron, por lo que no se atribuyen todos a un motivo demostrado.

Con la autorización de DEC-020 para downsampling **después** de medir, ControllerSession usa ahora presupuesto inicial50 emisiones/s. Binder sólo se ejecuta por evento válido nuevo: descarta intermedios antes del siguiente slot monotónico, usa últimos valores disponibles y avanza un único slot; tras pausa reinicia due sin catch-up burst. No timer de emisión ni nueva muestra, no cambia raw/valid Hz. Contadores locales emitted/budgetDropped independientes de RTC overwritten. El target50 no sustituye la tasa efectiva que se medirá. Fixtures de dos canales60Hz acreditan501 emisiones de1202 eventos en10s,701 descartes, sin replay/upsampling; generación nueva reinicia due.

La versión del módulo se identifica en diagnóstico como `t010-2-coarsened-clock`; presencia de `emissionPolicy.maximumHz=50` identifica además el presupuesto productivo actual. Resultados físicos finales deben acreditar ambos. Cache/SW anterior y traslado privado sin fragmento se conservan como intentos no medidos. Origen de laboratorio nuevo19443 autorizado para abrir QR directamente en Safari normal, sin modificar certificados/router/DNS, borrar permisos o exigir pestaña privada al producto. Causa exacta de estado Safari anterior no demostrada; no se rediseña Service Worker ni T011.

## Preflight final y cambio de prueba solicitado por PO

[Preflight iPhone](evidence/t010-sensor-capture-2026-10-09/iphone-budget-preflight.json) acredita INPUT_READY, NETWORK_READY, revisión t010-2-coarsened-clock y presupuesto50. Ventana de8.97s: motion53.3883Hz, orientation53.3943Hz, emisión44.0308Hz, motionErrors/errors0, measurement=null. No sustituye reposo15s ni gentle25s. Intento previo [rechazado](evidence/t010-sensor-capture-2026-10-09/physical-attempt-06-admission-rejected.json): HTTPS llegó a Java, ADMISSION_EXPIRED_OR_USED, sensores no iniciados; no medición.

PO solicita continuar con Android Emulator. Corridas físicas finales reposo/gentle permanecen NOT RUN. [Cleanup](evidence/t010-sensor-capture-2026-10-09/physical-06-cleanup.json): Unity/Java exit0, STOPPED/cleanupComplete, propios Java residuales0; retirada única regla TCP19443 propia, política/fullStatus iguales al baseline, firewall activo. No ampliar alcance ni cerrar #21 con pruebas emuladas.

Automatización auxiliar: `python3 tools/t010/android_capture.py /tmp/gorilla-t010-android-<nuevo>` usa SDK/API36 y Chrome existentes, AVD/ADB desechables y PWA real servida por Java hijo de Unity. HTTP localhost vía ADB reverse es un contexto seguro de laboratorio, **no HTTPS/TLS público ni LAN física**. Instrumentación CDP sólo etiqueta quality.source=emulator en el transporte para no atribuirlo a hardware físico; no sustituye sensor API, eventos, payloads ni tiempos. ADB controla sensores virtuales; capturas de página omiten barra de URL. Guarda reposo antes de gentle, métricas agregadas y cleanup, sin credenciales/SDP/vectores crudos. Dos primeros intentos fallaron por ruta de admisión a shell antiguo JOIN GAME; no son un defecto acreditado del producto, resultados conservados separados.

## Android Emulator — corridas completas auxiliares

[Resultado completo](evidence/t010-sensor-capture-2026-10-09/android-final-06-results.json), API36 / Chrome133.0.6943.137: PWA real, preflight INPUT_READY/revisión/budget50 PASS; eventos nativos del navegador del hardware virtual, sin dispatchEvent. Java/RTC/IPC/Unity recibió1752 observaciones declaradas emulator, ninguna physical. No se acredita HTTPS público ni LAN física (ADB reverse a localhost).

| Ventana emulada | Estado medición / gate | Motion Hz / válidas | Orientation Hz / válidas | Emisión Hz | submitted / sent / ack | motionErrors / errors / overwritten |
|---|---|---|---|---|---|---|
| [Reposo](evidence/t010-sensor-capture-2026-10-09/android-final-06-android-rest.json),15.5052s | COMPLETE / FAIL |58.22435094695257 /903|13.190034196384953 /163|48.53491674196463|753 /748 /749|0 /0 /5|
| [Movimiento virtual](evidence/t010-sensor-capture-2026-10-09/android-final-06-android-gentle.json),25.015s | COMPLETE / FAIL |58.57552960206278 /1464|47.9155148514058 /1197|48.80587752487338|1220 /1216 /1217|0 /0 /4|

Emisión bounded50 y transporte sin errores en ambas ventanas; sobrescrituras latest-only esperadas. ACK delta puede incluir mensajes enviados antes del comienzo de ventana: no exigir igualdad artificial con sent delta. Reposo termina NO_SENSOR_INPUT: motion age10.1ms, orientation age3127.5ms; inputState exige ambos canales frescos(<2000ms). Determinar si la ausencia de eventos de orientación con posición estable es delivery normal del navegador o interrupción antes de cambiar política. No descartar el fallo ni afirmar que faltan todos los sensores.

Gentle termina INPUT_READY pero registra27 campos de orientación inválidos; motion0, duplicates/timing/missing0. El contador invalidAxes no identifica los casos de rango en el código actual; causa por eje pendiente de observación adicional, no subir límites ni admitir campos inválidos. La observación inicial de variación acceleration/rotation del harness no es válida: JSON.stringify sobre objetos DOM no enumera necesariamente sus ejes; los flags false no prueban señal constante. Cifras de captura/transporte provienen del diagnóstico del producto y permanecen intactas; una comprobación posterior leerá ejes explícitos.

Primeros intentos automatizados fallidos conservados: ruta antigua JOIN GAME; pulsaciones de controles fuera de vista/ambiguas; un fallo de arranque AVD. Corridas completas usan ADB/taps desde árbol UI para CONECTAR/ACTIVAR CONTROL, y DOM click sólo para diagnóstico/medición. Ningún permiso/sensor sustituido. Unity/Java exit0, STOPPED/cleanupComplete, Java/AVD/ADB propios residuales0. No PR/commit/push/merge ni cierre de #21; AC físicos finales siguen pendientes.

### Comprobación adicional por eje, sin cambiar el producto

[Resultado diagnóstico07](evidence/t010-sensor-capture-2026-10-09/android-diagnostic-07-results.json) y [ventana gentle separada](evidence/t010-sensor-capture-2026-10-09/android-diagnostic-07-android-gentle.json):25.0068s COMPLETE, INPUT_READY, motion55.47160887706065Hz, orientation45.86217273547999Hz, emisión46.45006016847172Hz. Transport submitted1159/sent1140/ACK1141, motionErrors/errors0, overwritten19. No combinar con las otras corridas.

El observador corregido lee propiedades explícitas de los objetos DOM:1521 devicemotion y1217 deviceorientation, todos isTrusted; accelerationChanged/rotationChanged/orientationChanged=true. Esto acredita cambios de hardware **virtual**, no movimiento físico. Identifica8 valores alpha fuera de[0,360); beta/gamma0 fuera de rango. El producto registra8 campos orientation inválidos y los convierte en null, no permite esos valores. No se ha identificado aquí el valor exacto ni por qué lo entrega el emulador/navegador; no ampliar rangos ni declarar corregido un defecto sin más evidencia. Gate de esta prueba nominal mantiene FAIL por campos rechazados.

Software de producto sin cambios respecto del candidato ensayado. Helper de QA corregido para ruta actual, controles de medición DOM y observación de ejes; no dependencias nuevas/flags/TLS bypass. Java recibió y Unity observó1024 muestras emulator. Cleanup normal: Player0/Java0, STOPPED/cleanupComplete, Java/AVD/ADB propios residuales0. Firewall ya restaurado antes de Android. Management/Foundation locales PASS tras documentación; PWA51/build/Java54PASS+2SKIP anteriores retenidos, no nueva ejecución de suites sin fuentes cambiadas.

Conclusión: transporte y presupuesto50 observados sin errores, sensores virtuales con cambios demostrados; prueba completa no es PASS. T010 continúa OPEN/In Progress por corridas físicas finales NOT RUN y revisión acotada de frescura de orientación en reposo. No commit/push/PR/merge, no T011, F02 OPEN, Fase0 IN PROGRESS, Fase1 NOT STARTED.

## Diagnóstico acotado y aceptación PO del avance de laboratorio

Instrucción PO posterior a solicitar el gate físico: «por lo pronto no quiero usar dispositivos fisicos, que las pruebas pasen con el emulador, para mi ya pasa». Se registra aceptación del avance por el PO y suspensión de solicitudes de dispositivos físicos; reposo/gentle iPhone final permanecen NOT RUN / diferidos, no PASS. No se cambia DEC-020, readiness, contrato, AC históricos ni política de freshness. La aceptación PO se distingue del resultado objetivo de la prueba nominal: FAIL anteriores conservados. #21 sigue OPEN/In Progress, sin PR/cierre ni T011; no reasignar/cerrar T019 ni otros Issues automáticamente.

[Diagnóstico alpha09](evidence/t010-sensor-capture-2026-10-09/android-alpha-09-alpha-diagnostic.json): observador pasivo bounded desde activación, alphaViolationCount14, min360, max360, primeros8 valores todos360; beta/gamma0. No negativo epsilon ni valor mayor de360 observado en esta corrida. El intervalo web [0,360) sigue intacto:360 se convierte en null, sin clamp/modulo ni cambios al protocolo. Anomalía demostrada en este AVD/API36/Chrome133.0.6943.137; no declarar exclusivamente emulador frente a iPhone final no probado.

[Ventana09 separada](evidence/t010-sensor-capture-2026-10-09/android-alpha-09-android-gentle.json):25.0008s COMPLETE, INPUT_READY, motion59.39745428038163Hz, orientation52.36276407777539Hz, emisión49.56346487484926Hz, maximumHz50; submitted/sent/ACK1238, motionErrors/errors0, overwritten0. Orientation invalidFields13 dentro de ventana; observador14 acumulados incluye tiempo previo, no son el mismo contador/denominador. No combinar con otras corridas ni sustituir las nominales FAIL. [Cleanup09](evidence/t010-sensor-capture-2026-10-09/android-alpha-09-results.json): Unity/Java0, STOPPED/cleanupComplete, Java/AVD/ADB propios residuales0. Intento08 guardó medición pero perdió conexión CDP antes de persistir observador; conservado, no PASS.

[Controles enfocados](evidence/t010-sensor-capture-2026-10-09/focused-checks.json):5/5 PASS nuevos; diagnóstico bounded≤8, replay determinista de valores nativos inválidos conviertealpha360→null/preserva beta/gamma0, política vigente de freshness sin fabricar eventos/Hz, y presupuesto/errores de las dos ventanas emuladas conservadas. Comando reproducible: `node tools/t010/verify_diagnostics.mjs <alpha-diagnostic.json> <android-rest.json> <android-gentle.json>`. Estos controles no equivalen a nueva ejecución física ni convierten la corrida nominal FAIL en PASS. La prueba de freshness confirma comportamiento actual, no su adecuación para orientación estacionaria de todos los teléfonos.

Para repetir sólo diagnóstico bounded: `python3 tools/t010/android_capture.py /tmp/gorilla-t010-alpha-<nuevo> --gentle-only --skip-screenshot`. Guarda count/min/max y primeros8 alpha inválidos, no history largo; no modifica producto ni distribuye estos datos en Gorilla Protocol. Capturas anteriores retenidas; skip-screenshot evita dependencia de renderer CDP para preservar el diagnóstico. PWA51/build/Java54PASS+2SKIP previos retenidos; no suites costosas repetidas ni fuentes de producto modificadas en esta iteración. Management/Foundation y diff-check nuevos tras documentación.

Decisión de freshness: mantener política actual; sin iPhone reposo final no resolver casoA/casoB ni declarar fallo físico. AC1/AC2 no acreditados físicamente; AC3 PASS software retenido y controles bounded complementarios PASS. Aceptación PO de avance registrada separadamente. F02 OPEN, Fase0 IN PROGRESS, Fase1/T011 NOT STARTED.

## Acceptance Criteria

### Nota de arquitectura para backlog posterior

Unity conserva la autoridad de gameplay y resultados. Los futuros eventos GAME → PHONE usarán el canal de control fiable. SCORE_UPDATE, COMBO, ROUND_RESULT, HAPTIC_EVENT y UI_EVENT quedan para el Issue de protocolo/game-state correspondiente: T010 no implementa estos eventos ni amplía Gorilla Protocol.

| AC literal | Estado actual | Gate pendiente |
|---|---|---|
| AC1 muestras válidas/únicas, unidades/ejes trazables | PARCIAL | captura física medida; software válido/único/null/finito probado |
| AC2 objetivo50Hz/degradaciones60/50/30/20 | PARCIAL | medición Hz iPhone; clasificador fixtures PASS, no cuatro frecuencias físicas acreditadas |
| AC3 no simular Hz/potencia infinita | PASS software | tests no timer-samples/dedup/finite/latest-only; Power fuera de alcance |

No T011/reconnect/calibración/gestos/Power/Spin/PLAYER_READY/gameplay/cámara/Flutter implementados. Android físico/cross-device siguen T019 sin modificar sus AC/dependencias ni ejecutarlo. Abrir PR sólo tras AC acreditados; #21 OPEN/In Review hasta integración y aprobación PO.

## DEC-021 — salida autorizada y QA física diferida

**ACCEPTED por PO, 2026-10-09.** Esta decisión supersede la restricción histórica de esperar corridas físicas para abrir PR/cerrar T010; no altera sus evidencias ni el producto. Snapshot previo al PR: integración pendiente; hashes y estado final de integración se registran en #21/PR para conservar trazabilidad del candidato sin push directo a develop.

| Criterio | Literal evidence status | Exit status |
|---|---|---|
| AC1 | PARTIAL — physical final accreditation deferred | PASS FOR T010 EXIT under DEC-021 |
| AC2 | PARTIAL — physical final accreditation deferred | PASS FOR T010 EXIT under DEC-021 |
| AC3 | PASS | PASS |

**PASS FOR T010 EXIT != physical QA completed.** Software/emulador aceptados para implementación; no acredita hardware físico universal. Producto sin cambios tras candidato final aceptado; inventario SHA256 retenido verificable. Alpha360 sigue inválido/null; freshness2000ms intacta. El caso AVD estacionario es LAB OBSERVATION / physical applicability UNKNOWN. Resultados nominales FAIL anteriores permanecen FAIL.

### Deferred physical QA from T010 / DEC-021 → #33/T019

- iPhone physical REST ≥15s: NOT RUN.
- iPhone physical GENTLE ≥25s: NOT RUN.
- Physical actualMeasuredHz for motion and orientation: final accreditation NOT RUN.
- Physical emission rate with maximumHz50: final accreditation NOT RUN.
- Stationary-orientation freshness on real hardware: NOT RUN.
- Physical alpha/beta/gamma range behavior: NOT RUN.
- Android physical sensor capture: NOT RUN.
- Cross-device sensor-rate comparison: NOT RUN.
- No significant transport errors on physical devices: final accreditation NOT RUN.

T019 conserva AC originales, dependencias y bloqueo; no se inicia. El cierre de #21 y retiro de needs-device-test sólo proceden después de merge normal y validación post-merge; #22 Ready sólo después de confirmar #20/#21 Closed/Done. Sin T011/gameplay/Flutter. F02 OPEN, Fase0 IN PROGRESS, Fase1 NOT STARTED.
