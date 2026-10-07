# IPC probe v1 — Incremento 2A

Contrato interno aprobado por DEC-005; no es Gorilla Protocol móvil ni crea sesión de juego.

Transporte TCP IPv4 exclusivo 127.0.0.1, Java elige puerto 0, Unity cliente. Frame: uint32 big-endian (4 bytes) de longitud del JSON UTF-8 estricto; rango 1..4096 bytes. Lecturas parciales obligatorias; EOF intermedio invalida el frame. Un escritor y un PING pendiente por conexión.

Campos PING: ipcVersion entero 1, type PING, instanceId UUID canónico por lanzamiento, connectionId UUID canónico por conexión, sequence entero positivo que comienza en 1, payload. Primer payload contiene launchToken base64url sin padding (32 bytes aleatorios/43 caracteres); posteriores payload vacíos. PONG type PONG refleja identidades y sequence, payload vacío. Fixtures ping-first.json/pong.json son comunes a Java y C#; el token de fixture es público de prueba y nunca se usa por Unity en producción. El token real sólo existe en entorno del hijo/memoria y primer frame; no en argumentos, logs o evidencia.

Bootstrap: stdout emite una línea `GORILLA_IPC_READY ` + objeto ipcVersion/instanceId/pid/ipcPort/httpPort. Ambos listeners loopback, puerto real conocido sólo después de bind. Spring inicia normalmente y el modo administrado opt-in añade IPC al evento ApplicationReady. Configuración de logs específica ipc-logback.xml enruta Spring a stderr; los pipes se drenan desde inicio y stdout de readiness está limitado a 4096 caracteres. READY no basta: RUNNING requiere PONG correcto. El proceso se inicia con gorilla.ipc.managed=true; arranque HTTP normal sin esa opción conserva comportamiento previo.

Startup 15 s hasta PONG inicial, conexión <=2 s y dentro de startup, read/write <=2 s; cancellation cierra socket. Unity nunca espera I/O en el hilo de frames. Normal shutdown: supervisor cierra socket y stdin, Java detecta EOF/cierra contexto Spring y listener; Unity observa exit 0 <=10 s. Fallback sólo al hijo propio tras deadline, observado <=2 s; no cuenta como cierre limpio. No reconnect/restart/watchdog avanzado en 2A. Fallos muestran un error recuperable y no continúan al juego.

Desarrollo: configurar rutas absolutas GORILLA_IPC_JAVA y GORILLA_IPC_JAR al ejecutar Editor/Player. El JDK debe ser Java 21 completo y accesible al proceso (incluidos archivos de seguridad, no enlaces rotos en un sandbox). No buscar en PATH ni descargar al ejecutar. GORILLA_IPC_SMOKE_EXIT=1 ejecuta probe y solicita salida normal del Player después del cleanup. En tests PlayMode usar GORILLA_TEST_JAVA/GORILLA_TEST_JAR; si faltan, el test real se marca ignorado/NOT RUN, nunca PASS real. El runner no se activa por defecto ni modifica la escena foundation.

Smoke: 10 warmup +100 PING secuenciales con cadencia nominal suave 20 ms entre respuestas; no es benchmarking 50 Hz. Publicar startup, mínimo/P50/P95/máximo RTT (nearest-rank), N/errores/exit y versiones/hashes. RTT en worker incluye codec, TCP ida/vuelta y Java; no incluye consumo de gameplay en frame. No extrapolar a teléfono→gameplay.

Java valida campos/tipos/token/correlación y claves duplicadas. Unity valida tamaño/UTF-8, identidad/version/tipo/sequence/READY; revisión estricta de campos desconocidos/duplicados y fuzz/flood extensos del parser Unity quedan para hardening posterior. Los frames aceptados por Unity provienen del hijo identificado y no contienen acciones de juego. El contrato completo y requisitos diferidos permanecen en docs/PHASE0_UNITY_JAVA_DECISION.md.

## Incremento 2B — lifecycle administrado

Sin cambios al envelope, framing o DTO. Unity usa STOPPED, STARTING, CONNECTING,
RUNNING, STOPPING y FAILED. RUNNING requiere READY y primer PONG válido. En modo
sostenido, PING/PONG a 1 Hz como liveness: un request pendiente, un lector, deadline
PONG de 2 s. Cualquier fallo activo pasa por STOPPING/cleanup antes de FAILED.
Cero reconnect/restart automático; recuperación mediante acción explícita Unity,
hasta tres lanzamientos por ejecución, sólo con cleanup completo del intento previo.
Cada intento tiene nueva generación/instanceId/token/connectionId; no reutilizar READY.

El bootstrap Java administrado recibe GORILLA_IPC_LOCK_DIR: ruta absoluta estable
por usuario/producto, por defecto `$HOME/.gorilla-escape/managed` resuelta por Unity
(no dentro del repo/JAR). FileChannel.tryLock antes de Spring/listeners/READY;
exit 73 = ALREADY_RUNNING, exit 74 = LOCK_UNAVAILABLE. stderr contiene error
estructurado; stdout no publica READY para el rechazado. No borrar el archivo lock,
no matar al propietario ni adoptar otra JVM. El guard libera el lock en finally.
EOF de stdin se observa desde bootstrap y cierra Spring cuando refresh terminó;
READY y EOF se serializan para evitar publicar readiness después de EOF observado.

Presupuestos conservados: startup completo 15 s hasta primer PONG, connect hasta
2 s dentro de startup; read/write y PONG hasta 2 s; EOF→exit 10 s, fallback sobre
el hijo propio y observación 2 s; readers hasta 2 s adicionales registrados aparte.
No watchdog/Job Objects. Tokens sólo en entorno/memoria y primer PING; nunca CLI,
logs o archivo lock. El modo HTTP no administrado conserva su arranque anterior.
Validación Windows, resiliencia avanzada y performance extensiva quedan diferidas.
EOF/padre muerto no garantiza eliminar una JVM completamente congelada.
EOF/cancel y errores reales se prueban sin sustituir integración por mocks.
