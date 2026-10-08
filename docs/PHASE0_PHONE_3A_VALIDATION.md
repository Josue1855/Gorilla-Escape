# Validación 3A — FQDN / HTTPS confiable / LAN

**Diseño técnico Accepted; adaptación de software autorizada.** PR #94 continúa DRAFT, 3A IN PROGRESS. La experiencia de producto jamás instala CA/certificados/perfiles ni cambia DNS/browser en teléfonos. El procedimiento CA/SAN IP anterior queda histórico en Git (`50e48418ff286dcd3096c92800fd47f602ad27f2`); su evidencia no se elimina ni se presenta como prueba del nuevo gate.

Arquitectura: teléfono → Wi-Fi → FQDN controlado → DNS local router → HTTPS443 → redirección restringida → Java8443 → React empaquetado. IPC sigue exclusivamente127.0.0.1. Consultar [viabilidad](PHASE0_PHONE_3A_FEASIBILITY.md), [rediseño](PHASE0_PHONE_LAN_ONBOARDING_REDESIGN.md) y [procedimiento Linux preparado](PHASE0_PHONE_3A_LINUX_443.md).

## Configuración externa

Archivo absoluto **fuera de todo repositorio Git**; keystore/password también externos. En POSIX no se aceptan permisos de grupo/otros en ninguno de esos tres archivos: usar600 y directorio privado700. Revisar ACL en SO sin POSIX (Windows no validado). Ejemplo documental, no dominio disponible ni certificado emitido:

```properties
address=192.168.1.10
interface=wlan0
operatorConfirmed=true
hostname=play.example.org
publicOrigin=https://play.example.org
port=8443
keyStore=/ruta/privada/server.p12
passwordFile=/ruta/privada/password
```

`address` es IPv4 RFC1918 asignada a interfaz física explícita; `hostname` es FQDN ASCII minúsculo, labels válidos, sin wildcard/IP/single-label/URL. Para IDN preparar A-label ASCII; no conversión silenciosa. `publicOrigin` debe ser exactamente `https://<hostname>`, sin credenciales, puerto explícito, path, query o fragmento; representa443. `port` es escucha interna, default8443; puerto alto configurable permite pruebas efímeras/diagnóstico, nunca cambia el origen público. No confundir READY `httpPort` con443.

PKCS12: una única clave privada leaf con SAN DNS exacto (case-insensitive, sin wildcard), vigencia actual, uso TLS server adecuado y cadena ordenada completa. Todos los certificados incluidos deben estar vigentes; se validan firmas, CA/key usage/path length y PKIX sin consultas online de revocación durante startup. Root puede omitirse si el último issuer se resuelve en trust store del JRE; root incluido debe ser autofirmado válido. Material de test con root efímero es admitido para automatización: **la validación de cadena por sí sola no demuestra confianza pública en teléfonos**, que exige certificado público real y gate físico. No introduce trust-all, excepciones de hostname ni CA al cliente de producto. Mantener JRE/trust store actualizado durante preparación previa.

PasswordFile una línea (newline final permitido), ≤1024bytes; config≤4096bytes, keystore≤1MiB, cadena≤8certificados. Password se entrega a Spring en memoria, nunca argumentos/logs. Fallo produce `MOBILE_HTTPS_CONFIG_INVALID` sin contenido privado; no READY ni fallback HTTP en perfil móvil. Puerto ocupado falla startup sin elegir HTTP. Perfil IPC previo sin variable móvil conserva loopback histórico.

Lanzar **Unity**, sin root:

```text
GORILLA_MOBILE_CONFIG=<archivo absoluto privado>
GORILLA_IPC_JAVA=<java21 absoluto>
GORILLA_IPC_JAR=<JAR construido con PWA actual>
GORILLA_IPC_MODE=lifecycle
```

Variables heredadas al hijo; no cambios de contratos/supervisor Unity, singleton, EOF, cleanup ni límites de recuperación. Configuración neutral respecto de ACME/DNS; sin emisión/renovación automática dentro del juego. No QR, `/join`, sessionId, Flutter ni WS.

## Pruebas automatizadas reproducibles

1. `npm --prefix PWA test` y `npm --prefix PWA run build`.
2. JDK21: `Server/mvnw -B -f Server/pom.xml verify`. TLS real portable loopback con PKI efímera externa; preflight LAN se omite explícitamente sin dirección/interfaz física.
3. Repetir verify con `-Dgorilla.test.lanAddress=<IPv4 de test confirmada> -Dgorilla.test.lanInterface=<interfaz física>`: configuración móvil real, SAN DNS distinto de IP, origen443 separado del puerto alto, SAN incorrecto/IP-only, password/permisos/origen inválidos, cadena/vigencia/hostname verification, CA desconocida, puerto ocupado, assets/health, IPC real y EOF exit0.
4. Cliente de test conecta a IPv4 explícita con SNI DNS y verificación HTTPS del hostname; **no cambia DNS del SO**. No demuestra resolución de router ni443. Certificados temporales se destruyen; sin claves/secretos en evidencia.
5. Player Linux real existente + JAR actualizado: `tools/validate_phone_3a_player.py --player <Player> --java <Java21> --jar <JAR> --address <IPv4> --interface <interfaz> --output <directorio ignorado>`. TLS/SNI DNS real, PPid Unity confirmado, listeners IPv4LAN/loopbackIPC, PING/PONG, health/assets, EOF, exit0 y propios residuales0. No teléfonos simulados como gate.
6. Regresión `tools/validate_ipc_2b_player.py` con fixtures existentes, secuencialmente respecto de cualquier otro Player: once grupos2A/2B. Sin cambios Unity/Shared/codec no corresponde repetir benchmark600s ni reinterpretar métricas2C.
7. `python3 tools/validate_foundation.py`, `python3 tools/github/validate_management.py`, diff/secretos y `git diff --check`.

PWA conserva recursos empaquetados locales; diagnosticar imports/HTML/manifest/SW sin CDN obligatorio. APIs no-store, sin estado Connected inventado. Unity fuente/build existente se reutiliza sin cambios, con hash del artefacto registrado; no presentar ese hash como build Unity nuevo.

## Infraestructura y gate físico pendientes

No compras, certificados públicos emitidos, router alterado ni firewall aplicado en esta tarea. Dominio/accesoDNS, router compatible, preparación pública TLS y autorización operativa443 están pendientes. Procedimiento preparado no significa infraestructura probada.

Por Android/Chrome **e** iPhone/Safari físicos: registrar modelos, versiones, router/firmware, LAN/FQDN, IP/reserva/DNS, certificado público SAN/issuer/notBefore/notAfter/fingerprint público, PC/JRE y artefactos. Nunca claves/passwords/tokens/dump de entorno ni datos personales.

- Cero instalación CA/perfiles y cero DNS/manual/browser; URL `https://<FQDN>/`, sin puerto ni advertencias; contexto seguro.
- Primera carga sin cache y sin WAN, Wi-Fi activo: shell/assets/SW/health real. Prueba stock con datos móviles activos separada de instrumentación de ruta; no atribuir a LAN respuestas públicas/cacheadas.
- Diez health por plataforma, instancia diagnóstica coincide Java y contador crece; registrar fallos y duración de cada petición. Refresh/reapertura5veces; parar Java debe fallar≤5s y jamás devolver health cacheado.
- DNS privado/VPN/DoH/selección Wi-Fi-datos/rebinding/aislamiento y cambio de IP: resultados reales, sin prometer compatibilidad universal ni pedir configuración del jugador.
- PC443 por OUTPUT y teléfono443 por PREROUTING; IPC inaccesible por TCP desde segundo dispositivo, sin wildcard/IPv6/HTTP adicional.
- Unity cierre → EOF → Java exit0, Unity exit0, cleanup y propios residuales0; rollback/red sin cambios ajenos.

**Gate:** software automatizado puede PASS, infraestructura preparada puede quedar NOT RUN y físico NOT RUN. 3A sigue IN PROGRESS hasta ambos teléfonos reales y certificado público cumplan todo. 3B/4A/4B/4C NOT STARTED; Fase0 IN PROGRESS; Fase1 NOT STARTED.
