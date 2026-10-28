# Continuar Gorilla Escape en Windows

Estado de traspaso: 28 de octubre de 2026. El trabajo reciente está en la rama `feature/saul-jungle-party-alpha`; `main` y `develop` no equivalen a esa rama. El SHA exacto se obtiene con `git rev-parse HEAD` después de clonarla. GitHub Project 1 es independiente del checkout y permanece en la cuenta del propietario.

## Descargar el proyecto

Instalar Git y Git LFS, iniciar sesión en GitHub y ejecutar en PowerShell:

```powershell
git lfs install
git clone --branch feature/saul-jungle-party-alpha https://github.com/Josue1855/Gorilla-Escape.git
cd Gorilla-Escape
git lfs pull
git status -sb
git rev-parse HEAD
```

Comprobar que Git LFS descargó los archivos reales: las imágenes, FBX y otros assets del repositorio lo necesitan. Las carpetas `Unity/Library`, `Unity/Builds`, `PWA/node_modules`, `PWA/dist` y `Server/target` son locales y no se recuperan de Git. Unity regenerará Library; web y Java deben compilarse.

## Herramientas y arranque de desarrollo

- Unity Hub y **Unity 6000.6.0f1**, la versión indicada en `Unity/ProjectSettings/ProjectVersion.txt`; abrir la carpeta `Unity`, no la raíz. Abrir el proyecto con toda la carpeta `Shared` presente.
- JDK 21, Node.js 24 y Python 3 instalados para Windows. La primera descarga de paquetes requiere Internet. En PowerShell, desde la raíz:

```powershell
node --version
java -version
python --version
npm --prefix PWA ci
npm --prefix PWA test
npm --prefix PWA run build
.\Server\mvnw.cmd -f .\Server\pom.xml test
.\Server\mvnw.cmd -f .\Server\pom.xml -DskipTests package
python tools\validate_foundation.py
```

Las pruebas que necesiten cámara, sistema de audio, red, navegador o herramientas específicas pueden requerir configuración adicional; registrar fallos y omitidas. Después abrir Unity y comprobar importación/compilación. El menú **Gorilla Escape → Play Gorilla Escape** existe, pero su funcionamiento integrado en Windows no está validado.

## No confundir la copia del código con una versión Windows lista

- El paquete `Unity/Builds/FoundationLinux` se ignora en Git y contiene componentes de Linux. Su `Vision` utiliza V4L2 y Python de Linux; copiarlo a Windows no proporciona soporte de cámara para Windows. Preparar e integrar un runtime Windows y probarlo antes de usar seguimiento.
- El ejecutable Cloudflared preparado y su SHA fijado en el supervisor corresponden a Linux; se necesita soporte específico para un ejecutable Windows verificado y probar el arranque y recuperación. No se debe desactivar la verificación para hacerlo funcionar.
- El servidor del repositorio y la PWA se compilan por separado; el backend adicional de jugadores, si se necesita, está en `Josue1855/Gorilla-Escape-Backend` y no se incluye en este clon. El paquete Linux `backend.jar` es generado y no está en Git.
- El enrutador `play.gorillas.run` depende de Cloudflare y de una credencial privada de registro que **no está en Git**. Conservarla solo mediante un medio privado seguro o reprovisionarla en Cloudflare; nunca compartirla en el chat, en un commit ni en una captura. Consulte `tools/cloudflare-worker/README.md`. Tener la credencial no resuelve las incompatibilidades Windows anteriores.
- Ningún cambio registrado aquí acredita un build Windows, una prueba de teléfono físico o un instalador listo.

## Contexto para una nueva conversación de Codex/ChatGPT

Indicarle al nuevo asistente que lea este archivo, `docs/04-testing/FASE1_COCONUT_SLICE.md`, `docs/03-development/ROOM_ROUTER_VALIDATION.md`, `Unity/README.md` y el [Project 1](https://github.com/users/Josue1855/projects/1). El alcance actual indicado por Josué es: Coconut Slice, Bowling, Desafío del Tronco y Whac-a-Mole; Boxing, Baseball y Drums son demos. Los requisitos académicos históricos de Smash/Throw siguen pendientes de conciliación.

La cámara muestra ayuda visual local en pausa; la recuperación manual requiere nuevas muestras. El túnel tuvo dos pruebas reales correctas en Linux tras mejorar la resolución DNS, pero aún falta validación física y prolongada. La fase 0 y E1 siguen abiertas. No presentar pruebas emuladas como pruebas físicas.

Instrucciones del usuario para el nuevo chat: usar las habilidades gstack instaladas cuando correspondan; **antes de cada uso de REA, solicitar y esperar autorización explícita**, incluso para diagnosis o setup. No pegar credenciales, tokens ni logs privados. El historial de una conversación no se incluye automáticamente en un clon Git; conservar este documento y, si se desea el historial completo, usar las funciones de la aplicación para guardarlo o compartirlo.
