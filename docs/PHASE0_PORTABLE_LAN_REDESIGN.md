# Rediseño portable — Fase 0 / conectividad móvil local

**Estado: REJECTED como arquitectura final por corrección explícita del Product Owner.** Se conserva el análisis histórico; HTTP táctil no satisface paridad de movimiento PWA/Flutter. Véase [nueva decisión propuesta](PHASE0_SECURE_MOTION_TRANSPORT_DECISION.md). Cambio de requisitos recibido: dominio, DNS personalizado, certificado público, router especial y443→8443 dejan de ser obligatorios. Fecha2026-10-07 UTC /2026-10-06 local. Diseño solamente; no implementación, configuración, commit/push ni merge en esta tarea. PR #94 verificado OPEN/DRAFT, head `0d940f77f533bb6780bc3f07169dce414ea21e54`. 3A IN PROGRESS; físicos NOT RUN; posteriores NOT STARTED.

El nuevo requerimiento del PO prevalece sobre el gate HTTPS obligatorio anterior. No convertir pruebas históricas HTTPS en validaciones HTTP LAN. Los documentos previos conservan evidencia y decisiones de su momento; tras aprobar este diseño habrá que reconciliar v4/DEC-006/reglas/planes de validación antes de adaptar runtime. No se declara Accepted el diseño técnico nuevo por anticipado.

## 1. Arquitectura recomendada

Unity autoridad exclusiva de gameplay, física, estado jugable, scoring, GP y resultados → IPC TCP127.0.0.1 → Java único backend local. Java administra hosting, sesión técnica, conexiones, validación/adaptación de input; no estado competitivo paralelo. React/web táctil y Flutter Android opcional consumen mismo contrato/IDs/reglas/semántica; un solo registro técnico, distintas implementaciones de cliente.

Modo web base: `http://<IPv4-LAN>:8080/` (ejemplo conceptual, puerto configurable no privilegiado). Java sirve React/assets/API en un mismo origen; posteriormente WS en ese origen. No CDN/backend externo, DNS personalizado, mDNS obligatorio, Vite runtime ni proxy. Java sigue hijo supervisado por Unity, READY, EOF y cleanup existentes. No root ni regla443→8443 necesaria para este modo. Si puerto ocupado, error explícito; no binding wildcard ni cambio silencioso de esquema. Native HTTPS/WSS futuro usa mismo Java/sesión, listener seguro separado si se aprueba, no segundo backend.

No significa producto jugable hoy: #94 sólo contiene hosting/diagnóstico, no controles ni gameplay. Web táctil es el mínimo del roadmap revisado; la implementación de juego permanece fuera de esta tarea/Fase0 de conectividad.

## 2. Diferencias respecto a HTTPS obligatorio

| Antes | Propuesta portable |
|---|---|
| FQDN público/DNS-01/DNS router/443 obligatorios | URL por IPv4 local y puerto alto; ninguno de esos recursos obligatorio |
| Cliente web dependiente de contexto seguro | Web táctil HTTP LAN; sensores/SW no son prerrequisitos |
| PWA offline instalable como expectativa | Web disponible sin WAN porque Java sirve assets localmente; sin prometer SW/instalación/caché offline |
| WSS obligatorio para todos | WS para web HTTP; HTTPS/WSS con pinning para Flutter futuro; misma semántica y backend |
| App Links con dominio | Apertura manual de app + lector QR propio futuro; asociación verificada de IP privada no garantizada |
| Router con DNS especial | Cualquier LAN que dé IP y conectividad entre dispositivos; hotspot opcional probado por equipo |

TLS anterior conserva utilidad de test y puede ser perfil avanzado opcional después de evaluación; no añadir complejidad para conservarlo como gate base ni borrar evidencia funcional.

## 3. Modo A — Wi-Fi existente

Enumerar interfaces/IPv4 realmente activas, mostrar nombre/IP y pedir al operador seleccionar LAN. Filtrar loopback de candidatos móviles; distinguir VPN/túneles/virtuales de interfaces físicas, sin inferir seguridad por RFC1918. Primera implementación propuesta sólo IPv4 RFC1918 en red de juego autorizada; IPv6/link-local fuera del mínimo. No elegir por ruta default a ciegas, escanear LAN ni abrir automáticamente firewall.

Java mobile bind exclusivamente a IPv4 seleccionada, IPC bind127.0.0.1. Mostrar URL exacta sólo tras readiness; PC muestra estado real y error. DHCP existente basta; reserva opcional para estabilidad, no requisito. Cambio IP/interfaz invalida URL/QR y conexiones: detener/publicar nuevo endpoint con recuperación manual, no seguir anunciando IP vieja ni auto-restart ilimitado.

No detectar aislamiento sólo por ping: ICMP puede estar bloqueado. Diagnóstico progresivo: listener/IP local, permiso firewall, teléfono alcanza health TCP/HTTP, contraste con segundo cliente autorizado. Mensaje «no se puede alcanzar PC; posible aislamiento/firewall/ruta» mientras no haya evidencia de causa. Red de invitados/empresa que aísle clientes no soportada para juego; usar red existente permitida o hotspot disponible, nunca modificar router ajeno automáticamente.

## 4. Modo B — Hotspot de laptop

| Host | Capacidad documentada | Límite / estado del proyecto |
|---|---|---|
| Windows11 | Mobile hotspot comparte conexión por Wi-Fi | Windows/driver/ICS y arranque sin upstream deben probarse; no dar offline por garantizado |
| Windows10 | Documentación incluye mobile hotspot | Compatibilidad de legado propuesta sólo versión mantenida/ESU adecuado; fin soporte estándar2025-10-14; no validado |
| Linux NetworkManager | Función hotspot; AP depende hardware/driver; shared proporciona red local | En esta Pop!_OS AP=yes histórico, no hotspot arrancado. Verificar DHCP/IP/LAN sin WAN, suspensión y rollback |
| macOS | Internet Sharing, ejemplo Ethernet→Wi-Fi | Evaluación solamente; no asumir Wi-Fi cliente+AP en la misma radio ni AP sin upstream; fallback LAN |

Fuentes: [Microsoft hotspot](https://support.microsoft.com/en-us/windows/experience/connectivity-networking/use-your-windows-device-as-a-mobile-hotspot), [NetworkManager nmcli](https://networkmanager.pages.freedesktop.org/NetworkManager/NetworkManager/nmcli.html), [Apple Internet Sharing](https://support.apple.com/en-sa/guide/mac-help/mchlp1540/mac).

Sin upstream/WAN debe demostrar DHCP y conexión teléfono→host; compartir Internet no es prueba de red offline. No exigir AP+cliente simultáneo: una sola radio puede perder Wi-Fi actual. Antes de activar: mostrar conexión afectada, capacidades, permisos y rollback; autorización específica del operador. Priorizar UI nativa/configuración existente frente a servicio propio privilegiado. No scripts que editen DNS/firewall/perfiles ni instalar hostapd por defecto. Si no disponible, ModoA; no comprar adaptador por iniciativa propia.

## 5. Cliente web táctil y restricciones

Controles futuros: botones/pad y gestos táctiles con Pointer Events y eventos cancel/up; teclado de desarrollo opcional. Input neutral al desaparecer foco, ocultar página/perder conexión/cancelar gesto; Java/Unity deben detectar stale input, no dejar botón pulsado indefinidamente. En navegador suspensión/background no se garantiza streaming continuo. Audio después de gesto permitido; fullscreen/hápticos mejoras condicionadas, no criterios mínimos. No UI «Connected» derivada sólo de shell cargado.

| API / capacidad | HTTP IP privada LAN | Decisión |
|---|---|---|
| HTML/JS/React/fetch/táctil | Base viable propuesta, sujeto a browser/políticas/red física | Todos los assets locales; primera carga sin WAN |
| Secure context | IP privada de otra PC no es localhost del teléfono | `isSecureContext=false` esperado; nunca falsificar |
| Service Worker | Registro exige secure context; no prometer en HTTP LAN | Funcionalidad web no depende de SW; «PWA instalable offline» no describe este perfil |
| DeviceMotion / DeviceOrientation | Estándar exige contexto seguro; comportamiento de versiones puede variar | Sensores no disponibles en baseline, controles táctiles siempre; no flags/excepciones |
| iPhone Safari | Permisos de sensores/gesto cuando API se ofrece en contexto permitido | No llamar permiso en HTTP ni prometer sensores; medir físico si perfil seguro futuro |
| Android Chrome | Motion/orientation/Generic Sensor restringidos; permiso/políticas varían | Feature detection más eventos reales si futuro autorizado, nunca50Hz por promesa |
| WebSocket | WS no cifra; API no convierte HTTP en secure context | WS futuro mismo origen; WSS native. No reutilizar bridge HTTPS→WS inseguro como atajo universal |

[W3C Secure Contexts](https://www.w3.org/TR/secure-contexts/) establece excepciones loopback y requisito SW; [W3C Motion/Orientation](https://www.w3.org/TR/orientation-event/) marca APIs secure y permisos. [Chromium restricciones](https://chromium.googlesource.com/playground/chromium-org-site/+/refs/heads/main/Home/chromium-security/deprecating-powerful-features-on-insecure-origins.md); [WebSocket estándar](https://websockets.spec.whatwg.org/), [Mixed Content](https://www.w3.org/TR/mixed-content/).

No confundir offline sin Internet (Java vivo en LAN) con offline sin servidor. No prometer cache/instalación/sensores como mínimos. Políticas HTTPS-only pueden impedir HTTP sin ajuste: registrar incompatibilidad, no pedir excepción de seguridad. Chrome documenta LNA para público→local y expansión futura; elegir navegación directa IP + API/WS mismo origen reduce esos cruces, no garantiza cualquier versión futura. [Chrome LNA](https://developer.chrome.com/blog/local-network-access). Safari se distingue de permisos de apps nativas; no atribuir automáticamente al sitio la política de otra app. [Apple TN3179](https://developer.apple.com/documentation/technotes/tn3179-understanding-local-network-privacy). Validar versiones reales, sin extrapolar Chrome133 emulado.

## 6. Flutter Android opcional

Diseñar para acelerómetro/giroscopio, vibración, audio local, touch y lifecycle; calidad/frecuencia/reconexión sujetas a hardware/OS y medición. Mismos IDs/session/sequence/validación y autoridad Unity. Negociar capacidades sin cambiar contrato de negocio; touch web no obliga a simular sensor ni Flutter a crear otra plaza para el mismo jugador.

Sin dominio no prometer App Links verificados de Android. Ruta inicial futura: app desde launcher → escanear QR de PC con permiso de cámara nativo o ingresar endpoint/código; sin app QR abre web. El scanner Flutter es onboarding futuro, no implementación de cámara/gameplay aquí. Un único QR puede contener URL web con fragmento versionado y metadata pública para app; formato exacto pendiente3B/4C, nunca token IPC. Browser usa flujo web y native interpreta endpoint TLS + fingerprint; no redirección HTTPS por CA desconocida al navegador.

## 7. Emparejamiento común propuesto

Java crea sesión técnica efímera por lanzamiento, máximo4 plazas; IDs canónicos asignados por backend técnico y aprobación/rol de juego decidido Unity. Código temporal visible sólo en PC/confirmación de incorporación por operador, expiración, intentos acotados, invalidación al cierre y recuperación manual. No cuentas/deviceId de fingerprinting. Código es comprobación de admisión, no cifrado ni defensa contra MITM en HTTP.

Después de admisión, credencial efímera por cliente en memoria, sin URL/query/logs persistentes. Secuencia, versión, payload/tamaño/frecuencia/estado válidos para ambos clientes. Cambio web↔Flutter requiere handoff explícito y revocación anterior para evitar plazas duplicadas; fuera del incremento actual. QR público identificador de sesión no basta para autenticarse. Parámetros finales de códigos, expiración y cuotas se cierran en3B/4A, no Gorilla Protocol completo aquí.

## 8. Seguridad web y alternativa native

HTTP/WS revela y permite modificar página/mensajes. Códigos/nonce/Origin no autentican código entregado por HTTP ante atacante activo; seguridad mínima sólo para red de juego controlada, sin datos sensibles. No defender malware mismo usuario ni presentar red local como confiable. En red hostil el modo web no ofrece protección criptográfica: Flutter opcional seguro futuro, o rechazar esa red. WPA2/WPA3/router no sustituye TLS extremo a extremo.

Mitigaciones propuestas sin servicios cloud: bind IPv4 concreta, endpoints móviles explícitos (assets/health/join/input futuros), sin consola admin/remoto/archivos privados; no actuator/debug/listados en LAN. IPC y token sóloloopback; validación Host/origen exactos contra endpoint seleccionado, sin CORS*. WS necesita validación Origin propia, CORS HTTP no basta; native tiene política explícita distinta, no «permitir cualquier Origin ausente». Autorización para cada mensaje/slot, expirar credenciales, schema estricto, secuencia, deadlines, buffers y límites globales además de por conexión; negar antes de reservar recursos cuando sea posible. Cuotas numéricas pendientes4A; no afirmar presión/control móvil implementado por límites IPC existentes. No tokens en logs/capturas, no credenciales personales, mínima telemetría. Firewall permitido sólo mediante acción del operador, subred/interfaz concreta, sin UPnP/DMZ ni reglas globales.

Native futura: Java genera leaf efímero por instancia con SAN IP seleccionada y clave privada en PC. QR visible en PC entrega SHA256 del certificado DER + endpoint/puerto/instanceId; pin verifica certificado exacto durante handshake antes de enviar credencial/input. Comprobar vigencia/IP/instancia; mismatch/cambio key/expiry → rechazo y nuevo pairing físico. No TOFU silencioso, trust-all o fallback HTTP automático; no instalar CA en Android. Un QR suplantado también suplanta confianza: origen físico/autenticidad de pantalla y fingerprint son supuestos explícitos.

TLS conserva prueba de posesión de private key; pin bootstrap reemplaza CA pública sólo dentro del cliente app y sólo para esa sesión. Evaluar implementación auditable con SecurityContext/validación pin, no depender de aceptar cualquier fallo certificado. [Dart HttpClient](https://api.dart.dev/dart-io/HttpClient-class.html), [callback de certificado](https://api.dart.dev/dart-io/HttpClient/badCertificateCallback.html). La API permite decidir sobre un certificado no autenticado, **no demuestra pinning correcto por sí sola**. Spike4C debe probar mismatch, SAN, vencimiento, rotación y WSS; librería/adaptador sólo si evidencia justifica y PO autoriza. Fingerprint público puede ir en QR; secreto de cliente nunca viaja hasta autenticar TLS. WebHTTP y nativeTLS comparten semántica, no nivel de seguridad.

## 9. Matriz mínima de soporte

| Plataforma | Soporte propuesto | Implementado/probado hoy |
|---|---|---|
| Windows11 x64 mantenido | Unity Player/JRE21 compatibles, IPv4LAN, firewall autorizado; hotspot condicional | IPC diseño portable; Player Windows y mobileLAN no probados |
| Windows10 x64 mantenido | Compatibilidad condicionada a OS soportado/actualizado y build Unity/JRE compatibles; sin promesa para EOL sin mantenimiento | No probado; no plataforma preferida para nuevo despliegue |
| Linux x64 compatible | Unity/JRE21, sesión gráfica/controladores compatibles; NetworkManager sólo para hotspot opcional | Pop!_OS24.04 PC/IPC/hosting previos PASS; HTTP LAN físico nuevo NOT RUN |
| macOS Intel/AppleSilicon | Evaluación: build Unity/Java de arquitectura adecuada, permisos LAN/firewall; hotspot no universal | Ni build ni ejecución probados; no soporte comprometido |
| Android Chrome | Browser mantenido con JS/fetch/PointerEvents/WebSocket; acceso LAN, HTTP permitido; touch mínimo | Chrome133/API36 emulado10PASS/2BLOCKED/1SKIP histórico, no HTTP físico nuevo |
| iPhone Safari | iOS mantenido con APIs táctiles/fetch/WS y LAN accesible; sin sensores requeridos | Físico NOT RUN |
| Flutter Android | Opcional; versión mínima Android a fijar por Flutter/plugins y pruebas4C | NOT STARTED; no promesa de sensores/tasas |

No inventar mínimo iOS/Android a partir de simulación: antes de afirmar soporte, nombrar modelos/OS/browser probados y API/build constraints. Requisitos reales de entrada: PC capaz de Player/JRE y1–4 conexiones, assets empaquetados; LAN sin aislamiento entre teléfonos y PC; DHCP/IP válida; touch+browser compatible stock; autorización de puertos por operador. No garantía para hardware viejo, ARM desktop sin build, VPN/HTTPS-only/políticas empresariales. Hardware/performance final necesita medición, no nuevos umbrales arbitrarios.

## 10. Auditoría #94 y reutilización

Inspección real de App.jsx/main.jsx/health.js, application.properties, MobileHttpsSettings, bootstrap Java/ManagedProbe y diff94. Default HTTP127.0.0.1:8080 ya configurable por entorno, pero **no es aún perfil mobileLAN aprobado/adaptado**. GORILLA_MOBILE_CONFIG fuerza hostname/origenHTTPS/SAN DNS; no basta cambiar URL y declarar producto portable. UI actual indica HTTPS; registra SW cuando disponible y sólo diagnostica health. No controles jugables implementados.

| Componente | Tratamiento propuesto, no ejecutado |
|---|---|
| React empaquetado/estilos/health real/no-store y errores | Reutilizar; adaptar textos/diagnóstico de HTTP y SW ausente sin bloqueo |
| Java supervisado/READY/stdinEOF/locks/deadlines/cleanup | Conservar; no rediseñar lifecycle |
| IPC127.0.0.1/codec/identidades/token | Conservar sin exposición móvil ni reutilización del token |
| IPv4/interfaz/config privada/validaciones | Reutilizar selección/validación; separar modo webLAN del TLS anterior sin obligar keystore/FQDN |
| MobileHttpsSettings y testsTLS | Conservar evidencia histórica/fixtures útiles; decidir perfil opcional después, sin aflojar trust silenciosamente |
| Pruebas Java30/React5/Player11 e IPC | Evidencia histórica exacta; repetir únicamente afectadas al implementar adaptación |
| Emulador harness/resultados | Reutilizable,10PASS/2BLOCKED/1SKIP intactos; añadir test HTTP IP remota real, no convertir localhost secure en LAN segura |
| DocsHTTPS/443/preflight y gate físico | Históricos; actualizar referencias/norma tras revisión, no borrar intentos ni marcar nuevos gatesPASS |

## 11. Roadmap revisado propuesto

| Incremento | Alcance | Gate |
|---|---|---|
| 3A revisado | Hosting webLAN HTTP por IPv4/puerto explícitos, selección interfaz/URL manual, health/errores y seguridad de exposición; hotspot evaluación opcional separada | Android/Chrome + iPhone/Safari físicos: primera carga sinWAN, noCA/DNS/app, assets/health reales, cleanup/IPC aislado |
| 3B | QR único/sesión técnica/código de incorporación/1–4 plazas | Admitir/rechazar/expirar, sin jugadores duplicados ni autoridadJava |
| 4A | WS/contrato común/touch de prueba/validation y buffers/cuotas/stale-input | Input de ambos modelos de cliente hacia Java, no gameplay/scoring; WS sólo perfilweb |
| 4B revisado | Compatibilidad web táctil y capability checks; sensores sólo donde contexto permitido exista, fuera baselineHTTP | Android+iPhone con control táctil real y lifecycle; no desbloquear sensores mediante flags |
| 4C | Flutter Android opcional/sensores/lifecycle + TLS/WSS pin por QR | Paridad contrato/sesión, rechazo mismatch/expiry y comparación de input real |
| Posterior Fase0 | Hardening/comparación/web+native/1–4 clientes y portabilidad host | Medir por transporte/dispositivo, no reutilizar RTTIPC como móvil |

Touch en4A es harness input, no minijuego. UX jugable completa se integra con gameplay después de autorización de fase correspondiente. Reordenar sensoresPWA respecto del viejo4B requiere aprobación del roadmap; objetivo sensor obligatorio iPhone webHTTP es incompatible con estas restricciones, no se promete.

## 12. Riesgos y aceptación

Riesgos: MITM/escucha webHTTP; LAN aislada/firewall/VPN; cambioDHCP; browser políticas/permisos futuros; suspensión móvil; pinchazo/capacidad hotspot/caída de Wi-Fi actual; falta Windows/macOS/Android/iPhone físicos; seguridad pinFlutter aún no probada. Reducciones de alcance no borran riesgos; no certificación global por demo.

Gate3A nuevo propuesto: ambos teléfonos de referencia identificados, primera visita sinWAN/sin cache del origen ni salida celular usada para simular éxito, luego repetir stock con datos activos; URLIP:puerto, assets/API locales y≥10health/≥5recargas-reaperturas por plataforma; HTTP/noSecure/noSW descritos correctamente, no sensores/permisos pedidos; sólo interfazLAN, IPC inaccesible remotamente; errores observables y sin falsohealthcacheado; Unity/Javaexit0 y residualespropios0; firewall/red originales conservados o rollback de acciones expresamente autorizadas. Hotspot PASS sólo por host/driver ensayado, no gate universal si ModoA disponible. No «3A PASS» por táctil/gameplay aún inexistentes.

Gates posteriores: 1–4 clientes reales, misma sesión/protocolo, ingreso autorizado/límites/expiración, táctil usable sin sensores, stale-input neutral, app opcional y pin estricto, versiones OS/browser registradas. Cifrado no se atribuye a HTTP/WS. Fallo presupuesto/compatibilidad se registra con muestras reales, nunca alterar restricciones del teléfono.

Decisiones de revisión: aceptar perfilHTTP/WS y su riesgo en LAN controlada; aprobar gate3A sinPWAinstalable/sensores; puerto8080 configurable; prioridadLAN y hotspot condicional; roadmap4B revisado; alcance futuroTLS/WSS native porpin/QR. Ninguna dependencia externa nueva obligatoria, compra, servicio, instalación o modificación de red realizada. Mantener #94 DRAFT y detenerse tras diseño.
