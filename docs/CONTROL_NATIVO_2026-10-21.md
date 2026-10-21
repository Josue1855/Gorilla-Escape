# Control nativo Flutter

El escáner de Companion abre una ruta Flutter y conserva la admisión sólo en memoria. Se retiró `url_launcher`; el control no delega en Safari, Chrome ni una WebView. La PWA conserva su acceso independiente para personas sin la app.

## Integración

- WebRTC sin STUN/TURN, con `control` fiable y `motion` sin orden ni retransmisiones. Un adaptador conserva `maxRetransmits: 0`, que el serializador del plugin omite.
- Admisión de un uso, HELLO, heartbeat, ACK y reanudación autenticada. No se reintenta la admisión ni un guardado de perfil incierto. Los diagnósticos sólo incluyen estado y contadores.
- Rotación fusionada nativa, acelerómetros y giroscopio. Conversión al orden Z-X-Y del protocolo y velocidades en grados por segundo. Grupos ausentes declarados no disponibles.
- Emisión máxima de 50 Hz, orientación con caducidad de 350 ms y tres muestras nuevas antes de activar movimiento. Al abandonar la app se cancelan las suscripciones y se vacía el estado anterior.
- Pantalla despierta durante el control activo, posición vertical, activación explícita, calibración en Unity, reconexión y turno/fase comunicados por Unity. La figura móvil muestra inclinación local, sin inventar resultados ni equilibrio del juego.
- Nombre opcional mediante el endpoint autorizado de perfiles, sin acceso directo a la base de datos.
- Permisos iOS de cámara, movimiento y red local. Se conserva el icono y sus recursos explícitos para XCross.

## Validación

Ocho pruebas Flutter aprobadas: enlace, pantalla inicial, conversión de orientación en ambos hemisferios, caducidad y limpieza de muestras, ausencia de retransmisiones, aislamiento de turno/contexto, modo de laboratorio explícito y petición HTTP con longitud UTF-8 exacta y sin redirecciones. `flutter analyze`: sin incidencias. APK debug y compilación iOS release con XCross aprobados.

`tools/integration/android_native.py` verifica Flutter contra el Player Unity y Java reales, con sensores virtuales y datos temporales. HTTP de laboratorio sólo funciona en debug con `GORILLA_EMULATOR_LAB=true` y host 127.0.0.1. No acredita TLS/LAN física ni movimiento del iPhone.

Integración en Android API 36 aprobada: HELLO WebRTC, sensores y regreso automático de Unity a la prueba, contexto del tronco, segundo plano/regreso y ACK de movimiento. Se observaron 767 ACK y 705 muestras aceptadas por Unity, sin procesos Java residuales. El renderizado de software de este laboratorio no acredita rendimiento del juego.

Resultados del emulador: `docs/evidence/native-controller-2026-10-21/native-results.json`. La aceptación física exige escanear un QR nuevo dentro de la app, activar sensores y calibrar en la PC; instalar por USB no acredita ese recorrido.

No se han realizado merges ni publicado cambios.

## iPhone físico

Versión 1.1.0 (2) compilada y firmada con XCross, instalada por USB y abierta con el depurador. Registro de instalación confirmado en el iPhone 15; los cuatro iconos principales y los permisos de movimiento/red local están incluidos. Una operación de actualización de iOS quedó bloqueada; la instalación limpia permitió terminar. No se acredita todavía una partida física completa con escaneo y movimiento real.
