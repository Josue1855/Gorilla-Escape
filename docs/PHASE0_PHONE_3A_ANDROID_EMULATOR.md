# 3A — Automatización Android local, sin sustituir hardware físico

**Viable en esta PC, sin instalaciones nuevas.** Sólo QA de componentes/navegador; 3A sigue IN PROGRESS. No router/DNS/firewall, confianza del sistema, certificado público, runtime Unity/Java/React ni siguientes bloques modificados.

## Entorno comprobado

- Pop!_OS24.04 LTS / COSMIC Wayland, Intel i7-1355U x86_64 y VT-x.
- `/dev/kvm` accesible al usuario ya miembro de kvm; `emulator -accel-check`: KVM installed and usable.
- RAM31GiB total, aproximadamente21GiB disponibles al inspeccionar; disco233GiB libres.
- SDK existente en `~/Android/Sdk`, total11GiB incluyendo componentes que este ensayo no necesita.
- Emulator37.1.11 (821MiB instalado), platform-tools/ADB37.0.1 (22MiB), command-line tools (174MiB) e imagen API36 google_apis x86_64 revisión7 (4.3GiB) ya disponibles. Chrome de esa imagen:133.0.6943.137; registrar versión, no asumir equivalencia con navegador de teléfonos actuales.
- JDK21, OpenSSL, Python3.12 y módulo websockets10.4 existentes. No Gradle, Appium, Flutter, APK propio ni Android Studio completo necesarios para esta prueba.
- Existe AVD SportsParty_API_36; **no se utiliza ni borra**. Se crea otro AVD temporal con imagen instalada, sin snapshots ni cuentas personales; se elimina al finalizar. No se descargan ni instalan paquetes SDK.

Headless `-no-window` y GPU software funcionaron: evita depender de una ventana Qt/Wayland/Xwayland en COSMIC. No se afirma validación de GUI con GPU host. VM usa2GiB guest y2vCPU, con memoria/CPU adicional del renderer; se observó directorio temporal alrededor de1.1GiB durante arranque, no medición de pico. Reservar varios GiB temporales; la PC dispone de margen suficiente. Arranque frío consume CPU y puede tardar decenas de segundos; no representa rendimiento del juego.

El SDK permite invocar el emulador por línea de comandos y comprobar aceleración KVM; se reutiliza el ejecutable instalado, sin migraciones de herramientas. [Android Emulator CLI](https://developer.android.com/studio/run/emulator-commandline), [aceleración oficial](https://developer.android.com/studio/run/emulator-acceleration).

## Un comando reproducible

Desde la raíz del repositorio:

```bash
python3 tools/validate_android_3a.py
```

Usa JAR existente `Server/target/local-server-0.1.0-SNAPSHOT.jar` y Java21 `/usr/lib/jvm/java-21-openjdk-amd64/bin/java`. Si falta algún componente, genera BLOCKED; **no instala**. Para probar otra build/rutas:

```bash
python3 tools/validate_android_3a.py --sdk "$HOME/Android/Sdk" --java /ruta/java21/bin/java --jar /ruta/servidor.jar --output Unity/Logs/Android3AValidation
```

Cada ejecución crea `run-<identificador>` distinto, sin sobrescribir evidencia. Si cambia Java/React por desarrollo autorizado posterior, construir PWA y JAR con los comandos ya establecidos antes de invocar; esta herramienta no cambia ni recompila el producto por su cuenta.

## Flujo y aislamiento

1. Comprueba SDK/imagen/JAR/KVM y crea AVD/perfiles temporales fuera del repo con imagen local. No toca el AVD personal ni shell PATH permanente.
2. Inicia su propio servidor ADB en puerto loopback efímero y emulador con par de puertos disponible. Todos los comandos de dispositivo especifican serial propio; no utiliza ni mata servidorADB compartido5037.
3. Readiness: proceso vivo, ADB disponible y `sys.boot_completed=1`, deadline180s. Chrome disponible y UI inicial resuelta con deadline90s. Tap/swipe se calcula desde bounds de UI tree, no desde imágenes; SDK/UI inesperados producen BLOCKED/FAIL, no PASS por sleep.
4. Inicia Java existente como child del **harness QA**, con managed mode, lock temporal separado, token efímero y stdin abierto; espera READY≤20s. Esto prueba componentes, **no inicia Unity ni sustituye la evidencia previa Unity→Java**. Java escucha loopback; no altera configuración móvil externa de producto.
5. `adb reverse` conecta localhost del dispositivo temporal al localhost Java de host. `adb forward` conecta DevTools de Chrome al controlador QA loopback. Son túneles locales del ensayo, no cambios DNS/router/firewall ni nuevos transportes del producto. El WS DevTools es depuración QA, no implementación4A/WSS móvil.
6. Automatiza Chrome, navegación y assertions DOM, capturas y health. Reinicia sólo Chrome de su AVD. Java se detiene mediante EOF real para comprobar error visible, sin mock ni respuesta de health cacheada.
7. Inicia Java TLS de test en loopback con certificado efímero externo privado. Chrome **debe rechazarlo** con `ERR_CERT_AUTHORITY_INVALID`; no pulsa Proceed, instala CA, modifica almacenes de confianza ni desactiva validación TLS.
8. Captura logcat acotado al PID de Chrome y logs propios. Cierra Java por EOF, Emulator por su serial y ADB por su puerto aislado. Verifica handles y helpers propios identificados por descendencia/starttime; nunca kill global por nombre ni procesos de terceros. Elimina el AVD/PKI temporales.

Timeouts de ADB/CDP/acciones están acotados; las esperas consultan estados reales. Cancelación del operador ejecuta cleanup y deja informe. Logs de stdout/stderr Java drenados; secretos de test sólo en archivos privados/entorno temporal, no argumentos ni resultados publicados. No se prometen límites de disco globales para todo el SDK; revisar espacio entre corridas.

## Casos y límites

| Nivel | Casos automatizados | Alcance real |
|---|---|---|
| Entorno | Boot Android/KVM, Chrome disponible, AVD/ADB separados | Host local, no Android físico ni router |
| Componentes | React empaquetado, health real/no-store, Java managed/READY/EOF/exit0 | Perfil HTTP loopback existente, **no fallback móvil de producto** |
| Browser Android | Navegación, botón, DOM, recarga, restart Chrome, error al cerrar Java, screenshot | Chrome real dentro Android emulado; primera corrida sobre AVD temporal |
| TLS negativo | Stock Chrome rechaza certificado efímero no confiable | Seguridad de rechazo, **no confianza pública positiva** |
| Diagnóstico | Pantallas web/nativa, UI trees, logcat Chrome acotado y logs de procesos propios | Evidencia de laboratorio; no publicarla automáticamente sin revisión |
| Infraestructura | FQDN controlado, SAN/cadena pública, DNS router offline,443 y LAN remota | BLOCKED por requisitos existentes; emulador/túneles no sustituyen red física |
| Compatibilidad | Android físico, iPhone/Safari, datos móviles/DNS privado/VPN/Wi-Fi real | BLOCKED / NOT RUN; matriz física original conserva gate |
| Futuro | QR/sesión, sensores, WSS, Flutter | SKIP, fuera3A; no solicitar permisos sensores/cámara |

El HTTP de componentes usa localhost mediante ADB, que Chrome puede tratar como contexto seguro; esto **no es TLS**, `/api/health` registra `secure=false` y la UI muestra HTTPS:no. Ninguna captura o secure-context local demuestra HTTPS público del producto. La topología virtual Android dispone de rutas distintas de una LAN real. [Red del Emulator](https://developer.android.com/studio/run/emulator-networking).

## Resultados e informe

`results.json` contiene scope, SHA256 del JAR, Android/Chrome, cada caso PASS/FAIL/SKIP/BLOCKED y cleanup (exitCode, residuales propios/helpers). Evidencias: `react-health.png`, `native-react-health.png`, `connection-error.png`, `tls-rejected.png`, árboles de UI inicial y logs locales. Logcat ausente se marca SKIP, no se inventa diagnóstico.

Código de salida0 significa **casos de componentes PASS**, aunque los gates públicos/físicos permanezcan BLOCKED;1 indica fallo y2 entorno bloqueado. Cancelación130. Siempre consultar los estados individuales y scope; nunca convertir salida0 en3A PASS.

Se conserva historia de intentos: el primer harness falló por no tolerar ADB aún no disponible; se corrigió polling. El segundo arrancó Android/Java pero agotó la disponibilidad DevTools antes de completar UI inicial; se añadió espera por UI tree. Otro intento agotó la espera después de reiniciar Chrome; se corrigió la espera de navegación/recarga mediante cambio de loader y documento listo. Corridas posteriores demostraron componentes reales y rechazo TLS. Estos fallos no eran PASS ni se ocultan. No son cambios al producto.

La versión final obtuvo **10 PASS, 2 BLOCKED y 1 SKIP**; procesos Java/Emulator/ADB exit0, residuales propios/helpers0. La prueba de SDK ausente generó BLOCKED/exit2 sin iniciar procesos. [Resultados](evidence/android-emulator-3a-2026-10-06/results.json), [historia y hash del harness](evidence/android-emulator-3a-2026-10-06/manifest.json), [captura Android revisada](evidence/android-emulator-3a-2026-10-06/native-react-health.png).

Evidencia revisada específica se guarda separada en `docs/evidence/android-emulator-3a-2026-10-06/`; logs completos, imágenes de disco, perfiles y PKI permanecen fueraGit/ignorados y no se publican. Datos de infraestructura siguen pendientes y no bloquean estos casos locales.

## Instalación y estado

**No se necesita instalar nada en esta PC para la corrida limitada.** Coste incremental de descarga:0; impacto: procesos temporales de usuario, AVD/archivos de QA y carga CPU/RAM durante ejecución. No cambia grupos/permisos KVM, servicios del sistema, certificados, firewall, DNS ni dependencias del producto. Si otra PC carece de componente o la imagen no contiene Chrome, primero inventariar tamaño, espacio e impacto y pedir autorización; no auto-instalar ni recomendar Android Studio completo como requisito.

3A IN PROGRESS, #94 DRAFT, siete datos operativos pendientes;3B/4A/4B/4C/Fase1 NOT STARTED. No merge ni siguiente bloque. El siguiente trabajo físico sigue regido por [validación3A](PHASE0_PHONE_3A_VALIDATION.md) y [preflight](PHASE0_PHONE_3A_PREFLIGHT.md).
