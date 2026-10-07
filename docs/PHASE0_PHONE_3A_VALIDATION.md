# Validación 3A — HTTPS LAN sin onboarding

> **Histórico: no ejecutar setup CA en teléfonos como aceptación del producto.** El PO exige onboarding sin instalar certificados/perfiles ni cambiar DNS/browser. Consultar [rediseño vigente](PHASE0_PHONE_LAN_ONBOARDING_REDESIGN.md); #94 sigue DRAFT. Tests TLS efímeros previos no demuestran el nuevo gate de confianza pública.

Diseño [Accepted](PHASE0_PHONE_LAN_ONBOARDING_DESIGN.md). Sólo 3A autorizado. Base
`ec5880c94304e8c7d587c5f5d8c2cf28dbf5a760`, rama
`feature/mobile-phase0-lan-https-3a`. 3B/QR/sesión/sensores NOT STARTED.

## Preparación de red y TLS

Operador identifica PC/router y confirma interfaz e IPv4 privada asignada. No seleccionar
silenciosamente una interfaz ni cambiar router/firewall. Comprobar misma LAN real: una
red de invitados/escuela puede aislar clientes aunque comparta SSID. Java rechaza loopback,
link-local, direcciones públicas, wildcard e IPv6; interfaz caída, túnel, virtual o IP no
asignada. Linux requiere un dispositivo físico en `/sys/class/net/<interfaz>/device`;
no se admiten bridges/VPN como interfaz móvil. Windows no validado.

1. Preparar mkcert desde [su distribución oficial](https://github.com/FiloSottile/mkcert).
   Registrar versión (`mkcert -version`), origen/hash binario y responsable. No instalación
   automática desde el juego ni dependencia runtime. CA/leaf/password/PKCS12 viven en
   directorio privado **fuera del repositorio**. No distribuir rootCA-key.pem.
2. En ese directorio privado, establecer CAROOT para CA exclusiva de demo. Generar leaf
   con SAN **IP exacta confirmada** usando mkcert. Convertir leaf/key y cadena pública a
   PKCS12 mediante OpenSSL; introducir password por entrada interactiva o archivo privado,
   jamás argumento. PC Linux: directorio700, archivos privados600; revisar ACL en otros SO.
3. Transferir sólo rootCA.pem público a teléfonos mediante medio controlado. Verificar
   fingerprint con operador. Android: instalar CA de usuario y comprobar Chrome realmente.
   iPhone: instalar perfil y habilitar confianza completa SSL/TLS; perfil solo no basta.
   No visitar URL con excepción TLS ni desactivar validación.
4. Configurar archivo de operador absoluto fuera del repo, con campos siguientes (ejemplo,
   reemplazar dirección/interfaz/rutas; no copiar una IP sin confirmar):

```properties
address=192.168.1.10
interface=wlan0
operatorConfirmed=true
port=8443
keyStore=/ruta/privada/fuera-del-repo/server.p12
passwordFile=/ruta/privada/fuera-del-repo/password
```

Sólo esos campos se permiten; puerto opcional default8443 (1024–65535). PasswordFile es
una línea UTF-8 (se permite newline final); no se registra su contenido. Spring recibe
configuración en memoria; no se pasan passwords al proceso por argumentos.

5. Lanzar **Unity**, no otro Java, con `GORILLA_MOBILE_CONFIG` apuntando al archivo y las
   rutas Java/JAR existentes (`GORILLA_IPC_JAVA`, `GORILLA_IPC_JAR`). Usar modo lifecycle
   existente `GORILLA_IPC_MODE=lifecycle` para mantener el hijo activo. Ejemplo conceptual:

```text
GORILLA_MOBILE_CONFIG=<archivo absoluto externo>
GORILLA_IPC_JAVA=<java21 absoluto>
GORILLA_IPC_JAR=<JAR absoluto construido con esta PWA>
GORILLA_IPC_MODE=lifecycle
```

Estas variables se heredan al child. No cambia código ni contrato Unity: en Java el archivo
móvil opt-in prevalece sobre argumentos HTTP loopback existentes del supervisor. Sin esa
variable, los perfiles previos conservan sus defaults. READY sigue idéntico; httpPort
representa ahora el puerto HTTPS en modo móvil. IPC continúa 127.0.0.1:0, token separado.
Certificado inválido, SAN incorrecto, IP/interfaz inválida o puerto ocupado no producen
READY móvil ni fallback HTTP. Reconfiguración/reinicio es manual, límite3 de2B conservado.

6. Abrir manualmente `https://<IPv4>:8443/` en teléfono. No Vite, QR, `/join`, sessionId,
   deviceId, presencia, WebSocket ni permisos de sensores/cámara. La pantalla muestra
   contexto seguro/HTTPS y botón **Comprobar Java ahora**. La respuesta de health indica
   instancia diagnóstica de lanzamiento y contador real; comparar instancia con PC.
   La UI indica resultado puntual, nunca estado Connected ni conexión continua.

## Firewall y exposición

Primero intentar sin modificar firewall. Si es necesario y el SO usa UFW, el operador
revisa/autoriza la regla concreta; ejemplo a sustituir por interfaz/IP/subred comprobadas:

```text
sudo ufw allow in on wlan0 from 192.168.1.0/24 to 192.168.1.10 port 8443 proto tcp comment 'Gorilla-3A-demo'
sudo ufw delete allow in on wlan0 from 192.168.1.0/24 to 192.168.1.10 port 8443 proto tcp
```

Primera línea instala y segunda revierte; no ejecutar ambas como receta automática.
No deshabilitar firewall ni añadir UPnP/port forwarding. Si UFW no es firewall activo,
identificar la regla equivalente antes de cambiarla, no activar otro firewall encima.
Registrar listeners del PID propio: exactamente IPv4 LAN:puerto HTTPS y127.0.0.1:IPC,
sin wildcard/IPv6/HTTP paralelo. Desde segundo dispositivo LAN intentar IPC con un
cliente TCP real y registrar fallo. Un fallo de fetch/browser no prueba inaccesibilidad TCP.

## Automatización reproducible

- `npm --prefix PWA test` y `npm --prefix PWA run build` antes de Maven: el JAR incorpora
  dist real. No usar dist antiguo.
- `JAVA_HOME=/usr/lib/jvm/java-21-openjdk-amd64 Server/mvnw -B -f Server/pom.xml verify`:
  regresión IPC y pruebas TLS reales portables en loopback; test preflight SAN/password
  LAN explícito se declara omitido si no se proveen las propiedades de dirección/interfaz.
- Para añadir prueba automática de binding LAN real desde PC:
  `... verify -Dgorilla.test.lanAddress=<IP confirmada de test> -Dgorilla.test.lanInterface=<interfaz>`.
  Material TLS efímero creado por OpenSSL dentro del directorio temporal de JUnit, no
  certificado de demo ni fixture privada versionada. Cliente confía exclusivamente CA
  efímera; cliente default rechaza CA; SAN diferente rechazado con hostname verification
  activado. No trust-all. Pruebas incluyen assets, APIs404, puerto ocupado, no fallback,
  IPC LAN inaccesible desde PC, PING/PONG real y EOF exit0/child no residual.
- `python3 tools/validate_foundation.py` y `python3 tools/github/validate_management.py`.
- Player existente no cambió; usar JAR nuevo y `tools/validate_ipc_2b_player.py` con
  helpers de2B: once grupos incluyendo singleton, EOF, cleanup y límite de reintentos.
  No benchmark600s nuevo: codecs/lifecycle no cambiaron.

Automatización de PC no sustituye ninguna prueba física. Aun con CA instalada, Chrome
puede no confiar por SAN, policy o almacén: investigar y registrar, nunca marcar PASS.

## Checklist físico y evidencia

Completar **por Android/Chrome y iPhone/Safari** disponibles, marcar ausente NOT RUN:
modelo, OS/browser/version, PC/router/red, IPv4/interfaz/puerto, fingerprint público CA,
SAN/vigencia/reloj; setup inicial separado de tiempos normales. No guardar keys/passwords,
launch tokens, dump de entorno ni datos personales. No declarar hardware oficial si no elegido.

1. Abrir URL manual, cero advertencias, HTTPS sí, `isSecureContext` sí.
2. Shell/manifest/iconos/assets/sw locales, sin CDN ni recursos externos necesarios.
3. Botón health: instancia coincide PC, requestNumber aumenta, secure=true; Java recibe
   petición HTTPS real. Registrar mínimo10 intentos por plataforma, todos los fallos.
4. Desconectar WAN del router conservando LAN. Borrar datos del origen para primera carga
   **sin cache**. Repetir shell y health; worker active/control observado en inspector o
   diagnóstico físico. No sustituir con modo offline devtools ni datos móviles activos.
5. Refresh y cerrar/reabrir5veces; assets cacheados disponibles y APIs nunca de cache.
6. Detener Java desde Unity; volver a comprobar falla ≤5s. Reabrir shell cacheado sin
   anunciar falso éxito. Primer acceso sin cache con Java apagado puede ser sólo error
   navegador; no prometer que React pueda mostrar mensajes antes de cargar.
7. IP incorrecta/red ajena/SAN equivocado/CA no confiable: registrar comportamiento real;
   el operador guía cuando no puede cargar UI. Cambiar IP obliga certificado/URL/reinicio
   manual; no fallback ni excepción TLS.
8. IPC inaccesible desde segundo dispositivo LAN; puerto HTTPS correcto visible. Normal
   cierre Unity/Java exit0, cleanup y cero Java propio residual. No matar procesos ajenos.

Métricas por intento: inicio apertura URL→shell visible con cronómetro externo, shell→
respuesta Java válida (duración de petición mostrada), éxito/fallo/causa, primera carga,
refresh/reapertura separados. No mezclar setup CA ni relojes PC/teléfono; no latencia sensores
ni extrapolación a gameplay. Internet perdido con LAN activa es diferente de PC apagada.

**Gate:** PASS sólo para plataformas físicas realmente validadas, con TLS confiable,
primera carga sinWAN, API real, SW/refresh/reapertura y exposición/cleanup correctos.
Android PASS no implica iPhone PASS; absent/pendiente NOT RUN. Incremento3 IN PROGRESS,
3B NOT STARTED, Fase0 IN PROGRESS y Fase1 NOT STARTED. PR puede quedar draft mientras
falta gate físico, nunca se declara3A PASS por sólo automatización.
