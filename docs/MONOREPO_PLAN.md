# Gorilla Escape — Arquitectura del monorepo

**Actualizado:** 2026-10-05. **Estado:** base de Fase 0 compartida en main/develop; Unity pendiente de validación con licencia.
**Fuente normativa:** [v4 enmendada](Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md). **Cambio aprobado:** [DEC-002](DECISIONS.md) sustituye ASP.NET Core por Java/Spring Boot y añade React.

## Una base compartida

Todos los proyectos propios permanecen aquí. Unity conserva gameplay, física y presentación de PC; Java publica el control y prepara la frontera de red; React muestra el control móvil. Son componentes de un mismo producto local con arquitectura Modular Monolith, aunque Unity y JVM sean runtimes distintos. No hay repositorios anidados, microservicios, cuentas, base de datos ni cloud obligatorios.

La PC conserva autoridad. El teléfono solicita acciones y captura input; nunca calcula resultado oficial. Cámara produce observaciones locales sin guardar video/fotos ni reconocer personas. Los minijuegos consumirán PlayerInput normalizado, sin conocer React, Spring, WebSocket, MediaPipe u OpenCV.

## Inventario real

```text
Gorilla-Escape/
├── README.md / CONTRIBUTING.md / .editorconfig / .gitattributes / .gitignore
├── Unity/
│   ├── Assets/_Project/
│   │   ├── Art/ y Audio/              guías de incorporación de assets propios
│   │   ├── Editor/                    preparación explícita de escena Bootstrap
│   │   ├── Scripts/Core/              Bootstrap y ApplicationSettings
│   │   ├── Scripts/Input/             contratos internos de sección 9
│   │   ├── Scripts/Network/           guía del adaptador pendiente
│   │   ├── Scripts/CameraTracking/    guía del adaptador pendiente
│   │   ├── Scenes/Bootstrap/          destino de escena por generar en editor
│   │   ├── Settings/                  destino de tuning ScriptableObject
│   │   └── Tests/                     EditMode y PlayMode preparados
│   ├── Packages/manifest.json
│   └── ProjectSettings/              versión y serialización de editor
├── Server/
│   ├── pom.xml / mvnw / mvnw.cmd / .mvn/wrapper/
│   ├── src/main/java/com/gorillaescape/server/
│   │   ├── GorillaEscapeApplication.java
│   │   ├── hosting/                   endpoint HTTP de disponibilidad
│   │   └── protocol/                  versión y envelope de red
│   ├── src/main/resources/           configuración del host local
│   └── src/test/java/                contrato JSON e integración HTTP
├── PWA/
│   ├── package.json / package-lock.json / vite.config.js
│   ├── src/                          React, estilos y health check
│   ├── public/                       manifest e iconos propios
│   ├── tests/                        contrato y errores de conexión
│   └── tools/                        generación de service worker en build
├── Shared/
│   ├── Protocol/fixtures/            JSON normativo compartido
│   └── CSharp/                       paquete local UPM de DTOs puros
├── tools/                            validación y gestión GitHub
├── docs/                             requisitos, arranque, decisiones y evidencias
└── .github/                          formularios, plantilla PR y CI
```

Este árbol describe archivos existentes. Bootstrap.unity, ApplicationSettings.asset y packages-lock.json todavía no existen: se generan/revisan en Unity con licencia. No se crean carpetas vacías de futuros minijuegos para aparentar implementación. La distribución completa prevista de Art/Audio/Scenes y futuros módulos sigue en v4 §4 y se incorpora al trabajar su fase.

## Versiones y dependencias

| Área | Base actual | Origen verificable |
|---|---|---|
| Unity | 6000.3.23f1 LTS, C# | Unity/ProjectSettings/ProjectVersion.txt |
| Test Runner Unity | 1.6.0 core (DEC-003) | Unity/Packages/manifest.json |
| Java | JDK/target 21, Spring Boot 4.1.1, Web MVC + WebSocket | Server/pom.xml |
| Maven | 3.9.11, wrapper 3.3.2, SHA-256 de distribución | Server/.mvn/wrapper/maven-wrapper.properties |
| React | React/React DOM 19.3.0 | PWA/package.json y lockfile |
| Build web | Vite 8.3.2, plugin React 6.1.2, Node 24 | PWA/package.json y lockfile |
| JSON Java | Jackson de Spring Boot | dependencias administradas por parent Maven |
| Cámara | MediaPipe + OpenCV requeridos; versiones/adaptador pendientes | Spike de compatibilidad con PC oficial |

No usar latest en archivos del proyecto ni actualizar versiones unilateralmente. El lockfile web, parent Maven y wrapper son parte del commit. Las descargas de desarrollo no deben convertirse en descargas durante gameplay.

## Fronteras de dependencia

| Componente | Responsabilidad | Puede consumir | No debe decidir |
|---|---|---|---|
| Unity/Core y futuros minijuegos | ciclo visual/física/gameplay | input normalizado, tuning e interfaces | detalles de teléfono/cámara/red en minijuegos |
| Unity/Network | conversión futura de JSON a contratos internos | DTOs Shared/CSharp | un resultado oficial separado del gameplay |
| Unity/CameraTracking | observaciones locales futuras | MediaPipe/OpenCV detrás de adaptador | identificación facial o scoring final |
| Server | host Java local, recursos web, futuras conexiones | JSON/fixtures, servicios Spring locales | un segundo torneo o scoring duplicado |
| PWA | UI React y futuras solicitudes/sensores | protocolo JSON del host | score, ganador o resultado oficial |
| Shared | contrato interoperable y DTOs puros | tipos escalares | transporte, hardware o servicios de aplicación |

La fixture JSON es la referencia interoperable. Java no comparte una DLL con Unity. Shared/CSharp se carga como paquete UPM local, sin UnityEngine, ASP.NET ni Spring. Las transformaciones a Vector3/Quaternion viven en adaptadores Unity. No copiar DTOs C# a Server ni depender del runtime Java desde Unity.

Spring usa DI nativa; Unity compone dependencias explícitamente. No introducir contenedores extra, singletons generales ni dependencias circulares por anticipado. Separar un assembly sólo cuando haya una frontera o tests reales; no crear uno por clase.

## Desarrollo y ejecución

[TEAM_START](TEAM_START.md) contiene comandos para clonar, restaurar, verificar y arrancar en Windows/Linux/macOS. Construir PWA primero y después Server: Maven incorpora PWA/dist al JAR. No editar salidas ni mantener dos fuentes del frontend. JAR serve HTTP y React desde el mismo origen; el check /api/health sólo demuestra disponibilidad del host y versión v1.

En desarrollo Vite proxy /api a Java:8080. En distribución no se requiere Vite ni un servidor Node. Empaquetar JRE con el Player Unity y coordinar arranque/cierre sigue pendiente; el JAR actual no es una distribución final del juego. El host escucha localhost por defecto. HTTPS/WSS confiable y acceso LAN/sensores deben validarse antes de conectar teléfonos.

El service worker de producción guarda los recursos de interfaz y excluye /api. Su cache cambia con el contenido generado. El shell funciona desde caché en la prueba de escritorio; eso no valida instalación/actualización Android/iPhone ni conexión del juego.

## Git, assets y validación

Mantener fuentes, .meta, Packages y ProjectSettings. Ignorar Library/Temp/Logs, target, node_modules y dist. Preservar GUIDs; revisar escenas/prefabs en editor ante conflictos. .gitattributes fija texto y finales de línea; no hay LFS configurado sin inventario real de binarios grandes. No publicar assets comerciales sin licencia ni claves/certificados.

CI real: management-validation verifica documentos/backlog; java-react-foundation restaura, ejecuta tests web, compila React, ejecuta Maven verify y valida wiring/metadatos. Ambos pasan para la base publicada. No hay job Unity fingido: import/build/EditMode/PlayMode quedan pendientes de licencia y no se reemplazan por un validador de archivos.

[WORKFLOW](WORKFLOW.md) define ramas/PR; [TEAM_WORKFLOW](github/TEAM_WORKFLOW.md) define seguimiento. main/develop tienen PR/aprobación, check management-validation y protección contra force push/deletion. Los checks management-validation y java-react-foundation verifican la base compartida.

## Pendientes antes de completar Fase 0

- Unity con licencia: import limpio, metadatos/defaults/lock revisados, escena/settings, pruebas y Player PC.
- Acuerdo Unity↔Java: IPC, ciclo de vida, recuperación y responsable único del resultado (ambigüedad v4 §5 frente a §3).
- Protocolo: unidades/reloj y convención de tipos sensor/SENSOR antes de handlers.
- HTTPS/WSS, QR, sensores/reconexión y cámara en hardware oficial.
- Compatibilidad nativa MediaPipe/OpenCV y recursos/modelos disponibles localmente.
- Medir latencia/FPS/Player Lock; no declarar objetivos alcanzados sin evidencia.

Esto es base de arranque, no Spike completo ni autorización para Fase 1. El antecedente técnico previo al cambio Java/React se conserva en [TECHNOLOGY_RESEARCH](TECHNOLOGY_RESEARCH.md) y el historial Git.
