# Jungle Party / Gorilla Escape — módulo alpha

> Estado: **alpha muy temprana**. Esta carpeta es una captura ejecutable del prototipo y no reemplaza la arquitectura oficial de `Unity/`, `Server/` y `PWA/`. Debe revisarse antes de integrar cualquier parte a `develop`.

Este módulo contiene el proyecto Unity actual con Banana Bowling, Gorilla Boxing, Jungle Baseball y Jungle Drums, además del rastreador corporal local y el control del celular publicado mediante un túnel temporal de Cloudflare.

## Requisitos (Windows)

- Unity Hub y Unity **6000.6.0f1**.
- Webcam.
- Internet en el primer inicio para que Unity prepare Python, MediaPipe y Cloudflare.
- Celular con navegador y sensores de movimiento.

No se versionan `Library`, builds, el runtime local de Python, su entorno virtual ni `cloudflared.exe`; Unity los prepara como dependencias internas.

## Preparación inicial

1. Abrir en Unity Hub la carpeta `Prototypes/JosueAlpha/Unity` con Unity 6000.6.0f1.
2. Abrir `Assets/Scenes/Connection.unity` o agregar a Build Settings las escenas que aparecen en `ProjectSettings/EditorBuildSettings.asset`.
3. Pulsar Play. Unity prepara las dependencias silenciosamente si todavía no existen; el primer inicio puede tardar varios minutos.

## Cada vez que se juega

1. Abrir el proyecto en Unity y pulsar Play desde la escena `Connection`.
2. El módulo inicia automáticamente el rastreo, el control móvil y Cloudflare en segundo plano.
3. Escanear el QR mostrado por el juego, o pulsar **COPIAR ENLACE DEL CELULAR**.
4. En el celular, escribir el código de cuatro caracteres mostrado por Unity, activar el control y aceptar los permisos de movimiento.

Unity administra el ciclo de vida completo sin abrir consolas. El túnel gratuito cambia de dirección al reiniciarse y envía únicamente los paquetes de movimiento del control; la webcam se procesa localmente y no se publica por Cloudflare.

## Integración en otro proyecto

La carpeta `Unity/Assets/PortableModule` contiene los contratos y el anfitrión de procesos. Los minijuegos consumen `IPortableInputSource`, por lo que pueden trasladarse sin depender directamente de HTTP, MediaPipe o Cloudflare. Consulta su `README.md` para la disposición de archivos y la variable `JUNGLE_PARTY_MODULE_ROOT`.

## Alcance y riesgos del alpha

- Probado como prototipo local de Windows, no como distribución final.
- Usa un Quick Tunnel de Cloudflare, por lo que requiere Internet y no cumple todavía el objetivo final de juego sin Internet.
- Unity 6000.6.0f1 difiere de la versión 6000.3.23f1 fijada actualmente por el repositorio principal.
- El transporte HTTP integrado sigue siendo provisional; la arquitectura del equipo define Java/Spring Boot + React/WebSocket como destino.
- Requiere pruebas reales en otra computadora, otro teléfono y distintas redes antes de considerarse portable.
- Los recursos visuales y modelos deben pasar una revisión de licencia antes de una publicación pública o comercial.

## Qué conviene reutilizar

La rama sirve como evidencia y banco de integración. Los elementos más valiosos para migrar gradualmente son las escenas y controladores de los minijuegos, el contrato `PosePacket`, la experiencia de conexión y las pruebas físicas. El transporte y el sondeo HTTP deben tratarse como adaptadores temporales, no como la arquitectura final.
