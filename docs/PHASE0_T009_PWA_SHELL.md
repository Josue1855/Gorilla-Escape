# T009 / #20 — Gorilla Core shell y permisos

Fecha: 2026-10-09. Base integrada: `fe4d4df1e10a33323cc4dbc0d513329b2906cda2` (PR #96). Rama: `feature/mobile-t009-pwa-shell-permissions`. [Hashes de fuentes y artefactos probados](evidence/t009-pwa-shell-2026-10-09/tested-manifest.json). Estado: candidato validado para revisión; no integrado, #20 OPEN / In Review. F02 OPEN, Fase0 IN PROGRESS, Fase1 NOT STARTED. T010/T011 no iniciados.

## Alcance y decisiones

[DEC-019 Accepted](DECISIONS.md#dec-019--controller-ux--theme-strategy). Gorilla Core: bosque/sand/naranja, silueta angular, tipografía de sistema sin red, controles grandes y responsive; única apariencia PWA. App opcional y futuras skins sólo documentadas, sin ventaja competitiva ni cambio de layout. PlayerColor queda conceptual. Sin dependencias nuevas; React/HTML/CSS suficientes, no motor de temas/animación/haptics.

`ControllerSession` separa conexión/permiso de React; `permissions.js` sólo sondea APIs y pide permisos; `presentation.js` contiene texto/feedback y CSS usa tokens. Dos `requestPermission()` invocados sin await previo dentro del click ACTIVAR CONTROL; no solicitud en carga/CONNECT. No adquisición de stream, frecuencia, touch gameplay, calibración, reconexión automática ni offline/suspensión nuevos. `MobileClient`, SensorCapture, Gorilla Protocol, ICE, IPC y trust model sin cambios.

## Estados reales

| Estado UI | Evento / salida | Semántica |
|---|---|---|
| NO_ADMISSION | Sin fragment de sesión/admission | Pedir QR, no búsqueda simulada |
| CONNECT | QR disponible → CONECTAR | Presencia no implica admission vigente; Java decide |
| CONNECTING | T008 connect real | Sin pedir sensores |
| PERMISSION_REQUIRED | HELLO/ACK válido → NETWORK_READY | Conexión local, no input listo |
| REQUESTING_PERMISSION | ACTIVAR CONTROL / reintento explícito | Ambos permisos disponibles conservan gesto |
| PERMISSION_GRANTED | Granted o API sin requestPermission | Acceso API; signalVerified=false |
| PERMISSION_DENIED | Al menos un deny | Explicación, reintento real; permisos de sitio si navegador recuerda deny |
| SENSOR_UNAVAILABLE | Al menos una API ausente | Capability exacta en diagnóstico; soporte parcial conservado |
| ERROR | Conexión/solicitud fallida/contexto inseguro | QR nuevo para conexión, reintento para permiso |
| DISCONNECTED | Transport existente disconnected/failed | Wi-Fi/PC/QR nuevo; sin reconnect automático |

CONNECTED se representa por NETWORK_READY independiente del estado de permiso. INPUT_READY requiere stream válido (T010); CALIBRATION_REQUIRED/PLAYER_READY son futuros, no implementados. PERMISSION_GRANTED no verifica eventos/hardware. APIs sin requestPermission se registran `not-required-by-api`, no un grant explícito. Ausencia de API no inventa muestras cero ni certifica incompatibilidad total.

Advertencia visible antes del botón: **Sujeta firmemente tu teléfono. No lo sueltes ni lo lances durante el juego.** Foco visible, botones ≥48px, estado aria-live, reduced-motion, sin flashes ni múltiples efectos. Diagnóstico sólo aparece después de acción secundaria; conserva trace sanitizado T008 y añade únicamente enums de capability/permisos/estado, sin URL/tokens/SDP/identidades.

## Routing y conservación

QR producto HTTPS LAN ahora apunta `/` (shell React), conserva fragment one-use/session/expiry/replay y POST/SDP/ICE/HELLO originales. Único cambio Java: destino de página; test LocalTrustTest ajustado a esa decisión. `/mobile-lab/index.html` y harness RTC quedan conservados. Comprobación Health anterior permanece en `/?view=health`. Hosting/Service Worker existentes conservados; no se redefine T011 ni se promete caché persistente.

## Pruebas y gates

- Unit PWA: 25/25 PASS (14 previas +11 T009); fixtures explícitos, no sensores físicos.
- Build PWA: PASS; bundle JS 238.53 kB / gzip75.64 kB y CSS2.83 kB / gzip1.18 kB.
- Java verify: 56 tests,54 PASS,2 SKIP,0 failures/errors; build PASS. Skips existentes preservados.
- Browser component QA: [7 casos PASS](evidence/t009-pwa-shell-2026-10-09/summary.json), RTC/permission fixtures (NO hardware). Grant/deny/retry/absent/partial/request-error/no-requestPermission, noQR/Health; responsive320/390/768/1280, warning/focus/reduced-motion y diagnóstico sanitizado. Reproducible con `node tools/t009/browser-shell.mjs <directorio-nuevo>` tras build, Playwright/Chrome ya existentes o rutas GORILLA_PLAYWRIGHT/GORILLA_CHROME.
- RTC real PWA/Java/Unity regression: PASS,4 clientes; guardas/negativos/replay/HELLO/ACK preservados, STOPPED/exit0/cleanupComplete/Java propio residual0. Inputs sintéticos explícitos: NO frecuencia física ni nuevo gate T010. [Resumen sanitizado](evidence/t009-pwa-shell-2026-10-09/rtc-regression.json).
- iPhone15/Safari físico: Run A PASS (HTTPS, shell, warning, gesto y ambos permisos granted), [proyección sanitizada](evidence/t009-pwa-shell-2026-10-09/iphone-run-a.json). iOS27 reportado PO; build exacto/Safari UNKNOWN. Run B conecta y vuelve granted; PO confirma **no volvió a preguntar**. Denegación/recovery físicas NOT RUN: permiso recordado, sin reset artificial. [Run B](evidence/t009-pwa-shell-2026-10-09/iphone-run-b.json). No sensor stream/cadencia verificados.
- Android físico: NOT RUN, no dispositivo disponible; no sustituido con mocks/emulador. QA multiplataforma posterior T019 conserva obligación.
- Unity fuentes sin cambios: no rerun de suites editor/build costosas; regresión Player RTC requerida por cambio de ruta/packaging.

### Gate Safari

QR nuevo → HTTPS confiable → CONECTAR → ACTIVAR CONTROL con safety visible → permiso observable/diagnóstico sanitizado. Run A concedido. Run B deny sólo si iOS permite reproducción segura sin alterar artificialmente configuración; registrar limitación real de prompt recordado. No repetir CA, reset permisos, abrir UDP preventivo ni cambiar router/ICE.

### Gate físico ejecutado y limitaciones

Primer intento en8443 mostró Inear según PO aunque Java sirvió Gorilla con TLS verificado (HTTP200) y no recibió JOIN. Resultado: BLOCKED por contenido incorrecto; causa exacta UNKNOWN, sospecha de contenido previo/Service Worker, no inspección remota Safari. No se borraron datos ni se alteró otro proyecto. QA aislada en18443, puerto configurable existente, misma CA por instalación (sin reinstalación). Regla temporal TCP18443 **sólo iPhone actual/interfaz Wi-Fi/destino PC**, autorizada por PO; no UDP nuevo ni cambios ICE/router. La regla8443/subred ya existía y se preservó. [Cleanup firewall](evidence/t009-pwa-shell-2026-10-09/firewall-cleanup.json): única regla propia retirada, políticas/full status idénticos. [Cleanup procesos](evidence/t009-pwa-shell-2026-10-09/physical-cleanup.json): Unity exit0, Java exit0, STOPPED/cleanupComplete, residual0; QR efímeros eliminados.

Run A y B: NETWORK_READY/HTTP200/SDP PASS/ambos DataChannels OPEN/HELLO-ACK, permisos granted, signalVerified=false. 270/284ms son conexión interna únicamente; cronometraje humano NOT MEASURED. No se extrapolan a sensores/gameplay. Run B denegado no reproducible naturalmente al recordar Safari permiso; no settings/reset ni nuevo origen para forzar denial. AC2 se acredita por fixtures unit/browser y UX recuperable, no se presenta como deny físico. T019 conserva QA física multiplataforma/denegación/fallos/versiones y timings pendientes.

### Acceptance Criteria originales

| AC | Estado candidato | Evidencia |
|---|---|---|
| AC1 CONNECT y permiso accionado por usuario | PASS | Unit/browser y Run A Safari físico; sin petición automática |
| AC2 Negar permiso/no sensor explica y recupera | PASS | Unit/browser grant/deny/retry/absent/partial; deny físico NOT RUN por permiso recordado, documentado según condición PO |
| AC3 Warning visible | PASS | Browser responsive y confirmación PO Run A Safari |

PR de revisión normal hacia develop; sin merge automático ni cierre antes de DoD/integración. No T010. Deuda QA Android/versiones/dispositivos permanece explícita, sin ampliar DEC-018 para convertir NOT RUN en PASS.

## Enmienda e integración administrativa autorizada — 2026-10-09

PO completa DEC-019: **React + Vite PWA / Flutter + Dart app nativa futura**. Catálogo candidato y assets compartidos conceptuales registrados en DEC-019; ninguna dependencia instalada, estructura Shared/ControllerDesign creada ni Flutter implementado. Código/evidencia de este candidato sin cambios: PWA25/25, browser7, Java54 PASS/2 SKIP y Safari conservados, no ejecuciones nuevas por la enmienda documental.

PO acepta AC1/AC2/AC3 y fuente unit/browser para AC2, conservando denial/recovery físicos y Android físico NOT RUN. QA diferida T009→T019: Android PWA permisos; denegación/recuperación física donde sea reproducible; metadatos exactos OS/browser iPhone/Android; UX Gorilla Core entre dispositivos. No cambiar AC de T019 ni iniciarlo. **Frecuencia/Hz reales permanecen responsabilidad específica T010**, no se trasladan a T019 para eludir ese criterio.

Orden de integración autorizado: completar DEC-019 → cerrar F00 por evidencia previa integrada → revisar/checks/security del nuevo head de PR97 → merge normal → fetch/ff-only y validaciones post-merge → cerrar T009 completed/Done → retirar sólo blocked de T010 tras confirmar #16 y #20 completed. Los estados de candidato anteriores son el snapshot antes del merge; resultado efectivo y SHA se registran en PR97/Issues9,20,21/Project tras ejecutar el flujo, sin declarar una integración anticipadamente. No fuerza/bypass/push directo, no nuevo runtime, T010/T011 no iniciados, F02 OPEN y Fase1 NOT STARTED.
