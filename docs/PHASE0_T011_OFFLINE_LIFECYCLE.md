# T011 / #22 — offline local y lifecycle PWA

## Recuperación local y salida T011 — 2026-10-09

Se recuperó la rama local existente sin reset/clean/recreación. [Inventario inicial](evidence/t011-offline-lifecycle-2026-10-09/recovery-inventory.json): seis archivos modificados, cinco entradas no rastreadas, índice vacío; HEAD base a41257c18dd6dc525c076e0933fec0a180c8fa44. Los tres fuentes productivos locales conservan exactamente sus hashes previos a la recuperación; sólo se ajustaron harness, assertions de QA y documentación.

**DEC-022 ACCEPTED. Candidato listo para PR hacia develop; sin merge ni cierre de #22.** AC1 literal PASS LAB/EMULATOR, salida PASS FOR T011 EXIT under DEC-022; AC2 PASS literal; AC3 PASS LAB/EMULATOR, salida PASS FOR T011 EXIT under DEC-022. **PASS FOR T011 EXIT != physical mobile QA completed.** Todos los gates físicos siguen NOT RUN, ya transferidos a #33/T019 con AC/dependencias/bloqueo intactos. F02 OPEN, Phase0 IN PROGRESS, Phase1 NOT STARTED.

Ejecución nueva [Android final13](evidence/t011-offline-lifecycle-2026-10-09/android-final-13.json): perfil limpio usage0/SW0, primera y segunda carga sin WAN PASS, Java local200, seis recursos locales200/cache exacta, modelos NONE, requests externos runtime0. HOME→CONTROL_SUSPENDED, motionSubmitted oculto delta0, medición INTERRUPTED, retorno INPUT_READY con callbacks frescos; pérdida RTC propia después de >=3s oculta→DISCONNECTED/STOPPED, sin reconnect automático. Esto no mide throttling de hardware físico.

Pagehide acreditado con unit tests + navegación real a vista health + observación externa Unity/RTC: agregado escrito durante pagehide antes de destruir documento, sensor listeners0/0, intervals0, timeouts0, connection closed, input Unity post-drenaje delta0. CDP binding final13 permanece null aunque el agregado prueba que pagehide ocurrió: pérdida del canal de observación al destruir contexto (categoría C; dependencia de timing del harness D), no defecto de producto demostrado. No se cambia producto para sostener un observer. Un único agregado sin tokens/vectores en sessionStorage de QA desechable; ninguna API de debug o almacenamiento productivo agregado. Receptor HTTP de QA retirado. Unit comprueba cancelación de medición y estado DISCONNECTED sin READY viejo.

Listeners: RUNNING1 por canal, SUSPENDED0; ocho ciclos unit hide/show con notificaciones duplicadas, sin duplicar intervalos/timeouts; tras STOPPED0 listeners/intervals/timeouts y peer closed en emulador. Java/Unity exit0, cleanupComplete=true, forced=false, Java/ADB/AVD/helpers residual0; reglas del guest restauradas, red/firewall host sin cambios.

Ejecución nueva: PWA58/58 PASS [log](evidence/t011-offline-lifecycle-2026-10-09/pwa-unit-recovery.txt), build PASS [log](evidence/t011-offline-lifecycle-2026-10-09/pwa-build-recovery.txt); [auditoría de recursos/JAR](evidence/t011-offline-lifecycle-2026-10-09/resource-audit-recovery.json) PASS. Browser7 fixtures [retenidos](evidence/t011-offline-lifecycle-2026-10-09/browser-summary-final.json), no nueva ejecución; build idéntico al resource-audit-final. Java54 PASS/2 SKIP históricos y build Unity retenidos, fuentes intactas, sin nueva suite pesada local. Validaciones finales Management/Foundation/diff-check y security review registradas en integration-preflight.json. PR/head/checks remotos se registran en #22/PR para evitar push directo a develop.

Intentos01–11 FAIL/BLOCKED conservados íntegros; intento12 PASS conservado como checkpoint previo a retirar receptor sobrante; final13 PASS sobre helper final. Los snapshots siguientes conservan el estado pendiente anterior, superseded sólo por este resultado, sin transformar FAIL/NOT RUN en PASS.


Fecha: 2026-10-09. Base `a41257c18dd6dc525c076e0933fec0a180c8fa44`; rama `feature/mobile-t011-offline-lifecycle`. DEC-022 ACCEPTED por PO. #22 OPEN/In Progress hasta evidencia y PR; sin merge. F02 OPEN, Fase0 IN PROGRESS, Fase1 NOT STARTED. T019 OPEN/Validation/blocked, no iniciado.

## Auditoría inicial y alcance mínimo

`PWA/tools/build-service-worker.js` produce gorilla-shell-<content-hash>, lista cerrada de archivos dist (excluye sw.js). Precache sólo rutas build locales; handler sólo GET/same-origin/lista exacta y excluye /api/. /mobile/join, reconnect, admission y diagnostics no pertenecen a esa lista; cuerpos/respuestas/token/SDP/sensores no se guardan. `main.jsx` registra SW sólo import.meta.env.PROD. Mecanismo conservado sin Workbox/plugins/dependencias.

El build actual requiere index.html, JS/CSS empaquetados, manifest.webmanifest, iconos192/512 PNG y recursos estáticos propios. PWA runtime model dependencies = NONE. Ningún CDN/font/modelo remoto requerido. Java/PC sigue siendo necesario para nueva sesión con QR/admission vigente; shell cacheado no fabrica sesión.

SensorCapture ya poseía visibilitychange/pagehide/detach/reattach. Faltantes demostrados por auditoría: inputState podía acreditar muestras antiguas al volver antes de 2000ms; ControllerSession no distinguía suspensión y retenía medición/pending input. Corrección mínima: CONTROL_SUSPENDED, cancelar medición inmediatamente, borrar último pendiente no enviado; nueva generación/epoch, latest/validAt/readiness-count reiniciados al cambiar visibilidad. Volver requiere tres eventos nuevos por canal; PREPARING_SENSORS hasta adquisición fresca. Un frame enviado antes de ocultar puede terminar su ACK; no se retracta ni se confunde con nueva emisión oculta.

Pagehide dispone ControllerSession, captura, timers y transporte propio. Captura mantiene una sola arquitectura y listeners idempotentes. Si transporte murió, DISCONNECTED y nuevo QR, sin reconnect automático. captureRevision t010-2-coarsened-clock, maximumHz50, rangos/tiers/freshness2000ms y Gorilla Protocol v1 intactos. No gameplay/calibración/cámara/Flutter/eventos score.

## Pruebas y evidencia

Pruebas unitarias nuevas con reloj/document/window inyectados, sin timers frágiles: hidden detaches/zero emission/pending discard/measurement INTERRUPTED; retorno exige eventos frescos; ocho hide/show sin listeners duplicados; pérdida RTC no se convierte en READY; pagehide limpia timers/listeners/transporte. Prueba SW ejecuta generador en dist temporal y verifica lista local + no interceptar API/JOIN/reconnect/admission/diagnostics/otro origen.

Software inicial: PWA57/57 PASS (51 previas +6 nuevas), build PASS; browser7 escenarios de fixtures PASS. Son software/fixtures, no evidencia física. Java runtime sin cambios; JAR reempaquetado con build PWA actual sin repetir tests Java locales; checks CI aplicables ejecutarán verify.

Android: `python3 tools/t011/android_offline.py <directorio-nuevo>` usa SDK/API36 existentes, AVD/Chrome/ADB desechables, PWA real→WebRTC→Java hijo→IPC→Unity real. Sin inyección dispatchEvent ni modificación de sensores/time/rangos/permiso; instrumentación etiqueta quality.source=emulator, observa requests/listeners y cierra peer propio en caso determinista de pérdida. ADB reverse/localhost != physical LAN/TLS validation. La ausencia WAN se fuerza sólo dentro del guest con OUTPUT reject IPv4/IPv6 salvo loopback y endpoints de PC, datos móviles deshabilitados; no firewall/router/DNS/host network modificado. Probe browser externo y TCP IP pública deben fallar, endpoint local debe responder. Perfil nuevo + Storage.clearDataForOrigin(all) + Network.clearBrowserCache antes de navegación; usage0/SWnone previos acreditan primera carga sin preparación.

Resultados finales de emulador pendientes de terminar; no PASS provisional. Fallos de harness conservados como fallos, no evidencia de defecto del producto ni escenarios exitosos.

## QA física diferida — DEC-022 → #33/T019

Todos NOT RUN: primera carga física sin caché en LAN/sin WAN; recursos locales iPhone/Android; suspensión corta y larga, throttling/lock-screen/return; cero stale motion oculto; recuperación segura/DISCONNECTED; diferencias Safari/Chrome. Append al Issue preserva AC/dependencias/bloqueos anteriores. PASS FOR T011 EXIT != physical mobile QA completed.

### Intentos de harness conservados

Intento01 FAIL: Chrome VIEW activity no resolvía durante cold boot; AVD/ADB residual0. Intento02 FAIL: probe TCP invocado incorrectamente a través de sh -c; probe browser sí UNAVAILABLE, almacenamiento0/SWnone y cleanup Java/Unity/ADB/AVD0. Intento03 FAIL después de first-load PASS/assets200/cache exacta: segunda admisión cambió sólo el fragmento en el documento anterior; corregir harness para reapertura completa, no producto. Intento04 BLOCKED: first/second loads PASS, short hidden delta0/measurement INTERRUPTED/resume INPUT_READY/long DISCONNECTED PASS; UI-tree ACTIVAR CONTROL ausente en última navegación pagehide. Cleanup completo0, guest network restored. Ningún intento fallido se renombra como gate global PASS; última repetición con espera de UI-tree bounded, producto idéntico.

### Regresión mínima de cleanup detectada en revisión

El guard de dispose agregado en T011 podía devolver undefined en la segunda llamada, mientras cleanup React invoca .catch. Se corrigió a método async idempotente y se agregó prueba específica: PWA58/58 PASS, build PASS nuevos. Pagehide publica DISCONNECTED antes de disponer, evitando conservar READY en un documento anterior; nueva admisión/QR para continuar, sin reconnect automático. Misma política de adquisición/rangos/freshness/budget/protocolo. Se reempaquetó el JAR y se repiten las comprobaciones afectadas sobre este candidato.

Intento05 BLOCKED repite A–E exitosos y ausencia UItree de ACTIVAR CONTROL en tercera admisión; no se conoce una causa de producto demostrada. El escenario pagehide se trasladó a la primera sesión activa, antes de segunda carga, preservando A–F y evitando esa tercera navegación innecesaria. Intento06 FAIL en comprobación de pagehide: instrumentación QA introducía un alias global clearInterval que podía evitar su propio hook de contabilidad; se renombró sin cambiar timers productivos. No se afirma un valor de contador que no se haya retenido. Resultados completos de intentos previos permanecen intactos.
