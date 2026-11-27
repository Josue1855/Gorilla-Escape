# Comparador WebRTC aislado — Fase 0 Software/Lab

Java 21 real + webrtc-java **0.19.0**, JNI **linux-x86_64** + Chromium real automatizado. Sin audio/video tracks, STUN/TURN, backend cloud ni modificación de red/firewall. Host candidates UDP. No es signaling/onboarding productivo: Playwright intercambia SDP por pipes privados del proceso hijo. No se guardan SDP, fingerprints DTLS, credenciales o IPs en evidencia. La página vacía de laboratorio se sirve exclusivamente en loopback; no demuestra origen seguro de una PWA en un teléfono nuevo.

## Repetir

Con Java 21 y Maven ya disponibles, desde la raíz:

```sh
JAVA_HOME=/usr/lib/jvm/java-21-openjdk-amd64 "$HOME/.cache/gorilla-wt-spike/apache-maven-3.9.11/bin/mvn" -B -ntp -Dmaven.repo.local="$HOME/.cache/gorilla-rtc-spike/m2" -f tools/spikes/webrtc/pom.xml package dependency:build-classpath -Dmdep.outputFile=target/classpath.txt
node tools/spikes/webrtc/run.mjs docs/evidence/webrtc-comparator-2026-10-07/new-run.json
```

Usa Playwright existente, Chrome `/usr/bin/google-chrome`; `WT_PLAYWRIGHT_MODULE` permite configurar módulo existente. No instala paquetes del sistema. El comando rechaza sobrescribir resultados anteriores. Descargas Maven sólo durante preparación, nunca necesarias para la corrida una vez resueltas. Ni Java ni navegador corren como root.

## Límites

4 peers, 2 canales por peer, frame 4096 bytes, bufferedAmount máximo observado/admitido 8192 bytes, comando pipe 90000 caracteres, SDP 65536 caracteres, 8 peticiones pendientes de automatización. Sin cola de mensajes ni tareas por echo: consume/copia callback antes de retornar, envía inmediatamente con control de buffer. Credential efímera por peer y por canal; no secretos en argumentos/logs. Gather/SDP 10 s por operación; pipe 35 s; echo 2 s; Java EOF shutdown 10 s antes de kill exclusivamente del propio child. La API nativa controla sus propias colas internas: estos límites no prueban un bound global de memoria nativa ni flood hardening.

Pruebas: reliable ordered, unordered maxRetransmits=0, padding 32/1024 bytes, 100 mensajes por tamaño/canal/peer, reconexión manual con peer y credential nuevos, 4 peers concurrentes, disconnect uno mantiene otro, SDP inválido, credential inválida, payload inválido, oversized. Muestras RTT round-trip browser/Java: no motion-to-photon, sensores físicos ni Unity. Unordered usa requests secuenciales por peer: pérdida cero en esa corrida no prueba resistencia a congestión.

## Dependencias y distribución

Wrapper: [webrtc-java](https://github.com/devopvoid/webrtc-java), Apache-2.0; [guía oficial](https://jrtc.dev/guide/get-started). Native jar incluye `META-INF/licenses/webrtc/LICENSE.md` y `PATENTS`; preservar esos avisos y auditar notices transitivos antes de distribuir producto. No se incluyó paquete media/FFmpeg extra. Jar wrapper SHA256 `90e3ca56d2a11a474e07125fc07622a4f6be8f25f8cddab273d911739093ccdb` (132671 bytes); Linux native SHA256 `0195ae27572722906c8eccb466cd1c02ed22a1209925d69d009beda24b1004f4` (9876852 bytes). Artefactos en cache particular, no versionados. Windows y plataformas físicas DEFERRED.

## Limitaciones pendientes antes de producción

La biblioteca puede abrir sockets en interfaces enumeradas por libwebrtc; observar host candidates no garantiza bind exclusivo a una sola interfaz. Este Spike no abre UFW. Falta política de interfaces/puertos, signaling LAN autenticado desde origen seguro, expiración de peers inactivos, reinicio/aborto abrupto del runner, multicast/mDNS/LNA y redes con aislamiento. Auth temporal de payload no sustituye autenticar el fingerprint de la señalización. Estos puntos impiden llamar al comparador transporte productivo integrado.
