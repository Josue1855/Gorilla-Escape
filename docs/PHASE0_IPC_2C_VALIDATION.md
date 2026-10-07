# Procedimiento de validación IPC 2C

Diseño [Accepted](PHASE0_IPC_2C_DESIGN.md), precisiones del PO incluidas. Base
`7e3aa60aa53c65e7051f32477a77985fbe628907`; rama `codex/phase0-ipc-2c-hardening`.
Resultados finales, hashes y limitaciones se registran en [TEST_REPORT](TEST_REPORT.md).
Sólo Linux de desarrollo: Unity 6000.3.23f1, Player Mono, Java 21. Windows/PC oficial
NOT RUN. No distribución JRE, gameplay, móvil ni componentes de Fase 1.

## Contrato y límites implementados

Corpus común: `Shared/Protocol/ipc/cases/corpus.json`, 71 casos hex de frames completos
válidos/invalidaciones. Java y EditMode leen el mismo archivo; el test de JVM real
usa todos los rechazos con el contexto de handshake indicado y comprueba un PING
válido posterior en la misma JVM. Un frame inválido no provoca exit Java.
Fixtures públicas incluyen token ficticio; nunca se sustituyen por secretos reales.

C# IpcProbeContract sólo interpreta seis campos de envelope, payload vacío/token y
cinco de READY. Sin DOM, arrays ni valores generales. Nombres desescapados antes de
comprobar repetición, enteros int32 léxicos, strings limitados a 43 caracteres ASCII,
documento único. El esquema sólo admite raíz+payload (dos niveles); por tanto no
puede aceptar anidamiento mayor que cuatro. Entradas anidadas se rechazan en el
primer campo/valor no permitido, sin construir ni recorrer árboles arbitrarios.

Java reutiliza Jackson 3.1.5 resuelto, duplicate detection, trailing-token rejection
y StreamReadConstraints depth=4 exclusivo del mapper IPC. Strings sin coerción;
UTF-8 de framing estricto, límite previo 4096 y output buffer máximo 4096.
C# cuenta bytes antes de encoding; Java encode con buffer de capacidad fija y
rechazo de overflow, sin crecer según un prefijo malicioso.

Java atiende un socket/un writer. SO_TIMEOUT usa tiempo restante del deadline de
frame de 2 s completo; no se renueva con cada fragmento. Escritura en el worker,
un executor de deadline de un solo thread, una tarea activa/pendiente como máximo,
remove-on-cancel y finally cancel. Callback captura el socket de esa operación;
no accede al campo mutable client para cerrarlo. RESOURCE_SUMMARY informa pico de
deadlines pendientes, cero al terminar y executor terminado. No pool común ni
thread por PONG. La regresión conserva ownership/EOF y cleanup 10+2+2 s de 2B.

## Preparación reproducible

1. `JAVA_HOME=/usr/lib/jvm/java-21-openjdk-amd64 Server/mvnw -B -f Server/pom.xml verify`.
   Incluye suites 2A/2B, corpus y presión finita contra Spring/JVM real, además de
   sockets reales con send/receive buffers pequeños únicamente en tests para forzar
   un write timeout sin cambiar tuning del producto.
2. Reutilizar JDK21 de desarrollo con symlinks resueltos según
   [procedimiento 2B](PHASE0_IPC_2B_VALIDATION.md). Registrar Java version/hash, sin
   copiar el runtime al repositorio ni convertirlo en distribución autorizada.
3. Compilar `Server/src/test/helpers/LifecycleFixture.java` y
   `ProbeInvalidFixture.java` con javac21 en carpeta ignorada. Empaquetar el primero
   como `no-ready.jar` y `connect-refused.jar`; el segundo como `pong-duplicate.jar`
   y `ready-duplicate.jar`. Los nombres seleccionan fallos sólo dentro del helper
   test, nunca en el backend productivo. No cuentan como interop real del backend.
4. Editor Flatpak: pasar `--env=UNITY_DISABLE_LINUX_TOOLCHAIN_MIGRATOR=1` y
   `--env=UNITY_DISABLE_LINUX_AUTO_TOOLCHAIN_INSTALLATION=1`,
   `--command=<editor6000.3.23f1> com.unity.UnityHub -projectPath <Unity>`.
   EditMode/PlayMode: `-batchmode -nographics -runTests -testPlatform <modo>`
   `-testResults <xml-ignorado> -logFile <raw-ignorado>`.
   Para PlayMode añadir `GORILLA_TEST_JAVA`, `GORILLA_TEST_JAR` y
   `GORILLA_TEST_FIXTURES` mediante `--env` antes del app-id. Sin paths: NOT RUN,
   nunca PASS de integración. No ejecutar PlayMode y Player simultáneamente.
5. Build: `-batchmode -nographics -quit -executeMethod
   GorillaEscape.Editor.FoundationSetup.BuildDevelopmentLinux -logFile <raw>`.
   Conservar la misma build/JAR a lo largo de la evidencia Player, verificar hashes
   después de las pruebas y antes del commit.
6. Ejecutar React tests/build y validadores management/foundation. No cambios de
   PWA; su suite se usa como regresión del monorepo.

## Medición Player real

`python3 tools/validate_ipc_2c_player.py --player <exe> --java <jdk21/bin/java>
--jar <jar-real> --output <raw-ignorado>`.

El harness lanza tres Players independientes en modo explícito `GORILLA_IPC_MODE=benchmark`,
con smoke exit normal, y luego un cuarto en modo lifecycle con medición. Unity lanza
su Java, genera identidades/token y mantiene supervisor/lock de 2B. Verifica PPid
antes de operaciones sobre children, listeners únicamente 127.0.0.1, exit y residuales.
El resultado de cada corrida se conserva individualmente en measurement-results.json.

Warmup: 100 exchanges incluyendo handshake; medición 1000 posteriores. Enviados/
recibidos/válidos son totales incluyendo warmup (1100); N=1000 para percentiles.
DurationSeconds incluye operación posterior al primer PONG (warmup restante+medición),
excluye startup/shutdown; cadencia efectiva usa los 999 intervalos entre inicios de
las 1000 muestras retenidas. Ticks tardíos cuenta inicios >1 ms después del turno
previsto, incluyendo warmup restante. No acumulación/catch-up de mensajes.

Stopwatch worker desde antes de escribir/serializar hasta validación estricta de
PONG; nearest-rank de éxitos, sin omitir picos. Publicar cada startup/min/P50/P95/max,
duración/cadencia/ticks/shutdown/N/sent/received/valid/errors/timeouts, no percentil
sobre 3000 ni P95 significativo de startup con tres lanzamientos.
Referencia: P50≤5 ms, P95≤10 ms, max≤50 ms y nominal errors/timeouts=0. El pacing
50 Hz es exclusivo de este probe finito; smoke 2A mantiene su cadencia histórica.

Estabilidad: observar RUNNING real ≥601 s antes de solicitar cierre de ventana normal
(vía X11, supervisor cierra stdin). El flujo productivo sigue siendo heartbeat 1 Hz
con un request pendiente; conservar contadores, estado final STOPPED, exit0, cleanup
y residuales propios0. La lista RTT tiene capacidad fija 1024; después deja de retener
muestras, no detiene el lifecycle ni crea una cola. Los contadores siguen activos.

RSS externo cada 5 s, inicial tras primer PONG/final antes del cierre/pico observado.
Esta primera muestra no equivale a heap estabilizado tras GC/JIT: se conserva la serie
para interpretar evolución. Managed heap NOT MEASURED si no existe captura portátil
reproducible. Umbral max(64 MiB,25%) sólo investigación, sin afirmar ausencia global
de fugas ni memory safety. Los diagnósticos nativos históricos Unity siguen registrados.

## Presión y regresión

Presión finita: corpus de rechazos; 100 oversized sin cuerpos +100 JSON inválidos;
1000 valid frames con consumidor lento; 100 frames exactamente4096; productor sin
lector hasta1 MiB, detenido3.1s; prefijo/cuerpo parcial con goteo500ms y deadline2s.
Presión mantiene stdin abierto, proceso sano y PING posterior. Kernel buffering puede
absorber1MiB: este ensayo acepta timeout de lectura o backpressure/cierre; el test
separado de write deadline usa sockets reales con buffers pequeños para garantizar
escritura bloqueada y demuestra que un callback antiguo no afecta a nueva conexión.
No crecer indefinidamente hasta forzar un síntoma. Tests observan RSS inicial/final/
pico muestreado por grupo y recursos finales; RSS es observación, límites de producto
son gate directo. No ban/rate-limit ni fuzzing.

`tools/validate_ipc_2b_player.py` ejecuta los 11 grupos existentes con esta build/JAR
(incluido smoke2A N=100). El harness añade argumentos opcionales mode/measure para reutilizar ownership/ventana/cleanup.
El envío de tecla eleva/enfoca la ventana y espera su evento hasta1s por acción explícita;
cancel de startup usa cierre normal de ventana para activar RequestStop antes de READY,
sin depender de una tecla enviada antes del primer frame enfocado. No cambia el producto.
Repetir caída propia, retries manuales/3 launches/cuarto bloqueado, cancel, singleton,
READY timeout/refused helpers, liveness timeout real, fallback propio y parent EOF.
PlayMode conserva tests de eventos tardíos/generación y añade READY/PONG duplicados
mediante helpers JVM identificados. Tests Java/Unity no se omiten por mocks.

## Evidencia y cierre

Sólo JSON/CSV/XML sanitizados, resúmenes de métricas individuales, entorno y hashes.
XML sin system-properties/stdout/stacktraces; logs crudos y builds ignorados. Inventario
SHA256 de fuentes funcionales/test/harness para comparar candidato, sin hash de commit
autorreferencial. Revisar tokens/secretos y git diff --check antes de commit/push/PR.

Gate completo en TEST_REPORT; ningún skip se cuenta como integración PASS. Tras gate,
Estado tras integración autorizada de PR #93: 2A/2B/2C e Incremento 2 PASS / MERGED;
Fase 0 IN PROGRESS y Fase 1 NOT STARTED. Evidencia y SHA de merge en TEST_REPORT.
No merge automático, reconnection/restart automático, watchdog nativo, Job Objects,
process-tree kill, TLS, móvil, QR, sensores, cámara, gameplay ni trabajo posterior.
EOF no certifica una JVM congelada después de morir Unity; contención nativa diferida.
