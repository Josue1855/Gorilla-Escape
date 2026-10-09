# T008 / DEC-016 — CA por instalación, HTTPS LAN y QR

**ACCEPTED FOR IMPLEMENTATION / VALIDATION — PO, 2026-10-08.** Base `7e818d5e729aad09c3b6fe40cc22422268198d2a`; rama `feature/mobile-t008-local-trust`. T008/#19 único Issue operativo, OPEN/Validation. Sin cierre T009/F02/Fase0 ni inicio Fase1. Este documento sustituye para T008 el enfoque principal FQDN/DNS/443 de los documentos históricos, sin borrarlos ni reinterpretar sus resultados.

## Diseño implementado

Unity supervisa el mismo Java21; IPC127.0.0.1 sin cambio. Java carga/genera CA por instalación y leafSANIP, sirve PWA y señalización sobre `https://<LAN-IP>:<port>`. RTCDataChannel/GorillaProtocolv1 no cambian. No router, DNS, WAN forwarding, STUN/TURN público, cloud, TLS bypass o trust-all.

`GORILLA_LOCAL_TRUST_DIR` absoluto fuera de cualquier checkout activa DEC016. Conflicto con GORILLA_MOBILE_CONFIG histórico = error. `GORILLA_LAN_INTERFACE`/`GORILLA_LAN_ADDRESS` seleccionan entre candidatos físicos activos privados IPv4; si hay exactamente uno puede elegirse automáticamente, si hay varios = LAN_SELECTION_REQUIRED. Wi-Fi/Ethernet, no loopback/down/virtual/VPN. No elegir primera interfaz. Detecta cambios DHCP al siguiente inicio; no rebinding/reconnect automático durante una sesión.

`GORILLA_HTTPS_PORT` default8443,1024–65535. No443/privilegios/redirección. Origin exacto `https://<ip>:<port>`; QRHTTPS usa ese origen y `/mobile-lab/index.html` (nombre de recurso retenido, modo de producto distinto). Su admission efímera permanece en fragment,30s,one-use/session-bound/replay rejection; no secreto operador/IPC/privatekey. La página JOIN GAME confirma READY **técnico** tras canalesOPEN/HELLO ACK, sin sensores/permisos/calibración de T009/T010 ni gameplay. Fragment se retira después de join; no se persiste resumeToken en el teléfono.

El operador realiza POSTadmission por HTTPS desde la misma PC, con credencial efímera de lanzamiento en header; se valida dirección origen PC y secreto, nunca se envía ese secreto al teléfono. El helper del gate genera la credencial en memoria/env de lanzamiento, no en argumentos/logs/QR. LABloopback se conserva exclusivamente con `GORILLA_MOBILE_LAB=1`, nunca fallback por defecto. Los harnesses históricos lo declaran explícitamente.

## CA y leaf

`LocalTrust` usa keytool del JDK21 ya existente (sin librería/dependencia Maven nueva). El JRE final no se presume que incluya keytool: distribución/autosignado sinJDK/Windows pertenecen a gates posteriores, no se declaran resueltos. RootRSA3072/SHA256,CA/pathlen0/keyCertSign+cRLSign,5años. LeafRSA2048/SHA256,serverAuth,digitalSignature/keyEncipherment,7días,SANIPtipo7. Cadena root+leaf en PKCS12. Reutilización sólo si CAválida y leafcoincide IP/firma/EKU/vigencia≥1día; cambioIP o próximo vencimiento renueva **leaf**, mismaCA. CA vencida/incompleta/corrupta = fallo, no renovación silenciosa.

Directorio0700, archivos0600; PKCS12 cifrado y password aleatoria256bits. Root privada nunca exportada. `root.cer` es DERX.509 **público**, no keystore. Password/PKCS12 fuera de Git; guardia de directorio/repositorio y symlinks de archivos, FileLock por instalación, temporales privados y movimientos atómicos. En FS sin POSIX se falla con TRUST_PERMISSIONS_UNSUPPORTED: Windows ACL aún NOT RUN, no promesa de soporte inseguro.

Cada keytool tiene deadline10s; stdout/stderr descartados, password sólo opción `-storepass:file`, no valor en argumentos. Cancelación/EOF del padre interrumpe generación pendiente y termina únicamente su proceso keytool; EOF del runtime conserva cleanupJava existente. Un fallo parcial no se arregla inventando otra CA. Diagnóstico muestra sólo código/origen/fingerprint público. Preparación inicial puede afectar startup15s de Unity: medir gate real, no aumentar deadline silenciosamente.

**Regeneración explícita:** detener Gorilla Escape, preservar copia privada si el operador la necesita, retirar la CA anterior de teléfonos y retirar conscientemente el directorio completo de esta instalación. El siguiente inicio crea una CA nueva; todos los teléfonos deben volver a PREPARE DEVICE. No hay bandera automática de rotación, borrar archivos aislados causa TRUST_STATE_INCOMPLETE. Registrar fingerprint anterior/nuevo y motivo, sin claves. Compromiso de la PC/clave implica retirar esa confianza: no usar una root global distribuida.

## PREPARE DEVICE versus JOIN GAME

Con teléfono todavía no preparado, la URLHTTPS da errorTLS: **no ignorarlo**. Opciones de transferencia: archivo CA público por canal del operador, o bootstrapHTTP opt-in en el **mismo proceso Java**, `GORILLA_TRUST_BOOTSTRAP_PORT=18000`. Default0 deshabilitado. Este listener sirve únicamente `/prepare` y `/gorilla-root.cer`; no PWA, signaling, gameplay, credenciales ni archivos arbitrarios. Tiene workers/cola acotados y se detiene con Java. No segundo backend de sesiones.

La descargaHTTP de CA **no es autenticada**. Comparar SHA256 con el mostrado en la PC por un canal confiable antes de instalar; no confiar automáticamente en la propia páginaHTTP como prueba. Un atacante LAN podría sustituir CA+fingerprintHTTP. Key/root privada jamás transmitida. Alternativa de transferencia manual pública no cambia gameplayLAN.

1. Preparación única, cronometrada: conectarse LAN, obtenerCApública/verificarfingerprint conPC, instalar/confiar, abrir HTTPS sinwarning. Mantener duración íntegra, pasos exactos, OS/browser/modelo QA.
2. iPhone: instalar perfil descargado en Ajustes→General→VPN y gestión de dispositivos; después General→Información→Ajustes de confianza de certificados→activar confianza SSL/TLS de esaCA. Instalar perfil no garantiza confianza. [Apple](https://support.apple.com/en-us/102390).
3. Android: ruta de instalarCA depende OS/fabricante; probar confianza real Chrome físico, sin asumir paridad ni flags. Android físico NOT RUN mientras no exista dispositivo confirmado.
4. Join preparado: QR visible/usuario toma teléfono → escanear → abrir HTTPSLAN → JOIN GAME → canalesOPEN + HELLOACK → READY técnico. Target<60s. No usar tiempos automatizados de emulador para cumplir gatehumano.
5. Retirar CA cuando ya no se confíe en estaPC; perfiles instalados los retira el propietario. No restaurar como requisito certificado público/DNS ni ocultar la concesión aprobada.

## Procedimiento Linux del gate

Build PWA y JAR actuales; Player histórico con C# sin cambios. Configurar ejecutableJava21 y una carpeta privada **fuera del repo**. Helper reproducible:

```bash
python3 tools/t008_local_gate.py \
  --java /ruta/al/jdk21/bin/java \
  --trust-dir /ruta/privada/fuera-del-repo/trust \
  --output /ruta/nueva/evidencia-privada \
  --interface interfaz-confirmada --address IPv4-LAN-confirmada \
  --https-port 8443 --bootstrap-port 18000
```

No modifica firewall/DNS/router/OStrust. Inicia Player→Java, valida HTTPS real con CA añadida a contextoTLSvalidante (hostname verification intacta). Muestra origin/fingerprint; esperar teléfono preparado. Comando `admission` genera QR transitorio30s en output para mostrar enPC. No guardarURL/credencial/SDP en evidencia. `quit` cierra Player normalmente; elimina QR y publica summarysanitizado de cleanup. Si red bloquea entrada, detener y pedir autorización concreta antes de cambiar firewall. No ejecutar adbforwarding/loopback para acreditar iPhone físico.

Inicio/fin de cronómetros: preparación empieza al iniciar transferencia/verificaciónCA y termina al abrir HTTPSconfiable; join empieza QRvisible/usuario toma teléfono y termina READYtécnico. Registrar reloj observado/forma de medición, sin precisión inventada. Test WAN completo sigueT011, no cambiarWAN aquí.

## Validación (se completa con el candidato final)

Unit: CA únicas,reuse,leafDHCP,SANIP,wrongCA/wrongIP/expired,permisos,estadoincompleto,selecciónambigua y origins; TechnicalSession/QR conserva one-use/expiry/sessionbinding/replay.

IntegraciónJava real: TLS validante aIPLAN, rootconfiada/untrusted/wrongIP, rootpúblicaexacta,404privados,admissionHTTPSorigenexacto,operador sincredential403,bootstrapHTTPsóloCA/prepare,EOFexit0/puertoliberado. RunCI sin únicaLAN física marca ese gateSKIP explícito, noPASSfísico.

Frontend: npmtest/build y harnessRTC de regresión conLABexplícito; Shared Mobile/IPC sin cambios. Unity fuentes/config/binarios retenidos porhash, no afirmar suites nuevas ni que JAR anterior sigue idéntico: JAR nuevo requiere integraciónrealJava→IPC→Player.

Primera prueba de integración falló porque el **test** usó el mapper móvil acotado512chars para leer la imagenPNGbase64 del endpointoperator. Se corrigió el lector de respuesta del test, no el parserMobile ni su límite. Fallo/log privado conservado. La repetición de ese gateJava pasó. No nuevas afirmaciones físicas/latencia/cadencia sensores.

**AC1 PARTIAL; AC2 PARTIAL; AC3 PARTIAL (duracioneshumanasNOTRUN); AC4 PASS** mientras faltan gates físicos y PRintegración. #19 no se cierra por CI/lab ni porque DEC016Accepted. T009/#20 permaneceblocked, F02OPEN, Fase0OPEN/Fase1NOTSTARTED.

## Incidencias de ejecución conservadas

El primer harness RTC con Player se lanzó concurrentemente con el gate físico activo y terminó FAIL/deadline, sin nuevas observaciones Unity ni procesos propios residuales. La causa exacta no está acreditada por ese resultado; no afirmar que sea una regresión de producto ni un rechazo singleton demostrado. Se conserva y se repite en serie tras cerrar el gate físico. Ningún resultado anterior se cambia a PASS.

## Gate físico actual — 2026-10-08

El iPhone 15 no pudo cargar `/prepare` antes de descargar o instalar la CA. Cinco eventos **UFW BLOCK** del kernel coinciden con la IP del teléfono y los puertos del gate. Esto acredita filtrado de estos intentos; todavía no acredita fallo TLS, del certificado, de Safari o de RTC. La lectura administrativa `sudo -n ufw status verbose` fue rechazada: exit 1, `sudo: a password is required`. No se modificó el firewall ni router/DNS/trust del sistema.

**BLOCKED antes de preparación**. Confianza HTTPS física, QR/READY físico y ambas duraciones humanas: NOT RUN / NOT MEASURED. Android físico: NOT RUN. Gate cerrado: Player exit 0, Java exit 0, STOPPED, cleanupComplete=true, forced=false, procesos Java propios residuales 0. El almacén privado de esta instalación permanece fuera del repositorio para no cambiar la CA silenciosamente.

Siguiente intervención: autorización y ejecución administrativa de una excepción temporal mínima para los puertos TCP de preparación/HTTPS desde el iPhone confirmado y la interfaz Wi-Fi actual. Los puertos UDP RTC sólo se evalúan cuando la navegación HTTPS funcione; no abrir rangos arbitrarios. Se debe retirar únicamente la excepción creada al terminar. No se ejecuta ese cambio en este candidato. [Evidencia sanitizada](evidence/t008-local-trust-2026-10-08/physical-gate-01.json).

## Resultado final Software/Lab

Java 52/52 PASS (0 fail/error/SKIP), PWA 9/9 PASS/build PASS, validadores Management/Foundation PASS. Regresión RTC serial PASS con 4 clientes Chrome, 641 observaciones Unity, 18 negativos, manual recovery y cleanup normal (STOPPED, exit 0, residuales 0). Esta ejecución sustituye únicamente el gate de regresión requerido del nuevo JAR, no los resultados históricos fallidos ni los gates físicos pendientes. Evidencia: [software](evidence/t008-local-trust-2026-10-08/software-results.json) y [RTC](evidence/t008-local-trust-2026-10-08/rtc-regression-serial.json). AC3 permanece NOT RUN por ausencia de mediciones humanas.

## CI native prerequisite

Las primeras cuatro ejecuciones Foundation del candidato fallaron (Java 52: 49 PASS, 1 error, 2 SKIP de gates HTTPS LAN históricos). El diagnóstico final identifica `Initialize the default AudioDeviceModule failed`: el runner no tiene sistema de audio activo y la fábrica nativa RTC existente lo solicita incluso para DataChannel. No era un fallo TLS ni se demuestra inviabilidad de DEC-016. Fallos conservados en [evidencia](evidence/t008-local-trust-2026-10-08/ci-native-audio.json).

Corrección de entorno: el workflow prepara PulseAudio exclusivamente en el runner efímero de CI. No cambia la fábrica/contrato WebRTC, no salta la prueba real y no instala software en la laptop del usuario. No añade dependencia Maven ni servicio de gameplay. La repetición de checks se publica separadamente en el PR.
