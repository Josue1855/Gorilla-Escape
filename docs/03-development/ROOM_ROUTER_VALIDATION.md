# Enrutador de salas: validación y límites

Validación de desarrollo del 27 de octubre de 2026:

- `PublicRoomRtcLiveTest`: Quick Tunnel real, registro en el Worker publicado, página pública, admisión de un solo uso y canales WebRTC abiertos desde Chrome en esta PC. Es laboratorio con entrada sintética; no acredita un teléfono físico ni una partida completa.
- `ConnectivityTest`: la caída del túnel revoca admisiones pendientes, sustituye el QR por el de Cabaña y conserva los jugadores ya conectados. Tres fallos de renovación de sala marcan el túnel como fallido para que AUTO vuelva a Cabaña.
- La sala se renueva cada 30 minutos y su reserva caduca tras tres horas sin renovación. El registro requiere el secreto configurado por el operador; no incluirlo en el juego distribuido.
- El código de cuatro caracteres identifica la sala, pero escribir la URL no concede una admisión. Para entrar es necesario el QR con token de un solo uso. El emparejamiento manual requeriría un mecanismo de confirmación separado.
- El dominio y la negociación se probaron en este entorno. La conexión física de teléfonos, la partida en Unity y una distribución para otras PCs aún no están certificadas. El ejecutable Unity necesita recompilarse; el modo automatizado de Unity de esta PC rechazó la licencia.
# Comprobación de DNS de Quick Tunnel — 2026-10-27

En esta red, un nombre Quick Tunnel recién registrado devolvió NXDOMAIN desde el DNS local durante al menos 50 s mientras el DNS público ya devolvía registros A. El sondeo HTTPS agotó inicialmente su plazo. Se añadió una recuperación limitada a nombres `trycloudflare.com`: consulta DNS pública mediante HTTPS y conexión HTTPS al IP devuelto con SNI y verificación del nombre y certificado originales; no modifica el DNS del sistema. Si la recuperación falla se mantiene el fallo/fallback existente.

Dos ejecuciones posteriores de `PublicRoomRtcLiveTest` completaron el recorrido del navegador, sala pública y WebRTC; estas pruebas no demuestran disponibilidad sostenida ni uso desde un teléfono físico. Pasaron también las pruebas de DNS, conectividad, recuperación y HTTPS, y los tests del router y de la PWA. Una primera batería completa de servidor falló por una prueba de assets que esperaba rutas absolutas; tras corregirla, la batería completa pasó 144 pruebas, con 8 omitidas (136 ejecutadas). El cambio todavía requiere integración y validación física; ver issue #132.
