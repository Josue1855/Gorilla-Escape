# Incremento 2C — hardening y validación sostenida del IPC local

**Estado: Accepted.** Fecha: 2026-10-06. Aprobado explícitamente por el Product Owner para implementación exclusiva de 2C. Documento de diseño Accepted; resultados ejecutados se registran por separado en PHASE0_IPC_2C_VALIDATION y TEST_REPORT.

**Base revisada:** develop `7e3aa60aa53c65e7051f32477a77985fbe628907`, con 2B `88f1e309fc7adeffe6aa6499e195f53185ab36f9` integrado. Estados al preparar el diseño: 2A PASS / MERGED; 2B PASS / MERGED; Incremento 2 IN PROGRESS; 2C NOT STARTED; Fase 0 IN PROGRESS; Fase 1 NOT STARTED. Se conservan los cambios documentales locales previos del cierre de 2B.

Fuentes: [DEC-005](PHASE0_UNITY_JAVA_DECISION.md), [decisiones](DECISIONS.md), [v4](Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md), [reglas](DEVELOPMENT_RULES.md), [workflow](WORKFLOW.md), [TEST_REPORT](TEST_REPORT.md), [progreso](DEVELOPMENT_PROGRESS.md), [contrato IPC](../Shared/Protocol/ipc/README.md), [diseño 2B](PHASE0_IPC_2B_DESIGN.md) y [validación 2B](PHASE0_IPC_2B_VALIDATION.md). La precisión aprobada de 2B prevalece sobre la resiliencia más amplia todavía diferida de DEC-005.

## 1. Alcance exacto y diferencias respecto de la base

Endurecer únicamente framing, interpretación del probe v1, aislamiento de conexiones inválidas y límites de operaciones; añadir validación determinista de presión y mediciones reales. Mantener TCP IPv4 127.0.0.1, puerto Java dinámico, token efímero, autoridad Unity, ownership del hijo y recuperación manual de 2B.

La inspección muestra:

| Existe en 2A/2B | Falta demostrar o completar en 2C |
|---|---|
| Ambos codecs rechazan longitud 0/exceso antes de reservar el cuerpo; leen fragmentos y decodifican UTF-8 estricto | Matriz compartida completa: prefijos parciales, concatenación, extremos uint32, EOF y tráfico lento |
| Java usa Jackson existente con STRICT_DUPLICATE_DETECTION y conjunto exacto de campos | Profundidad 4 explícita, un solo documento completo, tipos de string sin coerción, rangos y Unicode estricto |
| Unity usa JsonUtility para READY/PONG y comprueba correlación | Campos exactos, duplicados, payload obligatorio/vacío, tipos léxicos y consumo completo; IpcPong actualmente no representa payload |
| Unity limita cada Exchange a 2 s y cierra el socket por cancelación | Java SO_TIMEOUT de 2 s por lectura permite renovar espera con fragmentos: necesita deadline absoluto del frame |
| Java acepta y atiende una conexión a la vez; escribe un PONG mediante CompletableFuture en el pool común | Verificar ausencia de acumulación de writers tras timeout y eliminar dependencia del pool común para este único escritor |
| READY limitado a 4096 caracteres; stdout/stderr drenados; EOF monitorizado desde bootstrap | Regresión de estos mecanismos; READY estricto con el esquema existente, sin cambiarlo a framing TCP |
| Liveness 1 Hz, un request pendiente, tres lanzamientos manuales, generaciones y cleanup | Regresión real completa; modo de medición acotado separado del heartbeat |

No duplicar singleton, monitor EOF, supervisor, estados ni política de retry. No añadir RECONNECTING: se conservan STOPPED, STARTING, CONNECTING, RUNNING, STOPPING, FAILED.

## 2. Archivos previstos, después de aprobación

| Archivo o conjunto | Cambio previsto |
|---|---|
| `Shared/Protocol/ipc/README.md`, fixtures existentes y nuevos `cases/` con manifiesto | Reglas exactas y corpus común válido/inválido; bytes inválidos/prefijos como hexadecimal, sin token real |
| `Shared/CSharp/Runtime/IpcFrameCodec.cs` | Límites de escritura antes de crear bytes; mantener lecturas exactas/UTF-8; reutilizar los DTOs READY/PONG |
| Nuevo `Shared/CSharp/Runtime/IpcProbeContract.cs` | Lector específico y acotado del probe; validación estricta que devuelve los DTOs existentes, sin UnityEngine ni librería nueva |
| `Server/src/main/java/com/gorillaescape/server/ipc/ProbeCodec.java` | Constraints locales del parser, tipos estrictos, documento único, reglas compartidas y códigos sanitizados |
| `Server/src/main/java/com/gorillaescape/server/ipc/ManagedProbe.java` | Deadline absoluto de lectura/escritura, aislamiento y un único escritor sin backlog de tareas |
| `Unity/Assets/_Project/Scripts/Network/JavaProbeSupervisor.cs` | Consumir validador estricto; clasificación de protocolo sin perder estado/causa; contadores y modo finito de medición |
| `Unity/Assets/_Project/Scripts/Network/IpcProbeRunner.cs` | Selección explícita del probe de validación y resumen acotado; conservar smoke 2A y controles 2B |
| Tests codec/contrato Java y EditMode existentes; nuevos tests de presión Java y PlayMode | Corpus común, propiedades de memoria/lectura y sockets/JVM reales; fixture JVM de PONG inválido identificado como tal |
| Nuevo `tools/validate_ipc_2c_player.py`; reutilizar `tools/validate_ipc_2b_player.py` | Orquestación externa, tres corridas y estabilidad; señales sólo a hijos propios verificados |
| Nuevo `docs/PHASE0_IPC_2C_VALIDATION.md`, este diseño, TEST_REPORT, DEVELOPMENT_PROGRESS y referencia en DEC-005 | Procedimiento, resultados y estados según evidencia, sin borrar requisitos diferidos |
| Nuevo `docs/evidence/unity-java-ipc-2c-<fecha>/` | Resúmenes sanitizados, XML, métricas y hashes; raw logs/builds quedan ignorados |

Los nuevos assets Unity tendrán sus `.meta` cuando corresponda. Sin cambios previstos a escenas, gameplay, PWA ni dependencias/pom/package manifest. IpcLifecycle, ManagedProcessGuard y ParentLifetime se mantienen; cualquier fallo descubierto se documentará antes de ampliar el cambio.

## 3. Entradas y respuesta esperada

Los códigos siguientes son categorías propuestas; errores simultáneos pueden registrar la primera validación que falla. En todos los rechazos TCP se cierra esa conexión sin PONG/error con detalle remoto. Un cliente válido posterior debe funcionar en la misma JVM de prueba. Unity trata PONG inválido como fallo y ejecuta su cleanup habitual, sin retry automático.

| Entrada/caso | Respuesta y comprobación |
|---|---|
| Longitud 0, 4097, 0x7fffffff, 0x80000000, 0xffffffff | FRAME_SIZE; sólo se leen 4 bytes, no se reserva ni consume un cuerpo de la longitud declarada |
| Prefijo de 1–3 bytes y EOF | FRAME_TRUNCATED; cerrar, sin parsear ni responder |
| EOF tras prefijo completo pero antes de N bytes | FRAME_TRUNCATED; descartar cuerpo parcial |
| EOF entre frames completos | CLIENT_EOF de transporte; no convertirlo en JSON inválido |
| Prefijo/cuerpo parcial que se detiene o gotea | READ_TIMEOUT absoluto; cerrar aunque lecturas individuales hayan entregado bytes |
| Frames completos concatenados en TCP | Válidos, uno por lectura lógica; cada respuesta conserva su sequence; no confundir con dos documentos en un cuerpo |
| Frame fragmentado en cualquier frontera | Válido si termina dentro del deadline; incluye UTF-8 multibyte cortado entre receives |
| Cuerpo de exactamente 4096 bytes | Framing válido; contrato válido sólo si es un probe correcto más whitespace JSON permitido |
| UTF-8 inválido, overlong, surrogate codificado como UTF-8, secuencia incompleta | UTF8; sin conversión con reemplazo |
| JSON incompleto, comentarios, coma final, NaN, BOM, dos raíces o basura final | JSON_INVALID; aceptar únicamente un objeto y whitespace JSON alrededor |
| Campo requerido ausente, null, tipo erróneo, raíz array/string | CONTRACT_FIELDS / CONTRACT_TYPE; sin defaults ni coerción |
| Campo desconocido o repetido, incluido nombre escapado equivalente | UNKNOWN_FIELD / DUPLICATE_FIELD; tanto raíz como payload |
| Anidamiento que supera 4 | JSON_DEPTH durante lectura, antes de construir árbol profundo; contenedores raíz=1, payload=2 |
| Versión distinta de entero 1; entero escrito como string, decimal o exponente | CONTRACT_VERSION / CONTRACT_TYPE |
| UUID no canónico, no string, instancia/conexión distinta de la esperada | IDENTITY; canónico lowercase con guiones, 36 caracteres, comparación ordinal |
| Primer mensaje distinto de PING/sequence 1 | CONTRACT_TYPE / SEQUENCE; no autenticar |
| Token ausente, incorrecto, null, no string o formato distinto de 43 base64url | TOKEN / CONTRACT_FIELDS; comparación de bytes contra token de lanzamiento sin revelar contenido |
| Token en segundo PING o en PONG | CONTRACT_FIELDS; payload debe estar vacío |
| Sequence 0/negativo/>2147483647, decimal/exponente/string, repetido, saltado o fuera de orden | SEQUENCE / CONTRACT_TYPE; sólo avanzar después de request válido |
| Type desconocido o PING recibido por Unity como respuesta | CONTRACT_TYPE; no dispatch genérico |
| PONG con payload ausente/no vacío, correlación incorrecta o duplicada | PROTOCOL_INVALID en Unity, STOPPING → FAILED tras cleanup |
| READY inválido/desconocidos/duplicados | READY_INVALID; no conectar ni RUNNING; cleanup del hijo propio |

Aceptar orden arbitrario de campos, whitespace JSON ASCII y escapes JSON válidos. Rechazar escapes Unicode con surrogates no emparejados. Los UUID/token/tipos resultantes tienen restricciones ASCII; no hacer normalización cultural. Duplicados se comparan por nombre decodificado, sensible a mayúsculas, antes de asignar valores.

## 4. Política de cierre y lifecycle

Detectar → un diagnóstico local sanitizado → cerrar socket/streams de esa conexión → liberar estado por conexión → volver a accept mientras Java sigue sano. No intentar resincronizar un frame roto, no enviar detalle a cliente no autenticado, no borrar lock ni terminar JVM por input inválido. EOF stdin sigue siendo el shutdown del proceso administrado.

Java no debe dejar salir excepciones de validación por el worker de accept. Capturar errores de parser/contrato previsibles y mantener visible un fallo interno inesperado; no ocultar errores graves de VM como si fueran input incorrecto. Un fallo de recursos del listener es distinto de un frame inválido.

En Unity, un protocolo/timeout inválido preserva causa diferenciada y sigue RUNNING/CONNECTING → STOPPING → FAILED con cleanup observado. Unity cierra stdin y puede terminar únicamente su hijo tras el deadline existente. Por ello la continuidad Java después de un mal PING se prueba con un cliente simulado que conserva stdin abierto; no se exige que Java sobreviva al shutdown legítimo solicitado por su supervisor Unity.

Conservar startup total 15 s hasta primer PONG, connect ≤2 s dentro de startup, Exchange Unity ≤2 s, lectura de frame Java ≤2 s total desde que comienza a esperar el prefijo y escritura Java ≤2 s. El tiempo de frame incluye espera del primer byte; el heartbeat de 1 s sigue compatible. SO_TIMEOUT se ajusta al tiempo restante, no se renueva el presupuesto con cada fragmento.

Propuesta mínima para escritura Java: escritura síncrona en el worker existente, con un único temporizador local de operación que cierra ese socket al vencer 2 s. Máximo una tarea de deadline activa, cancelada/removida al terminar; identificada por conexión para no cerrar la siguiente. No es watchdog del proceso ni cola de mensajes. Cancelar temporizador y cerrar recursos en destroy; probar carreras timeout/close/accept. Esto sustituye el CompletableFuture de PONG en pool común, sin crear otro productor de respuestas.

Shutdown conserva EOF→exit ≤10 s, fallback propio observado ≤2 s y readers ≤2 s aparte. No modificar recuperación manual, máximo tres lanzamientos por ejecución, singleton cooperativo ni aislamiento de generaciones.

## 5. Límites de memoria, colas y diagnósticos

| Recurso | Límite propuesto |
|---|---|
| Framing recibido | Prefijo 4 bytes + un cuerpo de 1..4096 bytes; verificar antes de allocation |
| Escritura | Contar bytes UTF-8 estrictamente antes de crear el byte[]; rechazar >4096 sin copia grande; nunca truncar |
| JSON | Fuente ya limitada por frame; profundidad máxima 4, sin estructuras ni strings que excedan ese cuerpo |
| Esquema | Exactamente 6 campos raíz PING/PONG; payload primero 1 campo, siguientes/PONG 0; READY 5 campos |
| Strings/números del probe | UUID 36, type 4, launchToken 43; enteros positivos int32, versión=1; puertos 1..65535; pid coincide con hijo |
| Trabajo TCP | Un cliente atendido por Java, backlog solicitado 2 ya existente; un lector/escritor y PONG pendiente; Unity un Exchange pendiente |
| Tareas | Un deadline Java activo y ninguna cola de PONG ni tarea por frame en pool común; limpieza verificable |
| Métricas | Corrida corta: array/capacidad fija de 1000 RTT retenidos; estabilidad: contadores y máximo 1024 RTT, cerrar medición al llegar al límite/tiempo |
| Logs propios | Una línea ≤512 bytes por cierre/causa, sin payload, token, entorno o texto libre de excepción/parser; no log de cada frame válido |
| Pipes | READY conserva límite 4096 caracteres y deadline startup; drenado stdout/stderr por chunks existente, sin retener historial |

No añadir la cola de eventos futura de DEC-005: 2B no la utiliza y 2C no introduce consumidor de gameplay. Buffers TCP del SO, heap JVM y Unity no equivalen al cuerpo 4096; informar memoria real, sin prometer RSS total de 4096 bytes ni modificar tuning de SO por anticipado. El lector C# debe usar límites antes de materializar strings; nunca copiar un string largo sólo para comprobarlo. No leer hasta EOF, acumular toda la conexión ni readAllBytes en caminos de producto.

Durante pruebas, drenar stderr/stdout y escribir evidencia acotada. La política de una línea por conexión no es un rate-limit contra conexiones ilimitadas; el cliente que eternamente ocupa el único slot puede degradar disponibilidad. No se promete fairness ante malware del mismo usuario.

## 6. Parsers, unknown/duplicate fields y alternativas

Java ya tiene Jackson con detección de duplicados: reutilizarlo, añadir StreamReadConstraints de profundidad 4 sólo al mapper IPC y exigir EOF lógico tras un objeto. Comprobar isTextual antes de leer strings; enteros léxicos int32, sin conversiones permisivas. No cambiar defaults del parser HTTP global. [Jackson: constraints de lectura](https://javadoc.io/static/tools.jackson.core/jackson-core/3.2.1/tools.jackson.core/tools/jackson/core/StreamReadConstraints.html). La API concreta se verificará contra la versión resuelta del proyecto; no actualizar dependencias por esta referencia.

JsonUtility omite campos que no representa y no expone las claves originales para validar duplicados; por sí solo no demuestra el contrato estricto. [Unity: JSON serialization](https://docs.unity3d.com/6000.0/Documentation/Manual/json-serialization.html). La implementación actual confirma además que PONG no tiene payload en el DTO, aunque el envelope lo exige.

**Alternativa mínima recomendada, sujeta a aprobar este diseño:** lector específico en Shared/CSharp para PONG/READY y validación del corpus PING, que decodifica tokens y llena los DTOs existentes. Sin DOM genérico, extensibilidad móvil ni sustitución del parser del resto del producto. El payload vacío se valida explícitamente aunque no se almacene como DTO independiente. Debe cubrir escapes, nombres duplicados tras desescape, límites, tipos, número íntegro y documento único mediante corpus cruzado. Evitar una regex o buscar strings en JSON; eso no detecta estas condiciones de forma fiable.

Mantener JsonUtility solamente dejaría la validación asimétrica y no cumpliría aceptación 2C. Adoptar otro parser C# agregaría una dependencia cuya compatibilidad/beneficio no se ha demostrado. Por eso no se propone automáticamente System.Text.Json/Newtonsoft u otro paquete. Si el lector limitado crece hasta convertirse en parser genérico o no supera el corpus, detener implementación y presentar coste/alternativa al PO, sin declarar PASS ni instalar una librería silenciosamente.

Token sigue siendo por lanzamiento: sólo en primer PING de cada conexión. No convertirlo en nonce consumible una única vez para toda la JVM. Un token de otro lanzamiento se rechaza; el mismo token vigente en primer PING posterior sigue permitido por el contrato. Después de sequence máximo cerrar sin overflow; Unity conserva error explícito/recuperación manual, no reconectar automáticamente.

## 7. Prueba corta: 1000 mensajes × 50 Hz × 3

Tres ejecuciones independientes de Player real, cada una inicia su Java real, nuevo instanceId/token/connectionId y lock compatible. No usar tres retries automáticos ni exceder el presupuesto de lanzamientos de una ejecución. PlayMode verifica el camino real; la baseline principal será Player Linux de desarrollo con el mismo build/JAR en las tres corridas.

Por corrida: startup desde antes de spawn hasta primer PONG válido; 100 intercambios de warmup incluyendo handshake, excluidos del histograma; después 1000 PING/PONG exitosos previstos con inicio objetivo cada 20 ms. Mantener un pendiente, sin cola ni ráfagas para recuperar retrasos. Un reloj monotónico programa la cadencia; descontar tiempo ya transcurrido para esperar el siguiente turno. Si un RTT supera el periodo, registrar retraso/cadencia efectiva y continuar sin ocultar la muestra; si hay fallo de transporte, la corrida falla y ejecuta cleanup, sin rellenar 1000 con otra conexión.

La base actual espera 20 ms después de cada PONG: no llamarla 50 Hz. El modo de medición nuevo aplica pacing sólo al probe finito explícito; smoke 2A y liveness 1 Hz permanecen iguales. La duración prevista medida es aproximadamente 20 s más warmup/startup/shutdown; publicar duración y frecuencia efectivas, no sólo nominales.

RTT: Stopwatch compatible con el perfil Unity, desde antes de serializar/escribir hasta validar completamente PONG en worker. No restar relojes de dos procesos, restar overhead artificialmente ni dividir por dos. Percentiles nearest-rank sobre éxitos ordenados: índice ceil(p×N)-1. Publicar cada corrida: N enviado/recibido/válido, warmup, errores, timeouts, mínimo, P50, P95, máximo, duración, cadencia, ticks tardíos, startup y shutdown. Ningún promedio entre corridas sustituye esas cifras.

Referencia DEC-005: P50 ≤5 ms, P95 ≤10 ms, máximo ≤50 ms en baseline idle; nominal errores/timeouts/desconexiones 0. Startup ≤15 s o error explícito; las tres corridas registran valores, pero no estiman P95 de startup a partir de tres lanzamientos. Si falla un presupuesto, investigar y reportar; no cambiar umbrales ni excluir picos para pasar.

## 8. Estabilidad de aproximadamente diez minutos

Una cuarta ejecución independiente Player→Java real: RUNNING sostenido al menos 600 s, usando RunLifecycleAsync/heartbeat real 1 Hz y un pendiente. El harness solicita Stop/cierre normal después del plazo observado; no termina Java directamente para simular éxito. No transformar este ensayo en 50 Hz durante diez minutos ni sustituirlo por cliente simulado.

Registrar handshake aparte, heartbeat enviados/recibidos/válidos, errores/timeouts/desconexiones, intervalo efectivo, estado RUNNING antes del stop, transiciones de cierre, estado final STOPPED, cleanupComplete, Unity/Java exit 0 y residuales propios 0. Número esperado cercano a 600, publicado realmente; última cancelación voluntaria identificada y separada de fallos. No permitir un mensaje sin respuesta fuera de ese cierre intencional.

Memoria por proceso, si reproducible: RSS y heap managed disponibles, baseline después de warmup, muestras cada 5 s, final antes de stop y pico; monitor externo con buffers fijos/salida a archivo. Comparar también GC/tendencia en ventanas, sin forzar GC ni cambiar JVM para maquillar baseline. Umbral propuesto de investigación: crecimiento final respecto de baseline mayor que max(64 MiB, 25% del baseline RSS) en cualquiera de los procesos, o crecimiento continuo entre ventanas finales. Es una alerta que debe explicarse, no prueba automática de leak ni presupuesto DEC-005 nuevo. Si el entorno no ofrece medición fiable, marcar NOT MEASURED con motivo; los límites deterministas del producto siguen obligatorios.

No afirmar ausencia de memory leaks a partir de esta corrida. Conservar los diagnósticos nativos del entorno Unity ya registrados en 2B y no confundir build sin warnings con ausencia de leaks globales.

## 9. Presión determinista y captura de métricas

Java real con stdin mantenido por el harness, socket loopback real y token sólo en memoria. Cada grupo parte de un proceso/lock de prueba identificado, mantiene presión finita y demuestra PING válido posterior en la misma JVM; no afecta al Java de otra ejecución.

| Grupo | Carga fija propuesta y observación |
|---|---|
| Pequeños válidos | 1000 frames correctos; luego 1000 enviados con productor sin pacing y lector PONG lento; sequences ordenadas y nada perdido/intercalado |
| Inválidos | 100 conexiones secuenciales distribuidas sobre matriz/corpus; cada rechazo observable, Java vivo y conexión válida después |
| Máximos | 100 frames válidos de 4096 bytes mediante whitespace, no campo padding desconocido |
| Oversized | 100 conexiones con prefijos 4097/uint32 extremos y sin cuerpos; cierre antes de intentar leer cuerpo; cliente válido posterior |
| Productor rápido/consumidor detenido | Ventana de envío por socket de hasta 1 MiB o 5 s, lo que llegue primero; no acumular todo en RAM; pausa lector 3 s, después drena/cierra. Demostrar backpressure o cierre por write deadline si se llena el buffer, no asumir que pocos PONG llenan TCP |
| Parcial detenido/goteo | Prefijo y cuerpo incompletos, incluyendo un byte cada 500 ms; comprobar cierre absoluto ≈2 s y cliente posterior, sin mantener JVM ocupada indefinidamente |

Los sockets del harness tienen deadlines, y el grupo completo watchdog externo de prueba de 30 s más shutdown normal. El watchdog del harness no se entrega como watchdog de producto. EOF/cleanup puede interrumpir un grupo; registrar el motivo, no PASS por abandonar la presión. Congelar semilla/lista de casos; no fuzzing ilimitado.

Capturar contadores por categoría, conexiones aceptadas/cerradas, bytes enviados/recibidos, tiempos de cierre, workers/deadlines activos al inicio/final, memoria baseline/final/pico, estado Java y respuesta válida posterior. Verificar como propiedades directas: no body read para tamaños inválidos mediante stream espía unitario; máximo un frame/writer pendiente; tareas de deadline no crecen con N; ningún worker residual tras cleanup. La variación RSS se interpreta con las mismas cautelas de estabilidad; una sola cifra final no acredita buffers acotados.

Guardar entorno (Unity/backend, JVM, OS/CPU/RAM, carga, versiones, rutas sanitizadas, cold/warm), SHA de fuentes, hashes JAR/Player y parámetros de cada ensayo. Evidencia en JSON/CSV/XML sanitizados; nunca tokens, dumps de entorno, payloads de rechazo ni stacktraces que los incluyan. Errores inyectados se separan de errores nominales. No extrapolar resultados a teléfono→Java→Unity→gameplay ni entrega al hilo principal.

## 10. Pruebas y regresión 2A/2B

| Nivel | Pruebas previstas |
|---|---|
| Unit Java/EditMode | Misma matriz de framing/JSON; lectura fragmentada/concatenada; Unicode/escapes/duplicados; enteros/rangos; token/sequence; máximo exacto; no allocation basada en longitud inválida; deadline absoluto y cancelación con reloj/streams controlados |
| Java real | Corpus sobre JAR/JVM real, presión descrita, cliente lento, socket cerrado por timeout; comprobar JVM/HTTP sanos, lock conservado y validación posterior. Regresión de todos los tests Java 2B |
| Unity PlayMode | Unity→Java real para PING/PONG, lifecycle/cancel/EOF/cleanup/singleton/manual retry/generaciones. Fixture JVM controlado sólo para PONG/READY malformados o timeout: identificado como fixture, no como servidor validado |
| Player real | Tres corridas cortas + diez minutos; repetir los 11 grupos 2B del harness existente y smoke 2A N=100. Mismos controles visibles/manuales, sin saltar ownership ni limpieza |
| Integración del repositorio | Java verify, EditMode, PlayMode, build Linux sin errores/warnings nuevos, React 4/4 y build existentes, validadores management/foundation |

Baseline a conservar: Java 13/13, EditMode 15/15, PlayMode 10/10, React 4/4 y 11 grupos Player 2B PASS; ampliar suites sin convertir skips en integración aprobada. Mantener como evidencia histórica READY timeout ≈15.02 s, liveness loss ≈3.04 s, fallback ≈10.04 s y EOF→exit ≈65.37 ms; no exigir idénticos tiempos en otra corrida, sí presupuestos y causas correctos.

Regresión explícita: Java muere; READY ausente; connect rechazado; cancel durante startup/RUNNING; segundo Java no roba lock; cierre Unity/EOF temprano y normal; cleanup sockets/streams/proceso; máximo tres lanzamientos y cuarto bloqueado; evento tardío no afecta generación nueva; liveness 1 Hz/un pendiente; kill sólo del hijo propio cuando corresponde; residual propio 0 en casos soportados. Simuladores apoyan invalidaciones, nunca sustituyen nominal Unity↔Java real.

## 11. Criterios de aceptación y cierre posterior

1. Corpus compartido pasa en Java y C# con reglas simétricas de framing/contrato; duplicados escapados, unknown fields y trailing documents rechazados. No dependencia nueva ni parser HTTP global alterado.
2. Tamaños inválidos rechazados antes de allocation/read del cuerpo; límites de frame 4096 y profundidad 4 demostrados. Fragmentación/concatenación válidas no producen falsos rechazos.
3. Input inválido cierra sólo la conexión Java, conserva JVM sana y permite PING posterior en pruebas reales. Diagnóstico comprensible/sanitizado sin respuesta detallada remota.
4. Timeout absoluto frena parcial detenido/goteo; writer/deadlines no se acumulan. Presión finita completa sin crash/deadlock, sin colas ilimitadas y cleanup verificado.
5. Tres corridas reales independientes con N=1000 y warmup 100, pacing objetivo 50 Hz/cadencia efectiva documentada; cada corrida nominal sin errores ni timeouts, presupuestos RTT DEC-005 satisfechos en baseline declarado. Si no, resultado real e investigación pendiente, sin marcar PASS global.
6. Corrida real ≥600 s con heartbeat 1 Hz, nominal sin errores/desconexiones, STOPPED/exit 0/cleanup completo y cero Java propio residual; memoria documentada si medible, sin afirmaciones de leaks no demostradas.
7. Suites/regresión real 2A/2B PASS, build y checks aplicables PASS; ownership, lock, token/loopback y presupuesto manual de lanzamientos intactos.
8. Evidencia identifica fuentes/binarios/entorno, casos no ejecutados y límites. Windows/PC oficial no ejecutados siguen NOT RUN y no se certifican mediante resultados Linux.

Sólo tras implementación, evidencia y aceptación se propondrá 2C PASS e Incremento 2 PASS, manteniendo 2A/2B PASS / MERGED; integración 2C requiere PR/revisión/merge normal para cumplir DoD. Fase 0 permanece IN PROGRESS y Fase 1 NOT STARTED. Este diseño no modifica estados de validación ni autoriza trabajo posterior.

## 12. Riesgos y decisiones para aprobación

- Lector C# específico exige más pruebas de escapes/tipos que JsonUtility; corpus cruzado y alcance cerrado mitigan divergencia. Si no puede mantenerse pequeño y fiable, solicitar nueva decisión, no relajar duplicados.
- Constraints Jackson y EOF completo deben verificarse contra la dependencia ya resuelta; no confundir ejemplo de documentación con versión realmente instalada.
- Temporizador de escritura puede competir con close/cancel/nueva conexión: referencia por conexión, cancelación y pruebas de carreras obligatorias. No dejar callbacks tardíos cerrando sockets nuevos.
- Deadline de frame incluye idle: mantener heartbeat 1 Hz y comprobar tolerancia real con scheduler/GC. No subir el límite silenciosamente para pasar pruebas.
- Backlog/atención serial no garantiza disponibilidad frente a cliente local persistente; la prueba acotada demuestra liberación y ausencia de acumulación, no seguridad ante malware.
- RSS incluye GC/JIT/native allocations y carga del editor; Player como baseline, muestras y entorno ayudan a interpretar. Linux no certifica Windows, ni el dispositivo de presentación pendiente.
- Las tres corridas y diez minutos pueden revelar fallos reales de 2B. Se reportan antes de ampliar scope; evidencia histórica no se reescribe como si hubiera probado 2C.

Se solicita aprobar conjuntamente: lector IPC C# limitado sin dependencia, deadline absoluto con presupuesto existente, sustitución de writer de pool común por único writer/temporizador acotado, matriz/cargas, pacing de modo de medición, alertas de memoria y gate de evidencia. Aprobación recibida; implementación autorizada exclusivamente en la rama 2C desde la base confirmada.

## 13. Exclusiones explícitas

Reconnect/restart automático, retries infinitos, watchdog de proceso/nativo, Windows Job Objects, kill de árboles arbitrarios, defensa contra malware del mismo usuario, TLS IPC, autenticación cloud/cuentas, ban/rate-limit complejo, fuzzing ilimitado y optimizaciones anticipadas. Tampoco Gorilla Protocol móvil, WebSocket/HTTPS móvil, PWA, QR, sensores, cámara, MediaPipe/OpenCV, gameplay, scoring, Input Fusion ni Fase 1.

La contención nativa, resiliencia avanzada y distribución JRE/PC oficial siguen diferidas según DEC-005; no se borran. 2C sólo cierra el alcance local aprobado de Incremento 2 y no el resto de Fase 0. El cierre inicial del diseño Proposed fue respetado. La aprobación posterior autoriza implementar sólo 2C y detenerse tras abrir el PR, sin merge automático ni trabajo posterior.

## Precisiones aprobadas por el PO

IpcProbeContract será exclusivamente probe IPC v1; detener y reportar si se convierte en parser general/difícil de auditar. Ninguna dependencia nueva. Writer Java único, máximo un deadline activo, sin cola de PONG/thread por PONG, cancelación efectiva y callbacks ligados al socket original. Límites deterministas obligatorios; RSS/heap y max(64 MiB,25%) sólo observación/investigación, sin declarar ausencia de fugas. Tres resultados de 1000 muestras independientes y completos, sin mezclar percentiles ni estimar P95 startup con tres launches. Regresión mínima ante defectos existentes. Tras gate PASS, commit/push/PR; no merge automático. Incremento 2 sigue IN PROGRESS hasta integración.
