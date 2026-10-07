# Incremento 2 — decisión Unity ↔ Java

**Fecha:** 2026-10-06. **Estado:** Accepted por instrucción explícita del Product Owner; 2A PASS / MERGED en develop y diseño 2B Accepted, exclusivamente lifecycle y fallos básicos. Las secciones iniciales conservan el contexto histórico anterior a #90. Fase 0 IN PROGRESS; Fase 1 NOT STARTED.

## Base y fuentes de verdad

Leídas: [v4](Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md) §§3–5, 8–9, 33–41, 48–53; [reglas](DEVELOPMENT_RULES.md), [flujo](WORKFLOW.md), [DEC-002](DECISIONS.md), [monorepo](MONOREPO_PLAN.md), [arranque](TEAM_START.md) y [Shared/Protocol](../Shared/Protocol/README.md). La instrucción actual autoriza preparación de Incremento 2, sin implementación, PWA móvil, QR, WebSocket de teléfonos, sensores, cámara, gameplay o Gorilla Smash. El shell React existente no se amplía.

Base real consultada mediante fetch y lectura remota: `origin/develop = e950bd820ea2dd6ede265943a3b7702f445be467`. GitHub muestra [PR #90](https://github.com/Josue1855/Gorilla-Escape/pull/90) **OPEN**, `mergedAt=null`, con foundation validada en `38e1e31ce0e36952b759061eee40136f7a0ef3f6`. Esto difiere del anuncio de integración del PO; no se integra ni se altera el PR por iniciativa propia. Rama documental creada desde develop: `docs/phase0-unity-java-architecture`. Antes de implementar, volver a verificar integración y trabajar sobre el nuevo SHA de develop.

En develop existe Spring Boot 4.1.1/Java 21, hosting HTTP `127.0.0.1:8080` configurable, `/api/health`, cierre graceful de Spring con fase de 5 s, empaquetado de recursos React y cuatro tests Java. El starter WebSocket está declarado, sin handler. Unity tiene Bootstrap/ApplicationSettings y contratos, sin supervisor Java ni transporte IPC. Shared tiene envelope genérico y fixture sensor. No hay torneo/scoring/ganador Java que retirar. `/api/health` no identifica una instancia ni demuestra readiness IPC. El JAR actual no incluye un runtime Java distribuible.

La foundation del PR pasó en Linux de desarrollo; ello no aprueba PC de presentación, backend final ni APIs de procesos/sockets de Unity en Windows. Los bloqueos de licencia que describen los documentos de develop son históricos respecto del PR, no una nueva ejecución fallida.

## Opciones evaluadas

Comparación cualitativa; ninguna latencia de estas alternativas se ha medido aquí.

| Opción | Simplicidad y complejidad operativa | Latencia | Windows/Linux | Recuperación | Observabilidad y testabilidad | Framing y riesgo de bloqueo |
|---|---|---|---|---|---|---|
| TCP loopback | APIs de sockets estándar en Java/C#; listener propio pequeño, sin framework nuevo. Requiere publicar puerto y codec acotado. | Candidato suficientemente ligero; hipótesis pendiente. TCP_NODELAY a evaluar, no promesa. | Mismo protocolo en ambos; validar en Player/backend elegido. | Reconectar sin reiniciar JVM; EOF y errores de socket observables. | Cliente simulado sencillo; logs correlacionados y captura local opcional sin secretos. | TCP es flujo: implementar lecturas parciales y límite previo a asignación. Escrituras serializadas, deadlines y colas acotadas evitan atasco. |
| WebSocket local | Reutiliza soporte Spring existente, pero añade HTTP upgrade, cliente Unity compatible y endpoint interno aislado del futuro móvil. | También plausible; no asumir que framing/upgrade lo vuelve lento ni rápido. | Estándar portable; validar librería/API Unity sin adoptar dependencia implícita. | Reconnect explícito igual que TCP; handshake adicional. | Herramientas conocidas, cliente Java real posible. | Framing estándar, pero hay que limitar mensajes completos/fragmentación; buffers/envíos siguen pudiendo bloquear. |
| Named pipes Windows / Unix domain sockets | Endpoints/permisos propios del SO; aumenta adaptación Java↔Unity y distribución. Java no ofrece un NamedPipeServerStream equivalente en su API estándar. | Potencialmente baja; ahorro no demostrado para este producto. | Named pipes Windows y sockets Unix no son el mismo contrato operativo; Java 21 dispone de UDS, soporte efectivo depende de SO/runtime. | Reabrir endpoint, manejar nombres/rutas abandonadas y permisos. | Cliente real por plataforma; menos herramientas uniformes que TCP. | Stream requiere framing igual; semántica message-mode no es portable. Operaciones deben cancelarse; limpiar socket pathname sin borrar endpoint ajeno. |
| stdin/stdout como transporte completo | Razonable si Unity es siempre padre de Java; evita puerto, pero mezcla lifecycle y mensajes con logs de Spring. | Plausible, sin medición. | Pipes de procesos disponibles, comportamiento Unity por validar. | Canal roto normalmente obliga a recrear proceso; peor para probar reconnect conservando JVM. | Harness padre/hijo real; inspección externa menos cómoda. | Delimitación explícita; drenar stdout y stderr simultáneamente. Buffers llenos pueden bloquear ambos procesos. |

Base técnica: [Java 21 ServerSocket](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/net/ServerSocket.html) permite binding explícito y puerto 0 elegido por el SO; [Java 21 UnixDomainSocketAddress](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/net/UnixDomainSocketAddress.html) documenta UDS. [Microsoft](https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-use-named-pipes-for-network-interprocess-communication) distingue implementación .NET en Unix; no se infiere compatibilidad automática con Unity Mono. [RFC 6455](https://www.rfc-editor.org/info/rfc6455/) define handshake/framing WebSocket. [ProcessStartInfo](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardoutput) describe deadlocks con streams redirigidos; [perfil Unity 6.3](https://docs.unity3d.com/6000.3/Documentation/Manual/dotnet-profile-support.html) exige verificar APIs del perfil efectivo, sin asumir todas las APIs del .NET moderno.

## Recomendación IPC

**Proponer TCP IPv4 en `127.0.0.1`, conexión persistente Unity cliente → Java servidor, JSON UTF-8 con prefijo de longitud.** Elegirlo por compatibilidad y facilidad de probar fallos/reconnect, no por una superioridad de latencia no medida. WebSocket permanece destinado a teléfonos; no se cambia la decisión congelada de comunicación móvil. TCP interno no añade un servicio remoto ni un despliegue independiente al producto modular local.

Usar pipes del proceso únicamente para bootstrap/readiness y propiedad del hijo: stdout reservado para un evento READY; stderr para logs; stdin abierto como canal de vida del padre. No transmitir input futuro por esos pipes. Esta combinación debe probarse como parte del lifecycle, y no exige un tercer runtime/supervisor externo.

Alternativa preferente si TCP falla objetivamente en el Player elegido: WebSocket loopback con endpoint dedicado y los mismos límites/lifecycle. Requiere evidencia, revisión productor/consumidor y nueva decisión; no hay fallback de transporte silencioso.

## Ownership

| Responsabilidad | Runtime propietario | Límite |
|---|---|---|
| Gameplay, física, estado jugable, scoring, GP, ganador y resultados oficiales | Unity | Ninguna copia autoritativa en Java. |
| Escenas, sesión/participantes/turnos desde la perspectiva del juego y persistencia oficial de resultados | Unity | Java podrá transportar snapshots emitidos por Unity; nunca reconstruir un torneo ni confirmar un resultado competitivo por sí mismo. |
| Host HTTP/PWA y futuras conexiones móviles/WebSocket | Java | Transporta solicitudes/estado; no interpreta una recepción de red como aceptación jugable. |
| Validación de formato, tamaño, versión, conexión y adaptación de input de transporte | Java | La validación de acción/intento y reglas de gameplay quedan en Unity; fusión futura no se define en este incremento. |
| Lifecycle de conexiones y disponibilidad del host | Java | Estado técnico de conexión, separado del estado de sesión jugable Unity. |
| Arranque y supervisión del proceso Java | Unity | Un servicio de aplicación persistente, separado de minijuegos y del transporte. |

**Aclaración aprobada de v4 §5:** «Si el resultado ya fue confirmado por el servidor, permanece válido» es ambigua tras DEC-002. Enmienda aprobada y aplicada documentalmente: «Si Unity, como autoridad de juego en la PC, ya confirmó el resultado oficial, permanece válido; Java sólo lo transporta». La aprobación explícita queda en DEC-005; no implementar scoring en Java. §3.4/§51 favorecen esta distribución. Mover autoridad a Java exigiría duplicar o trasladar simulación/estado y sincronizar Unity; no existe evidencia que lo justifique.

Incremento 2 no crea GameSession, scoring ni GP: sólo distingue lifecycle técnico del juego futuro. Un restart de Java no borra ni recalcula un resultado Unity; la sincronización de una sesión jugable futura será otro contrato aprobado.

## Lifecycle

Secuencia propuesta, única para ejecución administrada del spike:

```text
Game starts → Unity supervisor valida bundle → Java starts
→ Java toma lock y abre listeners → READY → Unity conecta y PING/PONG
→ session operates (sólo probe técnico) → disconnect/recovery
→ game exits → Unity cierra stdin → Java shuts down → Unity espera exit/cleanup
```

### Localización, exclusión y bootstrap

1. Unity resuelve rutas absolutas desde una configuración de distribución relativa a la raíz del producto: `Runtime/Java/bin/java[.exe]` y `Server/local-server.jar`. Son rutas **propuestas**, no archivos presentes. Desarrollo permite rutas explícitas a JDK 21/JAR de build, sin buscar un `java` arbitrario en PATH. Validar existencia y versión; argumentos separados sin shell/interpolación, cwd controlado. El layout/JRE distribuidor/licencia/arquitectura se aprueba antes de empaquetar; no se descarga nada al ejecutar el juego.
2. Un supervisor Unity por ejecución, con máquina de estados `STOPPED → STARTING → CONNECTING → RUNNING → RECONNECTING → STOPPING → STOPPED` y `FAILED` recuperable. Serializa start/stop/retry; una escena nueva no lanza otro servidor. En Editor, el scope es una ejecución PlayMode: teardown también en stop/reload, sin depender sólo de OnApplicationQuit.
3. Java toma un lock exclusivo del SO mediante FileLock en el directorio de datos de usuario/producto, antes de abrir listeners. El lock pertenece al proceso y se conserva toda su vida; no basta la existencia de un archivo o un PID. La segunda JVM sale con `ALREADY_RUNNING`; Unity no se adjunta a un servidor desconocido ni lo mata. Objetivo: una instancia administrada por usuario/producto. Probar semántica real del lock Windows/Linux y permisos del directorio.
4. Unity crea un `instanceId` UUID y un token aleatorio criptográfico de 256 bits por lanzamiento; se pasan al entorno del hijo, nunca a argumentos/logs/fixtures. El token vincula la conexión con el hijo esperado, no protege frente a un proceso malicioso del mismo usuario. Java recibe stdin redirigido, monitorizado desde el comienzo, y no lanza descendientes que hereden el pipe.
5. Java hace bind IPC **exclusivamente** a `127.0.0.1:0` y mantiene ese socket abierto; el SO asigna el puerto. No reservar/cerrar un puerto en Unity para volver a abrirlo en Java (carrera). El listener HTTP existente es separado; modo administrado del spike propone HTTP también en loopback y puerto 0, sin alterar el 8080 del arranque manual ni habilitar LAN.

### Readiness y operación

6. Tras listener IPC y contexto/HTTP listos, stdout emite y flush una sola línea UTF-8, máximo 4096 bytes: `GORILLA_IPC_READY ` seguido de JSON con campos exactos `ipcVersion:1`, `instanceId`, `pid`, `ipcPort`, `httpPort`. Ambos puertos entre 1 y 65535; PID coincide con el hijo, instanceId con el lanzamiento. Configurar logs Spring a stderr. Unity drena ambos streams desde el arranque, con buffers limitados; no espera a exit para leerlos. Ignorar líneas ajenas como logs acotados, sin aceptar un READY parcial, duplicado o con identidad distinta.
7. Deadline de startup propuesto: **15 s** desde antes de Process.Start hasta primer PONG válido (incluye lanzamiento, Spring, READY y conexión). Esperar eventos de stdout/exit y operaciones async cancelables; un temporizador sólo impone deadline, no sustituye readiness. `/api/health` existente por sí solo nunca satisface este paso. Conexión individual: máximo **2 s**, dentro del deadline global; primer PING valida token, versión e identidad y PONG prueba round-trip real. READY aislado no habilita RUNNING.
8. En RUNNING, un PING de vida por segundo si no hay probe activo; timeout de PONG **2 s**, máximo un PING pendiente. EOF/error/timeout cierra el socket y entra RECONNECTING; estado informado a Unity mediante cola, sin acceder a UnityEngine desde worker. No bloquear el hilo de frames con Connect/Read/WaitForExit. Deadline de lectura de frame y escritura **2 s** cada uno, cancelable; máximo una respuesta/frame pendiente y cola de eventos de 64 entradas. Saturación falla explícitamente, sin memoria creciente ni descartar resultados oficiales futuros.

### Recuperación y cierre

9. Si el hijo sigue vivo y stdin abierto, reconectar al mismo puerto/instanceId dentro de **10 s**, intentos con demora cancelable de **100, 250, 500, 1000 ms** (luego 1000 ms hasta deadline), sin intentos concurrentes. Son backoff de fallos, no sleeps de readiness. Crear connectionId nuevo, descartar pendientes de la conexión vieja y exigir primer PONG nuevo. No reemitir acciones jugables; no existen en este spike.
10. Si Java muere: observar exit, cerrar canales y marcar FAILED; no iniciar un bucle de reinicios automáticos. Propuesta: ofrecer reintento explícito; hasta **3 lanzamientos totales** por ejecución Unity, configurable. Antes de lanzar, confirmar que el hijo anterior terminó. Nuevo instanceId/token/puerto, nunca reutilizar una READY anterior. Si se agota reconnect sin morir Java, detener el hijo administrado antes de ofrecer restart.
11. Salida normal de Unity: entrar STOPPING, cancelar probes/reconnect, cerrar TCP y **cerrar stdin**. Su EOF ordena shutdown idempotente en Java: dejar de aceptar, cerrar sockets, cerrar contexto Spring y liberar lock. Unity espera asíncronamente exit **10 s**; si no sale, termina sólo su hijo aún vivo y espera otros **2 s**. No matar por nombre ni por PID leído de un archivo; conservar handle propio y comprobar estado. No usar Thread.Abort.
12. Si Unity se cae/termina abruptamente, el SO cierra el extremo escritor de stdin; Java detecta EOF en su monitor independiente y ejecuta el mismo cleanup. El shutdown Java tiene watchdog total **10 s**: si hooks/contexto no terminan, finalización forzada de su propia JVM. Si Unity queda vivo pero colgado, lease sin PING válido **15 s** tras RUNNING desencadena el mismo cierre; durante STARTING se aplica deadline Java **20 s** hasta conexión inicial válida. Debug suspendido >15 s también provoca cierre, que debe quedar documentado; no desactivar la protección silenciosamente.
13. Java es responsable de autoeliminarse al perder al padre; Unity de terminar su hijo atascado mientras siga vivo. Una JVM completamente congelada que no procese EOF/watchdog no se puede garantizar eliminada con este diseño portable. Recovery: próximo arranque informa lock ocupado, con PID diagnóstico, solicita cierre manual del proceso identificado; nunca adopta/borra a ciegas. Si el PO exige eliminación incluso ante JVM congelada y padre muerto, considerar Windows Job Objects/supervisor SO Linux; añade adaptación nativa y requiere decisión aparte.
14. Restart del juego crea identidad nueva; el archivo lock puede persistir, su lock efectivo lo libera el SO al morir el proceso. No borrar el archivo bloqueado. Reintentar una vez obtenido el lock, sin dos hijos simultáneos; procesos manuales existentes no se reutilizan.

Todos los valores son configuración inicial del spike propuesta, no tuning final ni APIs implementadas. Debe haber comprobación por compatibilidad del backend Unity real; APIs .NET de cancellation/kill/tree no se asumen disponibles sin probarlas. Si cancellation no interrumpe un read concreto, cerrar el socket/pipe y observar la terminación de la tarea, evitando tareas abandonadas.

## Contrato mínimo

Contrato interno **IPC probe v1**, distinto de Gorilla Protocol móvil v1. No usar `sessionId`, `playerId` o sensor timestamp ficticios para satisfacer el DTO actual: todavía no hay sesión ni jugador. Referencia futura única en `Shared/Protocol/ipc/` (especificación y fixtures válidas/negativas); proyecciones mínimas C# en Shared/CSharp y Java en Server justificadas por runtimes distintos. Tests de ambos consumidores deben usar esas mismas fixtures; no copiar variantes ad hoc, cambiar fixture sensor ni generar el Gorilla Protocol completo. No se crea código ni fixtures ejecutables en esta tarea.

Framing propuesto: **uint32 big-endian**, longitud del cuerpo en bytes, seguido de JSON UTF-8 estricto. Longitud válida **1..4096 bytes**; rechazar 0/exceso antes de reservar buffer. Leer exactamente 4 bytes y luego N con lecturas parciales, deadline y cancellation; EOF a mitad de frame invalida la conexión. Un envío lógico no equivale a un receive. Un escritor por conexión; nunca intercalar frames.

Ejemplo del primer intercambio (token de muestra, no secreto real):

```json
{"ipcVersion":1,"type":"PING","instanceId":"00000000-0000-4000-8000-000000000001","connectionId":"00000000-0000-4000-8000-000000000002","sequence":1,"payload":{"launchToken":"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"}}
```

```json
{"ipcVersion":1,"type":"PONG","instanceId":"00000000-0000-4000-8000-000000000001","connectionId":"00000000-0000-4000-8000-000000000002","sequence":1,"payload":{}}
```

Campos exactos, duplicados/desconocidos rechazados, raíz objeto, profundidad máxima 4. `ipcVersion` entero 1; type PING sólo cliente y PONG sólo servidor; UUIDs canónicos; sequence entero positivo <=2147483647, comienza en 1 y aumenta por conexión. `connectionId` elegido por Unity cambia en reconnect; Java lo fija tras el primer PING autenticado. PONG refleja exactamente identity/correlation del PING. Primer PING lleva token base64url sin padding de 43 caracteres; Java lo comprueba contra el recibido al lanzar. Siguientes PING tienen `payload:{}`. Token incorrecto, versión/identidad/sequence/campos inválidos: cerrar, contabilizar y emitir motivo acotado local, sin reflejar payload ni secreto. Al alcanzar máximo sequence, reconectar con connectionId nuevo, nunca overflow.

Timeout de primer frame/token **1 s** desde aceptación. Rechazar conexiones adicionales mientras haya una autenticada activa; preautenticación acotada a 2 sockets, con deadline independiente, para que un cliente mudo no bloquee accept para el padre. No habilitar callbacks remotos arbitrarios. El PONG confirma únicamente transporte/proceso, no sesión ni resultado jugable.

READY es control de proceso fuera del TCP, documentado junto al contrato IPC. EOF en stdin es la única orden de cierre: no se añaden SHUTDOWN/ACK, torneo, HELLO móvil, replay o persistencia. Medir shutdown mediante petición local de cierre stdin y observación de exit, distinguiendo graceful/forced. Desconexión se mide por EOF/error/deadline; reconexión por primer PONG válido en connectionId nuevo.

## Failure modes

| Fallo | Detección y reacción propuesta | Evidencia esperada |
|---|---|---|
| JRE/JAR ausente, versión incorrecta o spawn denegado | FAILED, sin socket/retry automático; explicación recuperable. | Código, etapa, exit si existe; ninguna JVM extra. |
| Spring/IPC falla o READY no llega | Exit event o deadline startup 15 s; cleanup/terminación del hijo propio. | Tiempo/causa, stderr acotado; lock liberado. |
| Puerto fijo de prueba ocupado | Fallar bind explícitamente; no conectar al ocupante ni cambiar puerto en silencio. Producción propuesta usa port 0. | Listener del ocupante intacto, fallo identificable. |
| READY corrupta/ajena o PONG incorrecto | No RUNNING; fallo de validación y cleanup. | Identidades/correlación, sin token en logs. |
| Java cae durante operación | Exit event → FAILED, cancelar/socket dispose; sólo retry explícito. | Exit code, pendientes cancelados, restart con nueva identidad. |
| TCP cae con Java vivo | RECONNECTING, backoff/deadline; nuevo connectionId, misma JVM. | Reconnect contado; ningún segundo Java. |
| Java acepta pero no responde / frame parcial | Deadline → cierre/reconnect; lease Java limita abandono. | Timeout por etapa; tareas terminan. |
| Unity cierra o cae | stdin EOF → cierre Java/lock; kill del hijo sólo si Unity sigue vivo y se agota grace. | Exit real y listener cerrado, no sólo mock. |
| Segunda ejecución/servidor residual | Lock exclusivo → ALREADY_RUNNING; error visible. | No duplicado, no kill ajeno. |
| Payload inválido/exceso/flood | Rechazo acotado/close; limitar conexiones, queues y logging (máximo 10 eventos/s por categoría). | No crecimiento de memoria ni caída del host. |
| JVM congelada y Unity muerto | Límite del watchdog; informar lock ocupado en siguiente inicio. | No garantía falsa de cleanup; escalación a adaptación SO si se exige. |

## Seguridad

IPC hardcoded a loopback IPv4 en modo administrado, separado de `GORILLA_SERVER_ADDRESS`, que sólo pertenece al HTTP actual. No aceptar `0.0.0.0`, direcciones LAN, hostname externo ni negociación que cambie binding. No abrir firewall/HTTPS/cloud/cuentas en este incremento. HTTP también loopback en el spike; futura exposición móvil requiere revisión propia, sin ampliar IPC.

Loopback no es autenticación: un token efímero de lanzamiento reduce conexión accidental/suplantación básica. No protege contra malware o inspección por el mismo usuario/administrador; ese atacante queda fuera del threat model del spike. Token sólo en memoria/entorno privado heredado; nunca versionado, registrado ni enviado en URL. Comprobar límites antes de deserializar; sin polymorphic DTO typing, comandos de SO en payload ni datos personales. Logs con instanceId/connectionId/sequence/código/duración, sin payload ni token; retención y colas acotadas, exportación sanitizada. Cleanup de sockets, streams, procesos propios, tareas y lock en todas las rutas, también cancelación durante STARTING.

## Estrategia de pruebas

Plan pendiente de implementación; **ninguna prueba IPC ejecutada ni PASS** en esta tarea.

| Nivel | Casos mínimos | Qué demuestra |
|---|---|---|
| Unit lifecycle Unity | Start idempotente, carreras start/stop/retry, cancelación en cada etapa, exit antes de READY, READY malformada, timeout, límites de intentos, deadline/reconnect, cleanup dos veces, reload Editor. Reloj/supervisor de proceso inyectados. | Transiciones y ownership deterministas; no prueba OS/JVM. |
| Unit codec Java y C# | Fixtures comunes, fragmentación de header/body, múltiples frames en un read, UTF-8 inválido, duplicate keys, profundidad, tamaño 0/4097, entero fuera de rango, type/version/sequence/token incorrectos. | Contrato simétrico y asignación acotada. |
| Integración Java ↔ cliente simulado real | JAR/JVM real, socket loopback real, PING/PONG, fragmented writes, reconnect con misma JVM, cliente lento/mudo, timeout, conexión extra y puerto ocupado. | Framing y lifecycle Java real; un mock no cuenta. |
| Integración supervisor padre ↔ Java | Streams reales, READY/exit, stderr abundante y stdout corrupto, EOF stdin antes/después de READY, lock simultáneo, stop/restart, padre terminado abruptamente, JAR faltante y startup lento. | Pipes, prevención de duplicados y cleanup SO. |
| Unity ↔ Java real | PlayMode y Player de desarrollo con JAR real; 20 start/stop, 20 desconexiones/reconexiones, matar sólo Java propio, salida normal y terminación abrupta de Player, endpoint no accesible desde LAN, timeout y puerto ocupado de prueba. | Camino completo, compatibilidad runtime y ausencia de procesos propios residuales. |
| Rendimiento | Procedimiento siguiente por plataforma, sin fake sockets ni HTTP health en sustitución de PONG. | Startup/RTT/errores/reconnect/shutdown medidos. |

Java simulado y unit tests pueden correr en CI sin licencia Unity; suite real Unity requiere editor licenciado/Player y JRE disponible. Probar Linux de desarrollo primero y Windows antes de aceptar portabilidad Windows; hardware oficial sigue pendiente. En casos normales/caída de padre exigir cero JVM propias residuales después de deadlines; caso JVM congelada documenta el límite, no se marca PASS universal. No hay tests de teléfonos/cámara/gameplay en este plan.

## Presupuesto de latencia

Valores propuestos de aceptación del **probe**, pendientes de PO y medición; no estimaciones presentadas como resultados:

| Métrica | Presupuesto inicial | Medición |
|---|---|---|
| Startup | P95 <=10 s; todas las ejecuciones <=15 s o error explícito | Reloj monotónico Unity desde antes de spawn hasta primer PONG válido; 20 launches, indicar primer arranque y warm filesystem. |
| RTT transporte | P50 <=5 ms; P95 <=10 ms; máximo <=50 ms en baseline idle | Unity worker monotónico: antes de serializar/escribir hasta validar PONG, incluye ida/vuelta y Java. No restar relojes de procesos. |
| Errores steady-state | 0 en baseline de 10 min | Total intentos, éxitos, timeout, inválidos, disconnect, error de I/O; fallos inyectados separados. |
| Reconnect con Java vivo | 20/20, cada recuperación <=10 s | Desde detección del fallo hasta nuevo primer PONG; reportar también instante de corte y delay de detección. |
| Shutdown | Graceful <=10 s; fallback de hijo observado <=12 s si Unity vivo | Desde cierre stdin hasta exit; forced por separado, no ocultarlo como graceful. |

Tomar 100 PING de warmup y luego al menos 1000 secuenciales a 50 Hz, un pendiente a la vez; registrar cadencia efectiva y deadlines incumplidos, sin omitir respuestas lentas. Mantener también una corrida de 10 min. P50/P95 por nearest-rank sobre RTT exitosos ordenados; publicar N válido, fallidos, max y tasa de error, nunca percentiles solos ni imputar timeouts como éxitos. Una corrida con errores falla aunque sus percentiles sean bajos. Repetir 3 veces; reportar cada corrida, no ocultar picos promediándolas. Registrar SHA de ambos componentes, hash JAR/Player, Unity/backend, JVM, OS/CPU/RAM, carga, configuración y estado cold/warm. No invocar APIs nuevas de Stopwatch/.NET sin verificar perfil Unity; usar reloj monotónico soportado.

Medir por separado entrega Unity al hilo principal y coste/frame bajo carga; RTT del worker no incluye que gameplay consuma el mensaje en un frame. El objetivo v4 P95 <150 ms es end-to-end futuro teléfono→gameplay; **no se acepta ni se extrapola desde PING/PONG**, tampoco se divide RTT/2 para afirmar latencia one-way. Si la prueba excede presupuesto, investigar colas/GC/framing y comparar opciones antes de optimizar o cambiar stack. No ajustar umbrales para fingir cumplimiento.

## Cambios necesarios

Diseño aprobado; ejecutar por subincrementos, comenzando sólo con 2A después del merge normal de #90:

1. Actualizar v4 §5 con la aclaración de autoridad aprobada, y guías que describen foundation/IPC como pendientes. Verificar primero el SHA integrado de foundation.
2. Acordar contrato IPC probe/READY y fixtures compartidas en Shared/Protocol; proyecciones mínimas C#/Java y tests cruzados, sin alterar Gorilla Protocol móvil.
3. Unity: servicio persistente de supervisión, adaptador socket/codec, configuración de rutas/timeouts y telemetría del probe. Integrar desde Bootstrap sin bloquear frames; sin minijuegos ni sesión de torneo.
4. Java: listener TCP separado del HTTP, lock de instancia, bootstrap/READY/monitor stdin, token efímero, lifecycle Spring y métricas. Usar JDK/Spring existentes, sin Netty/gRPC/nueva dependencia por defecto.
5. Preparar runner simulado, fault injection y pruebas reales Player/JVM. Empaquetar JAR y seleccionar JRE/layout offline por plataforma aprobada; no exigir Maven/Node en ejecución final.
6. Actualizar informe de pruebas con resultados reales/SHA y limitaciones; abrir PR pequeño a develop con revisión Arturo/Hiram o integrantes reales autorizados, sin inventar usuarios ni aprobación.

Ahora sólo se actualizan documentos y v4 §5 por aprobación explícita; no se cambia pom, manifest, lockfile, código, fixtures ni workflows. No se publica PR ni se realiza commit de esta preparación sin una solicitud adicional; el documento local está listo para revisión.

## Riesgos y decisiones requeridas

| Decisión / riesgo | Propuesta concreta | Quién resuelve y condición |
|---|---|---|
| Autoridad y frase v4 §5 | Unity confirma todo resultado; Java transporta. | Aclaración aprobada y aplicada; revisión técnica conjunta Arturo/Hiram antes de handlers. |
| IPC y contrato | TCP loopback, length-prefix, JSON probe v1 y bootstrap por pipes. | Recomendación aprobada; contrato mínimo aceptado, validar proyecciones mediante fixtures/tests. |
| Lifecycle y recovery | Unity padre, retry explícito limitado, lock por usuario/producto; EOF/lease/watchdog con límite ante JVM congelada. | Diseño aceptado; resiliencia avanzada diferida fuera de 2A. Contención SO exigiría diseño adicional. |
| PC/plataforma/backend/JRE | Desarrollo Linux primero; Windows no validado. Bundle privado de runtime Java 21, distribuidor/licencia/layout pendientes. | PO elige plataforma de presentación; equipo verifica distribución. PC pendiente no impide aprobar diseño portable ni un spike Linux explícitamente autorizado. |
| Presupuestos | Umbrales de probe y deadlines de este documento. | Presupuesto de diseño conservado para hardening, no gate de benchmarking exhaustivo 2A. Smoke inicial según precisión del PO; cambios posteriores requieren evidencia. |
| Foundation no integrada en remoto observado | PR #90 abierto. | Revisión/integración normal y nuevo fetch; no merge forzado. |
| APIs Unity/Flatpak | Process, pipes, cancellation y filesystem del bundle deben funcionar en Player real; sandbox del Editor puede diferir. | Prueba técnica de Incremento 2; no aceptar sólo tests C# de escritorio. |
| Diagnósticos anteriores de entorno | Mantener seguimiento [#89](https://github.com/Josue1855/Gorilla-Escape/issues/89), sin atribuirlos al nuevo IPC. | Investigación separada; no exigir log vacío como gate. |

Existe aprobación explícita de DEC-005, con ejecución gradual y gate de integración. El documento no fija fechas nuevas; selección de PC/JRE de distribución y hardening siguen pendientes de validación. No existe evidencia actual que requiera mover autoridad a Java.

## Criterios de aceptación

**Preparación de esta tarea:** comparación de las cuatro opciones completa; ownership y discrepancia normativa explícitos; lifecycle/readiness/recovery/cleanup definido; contrato mínimo y límites definidos; medición, pruebas y cambios futuros enumerados; decisiones pendientes separadas de aprobaciones. Revisión documental y de enlaces registrada en [progreso](DEVELOPMENT_PROGRESS.md). No declara IPC implementado ni medido.

**Gate de implementación 2A:** diseño aprobado por PO; foundation integrada normalmente y verificada en develop. Obtener el nuevo SHA, crear rama nueva desde ese SHA, confirmar correspondencia con el PR validado y reportar SHA/rama/archivos/tests/riesgos/aceptación antes de código. Layout de desarrollo puede ser provisional explícito; aprobación de distribución/JRE final y PC oficial exigida antes de aceptación de presentación.

**Aceptación futura del Incremento 2:** build de componentes y tests aplicables PASS; PING/PONG Unity↔Java reales con framing/correlación correctos; métricas dentro del presupuesto aprobado; readiness por evento+PONG sin sleeps; fallos de tabla cubiertos; cero duplicados ni hijos residuales en escenarios soportados; cleanup/cancellation verificables; IPC inaccesible por LAN; reinicios sin segunda autoridad; evidencia sanitizada con SHA exactos y PR revisado/integrado. Simulado por sí solo no cumple integración Unity. Si la PC oficial sigue pendiente, separar PASS de desarrollo de BLOCKED/NOT RUN de presentación.

**Estado actual:** DEC-005 Accepted; v4 §5 actualizada; implementación 2A BLOCKED por integración de #90, IPC NOT STARTED; mediciones IPC NOT RUN; PC de presentación pendiente; Fase 0 IN PROGRESS; Fase 1/2B NOT STARTED.


## Precisión aprobada — Incremento 2A

La aprobación del PO no exige implementar todo el diseño anterior de una vez. Se conserva íntegro como objetivo de Incremento 2; las siguientes reglas delimitan el primer subincremento y prevalecen sobre el gate completo de hardening.

**Gate previo obligatorio:** esperar revisión/integración normal de #90, sin modificarlo ni forzar merge. Después fetch develop, verificar SHA nuevo y contenido funcional contra la evidencia validada del PR (considerar merge/squash, no exigir igualdad de SHA del PR). Crear una rama nueva desde esa base, preservar esta documentación aprobada y reportar nuevo SHA, rama, archivos previstos, tests previstos, riesgos y aceptación antes de modificar código.

**Incluido:** hijo Java iniciado por Unity; rutas configurables de desarrollo a JDK 21/JAR existentes, sin distribución final JRE; stdout READY y stderr drenado; TCP 127.0.0.1:0; framing/JSON/4096 bytes; PING/PONG con instanceId/connectionId/sequence y token efímero; validación básica; startup/socket/read/write deadlines; cancellation, EOF stdin, cleanup y logs estructurados. Unit codec, tests Java reales y Unity↔Java real en PlayMode/Player. No sumar frameworks ni cambiar versiones.

**Diferido, sin borrar del diseño:** reconnect sofisticado, restart automático, fault injection extensa, flood testing completo, watchdog avanzado, lease/contención nativa Job Objects, benchmarking exhaustivo y optimización. La prevención básica de lanzar dos hijos propios y stop/start concurrentes pertenece al supervisor mínimo; el hardening global entre instancias/SO queda para revisión posterior. Una caída en 2A produce error recuperable y cleanup; no prometer resiliencia completa ni implementar de inmediato todas las filas de la matriz futura.

**Excluido:** Gorilla Protocol móvil, PWA, QR, sensores, cámara, gameplay, scoring, Input Fusion y Fase 1. No continuar automáticamente a 2B.

**Archivos previstos, todavía no creados:** Shared/Protocol/ipc para especificación/fixtures; Shared/CSharp/Runtime para DTO/codec puros; Server/.../ipc para listener/probe y lifecycle administrado, configuración y tests; Unity/.../Core y Network para supervisor/cliente/configuración; tests Unity EditMode/PlayMode y evidencia documental. Nombres finales y wiring se confirmarán al leer el develop integrado; no alterar la fixture móvil ni generar escena/settings manualmente.

**Pruebas previstas:** codec cruzado con fixtures comunes (framing parcial, longitud/tipo/correlación/token inválidos); Java/JAR real con cliente simulado/socket real y cierre por EOF; Java iniciado por Unity con READY+PONG en PlayMode y Player y cierre normal verificado por exit y ausencia de hijo residual. Timeouts/cancellation básicos y mensajes de error comprensibles; mocks sólo para apoyar unit tests. No considerar un health HTTP o un mock prueba de integración IPC.

**Smoke measurement:** startup desde antes de spawn hasta primer PONG válido; 10 warmup + 100 PING secuenciales por ejecución real PlayMode/Player como muestra inicial propuesta, con cadencia suave y un pending, sin saturar. Publicar N, RTT mínimo/P50/P95/máximo y errores, reloj monotónico en Unity y nearest-rank para percentiles de éxitos; informar timeouts aparte. Resultados de cada entorno por separado y hashes/versions. No optimizar ni confundir smoke con certificación estadística. Las 1000 muestras/50 Hz/10 minutos y repetición exhaustiva se reservan a performance/hardening; no extrapolar a teléfono→gameplay.

**Aceptación 2A:** evidencia real Java iniciado por Unity; READY válido más PING/PONG; framing correcto y binding sólo loopback; cierre limpio de ambos lados y cero Java propio residual después de cierre normal; errores recuperables comprensibles; tests aplicables PASS. Evidence con SHA exacto, log sanitizado y conteo de errores; forced cleanup se informa y no sustituye graceful. PC final pendiente no se declara validada.

**Riesgos vigentes 2A:** integración #90 pendiente; APIs de Process/pipes/cancellation en Player y entorno Flatpak; gestión correcta de stderr/stdout para evitar bloqueo; diferencias backend/SO; selección de rutas Java/JAR; límites de recuperación diferida. Validación inicial en desarrollo Linux, sin atribuir portabilidad Windows ni hardware final a los resultados.

## Gate de integración verificado

2026-10-06: PR #90 MERGED normalmente; develop `2a635b607a286bb70d1457aa0cf34f7e85f17abe` contiene `38e1e31ce0e36952b759061eee40136f7a0ef3f6` y 76/76 hashes funcionales validados. Rama `feature/gameplay-phase0-unity-java-ipc-2a` creada desde ese SHA. Las menciones anteriores a #90 abierto quedan como histórico del proceso; el gate está resuelto.

## Resultado del Incremento 2A

Gate previo cumplido, implementación mínima de desarrollo Linux validada; resultados y límites en [TEST_REPORT](TEST_REPORT.md), IPC-002A. Java8/8, EditMode7/7, PlayMode3/3 y Player real PASS; cierre normal exit0 sin hijo residual. Sólo rutas de desarrollo, sin empaquetado JRE final. El diseño de resiliencia anterior sigue diferido, incluidos watchdog/lock global entre instancias y parser Unity strict/fuzzing completo. 2A **PASS / MERGED** por PR #91, merge normal autorizado por PO; develop `9fbb566e5f24af8f4e92fa3c139968161e832d46` contiene el commit validado. Incremento2/Fase0 IN PROGRESS; 2B/Fase1 NOT STARTED. No se inicia trabajo adicional. Los estados BLOCKED anteriores son históricos y quedan superados por el gate de integración verificado.

## Precisión aprobada — Incremento 2B

El PO aprobó [el diseño 2B](PHASE0_IPC_2B_DESIGN.md) y sus precisiones: estados STOPPED/STARTING/CONNECTING/RUNNING/STOPPING/FAILED; liveness PING/PONG 1 Hz sólo RUNNING y un pendiente; cero restart/reconnect automático; recuperación manual con tres lanzamientos por ejecución y cleanup completo antes del siguiente; generaciones aisladas; FileChannel.tryLock antes de Spring/READY con liberación finally; EOF temprano y shutdown propio 10+2 s/readers 2 s aparte. Presupuestos y autoridad DEC-005 se conservan. La resiliencia avanzada de esta decisión sigue diferida. Estado y evidencia en TEST_REPORT; abrir PR sin merge automático, no iniciar 2C/Fase 1.

## Integración normal del Incremento 2B

2B PASS / MERGED por PR #92 autorizado por el PO; develop `7e3aa60aa53c65e7051f32477a77985fbe628907` contiene `88f1e309fc7adeffe6aa6499e195f53185ab36f9`, con árbol idéntico al candidato y 105/105 hashes funcionales validados. Evidencia 2B conservada en TEST_REPORT. Protecciones intactas: PR/checks obligatorios, force push deshabilitado. 2A PASS/MERGED; Incremento 2/Fase 0 IN PROGRESS; 2C/Fase 1 NOT STARTED. Sin rama ni implementación 2C.

Checks posteriores del merge 2B en `7e3aa60aa53c65e7051f32477a77985fbe628907`: **management-validation PASS / java-react-foundation PASS**. Protecciones comparadas antes/después, idénticas. Cierre de integración; 2C no iniciado.

## Precisión aprobada — Incremento 2C

El PO aprueba [PHASE0_IPC_2C_DESIGN](PHASE0_IPC_2C_DESIGN.md) como Accepted para hardening local y validación sostenida: parser C# exclusivamente probe v1, sin dependencia nueva; escritor Java único y deadline cancelable ligado al socket, sin pool común/cola de PONG/thread por mensaje; framing/JSON/corpus y presión finita; tres Players independientes con 100 warmup +1000 muestras a 50 Hz y una corrida de al menos600 s con heartbeat1 Hz. Publicar resultados individuales íntegros; RSS/heap sólo observación y umbral de investigación, sin afirmar ausencia global de fugas. Presupuestos DEC-005 y lifecycle/ownership/recovery manual de2B se mantienen. Gate completo antes de commit/push/PR; no merge automático. Incremento2 sigue IN PROGRESS hasta integración de2C; Fase0 IN PROGRESS/Fase1 NOT STARTED. [Procedimiento](PHASE0_IPC_2C_VALIDATION.md).
