# Progreso de desarrollo

## T003 — baseline técnico documentado, 2026-10-08

DEC-015 Accepted por autorización PO condicional, AC1/AC2 PASS: [baseline/comparativa/compatibilidad](PHASE0_T003_TECHNICAL_BASELINE.md). Configs/artifacts/modelo/nativos auditados; Java compile/package nuevo aislado PASS, sin reemplazar JAR histórico. Tests/build Unity y pruebas lab previas RETENIDAS con hashes idénticos, no ejecuciones nuevas. T001/T002 pendientes locales se publican junto con T003 vía rama docs/phase0-t003-baseline y PR normal hacia develop. No otros cierres/producto, no merge automático; E0/milestoneFase0 OPEN, Fase1 NOT STARTED. Los siguientes apartados describen snapshots anteriores cuando difieran de este estado.


> **Resultado remoto verificado T002:** #11 CLOSED/completed/Done; retirados sólo blocked y needs-device-test. #12 OPEN/PO Review, dependencia #11 satisfecha e intacta, blocked retirado; needs-decision conservado. No se inicia T003. T019 sin cambios, QA física pendiente. E0/milestone/Fase0 siguen OPEN; Fase1 NOT STARTED.


> **Enmienda vigente T002 — DEC-014 Accepted:** alcance documental por plataformas/capacidades, QA hardware no exclusivo; AC1/AC2 PASS. Android/router concretos no bloquean T002; físicos y versiones exactas permanecen en T019/gates correspondientes. Los faltantes anteriores son historia superseded. [Inventario vigente](PHASE0_T002_HARDWARE_INVENTORY.md). No iniciar T003 ni Fase1; sin nuevas pruebas/producto/commit/push.


## T002 — última reevaluación PO

PO proporciona iOS exacto27 y designa Android oficial, pero sus campos fabricante/modelo/OS/Chrome siguen `[PEGAR]`. AC1/AC2 PARTIAL por **único faltante: ficha real del Android físico**. Router/modelo/firmware y políticas LAN desconocidas se registran UNKNOWN, no blocker artificial. #11 OPEN/Validation, blocked/needs-device-test por Android; #12 sigue bloqueado, no iniciado. [Inventario vigente](PHASE0_T002_HARDWARE_INVENTORY.md). Sin pruebas nuevas ni commit/push.


## T002 — designaciones PO oficiales registradas

PC/Pop!_OS/webcam/iPhone15/Safari/familias de navegador/modelo Wi-Fi LAN aceptados por PO. [Inventario vigente](PHASE0_T002_HARDWARE_INVENTORY.md): AC1 PARTIAL/AC2 PARTIAL; faltan iOS exacto, Android físico oficial/OS/Chrome y router/firmware/restricciones físicas. #11 OPEN/Validation, blocked y needs-device-test por faltantes reales; #12 bloqueado por #11. Inspección sólo lectura (SMBIOS denegado, sin elevación); sin suites ni resultados históricos alterados. No commit/push ni T003/Fase1.


## Actualización T001/T002 — 2026-10-08

PO resuelve slow motion: P1, fuera MVP P0, no bloquea Smash; sin implementación autorizada. [DEC-013](DECISIONS.md#dec-013--slow-motion-p1-fuera-del-mvp-p0-t00110), T001 3/3 AC. Auditoría T002 sólo lectura: [inventario y faltantes](PHASE0_T002_HARDWARE_INVENTORY.md); #11 OPEN/Validation, hardware oficial y Android físico pendientes. Ninguna suite reejecutada ni resultado histórico alterado. Base integrada develop `65aecf8c3198ed960a43eaa50cfcc495f4562b53`; PR94 MERGED. Software/Lab COMPLETE; E0/milestone OPEN; #35 bloqueado por #34, Fase1 NOT STARTED. Siguiente Issue operativo autorizado: #11, sin empezar #12.


> **Fuente operativa vigente — 2026-10-08:** GitHub Project/Issues/dependencias/AC. Checkpoint técnico Fase0 **PASS — SOFTWARE/LAB**, conservado; **Project Fase0/E0/milestone siguen OPEN**. Fase1 **BLOCKED / NOT STARTED** por #35→#34; los estados READY del audit anterior no autorizan iniciar según Project. [Reconciliación vigente](PHASE0_PROJECT_RECONCILIATION.md).

## Estado vigente — FINAL PHASE0 AUDIT, 2026-10-08

**PHASE 0 FINAL AUDIT: PASS — SOFTWARE/LAB.** Fase0 — Base/Spike: **COMPLETED — SOFTWARE/LAB**. Fase1 — Gorilla Smash Vertical Slice: **READY TO START**, no iniciada. Physical validation debt remains open and is required before final MVP acceptance. 3A product/physical onboarding: **DEFERRED / IN PROGRESS**. #94 **OPEN/DRAFT**; candidato local sin commit distinto del PR remoto. Sin nuevo código de producto, commit/push/merge ni fase posterior. [Audit y DoD](PHASE0_FINAL_AUDIT.md), [evidencia](evidence/phase0-final-audit-2026-10-08/gate-summary.json). Los estados previos siguientes son snapshots históricos.

> **Lectura de estado (auditoría 2026-10-08):** las fechas, estados y próximos pasos de este documento son snapshots históricos del incremento descrito. El estado consolidado vigente se registra en [PHASE0_FINAL_AUDIT.md](PHASE0_FINAL_AUDIT.md). Las decisiones de transporte anteriores están **superseded / replaced by DEC-010 Mobile Transport**; se conservan resultados y limitaciones originales.

## Criterio vigente de cierre Software/Lab — aprobado por PO, 2026-10-07

Fase 0 puede cerrar como **COMPLETED — SOFTWARE/LAB** únicamente cuando arquitectura base implementada, integrada y todos los gates automatizados/laboratorio requeridos estén PASS. Esta autorización no equivale a declarar el cierre ahora. iPhone 15/Safari y Android físicos, cadencia móvil real, movimiento humano con teléfono/cámara y latencia física quedan **NOT RUN / DEFERRED**, obligatorios antes de aceptación final del MVP. Windows: DEFERRED. Los intentos fallidos de acceso iPhone y toda evidencia anterior se conservan; no se convierten en PASS. No se vuelve a solicitar teléfono para avanzar.

Orden obligatorio: A comparador WebRTC aislado → B DEC-010 y un transporte → C sesión/admission/QR POC → D protocolo común → E input móvil lab/Java/IPC/Unity → F webcam PC/pose/Unity → G asociación y temporal → H infraestructura Fusion/fixtures → I 1–4 clientes → J resiliencia/regresión → K auditoría → L cierre. No gameplay final, reconocimiento facial, cámara móvil, backend cloud ni Fase 1 antes del cierre. PR #94 permanece DRAFT. Sin push/merge autorizado para este trabajo. Secure-context/onboarding sin configuración del jugador sigue como riesgo independiente; resultados lab no demuestran compatibilidad física.

**Estado actual:** Fase0 IN PROGRESS; comparador CLOSED/PASS; DEC-010 WebRTC ACCEPTED/IMPLEMENTED FOR PHASE0 LAB; Session/Admission, QR POC, Gorilla Protocol, input laboratorio→Unity e integración1–4 clientes PASS. Camera/Input Fusion NOT STARTED; alineación/asociación/auditoría final pendientes. Android RTC nuevo BLOCKED; físicos/Windows DEFERRED. [Gate actual](PHASE0_MOBILE_INPUT_RUNTIME_VALIDATION.md). Foundation/IPC y resultados de laboratorio históricos conservados. Fase 1 NOT STARTED; sólo será READY TO START después de auditoría PASS e integración requerida.


Actualizado: 2026-10-06.

## Estado actual

Fase 0 existente en progreso; no se reinicia. **Estado vigente contrastado con GitHub y Git local el 2026-10-06:** develop = `ec5880c94304e8c7d587c5f5d8c2cf28dbf5a760`; main permanece en `e950bd820ea2dd6ede265943a3b7702f445be467` y no representa la integración actual. Unity 6000.3.23f1, Java 21/Spring Boot 4.1.1, React 19.3.0/Vite 8.3.2/Node 24 y contratos compartidos se conservan. No se crea rama en esta tarea ni se inicia Fase 1.

| Incremento existente | Estado vigente | Alcance/evidencia |
|---|---|---|
| Base / Unity Foundation | IMPLEMENTADO, VALIDADO Linux, MERGED #90 | Editor/tests/build/reimportación de desarrollo; PC oficial y Windows pendientes. |
| 2A | PASS / MERGED #91 | Child Java real, READY/PING/PONG, EOF y cleanup. |
| 2B | PASS / MERGED #92 | Lifecycle, fallos básicos, singleton, recuperación manual acotada. |
| 2C e Incremento 2 | PASS / MERGED #93 | Codec/corpus, deadlines/presión y tres corridas independientes, estabilidad 601.24 s. |
| 3A | IMPLEMENTADO PARCIAL / IN PROGRESS, #94 OPEN/DRAFT | Hosting/health/TLS/QA de componentes existentes; estrategia portable y gates físicos sin cerrar. |
| Investigación de transporte dentro de 3A | PROPUESTO / NOT RUN | WT/RTC, contexto seguro/preparación offline y sensores por comparar; ninguna pila seleccionada. |
| 3B / 4A / 4B / 4C | NOT STARTED | Nombres existentes se conservan; sus alcances se adaptarán al transporte decidido. |
| Webcam PC / Input Fusion / perfiles / gameplay | DISEÑO CONCEPTUAL; implementación NOT STARTED | No se adelantan por figurar en roadmap. |
| Fase 0 / Fase 1 | IN PROGRESS / NOT STARTED | Ninguna aceptación global ni inicio de fase nueva. |

Las secciones de cierre e intentos que siguen son **registros históricos fechados**. Estados como IPC BLOCKED/NOT STARTED o 2B/2C pendientes describen ese momento, no el estado vigente de esta tabla. La evidencia conserva sus resultados originales.

Estados vigentes: **2A/2B/2C e Incremento 2 PASS / MERGED en Linux de desarrollo; 3A IN PROGRESS (gate físico NOT RUN); 3B NOT STARTED; Incremento 3 IN PROGRESS; Fase 0 IN PROGRESS; Fase 1 NOT STARTED.**

## Incremento 2A — IPC mínimo

Implementado y validado en Linux de desarrollo: Unity inicia Java21/Spring, READY + TCP loopback PING/PONG, framing/token/identidades, deadlines/cancellation, stdin EOF y cierre observado sin Java propio residual. Build Player 0 errores/0 advertencias; Java8/8, EditMode7/7, PlayMode3/3, React regresión4/4 PASS. Smoke y límites en [TEST_REPORT](TEST_REPORT.md). Incremento 2A **PASS / MERGED** mediante [PR #91](https://github.com/Josue1855/Gorilla-Escape/pull/91), autorizado por el PO e integrado normalmente el 2026-10-06. develop `9fbb566e5f24af8f4e92fa3c139968161e832d46` contiene `a17dfb2968f79e35d479a6912e40d3b5d55538bd`; contenido funcional idéntico al validado. No declarar Done de hardware final. Fase0 IN PROGRESS; 2B/Fase1 NOT STARTED.

#90 integrado normalmente en `2a635b607a286bb70d1457aa0cf34f7e85f17abe`; foundation validada contenida y hashes confirmados. Único cambio de protección autorizado: approvals develop1→0; PR/checks/force-push prohibido conservados.

## Estado después del merge 2A

- Incremento 2A: **PASS / MERGED**.
- Incremento 2: **IN PROGRESS**.
- Incremento 2B: **NOT STARTED**.
- Fase 0: **IN PROGRESS**.
- Fase 1: **NOT STARTED**.

Resultados 2A conservados: Java8/8, EditMode7/7, PlayMode3/3 y React4/4 PASS; Player Linux build0errores/0warnings; READY/PING/PONG real PASS; listeners sólo127.0.0.1; Unity exit0, Java exit0, procesos Java propios residuales0. Player smoke N100: startup2.35s, RTT mínimo0.59ms, P500.80ms, P951.44ms, máximo1.89ms, errores0. Estas métricas IPC no se extrapolan a teléfono→gameplay. Checks posteriores al merge **PASS**: management-validation y java-react-foundation; 100/100 hashes del inventario 2A coinciden con develop. Protecciones develop verificadas: PR y ambos checks obligatorios, force push deshabilitado; ningún bypass.

## Incremento 2B — lifecycle y fallos básicos

Diseño [Accepted por el PO](PHASE0_IPC_2B_DESIGN.md), con precisiones explícitas de heartbeat, cleanup/generaciones, singleton y timeouts. Implementación exclusivamente 2B en `codex/phase0-ipc-2b-lifecycle`, desde develop `9fbb566e5f24af8f4e92fa3c139968161e832d46`. Estados explícitos, cero retries/restarts automáticos, recuperación manual hasta tres lanzamientos, FileLock Java por usuario/producto, EOF temprano y cleanup observado. Contrato/codecs/timeouts 2A conservados. Validación final **PASS Linux de desarrollo**: Java 13/13, EditMode 15/15, PlayMode 10/10, React 4/4, build Player 0 errores/0 warnings y 11 grupos Player reales/fixtures identificados, residual propio 0. Evidencia y límites en TEST_REPORT. 2A PASS/MERGED; Incremento 2/Fase 0 IN PROGRESS; 2C/Fase 1 NOT STARTED. 2B **PASS / MERGED** mediante [PR #92](https://github.com/Josue1855/Gorilla-Escape/pull/92), merge normal autorizado por el PO; develop `7e3aa60aa53c65e7051f32477a77985fbe628907` contiene `88f1e309fc7adeffe6aa6499e195f53185ab36f9`. Árbol completo idéntico al candidato y 105/105 hashes validados. Protecciones verificadas intactas. 2A PASS/MERGED; Incremento 2/Fase 0 IN PROGRESS; 2C/Fase 1 NOT STARTED. No se inicia trabajo posterior.

## Histórico de preparación del Incremento 2 — Unity ↔ Java

Diseño aceptado con precisión de alcance: esperar merge #90 antes de implementar sólo 2A. [Documento de decisión](PHASE0_UNITY_JAVA_DECISION.md) y DEC-005 **Accepted** cubren ownership, cuatro mecanismos IPC, lifecycle, contrato mínimo, seguridad, fallos, pruebas y presupuesto de latencia. Recomendación: Unity autoritativo del juego y supervisor; Java host/transporte; TCP loopback con PING/PONG y pipes sólo para lifecycle. Aprobación explícita del PO registrada; v4 §5 actualizada. Implementación exclusivamente 2A autorizada después del merge normal de #90; sin modificar ese PR.

Verificación remota 2026-10-06: develop continúa en `e950bd820ea2dd6ede265943a3b7702f445be467`; [PR #90](https://github.com/Josue1855/Gorilla-Escape/pull/90) aparece OPEN/mergedAt=null y su commit validado es `38e1e31ce0e36952b759061eee40136f7a0ef3f6`. El anuncio de integración no coincide con GitHub observado. Se conserva la validación de foundation de desarrollo del PR, sin fingir integración en develop ni reabrir bloqueos históricos. Revalidar base antes de implementar.

Estado: diseño aceptado; implementación 2A **BLOCKED** por integración #90, IPC **NOT STARTED**; mediciones IPC **NOT RUN**; PC de presentación **BLOCKED / NOT RUN**; Fase 0 **IN PROGRESS**; Fase 1 **NOT STARTED**. No se amplió PWA móvil, QR, WebSocket de teléfonos, sensores, MediaPipe/OpenCV ni gameplay.

Integración confirmada: #90 MERGED, develop `2a635b607a286bb70d1457aa0cf34f7e85f17abe`; commit validado contenido como ancestro y 76/76 hashes funcionales idénticos. Rama 2A creada desde ese SHA. El bloqueo descrito arriba es histórico y está resuelto.

## Implementado

- Servidor local con health endpoint y empaquetado de la PWA en el JAR.
- Shell React, errores recuperables y caché offline del shell.
- Contratos y fixture v1; metadatos Unity, configuración y pruebas preparadas.
- Guías de arquitectura, arranque, contribución y reglas de desarrollo.
- Formularios, workflows de validación y scripts de gestión del backlog.

## Validación

Como checks de regresión del cierre, Java 21/Maven verify y React test/build PASS (4/4 tests por componente), sin cambios de sus fuentes. UNITY-001 valida Unity Foundation en Linux de desarrollo: import limpio, resolución de cinco paquetes, escena/settings, EditMode 2/2, PlayMode 1/1, build Mono, Player con gráficos, cierre normal y clean re-import. Evidencia y advertencias en [TEST_REPORT](TEST_REPORT.md) y [UNITY_FOUNDATION_VALIDATION](UNITY_FOUNDATION_VALIDATION.md).

El batch licenciado funciona dentro del entorno Flatpak del Hub; los intentos directos anteriores con salida 198 siguen registrados como BLOCKED históricos. DEC-003 acepta el core Test Framework 1.6.0 conservando Unity 6000.3.23f1. SDK/toolchain Linux retirados mediante UPM; validación local usa las opciones de este editor que evitan su instalación/migración automática. No se incorporan como requisito permanente del equipo.

Escena, ApplicationSettings, lockfile, .meta y settings compartidos revisados forman parte del diff candidato. Defaults de física/calidad/tiempo no son tuning final. La PC de presentación permanece sin definir y NOT RUN. Unity Foundation / Development **PASS**, integrada mediante merge normal #90; presentación pendiente. Presentation PC **BLOCKED / NOT RUN**, rendimiento físico **NOT RUN**. Fase 0 **IN PROGRESS**, Incremento 2/Fase 1 **NOT STARTED**. Histórico 1A y auditoría conservados en [PHASE0_UNITY_FOUNDATION](PHASE0_UNITY_FOUNDATION.md).

## Requisitos y decisiones

[Especificación v4](Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md) como fuente principal; DEC-002 autoriza Java + React. La PC conserva autoridad, procesamiento local y funcionamiento sin Internet durante gameplay. No se requiere soltar el teléfono.

## Pendientes de Fase 0

- Definir PC de presentación y repetir aceptación de foundation en ese hardware; desarrollo Linux PASS.
- Resolver contexto seguro portable PWA, concesión inicial online, permisos/sensores físicos y experiencia de incorporación; no exigir dominio propio, CA móvil, router o DNS manual.
- Decidir y validar transporte móvil WT/RTC; IPC probe Unity/Java ya PASS/MERGED. El futuro contrato/adaptador de movimiento hacia Unity y MediaPipe/OpenCV no están implementados.
- Medir en hardware real antes de aceptar métricas.
- Resolver discrepancia de alcance entre secciones 42 y 50, autoridad de resultados y detalles del contrato.
- Revisar calendario con Product Owner; no cambiar fechas originales sin decisión.

## Publicación del repositorio

Base publicada con historial inicial limpio y ramas main/develop. El backlog y los vínculos del Project se reconstruyen desde el manifiesto versionado; resultados de gestión en docs/github/verification.json. Los PR anteriores no forman parte de esta publicación.

## Próximo paso

Próximo alcance propuesto dentro de 3A: Spike mínimo de interoperabilidad WT con Java 21, pin positivo/negativo, control fiable y datagrama; contraste RTC aislado sin STUN/TURN público. Primero confirmar dependencia/release/licencia, teléfonos y preparación segura. No ejecutarlo en esta tarea: publicación/certificado efímero/infraestructura y concesión online no están autorizados. [Gate mínimo y secuencia](PHASE0_SECURE_MOTION_SPIKE_PLAN.md#10-próximo-spike-mínimo-dentro-de-3a-sin-reiniciar-fase-0). Windows/PC oficial siguen pendientes; no ampliar a Flutter, webcam o gameplay.

Checks posteriores del merge 2B en `7e3aa60aa53c65e7051f32477a77985fbe628907`: **management-validation PASS / java-react-foundation PASS**. Protecciones comparadas antes/después, idénticas. Cierre de integración; 2C no iniciado.

## Incremento 2C — hardening y validación sostenida

Diseño [Accepted con precisiones del PO](PHASE0_IPC_2C_DESIGN.md). Fetch confirmó base develop `7e3aa60aa53c65e7051f32477a77985fbe628907`; rama `codex/phase0-ipc-2c-hardening` creada desde ese SHA, conservando documentación de merge 2B. Parser limitado al probe sin dependencia nueva, corpus compartido, framing/contrato estricto, deadline absoluto y writer acotado. Validación final **PASS / MERGED Linux de desarrollo**: Java22/22, EditMode19/19, PlayMode12/12, React4/4, build0errors/0warnings, corpus71 casos cruzados, cinco grupos de presión, tres Players independientes N1000 a50Hz y heartbeat real601.24s; errores/timeouts nominales0, STOPPED/exit0/cleanup y residual propio0. Regresión Player2A/2B11 grupos PASS. Gate, métricas individuales y límites en [TEST_REPORT](TEST_REPORT.md) y [procedimiento](PHASE0_IPC_2C_VALIDATION.md). Integrado mediante merge normal #93; Incremento 2 PASS / MERGED; Fase 0 IN PROGRESS; Fase 1 NOT STARTED.

## Integración normal del Incremento 2C

[PR #93](https://github.com/Josue1855/Gorilla-Escape/pull/93) integrado mediante merge normal autorizado por el PO el 2026-10-07 01:03:09 UTC (2026-10-06 local). develop `ec5880c94304e8c7d587c5f5d8c2cf28dbf5a760` contiene el candidato validado `e957775b0a848a001d37e1a4770eb14103888d0c`; árbol completo idéntico y 147/147 hashes funcionales/test/harness coincidentes. Checks posteriores **Management validation PASS / Foundation validation PASS**. Protección efectiva comparada antes/después, sin cambios: PR obligatorio, checks estrictos `management-validation` y `java-react-foundation`, force push y eliminación deshabilitados, reglas aplicables a administradores y conversaciones resueltas obligatorias. Sin bypass ni cambios al candidato.

**Estado vigente:** 2A PASS / MERGED; 2B PASS / MERGED; 2C PASS / MERGED; **Incremento 2 — Unity ↔ Java IPC PASS / MERGED**; Fase 0 IN PROGRESS; Fase 1 NOT STARTED. Evidencia exacta de 2C conservada: Java22/22, EditMode19/19, PlayMode12/12, React4/4, build0errores/0warnings, corpus71casos, regresión Player11grupos, residual Java propio0, tres corridas independientes N1000 y estabilidad601.24s PASS; errores/timeouts/desconexiones nominales0. No combinar ni reinterpretar corridas ni extrapolar a teléfono → gameplay. Trabajo detenido tras integración.

## Diseño del Incremento 3 — LAN, HTTPS y onboarding

Preparación original del [diseño posteriormente Accepted](PHASE0_PHONE_LAN_ONBOARDING_DESIGN.md), preparado únicamente sobre develop `ec5880c94304e8c7d587c5f5d8c2cf28dbf5a760` confirmado mediante fetch. Propone gates 3A LAN/HTTPS y 3B QR/sesión técnica, CA local previamente confiada en teléfonos físicos, assets React desde Java y pantalla QR React en PC. Sin rama, código, dependencias instaladas, certificados generados ni firewall modificado. Pruebas físicas y métricas de Incremento 3 NOT RUN; diseño pendiente de aprobación del PO. Estados de implementación conservados: Unity Foundation/Incremento 2 PASS / MERGED; Fase 0 IN PROGRESS; Fase 1 NOT STARTED. No se inicia Incremento 4.

## Incremento 3A — implementación LAN/HTTPS autorizada

Diseño general [Accepted](PHASE0_PHONE_LAN_ONBOARDING_DESIGN.md) por el PO; sólo3A
implementado sobre base confirmada `ec5880c94304e8c7d587c5f5d8c2cf28dbf5a760`, rama
`feature/mobile-phase0-lan-https-3a`. Configuración externa opt-in IPv4/interfaz/TLS,
health puntual y React servido desde JAR; Unity sigue supervisor y el IPC es loopback.
Sin nuevas dependencias ni código Unity/Shared; sin QR/onboarding/session/WS/sensores.

Automatización: Java26/26, React5/5/builds, TLS confiable/CA desconocida/SAN incorrecto,
Player propio HTTPS/IPC/EOF exit0 y regresión11grupos PASS, residualJava0. Guía y evidencia
en [TEST_REPORT LAN-003A](TEST_REPORT.md) y [validación física](PHASE0_PHONE_3A_VALIDATION.md).

**Estado vigente:** Incremento1 PASS/MERGED; Incremento2 PASS/MERGED; **3A IN PROGRESS**
por gate físico **NOT RUN** (Android/Chrome, iPhone/Safari, primera carga sinWAN, segundo
cliente LAN). Incremento3 IN PROGRESS; 3B NOT STARTED; Fase0 IN PROGRESS; Fase1 NOT STARTED.
Borrador de PR, sin merge automático. Se requiere participación del operador para setup
CA pública/red y pruebas físicas; modelos/versiones/router aún sin confirmar. Ningún
firewall/trust/router cambiado ni CA privada de demo generada. No iniciar3B automáticamente.

## Rediseño Incremento 3 — requisito dual-cliente

[DEC-006](DECISIONS.md) acepta PWA universal y Flutter Android opcional/preferido, con sesión/IDs/protocolo WSS/backend comunes y autoridad Unity. [Rediseño técnico Proposed](PHASE0_PHONE_LAN_ONBOARDING_REDESIGN.md) reemplaza CA móvil por propuesta de certificado público/FQDN y resolución LAN preparada sin ajustes del jugador. No código Flutter/Java/React/Unity modificado. #94 sigue DRAFT;3A IN PROGRESS, nuevo gate NOT RUN;3B/4A/4B/4C NOT STARTED. Incremento2 PASS/MERGED; Fase0 IN PROGRESS/Fase1 NOT STARTED. Pendiente aprobar dominio/red/HTTPS/onboarding; no continuar automáticamente.

## Evaluación operativa3A — sin cambios runtime

Arquitecturaobjetivo aprobada conceptualmente por PO. [Viabilidad Proposed](PHASE0_PHONE_3A_FEASIBILITY.md): routerdedicadoDHCP/DNS, dominio/certificado públicoDNS-01 previo y DNATlocal443→JavaHTTPS8443 para conservarchild/EOF sinprivilegiosJava. Dominio/router/teléfonos de referencia aún no acreditados; nuevos gates físicosNOT RUN. #94OPEN/DRAFT verificado;3AIN PROGRESS;3B/4A/4B/4C NOT STARTED;Fase0IN PROGRESS/Fase1NOT STARTED. Sólo lectura/documentación; ninguna compra/emisiónTLS/red/runtime modificada. Evidencia previa conservada, no descartada ni reinterpretada como nuevoPASS.

## 3A — Software FQDN / HTTPS443 aprobado y adaptado

Supersede los estados Proposed de los registros históricos anteriores: arquitectura técnica Accepted, autorización exclusivamente de adaptación software en #94, head previo confirmado `50e48418ff286dcd3096c92800fd47f602ad27f2`. SAN DNS exacto/cadena vigente PKIX, IPv4 de escucha independiente, origen público canónico443 y Java8443 default; archivos externos privados y validación sin secretos. Hosting/diagnóstico React, supervisor Unity/READY/IPC/EOF/singleton/límites de recuperación reutilizados, sin cambios Unity/Shared/PWA ni nuevas dependencias.

[Reporte](TEST_REPORT.md): Java30/30, React5/5, JAR/PWA empaquetada, Player HTTPS real/cleanup y regresión11 grupos PASS; Java propio residual0. TLS portable6PASS/2SKIP cubiertos por LAN explícita. Evidencia anterior conservada; intentos fallidos/incompletos nuevos documentados. [Procedimiento443](PHASE0_PHONE_3A_LINUX_443.md) preparado pero no ejecutado. No compras, certificado público emitido ni modificación router/firewall/DNS/trust.

Software validado **no cierra3A**. Dominio/accesoDNS/router/certificado público y autorización operativa pendientes; Android/iPhone físicos aún sin modelos/versiones confirmados. HTTPS443/DNS router/primera carga sinWAN/segundo dispositivoIPC NOT RUN. Certificado efímero y ruta directa de test no sustituyen confianza pública/resolución física.

**Estados:** #94 DRAFT;3A IN PROGRESS;3B/4A/4B/4C NOT STARTED; Incremento2 PASS/MERGED; Fase0 IN PROGRESS; Fase1 NOT STARTED. Publicar sólo esta adaptación y detenerse; no preparar infraestructura automáticamente.

## Preflight operativo 3A — sólo lectura y documentación

[Preflight](PHASE0_PHONE_3A_PREFLIGHT.md) sobre #94 OPEN/DRAFT head `034ab44aa5b27c1812fc1b5f9f3cc8f7593debf1`, checks SUCCESS. Pop!_OS24.04, Wi-Fi IPv4 `10.1.125.17/22`, gateway `10.1.124.1`, NetworkManager/resolved activos, UFW habilitado. DNS anunciado fuera de subred no acredita resolución offline. Sin listeners443/8443 ni proceso Java en snapshot; no se inició ningún servicio. Lectura de reglas nft/UFW impedida sin privilegios autorizados, pendiente resumen administrativo sanitizado. Router/dominio/teléfonos sin confirmar; reutilizar router actual si demuestra funciones antes de recomendar compra.

Sólo reporte/plan/matriz física/rollback y bloqueos; sin pruebas de aplicación nuevas, compras, certificados emitidos, cambios de red/firewall/servicios ni runtime. Evidencia previa intacta. #94 DRAFT;3A IN PROGRESS;3B/4A/4B/4C NOT STARTED; Fase0 IN PROGRESS; Fase1 NOT STARTED. No ejecutar automáticamente el procedimiento.

## 3A — QA Android automatizable disponible

[Android Emulator local](PHASE0_PHONE_3A_ANDROID_EMULATOR.md): herramienta reproducible por un comando, SDK/KVM existentes, sin instalación ni cambios Unity/Java/React. Chrome Android real emulado + Java real: 10 PASS, 2 BLOCKED físicos/públicos, 1 SKIP fuera de alcance; cleanup exit0/residuales propios0. Evidencia y fallos previos del harness separados de resultados históricos en [TEST_REPORT](TEST_REPORT.md).

Sólo QA local: no sustituye teléfonos/router/DNS/certificado público; siete datos pendientes. #94 permanece DRAFT con head intacto; 3A IN PROGRESS; 3B/4A/4B/4C NOT STARTED; Fase0 IN PROGRESS; Fase1 NOT STARTED.

## 3A — Evidencia de apoyo y preparación física

Se incorpora al candidato #94 la guía/harness/evidencia sanitizada Android, conservando 10 PASS / 2 BLOCKED / 1 SKIP. [Plan físico y matriz de router existente](PHASE0_PHONE_3A_PHYSICAL_PLAN.md): siete datos siguen sin confirmar suficientemente; PC/ruta Wi-Fi comprobadas sólo lectura, no se inventan router/firmware/dominio/permisos. Pruebas Android/Chrome e iPhone/Safari con primera visita sin WAN preparadas, NOT RUN. Sin runtime o infraestructura modificados; #94 DRAFT y 3A IN PROGRESS, estados posteriores intactos.

## Rediseño portable solicitado — sólo propuesta

Nuevo requerimiento del PO retira dominio/DNS personalizado/TLS público/443 como obligaciones base. [Rediseño portable](PHASE0_PORTABLE_LAN_REDESIGN.md) Proposed para revisión: web táctilHTTP LAN, hotspot condicionado por equipo, Flutter opcional con pinTLS futuro y contrato/sesión comunes. Sin implementación ni cambios de infraestructura; #94 OPEN/DRAFT head `0d940f77f533bb6780bc3f07169dce414ea21e54`. Evidencia histórica intacta, emulador10PASS/2BLOCKED/1SKIP; 3A IN PROGRESS, físicosNOT RUN, posterioresNOT STARTED. Gates/norma anteriores requieren reconciliación al aprobar diseño; no declarar nuevoPASS.

## Corrección de producto — web/Flutter con movimiento e Input Fusion

El PO rechaza HTTP LAN táctil como arquitectura final. [Decisión de PWA segura y transporte local](PHASE0_SECURE_MOTION_TRANSPORT_DECISION.md) Proposed: HTTPS estático gratuito con preparación online inicial y RTCDataChannel local; comparar WebTransport/pin en Safari real antes de congelar transporte. Concesión de primera carga online pendiente de aprobación, no sensores web garantizados ni peer Java disponible. Paridad semántica/perfiles/InputFusion Unity diseñados, sin implementación. Evidencia intacta; #94 OPEN/DRAFT mismo head; 3A IN PROGRESS, físicos NOT RUN, posteriores NOT STARTED.


## Investigación secure motion — base de Spike, 2026-10-06

DEC-007 registra corrección de producto y aceptación exclusivamente de la base de investigación. Documentados comparación WT/RTC, candidatos Java/licencias, pruebas/metrics/gates, sensores físicos, contrato conceptual, cámara PC, sincronización y boliche zurdo. Transporte definitivo y ejecución pendientes; sin prototipos, cambios runtime, despliegue, certificados ni configuración de red. Concesión primera preparación online pendiente de PO.

#94 OPEN/DRAFT, head conocido `0d940f77f533bb6780bc3f07169dce414ea21e54`; 3A IN PROGRESS; 3B/4A/4B/4C NOT STARTED; Fase 0 IN PROGRESS; Fase 1 NOT STARTED. Se preservan emulador 10 PASS, 2 BLOCKED, 1 SKIP y toda evidencia previa; físicos de esta arquitectura NOT RUN. [Plan](PHASE0_SECURE_MOTION_SPIKE_PLAN.md), [fusión conceptual](PHASE0_INPUT_FUSION_CONCEPT.md).


## Auditoría de continuidad — 2026-10-06

Inspección de sólo lectura: #90–#93 MERGED y sus cuatro heads validados ancestros de develop; #94 OPEN/DRAFT, base develop, head `0d940f77f533bb6780bc3f07169dce414ea21e54`, cinco commits, ambos checks SUCCESS. Checkout actual `feature/mobile-phase0-lan-https-3a` en ese mismo head. No fetch/pull/checkout/merge/push ni modificación remota necesaria: SHA remoto develop coincide con referencia local inspeccionada.

Inventarios históricos comparados con **sus candidatos respectivos**: foundation 76/76, 2A 100/100, 2B 105/105, 2C 147/147 y 3A FQDN 129/129 coinciden. Además, inventario 2C 147/147 coincide con develop integrado. No exigir que versiones antiguas de 2A/2B sean idénticas al hardening posterior ni reinterpretar sus tests. Cinco artifacts sanitizados Android y harness de HEAD coinciden con manifest; resultados **10 PASS / 2 BLOCKED / 1 SKIP** intactos. No se ejecutaron suites ni Spikes físicos.

Cambios locales preexistentes sin commit conservados: DECISIONS, DEVELOPMENT_PROGRESS, DEVELOPMENT_RULES, especificación v4, TEST_REPORT; documentos nuevos locales portable redesign, secure motion decision, Spike plan e Input Fusion. No forman parte del head remoto #94 ni de sus checks. Esta iteración actualiza sólo progress, decisión y plan existentes; sin nuevos documentos, runtime o dependencias. Gstack no localizado en las ubicaciones de skills disponibles; no se instala ni se atribuye uso ficticio.

Roadmap vigente de la **misma Fase 0**: conservar foundation/2A/2B/2C cerrados → terminar gates de 3A (contexto seguro + investigación sensores/transporte) → 3B QR/admisión/sesión → 4A protocolo común del transporte elegido → 4B control web → 4C Flutter opcional → bloques PC webcam/fusión/calibración → gate de Fase 0. Cada bloque requiere autorización propia; minijuegos/física/UI final no se inician ni se trasladan automáticamente a Fase 0. Alcances y dependencias detallados en el plan existente.

Validación de esta auditoría documental: management PASS (88 registros, 245 enlaces locales), git diff --check PASS. Sólo lectura de fuentes/evidencias y consultas GitHub; sin repetir Java/React/Unity/Player/emulador ni ejecutar nuevos Spikes. Checks remotos corresponden únicamente al head publicado de #94, no a estos cambios locales.


## Calibración inmersiva y justicia competitiva — requisito DEC-008

Accepted como requisito de producto; diseño de Input Fusion extendido con práctica jugable, normalización por rango cómodo, cuatro clases de perfil, límites competitivos Unity y snapshots fijos por ronda. Ajustes/recalibración explícitos entre rondas; ausencia de fuente y perfiles débiles tratados sin exigir fuerza máxima ni cámara móvil. Ejemplos deportivos ilustrativos, sin ampliar MVP ni iniciar gameplay.

Roadmap existente: tras sensores/transporte y fuentes/relojes fiables, preparar contrato/perfiles/snapshots y pruebas de normalización; desafíos finales y estudio humano sólo en bloque de gameplay autorizado. No se modifica próximo Spike de transporte ni se reabren foundation/2A/2B/2C. [Contratos, seguridad, privacidad y criterios físicos futuros](PHASE0_INPUT_FUSION_CONCEPT.md#8-calibración-inmersiva-y-justicia-competitiva--dec-008). Targets numéricos Proposed, equilibrio físico NOT RUN; no PASS nuevo.

#94 verificado OPEN/DRAFT, mismo head `0d940f77f533bb6780bc3f07169dce414ea21e54`; 3A/Fase 0 IN PROGRESS, posteriores/Fase 1 NOT STARTED. Sólo documentos locales, conservando cambios preexistentes y evidencia histórica. Sin runtime, tests costosos, compras, instalaciones, red, certificados, commit/push/merge.


## Reconocimiento de intención — requisito DEC-009

Actualización documental: patrón temporal y máquina de estados por jugador, umbrales personalizados vinculados a calibración inmersiva, intención separada de intensidad, acción única y recovery/rearme. Degradación sensor-only ante visión no fiable, asociación multijugador explícita y candidatos/decisiones internos Unity. [Diseño y aceptación futura](PHASE0_INPUT_FUSION_CONCEPT.md#9-reconocimiento-de-intención-y-prevención-de-activaciones-falsas--dec-009).

Dependencias futuras dentro del roadmap existente: fuentes/relojes/identidades y perfiles válidos → patrones/contexto → corpus de negativos/positivos → estudio físico por dispositivo/modalidad → integración en deporte autorizado. No implementar detector/cámara/minijuegos ni fijar thresholds o precisión sin medidas; no reabrir incrementos cerrados. WT/RTC permanece pendiente, próximo Spike de transporte intacto. #94 comprobado OPEN/DRAFT mismo head `0d940f77f533bb6780bc3f07169dce414ea21e54`; 3A/Fase 0 IN PROGRESS, Fase 1 NOT STARTED. Evidencia y cambios locales previos conservados.


## Spike de sensores PWA — captura aislada, 2026-10-07

IMPLEMENTADO únicamente en [PWA/spikes/sensors](../PWA/spikes/sensors/README.md), sin modificar shell productivo/Java/Unity/IPC/Shared/dependencias. DeviceMotion/Orientation, permisos, valores/timestamps/cadencia, diagnóstico y cleanup; no juego, recognizer ni potencia/calibración definitiva. VALIDADO unitario: 8/8; regresión PWA: 5/5. APIs/eventos simulados; físico Android/iPhone, datos reales y confianza HTTPS NOT RUN, ningún gate 3A completo.

DEC-010 corrige diseño de práctica/intención para las cinco disciplinas de v4 §§17–21 y conserva cantidades: Smash/Throw 3 intentos, Archery 5 flechas, Bowling 3 rondas, Slice 60 s por jugador. Banana Catch sólo propuesta post-MVP, v4 y torneo sin cambios. Evidencia integrada de foundation/IPC y Android emulador 10 PASS/2 BLOCKED/1 SKIP intacta. WT/RTC independiente pendiente.

Git inspeccionado antes de código: rama `feature/mobile-phase0-lan-https-3a`, HEAD `0d940f77f533bb6780bc3f07169dce414ea21e54`, develop `ec5880c94304e8c7d587c5f5d8c2cf28dbf5a760`; docs modificados/nuevos locales preexistentes conservados. #94 OPEN/DRAFT en ese head, base develop, checks remotos SUCCESS para candidato anterior, no para este Spike local. Sin nueva rama/commit/push/merge ni infraestructura.

Estado vigente: foundation/2A/2B/2C PASS/MERGED de desarrollo; 3A/Fase 0 IN PROGRESS, posteriores/Fase 1 NOT STARTED. Siguiente paso recomendado: confirmar contexto seguro autorizado y teléfonos físicos, medir adquisición por dispositivo según plan antes de elegir transporte/thresholds. No avanzar automáticamente. Gstack continúa no disponible en las ubicaciones inspeccionadas previamente; no se instala ni se atribuye uso.


## Spike ejecutable 3A-T — 2026-10-07

IMPLEMENTADO aislado en tools/spikes/webtransport. VALIDADO laboratorio Chrome desktop ↔ Java 21: seis escenarios PASS por ruta loopback y LAN-IP propia; pin correcto/incorrecto/vencido, AUTH negativa, tamaño, inválidos, reliable/datagramas, recuperación manual y cleanup. Cero child Java/grupo navegador propio residual; UDP liberado; métricas individuales [registradas](PHASE0_SECURE_MOTION_TRANSPORT_DECISION.md#15-spike-ejecutable-3a-t--2026-10-07). No pruebas físicas ni offline, no selección final WT. Cambios locales preexistentes conservados. Sin commit/push ni modificación de #94 OPEN/DRAFT. 3A/Fase 0 IN PROGRESS; 3B/4A/4B/4C y Fase 1 NOT STARTED. Histórico foundation/IPC/emulador/sensores intacto. Próximo Spike propuesto físico WT; no ejecutarlo automáticamente.


## Validación Android Emulator del estado disponible — 2026-10-07

Nuevo harness QA aislado android.py: final 16 PASS para React/Java/WT y página sensores existente, sin nuevas funciones de producto. Se encontró/corrigió exclusivamente defecto de invocación Window de timers en captura aislada; unit 9/9 y Android RUNNING/eventos virtuales/STOPPED PASS. Corrida fallida estricta preservada (13 PASS/1 FAIL); no altera evidencias históricas. [Detalle y límites](PHASE0_SECURE_MOTION_TRANSPORT_DECISION.md#16-android-emulator--validación-integrada-de-componentes-2026-10-07). Sensor→WT→Unity NO implementado, físico/offline NOT RUN. #94 OPEN/DRAFT mismo head; 3A/Fase0 IN PROGRESS, posteriores/Fase1 NOT STARTED. Sin commit/push/merge.

### Incremento aislado A/B y base C — 2026-10-07

Comparador RTC Chromium ↔ Java21 real: **PASS**, `webrtc-java 0.19.0`/JNI Linux fijados sólo en Spike. Corrida final `run04-reviewed.json`: 2400/2400 mensajes medidos, 0 pérdidas/timeouts/errores nominales; 1 peer, reconexión nueva y 4 peers activos. Padding32/1024, reliable ordered/unordered maxRetransmits0. Candidatos host UDP, sin iceServers/STUN/TURN. Negativos7 PASS: SDP inválido, oferta con ID duplicado aislada, payload inválido, quinto peer rechazado, desconexión aislada, credential incorrecta, oversized. Revisión detectó que rechazar una oferta con ID repetido podía cerrar el peer existente: corrección limitada al peer asignado por ese request y prueba de regresión real añadida; corridas anteriores conservadas.

Java startup observado264.52ms (un lanzamiento, no P95). RTT por escenario, sin combinar muestras: single N400, mayor P95 de sus subgrupos0.60ms/max1.50ms; recovery N400, mayor P950.90ms/max1.70ms; four N1600, mayor P951.00ms/max2.00ms. Subgrupos/tamaños/canales y métricas completas permanecen separados en JSON; esos máximos de percentiles no son un percentil combinado. No se comparan como benchmark equivalente con WT ni se extrapolan a teléfono/gameplay. Java exit0, peers residuales0, comandos pendientes0, navegador cerrado.

DEC-010 selecciona **WebRTC DataChannel** como dirección Software/Lab; no transporte productivo integrado aún. WT histórico permanece aislado. Sesión técnica Java: siete unit tests PASS para token one-use, cuatro players, expiración, liveness, desconexión/identity y reconnect con epoch. Regresión Java37 total:35 PASS/2 SKIP de variantes LAN,0 fallos/errores; no se cambian resultados históricos. Foundation wiring PASS; diff check PASS. No QR/endpoint de admisión implementado todavía; C PARTIAL, pipeline móvil/Unity, webcam/Fusion, multicliente productivo y auditoría final pendientes.

Evidencia: `docs/evidence/webrtc-comparator-2026-10-07/`; comando `tools/spikes/webrtc/run.mjs`; restricciones/dependencias `tools/spikes/webrtc/README.md`. #94 verificado OPEN/DRAFT, head0d940f77; sin commit/push/merge. Fase0 IN PROGRESS y Fase1 NOT STARTED. Físicos DEFERRED por decisión PO; ya no bloquean gates Software/Lab.

**Actualización del mismo incremento:** POC QR implementado con ZXing core3.5.3/Apache-2.0 (única nueva dependencia del servidor para QR), QR384×384 codificado y decodificado realmente en test; URL HTTPS con admission efímero en fragment, sin token IPC/clave/credencial permanente. Dominios `.invalid` son fixtures, no hosting desplegado ni concesión de infraestructura. Rechaza HTTP, userinfo, query/fragment previos; token consumido sólo una vez. Dos tests QR PASS. Credential de resume rota al reconectar y epoch anterior queda inválido; siete tests sesión PASS. Regresión final con interfaz LAN explícita: **Java39/39 PASS,0 SKIP,0 errores/fallos** (`technical-session-qr-java-final.json`). La corrida previa35PASS/2SKIP sigue conservada separadamente. C permanece PARTIAL: faltan endpoints/integración de admission/liveness con el transporte real/PWA; QR POC componente PASS. No pipeline móvil→Unity, webcam, asociación/Fusion ni auditoría final aún. No se declara Fase0 COMPLETED ni Fase1 READY.

## Sesión / QR / protocolo / WebRTC / PhoneInput — cierre Software/Lab, 2026-10-07

**PASS local**, con Chrome real sintético/replay, cuatro jugadores/peers independientes, el mismo IPC en 127.0.0.1 y Player Unity real. Java 48/48, Node 18/18, EditMode 23/23, PlayMode 12/12, build Linux con 0 errores y 0 warnings; Player 2A/2B: 11 grupos PASS. Regresión 2C: tres corridas independientes N=1000 y estabilidad de 601.22 s PASS. Se conservan 126 hashes de fuentes, hashes de artefactos, 18 casos negativos/lifecycle, cero cruces de identidad, recuperación manual, EOF, STOPPED, exit 0 y cero procesos propios residuales.

La variante nueva del emulador queda BLOCKED al abrir los canales RTC; contexto seguro, APIs y QR presentes, cleanup sin residuales. El histórico 16/16 no cambia. Intentos fallidos y primer cierre 2C forzado conservados; repetición con artefacto fijo PASS, sin establecer una causa definitiva. [Informe y deuda](PHASE0_MOBILE_INPUT_RUNTIME_VALIDATION.md).

DEC-010: ACCEPTED / IMPLEMENTED FOR PHASE 0 LAB. Session/Admission, QR POC, Gorilla Protocol, input de laboratorio → Unity e integración 1–4 clientes: PASS. Camera e Input Fusion: NOT STARTED. 3A y Fase 0: IN PROGRESS. Fase 1: NOT STARTED. #94 OPEN/DRAFT, head 0d940f77 sin cambios remotos; trabajo local sin commit/push/merge.

Único siguiente incremento recomendado: PC Webcam → CameraInput → Player Lock → temporal alignment → Input Fusion infrastructure. No iniciado.


## Webcam PC / asociación / alineación / Fusion — Software/Lab, 2026-10-08 UTC

**PASS — SOFTWARE/LAB**: CameraInput, Player Lock, Temporal Alignment e Input Fusion infrastructure.
Se conserva runtime móvil PASS y se añade adaptador cámara local supervisado/fixtures; no gameplay.
[Informe completo y evidencia](PHASE0_CAMERA_FUSION_VALIDATION.md), DEC-012.

- Java48/48, PWA18/18, EditMode64/64, PlayMode16/16 finales PASS; Linux build0 errores/0 warnings, PWA build PASS.
- Corpus12 camera cases,21 escenarios fusion y8 trazas conceptuales; Player4Chrome RTC reales + camera replay PASS,
  turnos1–4, aislamiento, lost/recovery, phone disconnect/manual resume y freshness.
- unavailable/vision failure no detienen cuatro inputs móviles; worker exit2 esperado, parents exit0, propios residuales0.
- Regresión Player2A/2B11 grupos PASS; runtime mobile real PASS con18 negativos/lifecycle. Bench2C anteriores
  independientes/601.2234171s conservados sin nueva ejecución; JAR y fuentes de transporte/PhoneInput intactos,
  regressions reales codec/proceso Java/PlayMode repetidas. Getter de lectura nuevo únicamente en IpcProbeRunner.
- Replay:342 frames,336 recibidos,29.868FPS;50 evaluaciones Eligible; delta P50/P953/13ms,max14ms.
  Process→Unity P50/P9510/43ms,max202ms. Datos completos sin exclusiones en e2e-replay03.json.
- Webcam física:640×480,102 frames,100 recibidos,17.611FPS; processing P50/P9539.341/65.884ms,
  read P50/P957.655/36.538ms; process→Unity P50/P9510/157ms,max402ms. Confianza .00136–.00687,
  cero frames fiables bajo configuración lab; captura→detector→store y release PASS técnico parcial,
  persona/salida/retorno fiable NOT RUN.30FPS no acreditados, hardware dropped frames desconocidos.

Normales: Unity/Java/vision exit0, STOPPED, camera/detector released, sin forced y propios residuales0.
Intentos fallidos de compilación/tests/harness y early Python exit134 preservados con reparaciones y
repeticiones separadas; no alteran históricos. No global leak claim ni accuracy/latencia física deportiva.

iPhone/Android físicos, Windows, movimiento humano, fusión física, tuning final y packaging siguen DEFERRED;
Android Emulator RTC nuevo BLOCKED. Secure-context/onboarding aún deuda; no reabrir comparador/WT ni usar
cámara móvil. A–J del cierre Software/Lab disponen de gates; **siguiente único bloque: FINAL PHASE0 AUDIT**,
no ejecutado aquí. Fase0/3A IN PROGRESS, Fase1 NOT STARTED; PR94 OPEN/DRAFT head0d940f77, nueva implementación
sólo local sin commit/push/merge. Auditoría decidirá cierre/integración; no se declara COMPLETED ni READY Fase1.


**Candidato final del mismo incremento:** EditMode64/64 y build03 Linux0/0; e2e-replay04 PASS después de
conservar RawPhone/RawCamera/ages/quality aun con input no elegible. No cambia algoritmo temporal, detector,
lifecycle ni protocolo. Replay04: 329 frames/326 recibidos, 29.870FPS; 48 Eligible,
delta P50/P95 9/15ms,max15ms; process→Unity P50/P95 10/42ms,max210ms.
Corridas anteriores y webcam física se mantienen separadas, no se sustituyen/combinan. Propios residuales0,
Unity/Java/vision exit0, sin forced. Sólo FINAL PHASE0 AUDIT pendiente; no ejecutado.

## Reconciliación operativa Project — 2026-10-08

SoftwareLab technical checkpoint PASS retenido; Project Phase0 stillOPEN. Se publican los cambios auditados en PR94; no nuevo código de producto. 27Issues reales reconciliados con AC/Dependencies/DoD:0cierres,9InProgress/8POReview/10Validation; metadata/dependencias preservadas. T0012/3,slowmotion pendiente; T016/T017/T019 físicos y T020PC limpia pendientes. #35blockedby#34;Fase1NOTSTARTED. [Matriz/evidencia](PHASE0_PROJECT_RECONCILIATION.md). Checks del head publicado inicialPASS; final e integración se verifican separadamente.

## T008 / DEC-016 — candidato local trust, 2026-10-08

Base integrada `7e818d5e729aad09c3b6fe40cc22422268198d2a`; rama `feature/mobile-t008-local-trust`. DEC-016 ACCEPTED FOR IMPLEMENTATION / VALIDATION: CA única por instalación, leaf corto con SAN iPAddress, origen HTTPS LAN y QR conectado al endpoint de producto. Preparación inicial de confianza y JOIN preparado son flujos/mediciones distintos; no cambia WebRTC/contrato/IPC ni añade T009. [Diseño y procedimiento](PHASE0_T008_LOCAL_TRUST.md).

**Nueva ejecución del candidato:** Java **52/52 PASS**, 0 errores/fallos/SKIP y build/package PASS; PWA **9/9 PASS** y build PASS. Incluye 4 tests nuevos (3 unit + 1 Java real) de CA/leaf/permisos/IP/QR/TLS/bootstrap/EOF. TLS normal: CA confiada valida; certificado no confiado, IP incorrecta y expiración se rechazan. No trust-all ni flags. Management/Foundation validadores locales PASS.

**Regresión serial nueva PASS:** Chrome real, 4 peers → WebRTC → Java hijo de Unity → IPC 127.0.0.1 → Player, 641 observaciones Unity y 18 negativos; recuperación manual, peers liberados, STOPPED, exit 0 y propios residuales 0. Entrada sintética/replay, no sensores físicos ni medición de onboarding humano. Cada corrida sintética permanece separada en [evidencia](evidence/t008-local-trust-2026-10-08/rtc-regression-serial.json). JAR final SHA256 `5c9728132226b9ac89ca057091e4aa4abffe0336529897896e4477d1eefb41de`.

Unity EditMode **64/64**, PlayMode **16/16**, build Linux **0 errores/0 warnings** son evidencia **retenida, no una nueva ejecución**: fuentes/configuración Unity intactas frente a la base. Player SHA256 `26901901cc6dd63212d741b339897f072d34cb7b3f08419fe451fc8e77c79a1f`. El JAR cambió; por ello se ejecutó la integración real nueva anterior. No se combinan/extrapolan métricas históricas 2C o móviles al gameplay físico.

Fallos conservados por separado: primer test Java real falló por lector de test acotado para PNG; corrección exclusiva del lector, repetición PASS. Primer RTC con gate físico concurrente falló por deadline (causa exacta no demostrada); repetición serial PASS. [Resumen de software](evidence/t008-local-trust-2026-10-08/software-results.json).

**iPhone 15 BLOCKED antes de instalar CA:** `/prepare` no carga. Cinco eventos UFW BLOCK coinciden con la IP del teléfono y los puertos del gate; lectura administrativa de estado rechazada con `sudo: a password is required`. No modificaciones de firewall/router/DNS/CA del sistema. No fallo TLS/Safari acreditado. Gate cerrado Player/Java exit 0, STOPPED/cleanup completo, residuales 0. Confianza HTTPS física, QR→READY humano y ambas duraciones NOT RUN/NOT MEASURED; Android físico NOT RUN. [Evidencia sanitizada](evidence/t008-local-trust-2026-10-08/physical-gate-01.json).

**Estado:** AC1 PARTIAL; AC2 PARTIAL; AC3 NOT RUN (preparación/JOIN humanos); AC4 PASS. T008/#19 OPEN / Validation, T009/#20 bloqueado por #19, F02/E0/Fase0 OPEN, Fase1 NOT STARTED. No cierre/merge automático. Siguiente gate exclusivo: intervención autorizada y administrativa para resolver el filtrado LAN observado, seguida de preparación/confianza iPhone. El nuevo candidato no constituye aceptación física ni integración en develop.

## CI native prerequisite

Las primeras cuatro ejecuciones Foundation del candidato fallaron (Java 52: 49 PASS, 1 error, 2 SKIP de gates HTTPS LAN históricos). El diagnóstico final identifica `Initialize the default AudioDeviceModule failed`: el runner no tiene sistema de audio activo y la fábrica nativa RTC existente lo solicita incluso para DataChannel. No era un fallo TLS ni se demuestra inviabilidad de DEC-016. Fallos conservados en [evidencia](evidence/t008-local-trust-2026-10-08/ci-native-audio.json).

Corrección de entorno: el workflow prepara PulseAudio exclusivamente en el runner efímero de CI. No cambia la fábrica/contrato WebRTC, no salta la prueba real y no instala software en la laptop del usuario. No añade dependencia Maven ni servicio de gameplay. La repetición de checks se publica separadamente en el PR.

## T008 gate físico con excepción UFW temporal — 2026-10-08

Código probado: `023c0725af3a6e015cf7921c452deed9438f3def`. Sólo documentación/evidencia cambia después de esta prueba; no nueva ejecución de suites retenidas ni modificación de WebRTC. [Evidencia sanitizada](evidence/t008-local-trust-2026-10-08/physical-firewall-gate-03.json).

LAN comprobada: `wlp0s20f3`, PC `10.1.125.17/22`, subred `10.1.124.0/22`, gateway `10.1.124.1`; iPhone confirmado `10.1.124.240`. Runtime real comprobado escuchando HTTPS TCP8443 y bootstrap TCP18000. Elevación normal `pkexec` autorizada por PO; no contraseña guardada/automatizada. UFW activo antes/después, políticas deny incoming / allow outgoing / disabled routed preservadas. Sólo dos reglas ALLOW IN TCP hacia la IP de la PC por esa interfaz y desde esa subred, comentarios `Gorilla Escape T008 PREPARE` y `Gorilla Escape T008 HTTPS`; ningún UDP abierto.

**Bootstrap físico PASS:** PO confirma PREPARE DEVICE visible. **HTTPS /prepare iPhone PASS:** PO confirma apertura en Safari sin advertencia tras instalar/confiar CA. Evidencia humana declarada, no inspección remota del teléfono. Misma CA de instalación, sin regeneración. iOS27 es dato PO previo; patch/build/Safari exactos no verificados. Android físico NOT RUN.

**JOIN FAIL / READY no acreditado:** tras QR y JOIN, Safari muestra `No se pudo conectar. Comprueba LAN, confianza TLS y QR vigente.` Snapshot posterior Java: peers0, received0, forwarded0; no inferir ausencia de peer transitorio desde ese snapshot. Filtro del journal desde emisión de QR: sólo UFW BLOCK UDP source iPhone / destination PC, **0 eventos observados**, ningún puerto UDP acreditado. La ausencia de logs no demuestra ausencia de tráfico o drops. No atribuir fallo a UDP/TLS/Safari/expiración sin evidencia. El QR tenía TTL30s al emitirse; hora de escaneo/llegada de JOIN no medida. Panel de archivo inicialmente queued, imagen después mostrada inline; no acreditar tiempo de visibilidad humana.

**FIRST-TIME PREPARATION: NOT MEASURED. PREPARED DEVICE JOIN: NOT MEASURED; READY no alcanzado.** Se solicitó cronómetro y duración, pero no se recibió valor. No estimar tiempos por respuestas del chat, excluir pasos manuales, afirmar target<60s ni reinstalar CA sólo para inventar medición inicial.

Cleanup PASS: eliminadas exclusivamente las dos reglas añadidas; ninguna regla propia restante. UFW activo, políticas intactas y estado completo coincidente con el anterior (comparación, no restauración de snapshot). Player/Java exit0, STOPPED, cleanupComplete=true, forced=false, Java propio residual0, listeners18000/8443 cerrados, QR efímero eliminado. CA privada sigue fuera de Git; confianza del teléfono no se retira automáticamente. No router/DNS/NAT/WAN/forwarding modificados.

API de rango ICE confirmada en artefacto0.19.0: `RTCConfiguration.portAllocatorConfig.minPort/maxPort`; guía oficial con extremos inclusivos y 0 no especificado. Config actual sólo deshabilita STUN/relay/TCP. No rango seleccionado/aplicado/abierto ni validación nativa de rango. **DEC-017 Proposed — ONE-TIME NETWORK SETUP**, pendiente necesidad UDP observada, dimensionamiento1/4peers y aprobación PO; no producto firewall integrado ahora.

AC1 PARTIAL global (iPhoneHTTPS PASS; Android físico pendiente); AC2 PARTIAL (QR/PWA accesibles; RTC/READY no); AC3 PARTIAL / mediciones no disponibles; AC4 PASS (registro/alternativas, Plan B no activado). #19 OPEN/Validation, #20 bloqueado; PR96 OPEN/DRAFT, sin merge; Fase0 IN PROGRESS, Fase1 NOT STARTED. Próximo bloqueo real: JOIN no alcanza READY y el mensaje no distingue fase/causa. Proponer intento controlado con QR nuevo y diagnóstico de señalización/ICE antes de modificar transporte o abrir UDP. No ejecutado después del cierre del gate.

## T008 JOIN — instrumentación y rollback acotados, 2026-10-08

[Diagnóstico y procedimiento](PHASE0_T008_LOCAL_TRUST.md). [Evidencia software](evidence/t008-local-trust-2026-10-08/join-diagnostics-software.json): verificación Java serial 54 PASS + 2 SKIP; ejecución suplementaria LAN 8/8 PASS cubre ambos SKIP (56 tests distintos PASS, sin reescribir resultados de corridas). PWA 14/14 PASS/build PASS; Management/Foundation locales PASS. Intentos intermedios y causas conservados; no cambios a tests históricos para obtener PASS.

[Regresión real RTC](evidence/t008-local-trust-2026-10-08/join-diagnostics-rtc-regression.json): cuatro clientes Chrome → Java hijo → IPC → Unity Player, 640 observaciones, 18 negativos PASS; manual resume y cleanup STOPPED/exit0/residuales0. Unity completo no repetido: fuentes/config intactas, evidencia anterior retenida. Ningún cambio a configuración ICE/transportes/puertos; DEC-017 Proposed.

Gate físico instrumentado pendiente; HTTPS iPhone anterior PASS conservado, fallo JOIN anterior y ausencia de UFW UDP observada no reinterpretados. AC1/AC2 PARTIAL, AC3 sin medición humana completa, AC4 registro PASS; #19 OPEN/Validation, PR96 OPEN/DRAFT, T009 bloqueado; Fase0 IN PROGRESS/Fase1 NOT STARTED.
