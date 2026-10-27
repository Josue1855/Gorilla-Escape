# Presentación móvil de los siete juegos — versión 2

Unity conserva la autoridad de simulación y resultados. Publica `payload.gameContext` por PING IPC autenticado, después de negociar PhoneInput. Java valida la extensión, la instancia, conexión y secuencia antes de aplicarla; la devuelve en el ACK de HEARTBEAT del peer. No aparece en cada muestra de sensores ni autoriza acciones.

El contexto contiene exactamente `version:2`, `gameId`, `phase`, `playerId` y `stats`. Juegos: `lobby`, `bowling`, `boxing`, `baseball`, `drums`, `coconut-slice`, `whack`, `log-balance`. Fases: `Ready`, `Countdown`, `Playing`, `Paused`, `Won`, `Lost`, `Finished`. Jugador 0 exclusivamente para lobby; 1–4 en un juego. El alpha utiliza jugador 1.

Stats contiene exactamente cinco pares bandera/valor: `hasScore`/`score` (0–1e9), `hasProgress`/`progress` (0–1), `hasElapsed`/`elapsed` (0–3600 s), `hasBalance`/`balance` (-1–1), `hasDistance`/`distance` (0–10000 m). Las banderas son booleanas, los valores numéricos finitos. Una bandera falsa significa que no se presenta esa métrica; cero no implica una medición disponible. No se deriva puntuación, progreso o equilibrio de sensores locales.

Unity publica a máximo 5 Hz y Java conserva el contexto hasta 3 s sin actualización. Los clientes lo descartan después de 2,5 s sin ACK nuevo. HEARTBEAT sigue siendo control fiable, a 5 Hz; motion continúa sin orden ni retransmisión, a máximo 50 Hz. Contextos desconocidos o caducados muestran el lobby, nunca una fase inventada. Otros jugadores reciben instrucciones de espera.

Se conserva lectura de la presentación v1 del tronco para compatibilidad con las fuentes recuperadas; las nuevas escenas emiten exclusivamente v2. Esta extensión no modifica el sobre base de sensores ni los contratos de acciones.

## Perfil opcional persistente

POST `/mobile/profile` recibe `peerId`, `resumeToken` y `displayName` sólo después de HELLO de un peer conectado. La validación limita nombre a 40 caracteres, normaliza NFC y admite letras/marcas/números, espacios y `_.'-`. La fachada llama al backend SQLite privado en loopback; `/api/players` no se expone por el túnel. El nombre no aparece en contexto de juego ni diagnósticos.

No se reintenta automáticamente un POST incierto. Un perfil confirmado se conserva para ese peer. Es registro local, no login ni recuperación de cuenta. El UUID de SQLite es distinto del slot 1–4. El registro es opcional: si falta o falla el backend se puede seguir jugando.
