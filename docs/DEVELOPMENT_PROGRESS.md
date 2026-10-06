# Progreso de desarrollo

Actualizado: 2026-10-05.

## Estado actual

Fase 0 en progreso; Spike no completado. Base compartida en main y develop: Unity 6000.3.23f1, servidor Java 21/Spring Boot 4.1.1, PWA React 19.3.0/Vite 8.3.2/Node 24, contratos JSON y paquete C# compartido. Crear ramas pequeñas desde develop. No se inicia Fase 1.

## Implementado

- Servidor local con health endpoint y empaquetado de la PWA en el JAR.
- Shell React, errores recuperables y caché offline del shell.
- Contratos y fixture v1; metadatos Unity, configuración y pruebas preparadas.
- Guías de arquitectura, arranque, contribución y reglas de desarrollo.
- Formularios, workflows de validación y scripts de gestión del backlog.

## Validación

Java y React tienen build y pruebas automatizadas. Unity no se ha compilado ni probado: el editor instalado requiere una licencia válida. Sensores, cámara, Player Lock, calibración y latencia física permanecen sin validar. Resultados reproducibles en [TEST_REPORT](TEST_REPORT.md).

## Requisitos y decisiones

[Especificación v4](Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md) como fuente principal; DEC-002 autoriza Java + React. La PC conserva autoridad, procesamiento local y funcionamiento sin Internet durante gameplay. No se requiere soltar el teléfono.

## Pendientes de Fase 0

- Licencia Unity, importación, generación de escena/settings/lockfile, EditMode/PlayMode y build PC.
- Resolver HTTPS local confiable, permisos y experiencia de incorporación de teléfonos.
- Validar WebSocket, IPC Unity/Java y adaptador MediaPipe/OpenCV.
- Medir en hardware real antes de aceptar métricas.
- Resolver discrepancia de alcance entre secciones 42 y 50, autoridad de resultados y detalles del contrato.
- Revisar calendario con Product Owner; no cambiar fechas originales sin decisión.

## Publicación del repositorio

Base publicada con historial inicial limpio y ramas main/develop. El backlog y los vínculos del Project se reconstruyen desde el manifiesto versionado; resultados de gestión en docs/github/verification.json. Los PR anteriores no forman parte de esta publicación.

## Próximo paso

Validar Unity con licencia y planificar únicamente el siguiente incremento del Spike de Fase 0.
