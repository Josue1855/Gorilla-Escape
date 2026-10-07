# Progreso de desarrollo

Actualizado: 2026-10-06.

## Estado actual

Fase 0 en progreso; Spike no completado. Base compartida en main y develop: Unity 6000.3.23f1, servidor Java 21/Spring Boot 4.1.1, PWA React 19.3.0/Vite 8.3.2/Node 24, contratos JSON y paquete C# compartido. Crear ramas pequeñas desde develop. No se inicia Fase 1.

Estados vigentes: **2A PASS / MERGED; 2B PASS en Linux de desarrollo, pendiente de revisión/integración; Incremento 2 IN PROGRESS; 2C NOT STARTED; Fase 0 IN PROGRESS; Fase 1 NOT STARTED.**

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

Diseño [Accepted por el PO](PHASE0_IPC_2B_DESIGN.md), con precisiones explícitas de heartbeat, cleanup/generaciones, singleton y timeouts. Implementación exclusivamente 2B en `codex/phase0-ipc-2b-lifecycle`, desde develop `9fbb566e5f24af8f4e92fa3c139968161e832d46`. Estados explícitos, cero retries/restarts automáticos, recuperación manual hasta tres lanzamientos, FileLock Java por usuario/producto, EOF temprano y cleanup observado. Contrato/codecs/timeouts 2A conservados. Validación final **PASS Linux de desarrollo**: Java 13/13, EditMode 15/15, PlayMode 10/10, React 4/4, build Player 0 errores/0 warnings y 11 grupos Player reales/fixtures identificados, residual propio 0. Evidencia y límites en TEST_REPORT. 2A PASS/MERGED; Incremento 2/Fase 0 IN PROGRESS; 2C/Fase 1 NOT STARTED. 2B PASS, pendiente de revisión/integración por PR; no merge automático.

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
- Resolver HTTPS local confiable, permisos y experiencia de incorporación de teléfonos.
- Validar WebSocket, IPC Unity/Java y adaptador MediaPipe/OpenCV.
- Medir en hardware real antes de aceptar métricas.
- Resolver discrepancia de alcance entre secciones 42 y 50, autoridad de resultados y detalles del contrato.
- Revisar calendario con Product Owner; no cambiar fechas originales sin decisión.

## Publicación del repositorio

Base publicada con historial inicial limpio y ramas main/develop. El backlog y los vínculos del Project se reconstruyen desde el manifiesto versionado; resultados de gestión en docs/github/verification.json. Los PR anteriores no forman parte de esta publicación.

## Próximo paso

2A ya está integrado. Próximo incremento recomendado, no autorizado ni iniciado: 2B para lifecycle/fallos básicos (timeouts, disconnect y cleanup), con alcance acordado antes de código. Decidir PC/plataforma oficial y aceptación física pendiente. Continuar a hardening/2B sólo con autorización posterior del PO; no sensores, cámara, móvil, gameplay ni Fase1. Seguimiento de espera #90 concluido al verificar merge.
