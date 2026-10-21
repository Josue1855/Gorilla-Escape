# Presentación de la prueba · extensión opcional v1

Unity es la autoridad de la prueba. El PING IPC autenticado puede incluir `payload.gameContext` después de negociar PhoneInput; el servidor extrae la extensión antes de validar el sobre base y sólo la aplica después de validar instancia, conexión y secuencia.

```json
{"version":1,"gameId":"log-balance","phase":"Crossing","playerId":1}
```

Exactamente cuatro campos. `gameId`: `lobby` o `log-balance`. `phase`: `Ready`, `Countdown`, `Crossing`, `Paused`, `Won`, `Fell`. `playerId`: entero 0–4; cero representa el lobby. No incluye nombres, puntuaciones, admisiones ni datos de sensores.

Java adjunta el contexto al ACK del HEARTBEAT del mismo peer autenticado; no lo envía en cada muestra de movimiento. Caduca tras tres segundos sin actualización IPC. La PWA descarta una presentación sin renovar después de 2,5 segundos. Un contexto desconocido o ausente muestra el lobby. Sólo el jugador seleccionado ve instrucciones de acción; los demás esperan turno.

Es información de presentación, no un resultado persistente ni una nueva autoridad de simulación. El dibujo de inclinación sigue representando el movimiento local del teléfono; el equilibrio real y la decisión de victoria permanecen en Unity.

## Registro opcional

POST `/mobile/profile` acepta `peerId`, `resumeToken` y `displayName` únicamente para un peer conectado que haya confirmado HELLO. Funciona en el origen HTTPS local o a través del ingreso público autorizado; `/api/players` permanece privado en loopback. El servidor de jugadores se inicia como proceso hijo del Java técnico desde `Java/backend.jar` y usa un puerto efímero en 127.0.0.1.

La fachada crea un jugador en SQLite usando el contrato existente. No reintenta POST ante un resultado incierto. Un registro confirmado se conserva en memoria para ese peer; no es inicio de sesión, recuperación de perfil ni publicación de puntuaciones. El UUID persistente del backend es distinto del slot 1–4 de la sesión de juego. La partida puede continuar si el registro no está disponible.
