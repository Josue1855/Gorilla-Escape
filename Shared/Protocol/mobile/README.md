# Gorilla Protocol v1 — Phase 0 Software/Lab

Un único contrato móvil. Java asigna identidad técnica; Unity conserva toda autoridad competitiva. Sin scoring, gestos deportivos, cámara ni Fusion.

## Envelope y canales

`protocolVersion=1`, `messageType`, `sessionId`, `playerId`, `deviceSessionId`, `sequence`, `clientTimestamp`, `serverReceiveTimestamp`, `capabilities`, `quality`, `payload`.

RTC `control`: ordered/reliable, HELLO/capabilities, HEARTBEAT, CLIENT_STATE, TOUCH, DISCONNECT. RTC `motion`: unordered/maxRetransmits=0, MOTION_SAMPLE. ACK es recibo técnico, nunca confirmación de gameplay. Conserva envelope/identidad/secuencia/timestamp del mensaje validado, payload vacío. ERROR lleva envelope con sequence/clientTimestamp/quality null si no puede autenticarse la muestra; payload.code=INVALID_INPUT. No se refleja contenido inválido.

JOIN y JOIN_ACK son bootstrap HTTP local: admission/offer y respuesta con identidad/epoch/resume/answer. Antes del JOIN no existe PlayerId/deviceSessionId: no se fabrica una identidad del cliente para rellenar el envelope RTC. No hay signaling cloud/STUN/TURN, media tracks ni webcam móvil.

## Unidades, ausencia y tiempo

MOTION_SAMPLE contiene acceleration y accelerationIncludingGravity (x/y/z, m/s²); rotationRate (alpha/beta/gamma, grados/s); orientation (alpha/beta/gamma, grados, convenciones DeviceOrientation); screenOrientation.angle (grados); touch (x/y coordenadas del cliente, pressed boolean). No se convierten Euler a quaternion ni se asumen ejes anatómicos/Unity o alineación con cámara. Esos pasos requieren calibración futura.

Cada grupo tiene `availability: present|partial|unavailable` y `values`. Unavailable = values null; partial conserva todas las claves, con ejes ausentes null. Present requiere todos los ejes. Cero y false son valores reales, no ausencia. El DTO IPC usa flags hasX/hasAlpha/hasPressed junto a valores; consultar flags antes de usar valores.

clientTimestamp es UTC Unix milliseconds del cliente; Java añade serverReceiveTimestamp UTC y Unity observa unityReceiveTimestamp UTC en la misma PC. LAB rechaza muestras anteriores a 2000 ms y futuras >250 ms. **Esto requiere relojes compatibles:** no demuestra sincronización de teléfonos físicos; offset/negociación de reloj queda como riesgo para validación física. RTT se mide con performance.now() del mismo navegador, sin depender del reloj Java. Las mediciones UTC de tramos sólo son válidas en el laboratorio de la misma PC.

sequence es entero seguro 0..2^53-1; controles y motion mantienen dominios de recepción independientes para permitir reordenamiento entre canales. Duplicados/fuera de orden/antiguos se descartan dentro de cada dominio. Manual reconnect conserva PlayerId/deviceSessionId y cambia epoch/credencial; Unity rechaza epoch previo. quality.source identifica synthetic/replay/emulator/physical, pero es declaración del cliente: evidencia física requiere una prueba externa real.

## Límites y ownership

Máximo 4 peers, admission one-use 30 s, liveness 5 s, resume 30 s. Sin reconnect/restart automático. Señalización 90 kB, SDP 64 KiB, <=32 candidatos host locales/mDNS. Negociación con deadline absoluto 10 s. RTC frame <=2048 bytes, profundidad JSON 8, strings 512, números 32 caracteres, 120 mensajes/s por peer. Duplicados JSON y documentos concatenados rechazados.

Cliente: <=8 recibos pendientes, deadline ACK 2 s, bufferedAmount <=8192 antes de enviar, una muestra motion en vuelo y una latest pending, RTT history <=4096. Java: 4 slots latest-only y batch IPC <=3500 bytes (frame total <=4096). Ni cola por muestra ni tarea por PONG nueva. Un timer compartido de liveness; recepción nativa RTC y writer IPC existentes. Los límites de aplicación no certifican toda memoria/colas internas de la biblioteca JNI.

Unity expone latest/age/freshness/capabilities/state/quality/sequence vía PhoneInputStore y TryPhoneInput. ProtocolSample conserva disponibilidad y campos raw. HasOrientation=false para Quaternion: la orientación Euler original sigue disponible, sin inventar una conversión. SERVER_STATE cambia estado sin volver fresca la última muestra. PhoneInput no calcula Power/Spin.

## Corpus y ejecución

`motion-v1.json`: fixture sintético con identidades ficticias. `corpus-v1.json`: 17 casos reproducibles, reloj inyectado. GorillaProtocolTest añade secuencias/aislamiento/rate/canales a esos casos. TechnicalSessionTest cubre expiración y replay con reloj monotónico determinista.

El QR de laboratorio se genera como PNG y se decodifica con ZXing antes del JOIN real; admission sólo en fragment. HTTP 127.0.0.1 es **contexto seguro de localhost de laboratorio**, no onboarding de teléfono por IP privada ni solución TLS de producto. No contiene token IPC/operador/clave privada. No guardar URLs QR, SDP ni credenciales reales en evidencias.
