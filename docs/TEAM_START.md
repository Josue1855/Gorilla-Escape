# Base de trabajo del equipo

Última revisión: 2026-10-26; conserva las instrucciones de develop revisadas el 2026-10-20. Guía operativa del monorepo en `develop`; contrastada con sus manifiestos y fuentes. Los resultados de pruebas y sus límites se consultan en el [estado verificable](00-product/DEVELOPMENT_STATUS.md) y el [índice de evidencias](04-testing/EVIDENCE_INDEX.md). Esta revisión documental no ejecuta ni acredita pruebas físicas.

## Arranque del equipo autorizado 2026-10-16

Está autorizado comenzar juegos, PWA y el cliente Flutter cuando se tome su tarea acordada, mientras continúa la validación física. La base compartida es `develop`: cada tarea se desarrolla en una rama pequeña y se entrega mediante PR hacia `develop`. `main` recibe promociones de entregas validadas y aprobadas.

La autorización permite iniciar implementación; T019, T020, F05 y E0 conservan sus criterios de aceptación pendientes. El botón JUGAR permanece deshabilitado porque los minijuegos aún no están implementados.

## Qué existe y qué falta

| Área | Base disponible | Trabajo pendiente |
|---|---|---|
| Unity | Foundation, menú, supervisión Java, admisión, PhoneInput, CameraInput, Player Lock y Fusion de infraestructura | Framework y cuatro minijuegos; validación de controles y equipo físicos |
| Conectividad | HTTPS, QR JOIN, señalización REST, input WebRTC DataChannel e IPC loopback; validación Software/Lab | T019 con teléfonos físicos y T020 en otra PC |
| PWA del monorepo | React, admisión RTC, permisos, sensores, lifecycle y service worker | Validación física final e integración acordada con UI/UX independiente |
| Backend separado | SQLite/JDBC, migración y GET/POST jugadores integrados en su develop (PR #14) | Publicador autorizado de resultados, idempotencia, backup e instalador final |
| PWA del fork UI/UX | Laboratorio React/SQLite y shell/IndexedDB integrados en su develop (PR #7) | Incorporación al upstream, adaptador técnico y QA física |
| Flutter | Inicio autorizado por tarea | Proyecto y cliente por implementar |

El cliente de [UI/UX independiente](00-product/REPOSITORIES.md) y `PWA/` son bases diferentes. El laboratorio /lab.html del fork ya consume persistencia real; el mando demo /index.html no integra RTC. `Server/` es el host técnico local, sin reglas de juego ni base de datos empresarial.

## Herramientas

| Componente | Versión fijada / fuente |
|---|---|
| Unity | 6000.3.23f1, `Unity/ProjectSettings/ProjectVersion.txt`; licencia válida |
| Java | JDK 21; Spring Boot 4.1.1 en `Server/pom.xml` |
| Maven | Usar `Server/mvnw` o `Server/mvnw.cmd`, con la configuración versionada |
| Web | Node 24; React 19.3.0 y Vite 8.3.2 en `PWA/package.json` |
| Contratos | `Shared/Protocol/` y paquete local `Shared/CSharp/` |
| Cámara | Runtime local MediaPipe/OpenCV; preparación en [T016](PHASE0_T016_CAMERA_RUNTIME.md) |

Conservar lockfiles y `.meta`. No actualizar paquetes o editor incidentalmente ni instalar dependencias de cámara con versiones arbitrarias. Las primeras restauraciones requieren Internet; eso no implica una dependencia WAN obligatoria durante la partida local. El empaquetado Linux tiene evidencia de laboratorio, pero no acredita PC limpia ni otras plataformas.

## Clonar y preparar una tarea

```sh
git clone https://github.com/Josue1855/Gorilla-Escape.git
cd Gorilla-Escape
git switch develop
git pull --ff-only
git switch -c feature/gameplay-nombre-de-tarea
```

Si ya existe un checkout, conservar sus cambios antes de actualizarlo. Elegir nombre y alcance según la Issue. El clonado público no requiere invitación; publicar ramas requiere escritura. Sin ese permiso, coordinar la entrega con el mantenedor sin compartir credenciales.

Seguir [preparación y ejecución](03-development/SETUP_AND_RUN.md) para restaurar dependencias, probar y arrancar cada componente. Esa guía distingue el servidor de desarrollo del producto supervisado por Unity.

## Empezar un juego en Unity

1. Abrir `Unity/` en Unity Hub con el editor fijado y esperar la importación del paquete local `Shared/CSharp`.
2. Usar **Gorilla Escape → Open Bootstrap** y **Play Gorilla Escape**. `Create Foundation Scene` se reserva para crear la foundation si falta; no regenerar escenas compartidas para iniciar una tarea.
3. Leer [Unity y autoridad](01-architecture/UNITY.md), [entradas y personajes](02-gameplay/INPUT_AND_CHARACTERS.md), [framework propuesto](02-gameplay/MINIGAME_FRAMEWORK.md) y la ficha de la disciplina.
4. Implementar reglas y simulación en Unity consumiendo input normalizado. Las interfaces del framework documental son propuestas; revisar clases reales antes de añadir adaptadores.
5. Coordinar escenas/prefabs con el equipo, entregar `.meta` y cubrir reglas, puntuación, turnos, reinicio y desconexión según la aceptación de la tarea.
6. Usar entradas sintéticas para desarrollar y registrar esa limitación. La aceptación de controles físicos requiere pruebas separadas.

El alcance vigente contiene [Gorilla Smash](02-gameplay/GORILLA_SMASH.md), [Coconut Throw](02-gameplay/COCONUT_THROW.md), [Coconut Bowling](02-gameplay/COCONUT_BOWLING.md) y [Jungle Slice](02-gameplay/JUNGLE_SLICE.md). Las reglas aún abiertas se acuerdan antes de implementarlas como definitivas.

## Conexión y límites entre componentes

Unity conserva la autoridad sobre jugadores competitivos, reglas y resultados. Java administra transporte y sesión técnica; la PWA captura intención y presenta estados. Shared mantiene contratos interoperables independientes del motor. No trasladar puntuación al teléfono ni crear un segundo sistema de sesiones.

El transporte móvil seleccionado es **WebRTC DataChannel**, con señalización REST; Unity y Java usan IPC loopback supervisado. La disponibilidad de WebSocket en dependencias o referencias históricas no cambia esa decisión.

La experiencia normal ofrece un QR JOIN. AUTO intenta la ruta pública durante un plazo acotado y conserva fallback HTTPS local; el origen de una admisión queda congelado. La confianza de certificados se trata mediante recuperación explícita y los permisos de firewall pertenecen al helper acotado. Unity, Java y cámara se ejecutan como usuario normal. No asumir sensores reales sobre HTTP LAN ni omitir errores TLS. Ver [contrato de conexión](PHASE0_PHONE_CONNECTION_FINAL.md).

## Entregar y revisar

Elegir Issue, alcance, dependencias y responsable antes de modificar archivos compartidos. Ejecutar los checks aplicables de [preparación](03-development/SETUP_AND_RUN.md), describir evidencia y límites en el PR y solicitar revisión de otro integrante. Los checks Java/web no prueban Unity ni hardware.

Usar el [flujo de ramas](03-development/GIT_WORKFLOW.md), las [convenciones](03-development/CODING_CONVENTIONS.md) y el [plan de integración](03-development/INTEGRATION_PLAN.md). No incluir `Library/`, outputs, SDK, credenciales ni assets sin derechos.

## Orden de lectura y antecedentes

El [índice único](README.md), el [estado verificable](00-product/DEVELOPMENT_STATUS.md) y las [decisiones vigentes](00-product/ARCHITECTURE_DECISIONS.md) orientan la implementación. Los informes fechados de Phase 0 y la especificación v4 conservan trazabilidad; sus restricciones sustituidas no anulan decisiones posteriores. Ante una contradicción nueva, resolverla con el equipo y actualizar la guía afectada antes de tratarla como requisito.
