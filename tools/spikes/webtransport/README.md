# Spike aislado 3A-T — WebTransport

Experimental, sin integración en producto ni elección definitiva de transporte. Java 21 + Chrome desktop real, HTTP/3/QUIC/TLS 1.3, streams bidireccionales y datagramas. No modifica Unity, Server, PWA productiva, IPC ni sensores. Resultados y límites: [informe](../../../docs/PHASE0_SECURE_MOTION_TRANSPORT_DECISION.md#15-spike-ejecutable-3a-t--2026-10-07).

## Repetir en Linux

Requiere Java 21, Python 3.12+ con `cryptography`, Node, Chrome y `playwright-core` ya disponibles. No instala globalmente estas herramientas. Si falta alguna, detenerse y solicitar autorización para instalación. Preparación descarga fuente fijada, Maven y dependencias públicas exclusivamente en `~/.cache/gorilla-wt-spike`; no utiliza `~/.m2`. Red necesaria para preparación, no para ejecutar el intercambio local una vez compilado.

Desde raíz del repositorio, configurar rutas existentes cuando los valores por defecto no correspondan:

```bash
export WT_JAVA_HOME=/ruta/al/jdk21
export WT_PLAYWRIGHT_MODULE=/ruta/a/playwright-core/index.mjs
export WT_CHROME=/ruta/a/google-chrome
python3 tools/spikes/webtransport/prepare.py
python3 tools/spikes/webtransport/run.py --output /tmp/wt-results.json
```

Preparación es separada; el último comando repite los seis escenarios, genera informe JSON sanitizado y termina con código 0 sólo si pasan las aserciones y cleanup. El runner soporta únicamente Linux/POSIX; biblioteca Java en Windows pendiente. Para apuntar a una IP privada propia añadir `--bind <IP-de-esta-PC>`; sólo en red de pruebas autorizada, no `0.0.0.0`. Es una prueba del endpoint en dirección LAN desde la misma PC, **no prueba física entre dispositivos**. No altera firewall/router/DNS.

## Escenarios y límites

1. Pin SHA-256 correcto: conexión, AUTH, 20 ecos fiables y 20 datagramas; INVALID fiable devuelve error, INVALID y datagrama de 257 bytes no se devuelven; cierre.
2. Pin incorrecto: rechazo en CONNECTING, no éxito por timeout.
3. Credencial inválida: rechazo en AUTHENTICATING.
4. Frame de 257 bytes: rechazo en OVERSIZE.
5. Recuperación manual: sesión nueva después de cierres anteriores, nuevos 20+20 ecos; sin retry automático.
6. Certificado vencido con su pin correcto: rechazo en CONNECTING.

Mensajes de laboratorio ASCII, prefijo uint16 big-endian para stream, máximo 256 bytes. No es el contrato IPC ni Gorilla Protocol. Credencial efímera de 256 bits, inyectada por harness privado y transmitida únicamente dentro del canal cifrado; no prueba UX de admisión del jugador. Límite 4 sesiones WT, 2 streams bidi por sesión, sin streams uni de aplicación; QUIC flow-control 64 KiB, colas de datagramas 256 entradas; no demuestra resistencia a flood ni límites globales de todas las conexiones pre-auth.

Startup 15 s, autenticación 2 s, stream idle 3 s, QUIC idle 5 s, operación browser 4 s, cleanup browser 2 s, harness 100 s, EOF/exit Java 5 s. Son presupuestos del laboratorio, no cambios a DEC-005. Cierre EOF, código exit, ausencia de child Java, grupo de procesos propio del navegador y rebinding UDP verificados. Fallback mata sólo children/grupo creado por este harness y produce FAIL. No se afirma ausencia de fugas de memoria.

Certificados temporales ECDSA P-256/SHA-256 con SAN y serverAuth, vigencia 2 días; caso vencido separado. Claves y credenciales en directorios temporales 0700/archivos 0600, eliminados al salir. No instala certificados, no cambia confianza del navegador, no usa `ignoreHTTPSErrors` ni flags de TLS. La página fixture usa HTTP loopback, contexto seguro reconocido por Chrome; no demuestra HTTPS público ni que HTTP por IP LAN habilite sensores en teléfonos. El pin no es secreto; su distribución autenticada real por QR sigue pendiente.

**Riesgo explícito:** `InsecureQuicTokenHandler` de Netty sólo representa validación de dirección/Retry QUIC, no bypass de certificado TLS. Es configuración de laboratorio, inadecuada como defensa de exposición productiva. Admisión, replay, abuso, varios jugadores y seguridad operativa requieren diseño/pruebas posteriores; no reutilizar este servidor tal cual en producto.

Métricas separadas por escenario/transporte, nearest-rank P50/P95, N=20 descriptivo. `sent/received/loss` son por cada modalidad del escenario positivo, no 20 totales entre ambas. No benchmark, no extrapolación a movimiento ni móvil. Android/iPhone, LAN física, hotspot, sin WAN real, persistencia offline PWA y Windows: NOT RUN. Upstream unit tests no ejecutados; integración real propia sí. Build tiene aviso de API deprecated en NioEventLoopGroup; no declarar 0 warnings.

## Dependencias y reversibilidad

Ver [DEPENDENCIES.md](DEPENDENCIES.md). Borrar exclusivamente esta carpeta y cache de este Spike revierte el experimento; no borra historial ni cambia producto. No hacer commit de target, temporales, claves o credenciales. No desplegar el fixture.

## Android Emulator — componentes existentes + Spike

Con SDK/imágenes ya instalados, KVM accesible y JAR de desarrollo existente, repetir desde raíz:

```bash
python3 tools/spikes/webtransport/android.py --output /tmp/wt-android-nueva-corrida
```

La carpeta de salida debe ser nueva; nunca sobrescribe evidencia. Admite `--sdk /ruta/al/SDK`; Java se configura con `WT_JAVA_HOME`. Usa imagen existente `system-images;android-36;google_apis;x86_64`, sin descargar ni instalar paquetes. Crea un AVD desechable headless, perfiles/ADB/logs privados; reutiliza el boot/cleanup del harness Android existente. JAR requerido: `Server/target/local-server-0.1.0-SNAPSHOT.jar`. No lo recompila ni cambia configuración productiva.

Prueba React/Health real, recarga, reinicio Chrome y error tras EOF del Java HTTP; después seis casos WT y recuperación tras recarga. Para WT, origen loopback de Android mediante **adb reverse TCP**; el transporte **QUIC UDP no pasa por adb reverse**, sino por `10.0.2.2`, alias del host en la red virtual del emulador, hacia Java ligado a 127.0.0.1. CDP local es herramienta de QA, no protocolo móvil. Certificados temporales/pin se manejan igual que laboratorio desktop, sin CA ni bypass. No demuestra LAN física ni confianza pública para navegación HTTPS; el escenario React usa HTTP loopback explícito.

Abre también la página de sensores ya existente: espera RUNNING y eventos virtuales, registra diagnóstico y STOPPED. No conecta sus muestras a WT/Java/Unity, no fuerza orientación ni inyecta eventos de sensores con CDP. Muestras proceden del modelo virtual del emulador; orientación estática puede generar pocos eventos. La cadencia observada no es frecuencia/precisión de un teléfono físico. No altera el código de captura ni inventa gameplay.

Resultados JSON, métricas independientes por sesión, capturas de página y hashes de código/JAR. Logcat limitado por PID se captura sólo temporalmente y se elimina; no se publican logs crudos, dumps de UI, claves o credenciales. Cleanup cierra servidores, Java mediante EOF, emulador y ADB propios. Android/iPhone físicos, offline PWA y LAN real siguen NOT RUN; estados históricos no cambian. Fallos ambientales se reportan BLOCKED, aserciones FAIL; no instalar para ocultarlos.
