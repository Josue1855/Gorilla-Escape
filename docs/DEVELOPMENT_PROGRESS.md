# Progreso de desarrollo

Actualizado: 2026-10-06.

## Estado actual

Fase 0 en progreso; Spike no completado. Base compartida en main y develop: Unity 6000.3.23f1, servidor Java 21/Spring Boot 4.1.1, PWA React 19.3.0/Vite 8.3.2/Node 24, contratos JSON y paquete C# compartido. Crear ramas pequeñas desde develop. No se inicia Fase 1.

## Incremento 2A — IPC mínimo

Implementado y validado en Linux de desarrollo: Unity inicia Java21/Spring, READY + TCP loopback PING/PONG, framing/token/identidades, deadlines/cancellation, stdin EOF y cierre observado sin Java propio residual. Build Player 0 errores/0 advertencias; Java8/8, EditMode7/7, PlayMode3/3, React regresión4/4 PASS. Smoke y límites en [TEST_REPORT](TEST_REPORT.md). Gate funcional 2A desarrollo PASS; commit/PR de 2A pendiente de revisión/integración. No declarar Done de hardware final. Fase0 IN PROGRESS; 2B/Fase1 NOT STARTED.

#90 integrado normalmente en `2a635b607a286bb70d1457aa0cf34f7e85f17abe`; foundation validada contenida y hashes confirmados. Único cambio de protección autorizado: approvals develop1→0; PR/checks/force-push prohibido conservados.

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

Revisar e integrar normalmente el cambio 2A con sus checks y evidencia; no merge automático. Decidir PC/plataforma oficial y aceptación física pendiente. Continuar a hardening/2B sólo con autorización posterior del PO; no sensores, cámara, móvil, gameplay ni Fase1. Seguimiento de espera #90 concluido al verificar merge.
