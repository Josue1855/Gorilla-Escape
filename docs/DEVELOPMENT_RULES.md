# Reglas de desarrollo — Gorilla Escape: Gorilimpiadas

## Source of Truth

Antes de desarrollar cualquier función o modificar código, lee:

`docs/Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md`

Ese documento es la fuente principal de requisitos, arquitectura, gameplay, UX, QA y alcance.

La especificación tiene prioridad sobre supuestos de implementación. Si existe contradicción entre código existente y la especificación, no modifiques silenciosamente requisitos, arquitectura ni alcance. Reporta la discrepancia. Una decisión sólo cambia con evidencia técnica suficiente, registro en docs/DECISIONS.md y actualización explícita de la fuente de verdad cuando corresponda.

## Prioridades

En este orden:

1. MVP demostrable.
2. Estabilidad.
3. Baja latencia.
4. Facilidad de desarrollo.
5. Experiencia del jugador.
6. Mantenibilidad.
7. Escalabilidad futura.

No implementar P1 o P2 mientras exista trabajo P0 pendiente.

## Arquitectura congelada

Mantener salvo evidencia técnica objetiva de inviabilidad:

- Unity + C#.
- PC autoritativa.
- Modular Monolith.
- Java 21 + Spring Boot local.
- PWA React como control móvil.
- WebSocket.
- JSON para protocolo MVP.
- MediaPipe + OpenCV.
- 1–4 jugadores.
- Procesamiento local.
- Cámara principalmente por turnos.
- Cinco minijuegos.
- Gorilla Points.
- Cuatro avatares sin estadísticas diferentes.
- Calibración adaptativa.
- ScriptableObjects para tuning.
- Persistencia JSON local.
- Telemetría local.
- Sin Internet requerido durante gameplay.
- Sin reconocimiento facial.
- El teléfono nunca se suelta físicamente.

No introducir:

- microservicios;
- cloud obligatorio;
- cuentas;
- base de datos remota;
- multijugador online;
- app móvil nativa salvo activación documentada del Plan B;
- frameworks adicionales sin justificación;
- servicios externos innecesarios;
- matchmaking;
- IA como característica adicional de producto (no impide el tracking MediaPipe previsto).

## Repositorio único

Todos los proyectos propios deben permanecer en este mismo repositorio: Unity, servidor Java 21 + Spring Boot, PWA, contratos compartidos, adaptadores de cámara cuando corresponda, tests, herramientas y documentación. No crear repositorios Git anidados ni separar proyectos en repos externos por iniciativa propia.

Consultar docs/MONOREPO_PLAN.md como organización vigente y docs/TECHNOLOGY_RESEARCH.md como investigación histórica, sin sustituir la especificación maestra. Crear carpetas sólo con contenido útil y dentro de la fase solicitada. Compartir repositorio no significa compartir runtime: validar interoperabilidad Unity/Java mediante JSON y fixtures; Java no comparte ensamblados C# con Unity. Las recomendaciones de versiones y dependencias requieren pruebas; no adoptarlas automáticamente ni cambiar el stack congelado.

## Regla de implementación

Cada tarea debe limitarse a la fase aprobada y a su alcance acordado.

No adelantes funcionalidades de fases posteriores.

Antes de modificar código:

1. inspecciona el repositorio;
2. identifica qué existe;
3. identifica qué falta;
4. identifica dependencias y riesgos;
5. presenta un plan breve;
6. implementa solamente el alcance solicitado.

## Calidad de código

Aplicar:

- separación clara de responsabilidades;
- interfaces donde desacoplen hardware y gameplay;
- dependency injection cuando aporte valor real;
- código testeable;
- CancellationToken donde corresponda;
- async/await correctamente;
- evitar singletons innecesarios;
- evitar dependencias circulares;
- dependencias explícitas y manejo correcto del ciclo de vida;
- evitar duplicación;
- evitar magic numbers;
- tuning en configuración;
- errores recuperables;
- logs estructurados.

Los minijuegos no deben conocer directamente:

- WebSocket;
- MediaPipe;
- DeviceMotion;
- OpenCV;
- Spring Boot;
- DeviceOrientation;
- hardware específico y detalles concretos del teléfono.

Los minijuegos consumen `PlayerInput` normalizado. Las fronteras de entrada se expresan mediante contratos como `PhoneInput`, `CameraInput`, `PlayerMotion` y `PlayerInput`; los contratos crudos no deben filtrarse a los minijuegos.

## Testing obligatorio

Cada cambio relevante debe incluir las pruebas aplicables:

- Unit Tests.
- Integration Tests.
- Unity PlayMode Tests.
- validaciones manuales cuando hardware real sea necesario.

No declares una función terminada sólo porque compila.

## Definition of Done

Una función P0 sólo está Done cuando:

- implementada;
- compila;
- cumple criterios de aceptación;
- tiene las pruebas aplicables;
- está integrada;
- PR revisado;
- funciona en hardware relevante cuando corresponda;
- no rompe comportamiento existente;
- trata errores recuperables;
- tiene logging suficiente;
- no contiene tuning hardcodeado innecesariamente;
- documentación mínima actualizada.

Un minijuego requiere:

- tutorial;
- input real;
- gameplay completo;
- scoring;
- feedback;
- resultado;
- reinicio;
- soporte de turnos/multijugador;
- ausencia de crashes conocidos.

## Métricas objetivo

Mantener como objetivos:

- Unity: 60 FPS.
- Webcam: >=30 FPS.
- Sensores: 50 Hz objetivo.
- Calibración: <30 segundos.
- Latencia P95: <150 ms.
- Ideal: <100 ms.
- Crashes: 0.
- Cambio accidental de Player Lock: 0.

No falsear estas métricas.

Cuando una medición requiera hardware físico, prepara la instrumentación y explica exactamente qué prueba humana debe ejecutarse.

## Seguridad

Nunca diseñar una mecánica que requiera soltar físicamente el teléfono.

Los movimientos excesivamente violentos no deben proporcionar una ventaja infinita. Aplicar saturación y validación. La cámara se procesa localmente: no guardar video ni fotografías, no identificación biométrica ni envío de video a Internet.

## Git

Trabajar mediante ramas pequeñas cuando corresponda.

No realizar refactors masivos no solicitados.

No eliminar código funcional sin justificarlo.

No hacer force push, reescritura destructiva del historial ni operaciones Git destructivas. Mantener cambios pequeños y revisables, sin mezclar fases.

Antes de finalizar una tarea:

1. ejecutar build;
2. ejecutar tests disponibles;
3. revisar errores/warnings importantes;
4. revisar diff;
5. actualizar documentación.

## Orden de desarrollo

- Fase 0 — Base técnica / Spike 0.
- Fase 1 — Vertical Slice completo de Gorilla Smash.
- Fase 2 — Sistemas reutilizables.
- Fase 3 — Coconut Throw, Jungle Archery, Coconut Bowling y Jungle Slice, uno por uno y en ese orden.
- Fase 4 — Integración del torneo.
- Fase 5 — Polish.
- Fase 6 — Feature Freeze, QA, bugs, build y evidencias.

No cambiar el orden sin razón técnica documentada. La preparación documental precede a Fase 0 y no la autoriza. No iniciar automáticamente la fase siguiente.

## Definition of Ready

Una tarea puede comenzar con objetivo claro, responsable, prioridad, dependencias conocidas, criterio de aceptación, contrato/diseño disponible cuando corresponda y alcance suficientemente pequeño. No comenzar tareas ambiguas enormes.

## Configuración y tuning

Mantener configurables DeadZone, MaximumAcceleration, SynchronizationWindow, MinimumCameraConfidence, SensorRate, spin, fricción, sensibilidad, hitboxes y ventanas temporales. Usar ScriptableObjects para tuning Unity y configuración central para otros módulos. No asumir valores finales sin pruebas reales ni agregar patrones por sofisticación.

## Cobertura de testing

- Unit Tests: normalización, scoring, Gorilla Points, desempates, validaciones y transformaciones.
- Integration Tests: WebSocket → PhoneInput, CameraInput → InputFusion, PlayerInput → gameplay y resultados → torneo.
- Unity PlayMode: escenas, sesión, reinicios, resultados, desconexiones y recuperación.
- Manual: sensores y teléfonos reales, webcam, movimiento físico, UX y game feel.

Nunca declarar aprobada una prueba física no ejecutada. Registrar procedimiento, hardware, resultados y evidencia en docs/TEST_REPORT.md. Si no existe proyecto ejecutable, declarar build y tests de aplicación no aplicables; no inventar éxitos.

## Reporte obligatorio de cierre

Cada tarea de desarrollo debe cerrar con estas secciones:

### Implementado

Qué se realizó.

### Archivos creados/modificados

Archivos principales.

### Build

Comando y resultado, o motivo por el que no aplica.

### Tests

Pruebas ejecutadas y resultados reales.

### Validación manual requerida

Qué debe probarse físicamente y cómo.

### Riesgos / deuda técnica

Problemas y pendientes.

### Estado de criterios de aceptación

Aceptados, pendientes o bloqueados con evidencia.

### Próximo paso recomendado

Únicamente la siguiente fase lógica; no iniciarla automáticamente.

Actualizar siempre docs/DEVELOPMENT_PROGRESS.md. Después detenerse. El formato de entrega acordado para cada tarea tiene prioridad.
