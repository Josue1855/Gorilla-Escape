# Progreso de desarrollo

Actualizado: 2026-10-06.

## Estado actual

Fase 0 en progreso; Spike no completado. Base compartida en main y develop: Unity 6000.3.23f1, servidor Java 21/Spring Boot 4.1.1, PWA React 19.3.0/Vite 8.3.2/Node 24, contratos JSON y paquete C# compartido. Crear ramas pequeñas desde develop. No se inicia Fase 1.

## Implementado

- Servidor local con health endpoint y empaquetado de la PWA en el JAR.
- Shell React, errores recuperables y caché offline del shell.
- Contratos y fixture v1; metadatos Unity, configuración y pruebas preparadas.
- Guías de arquitectura, arranque, contribución y reglas de desarrollo.
- Formularios, workflows de validación y scripts de gestión del backlog.

## Validación

Como checks de regresión del cierre, Java 21/Maven verify y React test/build PASS (4/4 tests por componente), sin cambios de sus fuentes. UNITY-001 valida Unity Foundation en Linux de desarrollo: import limpio, resolución de cinco paquetes, escena/settings, EditMode 2/2, PlayMode 1/1, build Mono, Player con gráficos, cierre normal y clean re-import. Evidencia y advertencias en [TEST_REPORT](TEST_REPORT.md) y [UNITY_FOUNDATION_VALIDATION](UNITY_FOUNDATION_VALIDATION.md).

El batch licenciado funciona dentro del entorno Flatpak del Hub; los intentos directos anteriores con salida 198 siguen registrados como BLOCKED históricos. DEC-003 acepta el core Test Framework 1.6.0 conservando Unity 6000.3.23f1. SDK/toolchain Linux retirados mediante UPM; validación local usa las opciones de este editor que evitan su instalación/migración automática. No se incorporan como requisito permanente del equipo.

Escena, ApplicationSettings, lockfile, .meta y settings compartidos revisados forman parte del diff candidato. Defaults de física/calidad/tiempo no son tuning final. La PC de presentación permanece sin definir y NOT RUN. Unity Foundation / Development **PASS**, cierre autorizado para commit/PR a develop y pendiente de integración por revisión normal. Presentation PC **BLOCKED / NOT RUN**, rendimiento físico **NOT RUN**. Fase 0 **IN PROGRESS**, Incremento 2/Fase 1 **NOT STARTED**. Histórico 1A y auditoría conservados en [PHASE0_UNITY_FOUNDATION](PHASE0_UNITY_FOUNDATION.md).

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

Cierre de desarrollo autorizado: diff revisado, checks finales y commit/PR a develop para revisión normal; no forzar merge. Diagnósticos separados como **ENVIRONMENT / REQUIRES FOLLOW-UP**, [deuda #89](https://github.com/Josue1855/Gorilla-Escape/issues/89); no bloquean la integración sin evidencia de fallos del proyecto. Próximo paso recomendado, no ejecutado: definir PC/plataforma oficial y cerrar la aceptación de presentación del Incremento 1. Decisiones arquitectónicas antes de Incremento 2 sólo bajo nueva autorización del PO. Métricas físicas y Spike completo pendientes.
