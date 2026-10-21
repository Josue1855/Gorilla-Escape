# Jungle Party / Gorilla Escape — alpha de Josue

> Estado: **alpha muy temprana**. Esta carpeta es una captura ejecutable del prototipo y no reemplaza la arquitectura oficial de `Unity/`, `Server/` y `PWA/`. Debe revisarse antes de integrar cualquier parte a `develop`.

Este módulo contiene el proyecto Unity actual con Banana Bowling, Gorilla Boxing, Jungle Baseball y Jungle Drums, además del rastreador corporal local y el control del celular publicado mediante un túnel temporal de Cloudflare.

## Requisitos (Windows)

- Unity Hub y Unity **6000.6.0f1**.
- Python **3.12 de 64 bits**, disponible como `py` o `python`.
- Node.js **20 o superior**, disponible como `node`.
- Webcam.
- Internet para instalar Python y descargar/iniciar Cloudflare Quick Tunnel.
- Celular con navegador y sensores de movimiento.

No se versionan `Library`, builds, el entorno virtual de Python ni `cloudflared.exe`; son archivos generados o dependencias descargables.

## Preparación inicial

1. Ejecutar `PREPARAR_ALPHA.cmd`. Crea un entorno Python local, instala OpenCV/MediaPipe y descarga Cloudflare Tunnel.
2. Abrir en Unity Hub la carpeta `Prototypes/JosueAlpha/Unity` con Unity 6000.6.0f1.
3. Abrir `Assets/Scenes/Connection.unity` o agregar a Build Settings las escenas que aparecen en `ProjectSettings/EditorBuildSettings.asset`.

## Cada vez que se juega

1. Ejecutar `INICIAR_SERVICIOS.cmd`. Se abren dos ventanas:
   - **Camera Tracker**, local en `http://127.0.0.1:8790/pose`.
   - **MotionLink + Cloudflare**, local en el puerto `8788` y con una dirección temporal `https://...trycloudflare.com`.
2. Copiar la dirección de Cloudflare que aparece en la ventana de MotionLink.
3. Abrir el proyecto en Unity y pulsar Play.
4. En el celular, abrir la dirección de Cloudflare, escribir el código de cuatro caracteres mostrado por Unity, activar el control y aceptar los permisos de movimiento.

Los dos servicios deben permanecer abiertos mientras se juega. El túnel gratuito cambia de dirección al reiniciarse y envía únicamente los paquetes de movimiento del control; la webcam se procesa localmente y no se publica por Cloudflare.

## Alcance y riesgos del alpha

- Probado como prototipo local de Windows, no como distribución final.
- Usa un Quick Tunnel de Cloudflare, por lo que requiere Internet y no cumple todavía el objetivo final de juego sin Internet.
- Unity 6000.6.0f1 difiere de la versión 6000.3.23f1 fijada actualmente por el repositorio principal.
- El relay Node es provisional; la arquitectura del equipo define Java/Spring Boot + React/WebSocket como destino.
- Requiere pruebas reales en otra computadora, otro teléfono y distintas redes antes de considerarse portable.
- Los recursos visuales y modelos deben pasar una revisión de licencia antes de una publicación pública o comercial.

## Qué conviene reutilizar

La rama sirve como evidencia y banco de integración. Los elementos más valiosos para migrar gradualmente son las escenas y controladores de los minijuegos, el contrato `PosePacket`, la experiencia de conexión y las pruebas físicas. El relay y el sondeo HTTP deben tratarse como adaptadores temporales, no como la arquitectura final.
