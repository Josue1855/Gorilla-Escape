# Progreso de desarrollo

Actualizado: 2026-10-06.

## Estado actual

Fase 0 en progreso; Spike no completado. Base compartida en main y develop: Unity 6000.3.23f1, servidor Java 21/Spring Boot 4.1.1, PWA React 19.3.0/Vite 8.3.2/Node 24, contratos JSON y paquete C# compartido. Crear ramas pequeñas desde develop. No se inicia Fase 1.

## Preparación del Incremento 2 — Unity ↔ Java

Diseño aceptado con precisión de alcance: esperar merge #90 antes de implementar sólo 2A. [Documento de decisión](PHASE0_UNITY_JAVA_DECISION.md) y DEC-005 **Accepted** cubren ownership, cuatro mecanismos IPC, lifecycle, contrato mínimo, seguridad, fallos, pruebas y presupuesto de latencia. Recomendación: Unity autoritativo del juego y supervisor; Java host/transporte; TCP loopback con PING/PONG y pipes sólo para lifecycle. Aprobación explícita del PO registrada; v4 §5 actualizada. Implementación exclusivamente 2A autorizada después del merge normal de #90; sin modificar ese PR.

Verificación remota 2026-10-06: develop continúa en `e950bd820ea2dd6ede265943a3b7702f445be467`; [PR #90](https://github.com/Josue1855/Gorilla-Escape/pull/90) aparece OPEN/mergedAt=null y su commit validado es `38e1e31ce0e36952b759061eee40136f7a0ef3f6`. El anuncio de integración no coincide con GitHub observado. Se conserva la validación de foundation de desarrollo del PR, sin fingir integración en develop ni reabrir bloqueos históricos. Revalidar base antes de implementar.

Estado: diseño aceptado; implementación 2A **BLOCKED** por integración #90, IPC **NOT STARTED**; mediciones IPC **NOT RUN**; PC de presentación **BLOCKED / NOT RUN**; Fase 0 **IN PROGRESS**; Fase 1 **NOT STARTED**. No se amplió PWA móvil, QR, WebSocket de teléfonos, sensores, MediaPipe/OpenCV ni gameplay.

## Implementado

- Servidor local con health endpoint y empaquetado de la PWA en el JAR.
- Shell React, errores recuperables y caché offline del shell.
- Contratos y fixture v1; metadatos Unity, configuración y pruebas preparadas.
- Guías de arquitectura, arranque, contribución y reglas de desarrollo.
- Formularios, workflows de validación y scripts de gestión del backlog.

## Validación

Java y React tienen build y pruebas automatizadas. El texto original de develop registraba Unity bloqueado por licencia; la foundation posterior del PR #90 pasó import, paquetes, escena/settings, EditMode 2/2, PlayMode 1/1, build Linux/Mono, Player/cierre y reimportación reproducible. Esa evidencia pertenece al commit del PR, no al SHA actual de develop. Sensores, cámara, Player Lock, calibración y latencia física permanecen sin validar. El [TEST_REPORT](TEST_REPORT.md) de esta base sigue siendo histórico.

Validación de la preparación: foundation estática y gestión documental PASS; revisión del diff sin errores y sólo documentos. No build ni tests nuevos de aplicación: fuentes/configuración ejecutable sin cambios. Todas las pruebas y métricas IPC están preparadas, todavía NOT RUN.

## Requisitos y decisiones

[Especificación v4](Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md) como fuente principal; DEC-002 autoriza Java + React. La PC conserva autoridad, procesamiento local y funcionamiento sin Internet durante gameplay. No se requiere soltar el teléfono.

## Pendientes de Fase 0

- Verificar integración de foundation ya validada en desarrollo; PC/plataforma oficial y aceptación de presentación pendientes.
- Resolver HTTPS local confiable, permisos y experiencia de incorporación de teléfonos.
- Validar WebSocket, IPC Unity/Java y adaptador MediaPipe/OpenCV.
- Medir en hardware real antes de aceptar métricas.
- Resolver discrepancia de alcance entre secciones 42 y 50. DEC-005 aprobada; implementar exclusivamente 2A después del gate de integración. Resiliencia, watchdog y performance/hardening diferidos, no eliminados.
- Revisar calendario con Product Owner; no cambiar fechas originales sin decisión.

## Publicación del repositorio

Base publicada con historial inicial limpio y ramas main/develop. El backlog y los vínculos del Project se reconstruyen desde el manifiesto versionado; resultados de gestión en docs/github/verification.json. Los PR anteriores no forman parte de esta publicación.

## Próximo paso

Esperar revisión y merge normal de #90. Después actualizar develop, verificar SHA/contenido de foundation, crear rama nueva desde ese SHA y reportar SHA/rama/archivos/tests/riesgos/aceptación antes de código. Implementar sólo 2A con integración real y smoke startup/RTT; no continuar automáticamente a 2B ni Fase 1.

Seguimiento de la dependencia: comprobación horaria de #90 en este chat, silenciosa mientras siga sin cambios. Al confirmarse el merge, retomar únicamente 2A según autorización del PO, con reporte previo obligatorio y sin avanzar a 2B.
