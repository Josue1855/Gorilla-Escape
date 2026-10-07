# Incremento 2B — lifecycle y fallos básicos

**Fecha:** 2026-10-06. **Estado:** Accepted por el Product Owner el 2026-10-06, con precisiones obligatorias registradas abajo. Implementación exclusivamente 2B autorizada. Base remota comprobada: `develop = 9fbb566e5f24af8f4e92fa3c139968161e832d46`. 2A **PASS / MERGED** por PR #91. Incremento 2/Fase 0 IN PROGRESS; Fase 1 NOT STARTED. La implementación 2B se registra en TEST_REPORT.

## 1. Alcance exacto y revisión de 2A

Fuentes revisadas: [DEC-005](DECISIONS.md), [v4](Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md) §§3/5/38/40/48–50, [TEST_REPORT](TEST_REPORT.md) IPC-002A, [DEVELOPMENT_PROGRESS](DEVELOPMENT_PROGRESS.md), [contrato IPC](../Shared/Protocol/ipc/README.md), supervisor/runner Unity, codec compartido y Java, ManagedProbe, configuración Spring, tests Java reales y tests Unity.

| Lo existente en 2A | Consecuencia para 2B |
|---|---|
| TCP loopback/puerto 0, token/UUID/sequence, frame UTF-8 uint32 big-endian <=4096 y fixtures comunes | Reutilizar sin cambiar contrato, DTO móvil, codecs, versiones o dependencias. |
| Startup hasta primer PONG en 15 s, connect de 2 s dentro de ese presupuesto, read/write de 2 s y cancellation que cierra socket | Mantener los presupuestos y reutilizar primitivas; diferenciar causas/estados y comprobar sus rutas fallidas. |
| RunAsync ejecuta 10 warmup + 100 PING y siempre llama StopAsync al terminar | Es un smoke finito, no un lifecycle persistente. Conservar ese modo como wrapper de regresión y separar inicio/operación/cierre para las pruebas de lifecycle. |
| bool started impide repetir arranque en el mismo supervisor; stage sólo etiqueta excepciones | No protege entre JVM distintas ni permite recuperación manual del mismo servicio. Sustituir por estados y ownership por intento, sin segundo supervisor general. |
| Unity drena stderr en chunks de 1024; stdout primero lee READY hasta 4096 caracteres y luego drena; error original se conserva | Conservar lectores; observar su terminación en todas las rutas y clasificar errores. No introducir un segundo parser de logs para readiness. |
| StopAsync cierra TCP/stdin, espera exit durante 10 s, termina sólo su hijo si vence y observa 2 s más; drains hasta 2 s | Reutilizar el camino; hacerlo idempotente ante stop/cancel/exit concurrentes y conservar resultado primaryError + cleanupOutcome. |
| SHUTDOWN_FORCED también se usa si Java sale por sí mismo con exit distinto de 0 | Diferenciar exit inesperado, exit no limpio y terminación forzada; no afirmar que hubo kill si no lo hubo. |
| Application.wantsToQuit cancela y espera Execute; OnDestroy sólo cancela; Dispose sólo cancela CTS | Cubrir cierre durante RUNNING/startup y teardown Editor; observar cleanup, liberar CTS/suscripciones y evitar múltiples FinishQuit. |
| Java vigila stdin sólo en ApplicationReadyEvent, después de iniciar Spring/HTTP y listener; destroy cierra sockets, sin join explícito | Añadir bootstrap administrado mínimo para lock/EOF temprano, coordinar cierre seguro de contexto y esperar workers con el presupuesto existente. |
| Java recibe tráfico con read timeout de 2 s; no hay monitor independiente de exit en Unity | EOF/read/write pueden revelar pérdida, pero hay que observar process exit como evento y asignar causa correcta. Un TCP caído no prueba Java muerto. |
| Tests: Java 8/8, EditMode 7/7, PlayMode 3/3, Player real/exit 0, error de ruta ausente | Mantenerlos; añadir casos de fallo, singleton y transiciones. No repetir trabajo de codec ni certificar casos nuevos mediante esas cifras. |

**Incluido sólo en 2B:** muerte de Java; READY ausente; conexión rechazada/perdida; segundo Java administrado; cierre Unity y EOF padre; cancelación en startup/operación; cleanup observado; estados explícitos; errores diferenciados; recuperación manual acotada. Ninguna lógica de sesión competitiva.

Para sostener una conexión técnica RUNNING hasta Stop, usar el **mismo PING/PONG** de vida a cadencia de 1 s, un request pendiente, con deadlines ya existentes. Es necesario para distinguir una conexión viva de una rota y evitar que el read timeout de 2 s de Java cierre un socket simplemente ocioso. No crear mensaje HEARTBEAT nuevo, telemetry pipeline, benchmark ni watchdog. El smoke finito de 2A mantiene sus 100 muestras y cierre explícito; en 2B el servicio técnico puede quedar abierto hasta stop/cancel. Ésta es una extensión explícita del lifecycle y requiere aprobación del diseño.

## 2. Modelo de estados

Estados: **STOPPED, STARTING, CONNECTING, RUNNING, STOPPING, FAILED**. No RECONNECTING: 2B no reconecta al socket anterior ni reinicia automáticamente Java. La recuperación manual inicia otro intento completo con identidad/token/puerto nuevos.

| Estado | Evento / condición | Siguiente estado | Acción |
|---|---|---|---|
| STOPPED | Start requested, configuración válida, presupuesto de lanzamientos disponible | STARTING | Crear intento y cancellation propios, drenar pipes desde spawn, observar exit. |
| STOPPED | Configuración/ruta inválida | FAILED | Error CONFIG_INVALID; sin proceso ni listeners. |
| STARTING | process started | STARTING | Registrar handle/PID propios; no declarar readiness. |
| STARTING | READY received válido del hijo esperado | CONNECTING | Fijar endpoint 127.0.0.1 y conectar dentro del tiempo restante de startup. |
| CONNECTING | TCP connected | CONNECTING | Enviar primer PING con token; socket abierto no basta para RUNNING. |
| CONNECTING | Primer PONG válido | RUNNING | Publicar disponibilidad técnica; iniciar probe de vida cada 1 s. |
| STARTING/CONNECTING/RUNNING | Java exits fuera de stop | STOPPING | Guardar JAVA_EXITED + exitCode, cancelar I/O, iniciar cleanup. |
| STARTING | READY inválido / EOF stdout sin READY / timeout | STOPPING | Guardar READY_INVALID / READY_MISSING / READY_TIMEOUT. |
| CONNECTING | Connect rechazado / timeout / primer PONG fallido | STOPPING | CONNECT_FAILED / CONNECT_TIMEOUT / HANDSHAKE_FAILED. |
| RUNNING | connection lost, EOF TCP, error I/O o deadline | STOPPING | CONNECTION_LOST / IO_TIMEOUT; cerrar socket y detener hijo propio, sin replay. |
| STARTING/CONNECTING/RUNNING | cancel o shutdown requested | STOPPING | Registrar razón intencional; cerrar socket/stdin y observar exit. |
| RUNNING | Smoke finito terminado correctamente | STOPPING | Stop normal del wrapper de 2A; no es fallo. |
| STOPPING | Cleanup completo, sin fallo primario y cierre normal | STOPPED | Resultado cancelado o detenido intencionalmente; no confundir cancel con error técnico. |
| STOPPING | Cleanup completo con fallo primario, exit no limpio o fallback forzado | FAILED | Publicar error primario y resultado de cleanup por separado; recuperar sólo manualmente. |
| STOPPING | Child sigue vivo después de deadlines / cleanup incompleto | FAILED | CHILD_RESIDUAL/CLEANUP_FAILED; bloquear reintento y conservar handle diagnóstico mientras exista. |
| FAILED | Retry requested explícito, cleanup confirmado y lanzamientos disponibles | STARTING | Nuevo intento completo; nunca adoptar otro proceso. |
| FAILED | Reset explícito sin lanzar, recursos propios terminados | STOPPED | Mantener histórico del fallo; no borrar evidencia. |
| STOPPING/STARTING/CONNECTING/RUNNING | Segundo Start/Retry | Sin cambio | Rechazar BUSY sin nuevo proceso. |
| STOPPED | Stop/Cancel repetido | STOPPED | No-op idempotente. |
| STOPPING | Stop/Cancel/Exit concurrentes | STOPPING | Compartir la misma tarea de cleanup; no duplicar Close/Kill/quit. |

Invariantes: máximo un intento activo/un hijo por servicio Unity; RUNNING exige READY+PONG; nunca FAILED→RUNNING directo; ningún start antes de comprobar el fin del intento anterior. FAILED describe fallo, no demuestra ausencia de recursos: publicar **cleanupComplete** de forma separada. Todos los eventos de un intento llevan instanceId/generación; los eventos tardíos no cambian el estado de un intento nuevo.

Un reductor pequeño de transiciones, reloj/deadline y adaptador de proceso son suficientes para unit tests. No introducir framework/DI/event bus. La transición se serializa; callbacks de socket/process no modifican UnityEngine desde worker. Notificaciones Unity por canal acotado, conservando todos los eventos de estado/errores terminales; no encolar cada PING. Error de capacidad falla de forma visible, no pierde un terminal silenciosamente.

## 3. Política de retry/recovery

**Cero retries/restarts/reconnects automáticos.** Máximo **3 lanzamientos reales por ejecución Unity**, incluido el inicial: como máximo dos reintentos manuales, manteniendo el límite inicial de DEC-005. No contar validación de ruta que no creó proceso. Un segundo proceso rechazado por singleton sí cuenta como lanzamiento. Cambiar de escena no reinicia el contador. Agotado el límite, pedir cerrar/reabrir la prueba; no reset automático oculto.

Java muerto, TCP roto o timeout llevan a cleanup y FAILED. Si TCP cae pero Java vive, cerrar stdin y esperar/terminar sólo ese hijo antes de ofrecer Retry. No reutilizar Java con otra conexión en 2B. Java ajeno/otro supervisor conserva su sesión técnica; el proceso rechazado sale sin modificarlo.

Recuperación Unity: una acción explícita **Reintentar backend local** y **Detener** en la superficie mínima de diagnóstico de desarrollo del runner, utilizable en Player y Editor. Mostrar estado y mensaje sencillo; Retry habilitado sólo en FAILED+cleanupComplete+presupuesto disponible. No UI de producto, menús de juego ni PWA. El harness puede invocar la misma acción para reproducir el test, sin convertirlo en política automática. Configuración incorrecta debe corregirse antes del reintento; token nunca visible.

## 4. Singleton Java

| Opción mínima | Evaluación |
|---|---|
| FileChannel.tryLock exclusivo | **Recomendado:** Java 21 estándar, entre JVM cooperativas, no cambia puerto ni IPC. Adquirir una vez, canal abierto toda la vida. |
| Ownership de puerto | Puerto0 asigna puertos diferentes y no evita dos servidores. Puerto fijo introduciría conflicto/discovery y cambiaría decisión aprobada. No adoptar. |
| Archivo marcador/PID o bandera Unity | Puede quedar stale, sufrir PID reuse y no excluye otras JVM. Sólo diagnóstico/invariante interna, no autoridad de singleton. |
| Mutex nativo/Job Objects/supervisor externo | Adaptación adicional innecesaria para este alcance; diferida. |

**Scope:** una JVM en modo administrado por usuario y producto, incluso entre dos Players Unity/copias del repo del mismo usuario. No bloquear el servidor HTTP manual no administrado ni otros usuarios. No usar ruta en JAR/repo/cwd/temp único por intento: rompería el scope.

Unity resuelve una carpeta local de datos por usuario/producto fuera del repositorio y pasa una ruta absoluta no secreta al bootstrap administrado, igual para todos los intentos y visible dentro de Flatpak. Java valida carpeta local/escribible y abre `managed-java.lock` sin truncarlo, antes de Spring/HTTP/listener/READY. Usar un canal único, lock exclusivo no bloqueante. Failure de permisos/FS: LOCK_UNAVAILABLE; lock ocupado: ALREADY_RUNNING, exit propuesto 73 reservado/documentado del producto, **sin READY ni sockets**, log stderr estructurado. No crear un nuevo mensaje IPC para error previo a READY; Unity distingue este exitCode y el estado inicial.

No eliminar ni renombrar el archivo lock al cerrar, ni aceptar un PID del archivo como permiso de kill. Un archivo existente sin lock válido no bloquea: tryLock puede adquirirlo. El lock lo libera Java al cerrar el canal/JVM; un crash no deja un lock válido stale. Si el proceso vivo está colgado y retiene lock, no forzar unlock ni borrar el archivo: informar «Ya existe un backend local administrado» y dejar decisión manual al propietario. No prometer eliminación de JVM congelada con padre muerto.

La documentación oficial de [FileLock](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/nio/channels/FileLock.html) describe liberación por canal/JVM y dependencia del SO; tratar locks como cooperativos. [FileChannel.tryLock](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/nio/channels/FileChannel.html#tryLock()) permite intentar sin esperar. Probar localmente Linux y después Windows antes de afirmar portabilidad validada; no usar FS de red. La ruta/permisos y misma vista de archivo entre sandbox/host son condiciones de aceptación.

## 5. Timeouts

No reducir ni ampliar presupuestos DEC-005/2A. Reloj monotónico, deadlines absolutos, cancellation y eventos; sin sleeps como readiness.

| Etapa | Valor propuesto | Regla y resultado |
|---|---:|---|
| Startup total | 15s | Desde antes de spawn hasta primer PONG válido; deadline único, nunca sumar 15 s por etapa. |
| READY | Hasta el resto del startup de 15 s | READY debe dejar tiempo para connect/PONG dentro del mismo budget. Al agotarse sin READY: READY_TIMEOUT. No crear otro timeout adicional de 15 s. |
| TCP connect | <=2s | min(2s, tiempo restante del startup); negativa/refused distinta de timeout. |
| Primer PONG | Read/write <=2s cada uno | Además limitados por startup restante; token/version/identidad fallidos = HANDSHAKE_FAILED. |
| RUNNING read/write | <=2s cada operación | PING cada 1 s tras completar el anterior; un pendiente. Java: 2 s sin recibir conserva su política actual. Scheduler lento puede producir timeout real y se informa. |
| Detección de exit Java | Evento + comprobación inmediata | Suscribir antes de que un exit rápido se pierda; consultar HasExited tras suscripción. No esperar al siguiente PING para un exit conocido. |
| Shutdown / espera después de EOF | 10s total | El reloj empieza al cerrar stdin, no 10 s adicionales después de otro shutdown. Java close/contexto/workers deben caber aquí; Spring mantiene fase de 5 s. |
| Terminar hijo propio si 10 s vencen | +2s | Único fallback existente, observado. Forzado no cuenta como graceful. |
| Drenaje/finalización lectores | <=2s | Mantener límite de 2A; correrlo dentro de la ventana de shutdown si los pipes ya alcanzaron EOF. Si sólo se puede observar tras exit, límite total de finalización supervisor de 14 s (10+2+2), distinto de límite de exit de 12 s. No redefinir 12 s como garantía de todos los recursos. |
| Retry manual | Sin temporizador/bucle | Sólo tras cleanupComplete y acción explícita; lock ocupado no se poll/reintenta por sí mismo. |

Medir detection, EOF→exit y cleanup completion por separado; no confundir timeout de prueba con budget de producto. Los harnesses esperan eventos con deadlines mayores que la operación para capturar resultado (p.ej. 20 s tras cancel), nunca sleeps para declarar éxito.

## 6. Ownership, EOF y cleanup

Unity conserva el handle **del proceso que lanzó** y sus streams/socket/CTS/tasks/subscriptions; no busca ni mata `java` por nombre, no usa PID ajeno, no adopta servidor existente. Por intento instanceId/token/connectionId nuevos; primero cleanup/exit observado, después crear otro. Java no posee ni confirma estado competitivo: sólo backend/transporte, sin nuevo GameSession/GP/scoring.

Mover el guard administrado y la detección EOF al bootstrap de Java **antes de iniciar Spring**, exclusivamente en modo administrado. EOF temprano establece stopRequested; no debe publicar READY después de observarlo. Coordinar contexto cuando esté en un punto válido de cierre; no llamar context.close a ciegas durante refresh concurrente. Si EOF llegó antes de tener contexto, registrar intención y cerrar al publicarlo/listo. Un bootstrap realmente bloqueado se resuelve por el fallback del padre: 10 + 2 s, no por nuevo watchdog; informar FORCED, no graceful. Crear/release lock dentro de un único ownership que cubra fallos de boot y hooks. Salida normal/rechazo singleton/startup fallido deben liberar sólo sus propios recursos.

Un único camino de cleanup Unity por intento, esperado por Stop/Cancel/Quit y callbacks concurrentes. Orden: marcar STOPPING/inhibir start → cancelar operaciones/PING → cerrar socket → cerrar stdin una vez → observar exit/recolectar exitCode → fallback propio si procede → terminar/observar lectores y tareas → liberar streams/CTS/subscriptions/Process → confirmar cleanupComplete. Conservar la causa primaria incluso si falla el cleanup; publicar ambas, sin ocultar CHILD_RESIDUAL. No desechar el último handle disponible de un hijo vivo y habilitar Retry por error.

En Java, EOF/cierre esperado coordina running=false, cierre de listener/client, contexto Spring y workers; joins/acotación compatibles con shutdown existente. Dispose repetido y listener parcialmente inicializado no deben omitir recursos porque running aún sea false. No recrear executor por request; tampoco cambiar arquitectura por anticipado: comprobar terminación del write pendiente al cerrar su socket y observar su Future. No Thread.Abort ni waits en el hilo Unity de frames.

`Application.wantsToQuit`: pedir stop una vez, esperar tarea de cleanup y permitir salir sin ciclo de FinishQuit duplicado; conservar suscripción hasta que sea seguro permitir quit. Stop/reload Editor también cancela/observa recursos mediante hooks mínimos de Editor si hicieran falta. La salida abrupta del padre debe cerrar el pipe y activar EOF Java; test real acotado. JVM congelada/padre muerto queda fuera de garantía de 2B y documentada, no escondida bajo «cero residual» universal.

## 7. Errores y seguridad

| Código propuesto | Mensaje Unity | Retry |
|---|---|---|
| CONFIG_INVALID / SPAWN_FAILED | No se pudo iniciar el backend local. Revisa las rutas de Java y del servidor. | Tras corregir y sin recursos activos. |
| ALREADY_RUNNING | Ya existe un backend local administrado. Cierra la otra ejecución antes de reintentar. | Manual, nunca cerrar al ocupante. |
| LOCK_UNAVAILABLE | No se pudo reservar la ejecución local. Revisa la carpeta de datos y sus permisos. | Tras corregir causa. |
| READY_TIMEOUT / READY_MISSING | El backend no quedó listo a tiempo / terminó antes de quedar listo. | Tras cleanup. |
| READY_INVALID / HANDSHAKE_FAILED | El backend no respondió con la identificación esperada. | Tras cleanup; corregir versión/configuración. |
| CONNECT_FAILED / CONNECT_TIMEOUT | No se pudo conectar con el backend local. | Tras cleanup. |
| JAVA_EXITED | El backend local se cerró inesperadamente. | Tras confirmar exit y cleanup. |
| CONNECTION_LOST / IO_TIMEOUT | Se perdió la conexión / el backend dejó de responder. | Después de detener sólo el hijo propio. |
| SHUTDOWN_FORCED | El backend no se cerró a tiempo y se terminó la ejecución propia. | Sólo si exit/cleanup confirmados. |
| CLEANUP_FAILED / CHILD_RESIDUAL | No se pudo completar el cierre. Reintento bloqueado hasta finalizar la ejecución propia. | No habilitado. |

Cancel/Stop intencionales no muestran una falsa caída. Si exit y socket EOF compiten, un exit del hijo confirmado prevalece como causa JAVA_EXITED; si el exit no se confirmó al detectar fallo TCP, publicar CONNECTION_LOST y agregar exit posterior como evidencia, sin mutar retroactivamente logs. En stop solicitado, un exit esperado es resultado de shutdown, no fallo primario nuevo. La observabilidad registra orden de eventos real.

Logs estructurados: component, timestamp monotónico/duración, instanceId, estado anterior/nuevo, evento, errorCode, exitCode, terminationRequested/forced, cleanupComplete y contador de lanzamientos; connectionId cuando aplique. Sólo eventos de estado y operaciones relevantes, buffers acotados. Sin secretos/payload, sin stack trace en la superficie del usuario. stderr se drena aun durante cancel; no usar texto arbitrario de stderr como orden de control.

Mantener loopback 127.0.0.1, token efímero de 256 bits en entorno/memoria/primer PING, frame de 4096 bytes, UUID/sequence y stdout READY existentes. Sin cloud/cuentas, nuevos mecanismos auth ni LAN. Token nunca en CLI/logs/metadatos de lock. ExitCode 73 sólo identifica rechazo del hijo esperado; no concede autoridad a un archivo/PID o servidor ajeno. Manejo mínimo del lock no sustituye protección frente a malware del mismo usuario ni parser hardening futuro.

## 8. Archivos que cambiarían tras aprobación

| Ruta prevista | Cambio concreto |
|---|---|
| Unity/Assets/_Project/Scripts/Network/JavaProbeSupervisor.cs | Separar Start/Stop/monitor técnico y wrapper smoke; estados/exit observado/errores, cleanup compartido, límites de intentos. Reutilizar codec/deadlines. |
| Unity/Assets/_Project/Scripts/Network/IpcProbeRunner.cs | Acciones explícitas Retry/Stop, diagnóstico mínimo Editor/Player, cierre/reload sin carreras. |
| Unity/Assets/_Project/Scripts/Network/IpcLifecycle.cs + .meta (nuevo si mejora tests) | Estado/reductor/error/result por intento independiente de UI; no framework. |
| Unity/Assets/_Project/Tests/EditMode/IpcLifecycleTests.cs + .meta | Tabla de transiciones, timers/cancel/error precedence y ownership con fakes limitados. |
| Unity/Assets/_Project/Tests/PlayMode/JavaProbeTests.cs | Mantener regresión 2A y añadir casos reales death/cancel/retry/segundoJava. Harness de señales/estado. |
| Server/src/main/java/com/gorillaescape/server/GorillaEscapeApplication.java | Bootstrap administrado opt-in, guard/EOF temprano, preservar arranque manual HTTP. |
| Server/src/main/java/com/gorillaescape/server/ipc/ManagedProbe.java | Reutilizar listener y codec; conectar lifetime temprano, errores/cleanup/workers observados. |
| Server/src/main/java/com/gorillaescape/server/ipc/ManagedProcessGuard.java (nuevo) | FileLock/exitCode/lifetime del canal; Java 21 estándar. |
| Server/src/main/java/com/gorillaescape/server/ipc/ParentLifetime.java (nuevo sólo si separa responsabilidad) | EOF/stopRequested/contexto seguro; no watchdog. |
| Server/src/test/java/com/gorillaescape/server/ipc/ManagedProbeProcessTest.java y pruebas guard | Competencia dos JVM, EOF/boot/cancel/release y exit real. |
| Shared/Protocol/ipc/README.md | Documentar lifecycle y error de bootstrap/lock. PING/PONG/fixtures/DTO/framing sin cambios. |
| docs/TEST_REPORT.md, DEVELOPMENT_PROGRESS.md y evidencia de 2B | SHA/manifest de resultados reales nuevos, sin sobrescribir 2A. |

No cambiar pom/paquetes/versiones, escenas/settings, códigos de gameplay, protección GitHub o PWA. Fixtures/harness de **test** acotados para READY ausente/endpoint roto pueden ser nuevos bajo carpetas de tests; nunca flags de fallo escondidos en producto. Nombres finales revisables antes de implementación, no creados aquí.

## 9. Pruebas reproducibles propuestas

Matriz prevista durante el diseño; entonces NOT RUN. Los resultados de implementación están en [TEST_REPORT](TEST_REPORT.md), IPC-002B. Esperar señales STARTING/RUNNING/READY/exit, no dormir una cantidad fija para inyectar fallos.

| Escenario y procedimiento | Unit | Java real | Unity PlayMode | Player real | Aceptación |
|---|---|---|---|---|---|
| Muerte Java después de RUNNING | Exit vs EOF/cancel concurrentes | Lanzar JVM real, terminar sólo handle propio | Esperar RUNNING, terminar child de ese intento mediante harness | Mismo recorrido con Player/JAR reales | JAVA_EXITED, STOPPING→FAILED, no residual ni restart; Retry crea identidad nueva. |
| READY nunca llega | Fake stdout abierto/clock deadline | Proceso JVM de test que mantiene pipes sin emitir READY; no cuenta como integración del servidor | Lanzar helper JVM acotado, conservar deadline de 15 s real | Un caso con helper y diagnóstico identificable | READY_TIMEOUT; cleanup observado; sin RUNNING. Se informa el helper, no se afirma PONG real aquí. |
| Connect falla / puerto ocupado | Connect refused/timeout determinista | Socket real cerrado o ocupante que no contesta handshake | Harness intercepta endpoint después de READY sin cambiar el código productivo o usa helper READY/puerto cerrado | Caso endpoint inválido/controlado | CONNECT_FAILED/TIMEOUT o HANDSHAKE_FAILED; nunca adoptar ocupante. Port0 productivo no tiene «puerto fijo ocupado»; no introducirlo por el test. |
| TCP cae con Java vivo | Read EOF/error sin exit | Cerrar socket real / cliente simulado | Tras RUNNING cerrar canal del intento por seam de test | Igual; verificar JVM seguía viva al corte | CONNECTION_LOST, detener hijo por EOF; no reconexión automática; manual nuevo intento. |
| stdin EOF después de READY | Stop repetido | JAR real, cerrar pipe padre, esperar exit 0 y listener cerrado | Stop/Quit con child activo | Cierre normal ventana durante RUNNING | STDIN_EOF, exit 0, lock liberado, residual 0. |
| stdin EOF antes de READY | EOF temprano/cancel carrera | JAR real con EOF inmediato durante bootstrap; repetir en ventanas señalizadas disponibles | Cancel al observar STARTING/processStarted | Un caso startup cancel | Nunca RUNNING; sin READY después de EOF observado; normal cleanup o fallback explicitado. |
| Cancel durante RUNNING | Pending I/O/tarea cancelled | Padre cierra stdin mientras hay tráfico real | Cancel tras RUNNING y validar estado/recursos | Cancel/Stop del control desarrollo | STOPPED si cierre limpio; cancel no es fallo; residual 0. |
| Segundo Java | Start doble/BUSY | Dos JAR/JVM mismo directorio lock: A READY + PONG; B exit 73 sin READY; A responde después | Dos solicitudes / dos owners usando misma ruta | Dos Players mismo scope de usuario | Sólo A válido; B limpio; UnityB ALREADY_RUNNING sin kill de A. |
| Lock stale / crash/release | File guard dispose idempotente | Archivo preexistente sin lock; A terminado; B puede adquirir sin borrar archivo | Retry después de exit confirmado | Reabrir ejecución tras cerrar A | Sin bloqueo por archivo stale, sin PID-based kill. |
| Cleanup incompleto / timeout cierre | Fake proceso no termina y readers pendientes; Retry inhibido | Helper cooperativo de test retrasa EOF; fallback propio | Verificar quit no dispara varios cleanup | Un caso cierre tardío acotado si hace falta | Outcome forced vs clean distinto; residual 0 tras fallback soportado o CHILD_RESIDUAL sin restart. |
| Recuperación manual | 3 lanzamientos máximo, eventos tardíos ignorados | Nuevo token/id/puerto y PONG real | Acción Retry luego de fallo, sólo tras cleanup | Botón Retry produce JVM nueva | 0 lanzamientos automáticos; nuevas identidades; límite 3; no dos hijos simultáneos. |

Mantener suite y smoke de 2A (N=100 sin ampliar benchmark). Reducir fixtures reales a escenarios explícitos, no construir plataforma de chaos/fault injection. Fakes apoyan estados/deadlines; no sustituyen JVM/JAR ni Unity. Los helpers sólo prueban mecanismos de timeout/cleanup; el camino exitoso y casos death/EOF/lock/Retry usan **servidor real**. Las señales de harness no se exponen como comandos IPC de producto.

Primer target Linux de desarrollo con Unity 6000.3.23f1/Mono y Java 21 existente; repetir aceptación en Windows/PC oficial cuando se elijan, sin declarar equivalencia por documentación. Incluir ruta lock compartida Editor Flatpak/Player host y verificar efectivamente la misma exclusión. En proceso residual usar handle/estado de hijo propio, no sólo un PID reutilizable o lectura casual de lista global.

## 10. Criterios de aceptación y riesgos

**Gate previo (cumplido):** PO aprobó este alcance y las precisiones al final del documento. Base integrada verificada; rama de implementación `codex/phase0-ipc-2b-lifecycle` creada desde el SHA aprobado. No autoriza 2C ni Fase 1.

**Gate futuro de 2B:** todos los casos básicos anteriores cubiertos al nivel indicado; transición/causa observables y sólo válidas; ninguna operación de I/O o wait bloquea frames; ningún hijo propio residual en cierre normal/cancel/fallos soportados; no kill ajeno ni segundo backend válido del mismo scope; lock libera en exit real; EOF temprano/tardío tratado; error principal y cleanup separados; reintento manual real sólo tras cleanup, sin loop/restart automático; regresión 2A PASS; builds/tests y evidencia con SHA exactos. Si cleanup no termina, FAILED+Retry bloqueado y evidencia; no PASS para ese caso ni promesa universal ante JVM congelada/padre muerto. Mantener resultado 2A intacto y no extrapolar sus métricas a gameplay.

**Riesgos:** FileLock cooperativo/FS local y vista sandbox; cierre Spring durante bootstrap/EOF sin carrera; PID/handle y eventos tardíos; timers/I/O de Mono diferentes de .NET moderno; sesión sostenida requiere PING cada 1 s frente a readTimeout de 2 s (debug suspendido puede fallar, documentarlo); cleanup forcible no garantiza JVM congelada sin padre; pruebas de helper confundidas con integración real si no se etiquetan; mínima superficie Retry/Stop requiere funcionar en Player, no sólo ContextMenu Editor. Mitigar con tests por evento/ownership y evidencia real; no añadir nuevas dependencias o contención nativa para resolverlos silenciosamente.

## 11. Diferido a 2C o posterior

Reconnect a JVM conservada, retry automático/backoff/restart ilimitado, watchdog/lease sofisticados, Job Objects/contención SO, parser strict exhaustivo/fuzzing/flood, stress, benchmark de 1000 mensajes / 50 Hz / 10 min/optimización, empaquetado JRE/plataforma final y validación física cuando corresponda. Se conserva DEC-005 y sus presupuestos completos; 2B sólo desarrolla un subconjunto autorizado tras aprobación.

WebSocket móvil/PWA/QR/sensores/cámara/gameplay/Input Fusion/Gorilla Smash/Fase 1 continúan fuera; no se asignan automáticamente a 2C por nombrarlos aquí. Ningún siguiente incremento comienza automáticamente.

**Histórico del cierre de la preparación, anterior a la aprobación:** documento completo Proposed; cambios únicamente documentales, validación de enlaces/diff aplicable. No nuevo build/tests de aplicación: ninguna fuente ejecutable cambió. Implementación 2B NOT STARTED, pruebas 2B NOT RUN, 2A PASS/MERGED, Incremento 2/Fase 0 IN PROGRESS, Fase 1 NOT STARTED.

## Precisiones de implementación aceptadas

PING/PONG a 1 Hz sólo en RUNNING, máximo un pendiente, sin acumulación ni lectores concurrentes; PONG dentro de 2 s o pérdida de conexión. Cero restart/reconnect automáticos. Máximo tres lanzamientos por ejecución Unity. Retry sólo tras socket/streams cerrados, readers finalizados, exit confirmado y cero hijo propio residual; el lock se libera al terminar Java. Cualquier fallo activo pasa por STOPPING antes de FAILED. Eventos se aíslan por generación. FileChannel.tryLock antes de Spring y READY, liberado en finally, sin borrar archivo; ocupado falla rápido sin tocar al propietario. Verificar ubicación compartida en Flatpak. Shutdown: EOF→exit 10 s; kill únicamente child propio si vence y observación 2 s; readers hasta 2 s adicionales. Startup completo 15 s, connect/read/write 2 s dentro del presupuesto que aplique. Separar pruebas simuladas y reales, preservar regresión 2A. Abrir PR hacia develop con evidencia sanitizada; no merge automático ni 2C/Fase 1.
