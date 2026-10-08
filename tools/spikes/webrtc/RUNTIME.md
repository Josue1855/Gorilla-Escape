# PhoneInput runtime gate — Linux Software/Lab

El comparador `run.mjs` está cerrado. **No se ejecuta para este gate.** `runtime.mjs` usa adaptadores nuevos de runtime Java/PWA y el Player real que lanza Java; no pipes como señalización productiva.

Requisitos existentes: Java21, Node24, Chrome instalado, playwright-core del runtime local, Unity6000.3.23f1 Linux development Player, X11/Xwayland/libXtst para cierre normal de su ventana. ZXing3.5.3 y webrtc-java0.19.0 en Maven; versiones/licencias del JNI ya auditadas por el comparador. Sin instalar componentes al ejecutar el harness.

1. `npm --prefix PWA run build`
2. `JAVA_HOME=<jdk21> ./Server/mvnw -f Server/pom.xml test package` (LAN tests requieren propiedades de interfaz/IP actuales).
3. Generar el Player Linux con FoundationSetup.BuildDevelopmentLinux en Unity autorizado.
4. `node tools/spikes/webrtc/runtime.mjs docs/evidence/mobile-input-runtime-2026-10-07/<nombre-nuevo>.json --unity`

El comando rechaza sobrescribir evidencia. Usa cuatro contextos Chrome aislados, QR PNG decodificado mediante herramienta Java/ZXing, capturador SensorCapture existente con eventos DOM sintéticos, clientes con identidad otorgada por Java, cargas 30/50/60Hz sintéticas, burst500, suspend/resume, casos negativos y reconnect manual. Sin falsa atribución física/emulator. No TLS flags/bypass; localhost es contexto seguro de laboratorio.

**Ejecutar suites que lanzan Java administrado una por una.** Comparten el FileLock existente de producto: el segundo servidor válido es correctamente rechazado. No cambiar/eludir el lock para paralelizar pruebas. El protocolo y el hosting viejo funcionan con RTC desactivado; sólo este gate establece GORILLA_PHONE_INPUT=1 y secreto de operador efímero privado.

Sólo se cierra el Player iniciado por este harness. Unity controla el child/EOF. Error/deadline deja gate FAIL; no se ocultan resultados previos. Evidencia sin URL QR/admission/resume/token operador/IPC/SDP; logs crudos quedan fuera del repo. Backend diagnostics son operadores loopback autenticados y jamás gameplay/scoring.

Para PlayMode desde Flatpak Unity Hub, el JDK del host usa symlinks de `/etc` que el sandbox no expone. La copia temporal, con symlinks resueltos, del **mismo JDK ya instalado** en `~/.cache/gorilla-phone-jdk21` permite usar `GORILLA_TEST_JAVA=<cache>/bin/java`, `GORILLA_TEST_JAR=<jar>` y `GORILLA_TEST_FIXTURES=<fixtures2C>`. No se descarga JDK, modifica trust ni cambia configuración persistente de Flatpak. PlayMode y Player deben ejecutarse de forma serial.

Android Emulator para este runtime: variante android_runtime.py ejecutada, BLOCKED al abrir canales RTC; histórico16/16 conserva alcance previo. Browser real sintético es la variante alternativa permitida por el PO. Java/Emulator/ADB/Player propios cerraron con exit0 en esa variante; no usarlo como PASS Android. Background/foreground móvil NOT RUN. iPhone/Android físicos/Windows DEFERRED. Onboarding HTTPS de teléfono nuevo permanece riesgo independiente.
