# Preparación y ejecución verificable

Última revisión: 2026-10-26; reconciliación con las instrucciones de develop del 2026-10-20. Instrucciones del monorepo `develop`, contrastadas con manifiestos, scripts y menús del editor. Los resultados previos se conservan en el [índice de evidencias](../04-testing/EVIDENCE_INDEX.md); ejecutar un comando no acredita automáticamente su resultado.

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

Estos componentes no se arrancan desde `Server/` ni reemplazan al servicio técnico de Unity. Su integración dentro del instalador/supervisor del juego sigue pendiente.

### PWA de laboratorio

En [Josue1855/Dise-o_pwa](https://github.com/Josue1855/Dise-o_pwa), el laboratorio ya está integrado en `develop` mediante el [PR #7](https://github.com/Josue1855/Dise-o_pwa/pull/7). Con Node 24, desde su checkout:

```sh
npm ci
npm test
npm run build
npm run lab
```

Abrir http://127.0.0.1:4173/lab.html con el backend en loopback8090. El build genera SW y recursos compilados; el servidor de laboratorio sólo escucha loopback y actúa como proxy. La recarga offline requiere una visita previa con recursos guardados. HTTP LAN no garantiza HTTPS/permisos móviles. El upstream `Quintana-117/Dise-o_pwa` sigue sólo con main y acceso READ; no confundirlo con el fork integrado. /index.html conserva el mando demo, sin integración RTC/Unity.

### Backend de jugadores

En [Gorilla-Escape-Backend](https://github.com/Josue1855/Gorilla-Escape-Backend), el [PR #14](https://github.com/Josue1855/Gorilla-Escape-Backend/pull/14) ya está integrado en `develop`. Con JDK21, desde ese checkout:

```sh
./mvnw -B -ntp verify
java -jar target/backend-0.1.0-SNAPSHOT.jar --gorilla.storage.directory=/ruta/aislada/laboratorio
```

En Windows usar `mvnw.cmd`. La ruta es un ejemplo para un directorio de laboratorio propio; no reutilizar datos reales. Escucha127.0.0.1:8090 por defecto, con SQLite/JDBC y migración local. Los datos quedan fuera del JAR; no se necesita instalar un servidor SQL. La primera restauración requiere red; ejecutar el JAR compilado no descarga componentes. [Dossier y scripts SQL](https://github.com/Josue1855/Gorilla-Escape-Backend/tree/c83b8343f1e37c405b127304567e149a1416eddb/docs/laboratorio). El instalador final con JRE sigue pendiente. Flutter aún no tiene proyecto en esta base.
