# Preparación y ejecución verificable

Última revisión: 2026-10-20. Instrucciones del monorepo `develop`, contrastadas con manifiestos, scripts y menús del editor. Los resultados previos se conservan en el [índice de evidencias](../04-testing/EVIDENCE_INDEX.md); ejecutar un comando no acredita automáticamente su resultado.

## Entorno y rama

Usar JDK 21, Node 24 y Unity 6000.3.23f1 con licencia válida. El wrapper Maven y los lockfiles fijan las dependencias. Crear una rama de tarea desde `develop` como indica [TEAM_START](../TEAM_START.md). Las primeras restauraciones requieren Internet.

Desde la raíz del repositorio:

```sh
npm --prefix PWA ci
npm --prefix PWA test
npm --prefix PWA run build
Server/mvnw -B -f Server/pom.xml verify
python3 tools/validate_foundation.py
python3 tools/github/validate_management.py
```

En Windows sustituir `Server/mvnw` por `Server\mvnw.cmd` y usar el ejecutable Python disponible. Las pruebas nativas WebRTC necesitan un entorno compatible; CI prepara un servicio de audio para ellas. Registrar fallos o dependencias ausentes sin omitir pruebas para declarar PASS.

## Desarrollo Java y web

Después del build web y de Maven:

```sh
java -jar Server/target/local-server-0.1.0-SNAPSHOT.jar
```

El JAR incorpora `PWA/dist`; abrir `http://127.0.0.1:8080` para desarrollo HTTP local. Para editar la interfaz, mantener Java iniciado y ejecutar en otra terminal:

```sh
npm --prefix PWA run dev
```

Vite escucha en localhost y envía `/api` al host Java. Este arranque sirve para desarrollo web/servicios; no sustituye la supervisión IPC de Unity ni acredita una admisión móvil. El modo health está disponible con `?view=health`. Reconstruir web y JAR para actualizar recursos empaquetados; no editar `Server/target` ni copiar manualmente el frontend.

## Unity y producto integrado

Abrir `Unity/` con el editor fijado. Usar **Gorilla Escape → Open Bootstrap** y **Play Gorilla Escape**. El menú **Create Foundation Scene** permite crear la base si falta; no regenerarla como paso habitual. Resolver el paquete local `Shared/CSharp` sin copiar DLL a mano.

Para controles físicos y runtime integrado, seguir [T020: preparación y empaquetado](../PHASE0_T020_CLEAN_PC_RUNBOOK.md) y el [contrato de conexión](../PHASE0_PHONE_CONNECTION_FINAL.md). Se requieren recursos/runtime preparados; el arranque Java aislado anterior no reproduce el producto integrado. No ejecutar el juego como root ni abrir firewall amplio.

La conexión normal usa QR JOIN, HTTPS y WebRTC DataChannel; HTTP LAN no garantiza permisos de sensores. La recuperación de confianza del teléfono es explícita. Probar iPhone/cuatro teléfonos corresponde a T019; probar una PC física separada corresponde a T020. Ambas aceptaciones siguen pendientes.

## Validación según el cambio

- Documentación: ejecutar `python3 tools/github/validate_management.py`, revisar enlaces/anclas y `git diff --check`.
- PWA/Java/Shared: ejecutar tests/build afectados y comprobar consumidores de cualquier contrato modificado.
- Unity: importar sin errores y ejecutar EditMode/PlayMode y build/Player aplicables. Un check de metadatos no sustituye al editor.
- Cámara/red/hardware: usar los runbooks y registrar dispositivo, entorno, revisión, logs y resultado real. Un fixture o emulador no acredita sensores físicos.

## Repositorios independientes

La PWA UI/UX externa tiene su propia preparación y estado, registrados en [repositorios](../00-product/REPOSITORIES.md). El build auditado no valida interacción, instalación ni integración RTC; no aplicar automáticamente los comandos del monorepo. El mantenedor debe preparar `develop` antes de iniciar tareas según la [política de ramas](GIT_WORKFLOW.md).

El backend separado tiene base documental; no hay aplicación, manifiesto ni comando de arranque acreditados por esta guía. Acordar stack, motor y entorno de datos antes de publicar instrucciones ejecutables. Flutter aún no tiene proyecto en esta base.
