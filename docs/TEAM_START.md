# Base de trabajo del equipo

Fecha: 2026-10-06. Alcance: arranque de Fase 0; no completa Spike 0 ni implementa juego. Arquitectura aprobada en DEC-002 y v4 enmendada.

## Stack fijado

| Componente | Versión / herramienta | Estado |
|---|---|---|
| Juego PC | Unity 6000.3.23f1 LTS, C# | Foundation validada en Linux de desarrollo; presentación pendiente |
| Servidor local | Java 21, Spring Boot 4.1.1 Web MVC + WebSocket | Build y cuatro tests PASS |
| Build Java | Maven 3.9.11, wrapper 3.3.2 | Wrapper Windows/Linux/macOS, hash de descarga fijado |
| Control | React 19.3.0, React DOM 19.3.0 | Pantalla inicial y health check; sin gameplay |
| Build web | Vite 8.3.2, plugin React 6.1.2, Node 24 | npm lockfile, cuatro tests y build PASS |
| Tracking | MediaPipe + OpenCV | Adaptador y versiones pendientes del Spike; no instalar arbitrariamente |
| Persistencia | JSON local | Requisito; no implementada aún |

No requiere PostgreSQL, JPA, Lombok, Redis, Docker ni cloud para esta base. Node/Maven se usan para desarrollo/build; el producto final debe incluir recursos y runtime necesarios sin descargarlos durante gameplay. Empaquetado final JRE/Unity pendiente.

## Clonar y empezar

El clonado público no requiere invitación. Para publicar ramas en el repo se necesita permiso de escritura; también es posible proponer cambios desde un fork. Los permisos del equipo no se asignan por cargo académico.

Usar develop como base compartida y origen de ramas pequeñas. La preparación técnica está incluida en ambas ramas permanentes.

```sh
git clone https://github.com/Josue1855/Gorilla-Escape.git
cd Gorilla-Escape
git switch develop
npm --prefix PWA ci
npm --prefix PWA test
npm --prefix PWA run build
Server/mvnw -B -f Server/pom.xml verify
java -jar Server/target/local-server-0.1.0-SNAPSHOT.jar
```

En Windows sustituir Server/mvnw por Server\mvnw.cmd. Instalar JDK 21 y Node 24 desde sus distribuidores oficiales; comprobar java -version y node --version. El wrapper descarga Maven al primer uso; npm ci y Maven requieren Internet inicialmente. Dependencias resueltas/recursos empaquetados permiten ejecución local posterior.

Abrir http://127.0.0.1:8080. El JAR incluye la salida PWA/dist generada por Vite. Nunca editar Server/target ni mantener una copia manual del frontend dentro de Server. Para cambios web: reconstruir PWA y volver a empaquetar el JAR.

Durante desarrollo web usar npm --prefix PWA run dev: Vite en localhost y proxy /api hacia Java:8080. El health check verifica sólo disponibilidad del host y versión; no significa teléfono incorporado a una partida.

El servidor escucha 127.0.0.1 por defecto. GORILLA_SERVER_PORT cambia puerto; GORILLA_SERVER_ADDRESS permite configurar interfaz para pruebas LAN planificadas. No hay CORS global abierto ni un endpoint WebSocket de juego todavía. HTTPS/WSS confiable en teléfonos es requisito pendiente: HTTP a una IP LAN no garantiza sensores, instalación o service worker. No publicar claves/certificados.

## Unity

1. Unity Hub: añadir Unity/ al proyecto y usar exactamente 6000.3.23f1 con licencia válida. No actualizar automáticamente.
2. Resolver el paquete local Shared/CSharp y Test Framework core 1.6.0 (DEC-003). Manifest y lockfile revisados; NUnit 2.0.5 e IMGUI 1.0.0 son transitivos. No agregar NUnit ni toolchains Linux al manifest compartido.
3. Ejecutar Gorilla Escape → Create Foundation Scene. Crea la escena Bootstrap y ApplicationSettings; conserva una escena existente y pide guardar cambios antes de reemplazar la escena abierta. Revisar/versionar escena, settings y .meta generados.
4. Test Runner: EditMode y PlayMode. Bootstrap configura targetFrameRate y lo restaura al destruirse. EditMode verifica el fixture y la escena guardada; PlayMode verifica aplicación/restauración. 60 configurados no equivalen a 60 FPS medidos.
5. Para desarrollo Linux/Mono, usar FoundationSetup.BuildDevelopmentLinux y registrar versión/OS/logs. Import, EditMode 2/2, PlayMode 1/1, build, Player con gráficos, cierre y reimportación limpia PASS con el procedimiento de [UNITY_FOUNDATION_VALIDATION](UNITY_FOUNDATION_VALIDATION.md). PC oficial y backend de presentación pendientes.

En este host Flatpak, ejecutar batch dentro del entorno del Hub; el arranque directo anterior falló con 198. Las opciones locales que evitan la migración automática de toolchains se detallan en la guía; no son dependencias de todos los equipos. Cerrar editores del checkout antes de validar para evitar escrituras concurrentes.

Metadatos .meta de las fuentes y assets de foundation están incluidos en el cambio candidato. No incluir Library/Temp/Logs, credenciales ni assets comerciales. Render pipeline/plugin cámara deben validarse antes de adoptarse; no se instaló URP por defecto.

## Fronteras y responsables

| Ruta | Responsable v4 | Frontera |
|---|---|---|
| Unity/ | Arturo | Gameplay autoritativo PC; consume PlayerInput |
| Server/ | Hiram | Host local Java, conexiones futuras, publicación de PWA |
| PWA/ | Hiram + Saul | Captura futura/UI personal React; no calcula score ni ganador |
| Shared/Protocol/ | Hiram + Arturo | Fixtures JSON, versión y acuerdos interoperables |
| Shared/CSharp/ | Arturo | DTOs independientes del motor; no cargar Java ni net10 DLL en Unity |
| Unity/.../CameraTracking/ | Austin | Adaptador MediaPipe/OpenCV local; produce CameraInput |
| Unity/.../Art y Audio/ | Saul | Arte/audio propios, licencias y UI |
| docs/ y Project | Josue + Erik | Alcance/aceptación y seguimiento |

Spring DI nativa en Java; composición explícita en Unity. Módulos del mismo producto, sin servicios remotos ni lógica duplicada del torneo. El starter WebSocket está disponible, pero no hay handler de partida. El IPC/ciclo de vida Unity↔Java y la confirmación del resultado siguen abiertos; el DTO no decide esa autoridad.

## Protocolo

Shared/Protocol/fixtures/sensor.json procede de v4 §8 y lo consumen tests Java/JS/Unity. Los tipos C#/Java usan escalares, nunca tipos de un motor ajeno. Java usa Jackson incluido en Spring. Shared no es una librería ejecutable común a Java/C#; el contrato interoperable es JSON. No generar/copiar DLLs a mano. No confundir el ejemplo type:sensor con una resolución de todos los tipos mayúscula/minúscula: revisar esta discrepancia antes del handler WebSocket. Timestamp/unidades y validación de frontera se acuerdan durante el Spike.

## Ramas y entrega

Crear feature/mobile-..., feature/gameplay-..., feature/camera-..., feature/ui-... o fix/... desde develop; no editar main/develop directamente. Actualizar develop antes de crear la rama. Los PR de tareas apuntan a develop y cumplen sus revisiones y controles.

Un check verde Java/web no valida Unity ni hardware. Las Issues permanecen abiertas; este arranque aporta parte de T003–T007, no cierra su Definition of Done. Revisar el PR con otro integrante antes de integrar.

## Pruebas humanas pendientes

- Unity: importación limpia, cero errores, tests EditMode/PlayMode y Player PC. Versionar defaults/lockfile generados revisados.
- PWA: instalar desde HTTPS confiable en Android/iPhone; revisar layout, activación, actualización y shell sin Internet. El service worker sólo guarda recursos de interfaz; /api nunca se cachea.
- Spike siguiente dentro de Fase 0: QR, WebSocket, permisos, sensores, cámara, integración Unity/Java, métricas físicas. No pasar a Gorilla Smash completo sin evidencia y decisión de alcance.

## Referencias revisadas

- [comp_alumno](https://github.com/alfonsojbarroso/comp_alumno/blob/master/pom.xml): Java 21, Spring Boot 4.1.1 y WebFlux. Se conserva una ruta Web MVC más pequeña para el host local.
- [base-alumno](https://github.com/alfonsojbarroso/base-alumno): Java/Spring; no se adopta su base de datos.
- [pwa-alumno](https://github.com/alfonsojbarroso/pwa-alumno): ramas develop/master/feat/serviceworker/feat/fetch_company consultadas; HTML/JS/SW, sin React. React proviene de la solicitud del usuario.
- [Spring Boot](https://spring.io/projects/spring-boot) y [React/Vite](https://react.dev/learn/build-a-react-app-from-scratch). No se copió código de los repos de alumno.

## Checklist antes de compartir una tarea

1. Clonar la rama base disponible y confirmar versiones de JDK/Node/Unity.
2. Restaurar dependencias, generar PWA y ejecutar Maven verify; validar documentos y foundation.
3. Para Unity, importar con licencia y ejecutar tests/build; registrar cualquier bloqueo antes de declarar Ready/Done.
4. Acordar Issue, dueño, dependencia y aceptación con el equipo. Crear rama de tarea pequeña; no editar main/develop directamente.
5. Abrir PR con evidencia real y revisión de otro integrante. No confundir base operativa Java/web con Spike o minijuego completo.
