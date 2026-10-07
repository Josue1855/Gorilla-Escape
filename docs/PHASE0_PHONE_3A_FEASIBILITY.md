# Viabilidad técnica y operativa 3A — HTTPS LAN sin configurar teléfonos

**Estado: Proposed; evaluación, no implementación.** Arquitectura objetivo aprobada
conceptualmente por PO: PWA universal, Flutter Android opcional/preferido, Java único
backend, Unity autoridad, sesión/contratos/WSS comunes y enlace/QR único. La solución
operativa que sigue requiere aprobación. Base integrada develop
`ec5880c94304e8c7d587c5f5d8c2cf28dbf5a760`. Candidato #94 revisado:
`85c88ff364ca4f1382e02c63266e5269b03b4fe2`, OPEN/DRAFT.
[Rediseño](PHASE0_PHONE_LAN_ONBOARDING_REDESIGN.md), [DEC-006](DECISIONS.md),
[evidencia anterior](TEST_REPORT.md). Ningún cambio runtime/red, compra ni emisión TLS.

## 1. Recomendación y condiciones de viabilidad

**Router dedicado configurable + FQDN real controlado + certificado público DNS-01
preparado previamente + Java HTTPS en IPv4 LAN:8443 + DNAT local acotado443→8443.**

```text
Teléfono sin CA/DNS/browser mods
  → Wi-Fi del router de demo
  → DHCP entrega red y DNS local normalmente
  → FQDN controlado resuelve a IPv4 reservada de PC
  → HTTPS443 → redirección del kernel PC → Java HTTPS8443
  → React/assets del JAR y API Java
Unity → hijo Java / stdin EOF
IPC Unity↔Java → exclusivamente127.0.0.1
```

Un único backend, sin Vite público, proxy TLS, daemon extra de aplicación o Java root.
DNS ya reside en router; redirección usa firewall/kernel existente. El esquema permite
partida sinWAN, **sujeto a teléfonos/red de referencia comprobados**. No satisface una
promesa de compatibilidad offline con toda VPN/DNS privado posible: ese límite requiere
aceptación explícita del PO, no excepciones TLS ni configuración obligatoria del jugador.

## 2. Evidencia, propuestas y bloqueos

| Estado | Hecho |
|---|---|
| Demostrado en evidencia #94 | Java26/26, React5/5, Player HTTPS con Java hijo, IPC loopback, EOF/cierre limpio y11 grupos Player; certificados efímeros de test |
| Comprobado read-only ahora | #94 OPEN/DRAFT; NetworkManager instalado; `wlp0s20f3` conectada y anuncia AP=yes; `nft` disponible; Ethernet física existe pero está unavailable |
| No demostrado | Hotspot estable, AP simultáneo con Wi-Fi cliente, DHCP/DNS personalizado, DNAT 443, HTTPS de CA pública, Android/iPhone reales sin preparación, offline primera carga |
| BLOCKED por preparación no acreditada | Dominio controlado/DNS delegado no confirmado; router/modelo/administración no confirmados; teléfonos/modelos/versiones sin identificar |
| Propuesto | Router dedicado, certificado público, DNS offline, DNAT 443→8443, publicación futura assetlinks |

No concluir que el PO no posee dominio/router: **no existe confirmación de disponibilidad**.
No probar hardware por emulador. Disponibilidad AP anunciada no es prueba de onboarding.

## 3. Dominio y proveedor DNS: requisitos exactos

Necesario un FQDN estable, p.ej. `play.<dominio-controlado>`; es ejemplo, no nombre adquirido.
Operador tiene autorización del titular y acceso a zona DNS o delegación controlada de
`_acme-challenge.<hostname>`. Registrador y proveedor DNS pueden ser distintos.

Proveedor debe permitir TXT DNS-01 (UI basta para emisión manual del spike; API acotada
preferible para renovación reproducible), registrar TTL/propagación y soportar CAA que
permita al emisor elegido. Delegación CNAME/NS de challenge evita entregar credenciales
de toda la zona al host. No subir tokens DNS a Java/JAR/QR/repo/logs ni exponer API DNS al
jugador. Sin necesidad de publicar el servidor de juego a Internet para emitir.
[Let's Encrypt DNS-01](https://letsencrypt.org/docs/challenge-types/).

Si no hay dominio disponible, opciones en orden: reutilizar dominio de equipo con permiso;
subdominio institucional delegado; proveedor de subdominio sólo si concede TXT/delegación,
control estable y futura asociación Android; compra de dominio tras aprobar coste anual.
No registrar ni pedir certificado en esta tarea. Dominio dinámico/gratuito sin TXT no
resuelve automáticamente DNS-01. No usar IP privada/`.local` como identidad pública TLS.

## 4. Certificado y preparación previa

Leaf con SAN **DNS hostname exacto**, fullchain apropiada y clave privada en host privado;
la IPv4 del bind ya no es identidad certificada. Evitar wildcard si basta un hostname;
una clave por estación/servicio. Emisor público de confianza en Android/iOS de referencia;
Let's Encrypt es candidato, no solicitud realizada. Emisión DNS-01 requiere WAN previa.
Convertir a PKCS12 para Spring existente; datos privados fuera del repositorio con
permisos600/directorio700 o ACL equivalente. No compartir keystore/private key con
publicación assetlinks; sitio público puede usar su propio certificado del mismo nombre.

Registrar emisor/perfil/notBefore/notAfter/SAN/chain y fingerprint público, sin secretos.
No fijar90días como garantía: perfiles y duraciones cambian; verificar certificado emitido
real. Renovación programada en preparación usando recomendaciones ACME/ARI del emisor,
con alertas; no depender de que Java renueve durante gameplay.
[Let's Encrypt: evolución de vigencia](https://letsencrypt.org/2026/02/24/rate-limits-45-day-certs.html).

Propuesta operativa: preparar/verificar antes de cada demo; vigencia cubre evento +14días
(margen propuesto, no criterio ya aprobado). Verificar reloj PC, chain y confianza de
stock phones. Expiración/SAN/chain inválidos bloquean el arranque/demostración, sin fallback
HTTP ni trust-all. Renovar leaf y reiniciar manualmente Java tras revisión, conservando
supervisión/EOF. Preparación online no implica nube obligatoria durante partida; renovación
o verificación externa que no pueda completarse offline se declara límite operativo.

## 5. Comparación DNS/red offline

| Opción | Jugador / Android+iPhone | Operador, seguridad y reproducibilidad | Coste / decisión |
|---|---|---|---|
| A Router dedicado DHCP/DNS | Sólo seleccionar Wi-Fi; DNS automático estándar. Stock teléfonos por validar; DNS privado/VPN pueden omitir DNS LAN | Reserva IP, registro FQDN local persistente, AP isolation off en segmento de juego, WAN desconectable, backup/restauración de config. PC puede usar Ethernet | Equipo si no existe; **recomendada**, menos dependencia del escritorio |
| B Hotspot PC controlado | Misma UX nominal, mismas limitaciones de resolver/OS | NetworkManager shared ofrece DHCP/DNS; personalización dnsmasq/driver/radio/suspensión añade diagnóstico. AP=yes no prueba concurrencia ni alcance. Desconecta Wi-Fi actual si radio no soporta ambos roles | Puede evitar compra; fallback si prueba real demuestra estabilidad, no cambiar red actual ahora |
| C Router existente administrable | Puede ser igual a A si tiene overrides DNS/reserva/control WAN/no aislamiento | Menor instalación, pero red escolar/guest administrada sin permisos no es reproducible. Reusar sólo con autorización/config verificable | **Menor coste** si cumple requisitos, alternativa antes de comprar |
| mDNS `.local`, hosts en teléfono o DNS público sólo | Hosts/DNS manual incumplen UX; nombre privado no obtiene cert público estándar; DNS público depende WAN/cache | No resuelve simultáneamente primera carga offline y confianza | Rechazadas como solución principal |
| Servicio DNS adicional en PC + router básico | DHCP debe entregar DNS PC sin ajustes del jugador | Otro componente/proceso/puerto53 y firewall en PC, duplica infraestructura respecto A | No preferida si router ya puede resolver |

NetworkManager shared permite configuración adicional de dnsmasq, pero asigna red y
forwarding; no activar ni modificar en esta tarea.
[NetworkManager oficial](https://networkmanager.pages.freedesktop.org/NetworkManager/NetworkManager/nm-settings-nmcli.html).
Router configurable tipo OpenWrt/dnsmasq es referencia de funciones, **no recomendación de
modelo ni autorización de flasheo**. [OpenWrt DHCP/DNS](https://openwrt.org/docs/guide-user/base-system/dhcp_configuration).

Requisitos router: DHCP con reserva PC, DNS A exacto del FQDN, respuesta autoritativa local
sin upstream para ese nombre, mismo segmento accesible teléfono/PC, WPA2/WPA3, contraseña
de operador, configuración exportable/versión firmware, WAN físicamente desconectable;
no captive portal/login obligatorio, no UPnP/port forwarding ni dependencia de Internet
para arrancar administración. Gestión router protegida; segmentar demo de otras redes,
no desactivar firewall general. No inventar registro AAAA si Java sólo IPv4.

## 6. Riesgos de resolución y selección de red

| Condición | Límite y tratamiento propuesto |
|---|---|
| Android DNS privado automático | Puede negociar resolver diferente; probar stock comportamiento offline, no asumir DHCP siempre gana |
| DNS privado estricto/DoH explícito | Resolver externo puede ser inaccesible sinWAN; router no puede descifrarlo ni sustituirlo de forma segura. No pedir apagarlo como requisito ni interceptar TLS; caso puede quedar no soportado |
| VPN/always-on/kill switch | Puede capturar DNS/rutas y bloquear LAN; no existe garantía general sin cambios del usuario. Test obligatorio, bloqueo reportado |
| Wi-Fi sin Internet / datos móviles | OS puede pedir confirmar permanencia en Wi-Fi, rechazar/redirigir o usar cellular. Conectar red es UX permitida; exigir cambios de DNS/CA no. No spoof de endpoints de conectividad/captive portal |
| iPhone Wi-Fi Assist | Puede emplear datos móviles; verificar que tráfico va a Java LAN, no confundir WAN celular con offline. No depender de deshabilitarlo para afirmar stock compatibilidad |
| AP/guest isolation | MismoSSID no prueba alcance; router de referencia debe permitir teléfono→PC sin abrir administración a todos |
| IP PC cambia | Reserva DHCP evita caso normal. Cambiar reserva/DNS/kernelregla de forma coordinada antes de nueva demo; SAN DNS permanece. No migración automática del runtime |
| DNS rebinding | Registro local exacto evita depender de respuesta pública RFC1918; conservar protección general. Si firmware bloquea, excepción sólo FQDN revisada por operador, nunca deshabilitarla globalmente |
| Cache DNS público anterior | Misma persona puede haber resuelto hostname al sitio público de assetlinks antes de entrar LAN; cambio de red/TTL no garantiza limpieza instantánea. Probar transición física, no exigir vaciar/configurar browser |

**Inferencia técnica:** DNS cifrado/VPN hace imposible prometer «cualquier teléfono arbitrariamente configurado +
WAN ausente + cero cambios» por sólo añadir un router. No aceptar la arquitectura como
PASS universal: definir stock dispositivos/red de referencia y reportar límites, conservando
PWA universal como cliente de producto, sin confundirlo con soporte de toda política de red.
[Google DNS/VPN](https://developers.google.com/speed/public-dns/docs/using),
[Apple Wi-Fi Assist](https://support.apple.com/en-us/102228).

Para demostrar offline: WAN desconectada y prueba instrumental sin salida celular (teléfono
de laboratorio sinSIM o modo avión + Wi-Fi como condición de ensayo, no paso obligatorio del
jugador); adicionalmente repetir con cellular activo para validar selección real sin mods.
Primera carga sin cache y resolución fría son obligatorias; cache PWA/DNS no sustituye gate.

## 7. HTTPS443 sin privilegios excesivos

| Mecanismo | Compatibilidad con Unity→child Java | Complejidad / decisión |
|---|---|---|
| CAP_NET_BIND_SERVICE sólo al proceso Java mediante launcher privilegiado acotado | Un helper que termina con exec puede preservar PID/stdin, pero hay que auditar permisos, argv, herencia y cleanup | Posible, añade código privilegiado y revisión; no elegir para este spike |
| Unidad systemd con capacidad limitada / socket activation | systemd pasa a iniciar/supervisar Java o exige FD/socket heredado que Spring actual no consume directamente | Altera ownership/EOF probado; no implementar como atajo |
| DNAT local de kernel443→IPv4 PC:8443 | Unity lanza Java normal; mismo PID/token/EOF, sin cap/root/proxy | **Recomendada Linux**, pocos componentes, preparada por operador |
| setcap sobre Java compartido / bajar umbral global de puertos / Java root | Concede privilegio o superficie a otros procesos | Rechazadas |
| URL:8443 sin443 | TLS confiable sería posible, pero no satisface443 ni simplifica verificación estándar App Links | Sólo diagnóstico, no propuesta final |

Propuesta DNAT (descripción, **no comandos ejecutados**): regla dedicada identifica
interfaz LAN, subred origen, IPv4 destino PC y TCP443 y cambia destino a esa mismaIPv4:8443.
Preferir destino explícito a REDIRECT genérico que puede elegir otra IP en interfaz con
múltiples direcciones. Necesita preparación privilegiada del operador, no del Java/Unity.
No abrir WAN, no proxy, no segunda terminación TLS; WSS futuro pasa por el mismo TCP.
[Netfilter/nftables oficial](https://netfilter.org/projects/nftables/manpage.html).

Tráfico desde la propia PC hacia su FQDN no atraviesa PREROUTING: requiere regla OUTPUT
igualmente acotada al destino443/local de demo, o diagnóstico separado a8443 (este último
no prueba URL final). Firewall debe permitir443 traducido, conservar IPC privado y limitar
8443 directo de LAN cuando proceda; interacción con UFW/firewalld/orden de reglas se revisa
antes de aplicar. No mezclar gestores sin conocer protección efectiva.

Reglas en tabla/cadena identificable, backup/rollback, contador y logging limitado; reversión
elimina exclusivamente reglas propias, nunca flush del firewall. Pueden prepararse una vez
para demo y verificarse antes de iniciar; tras cerrar Java no queda servidor8443, aunque la
regla de kernel permanezca hasta reversión explícita. Preparación/retirada y reinicioPC/IP
son pruebas operativas obligatorias. Conflicto443 con otro servicio bloquea setup; no redirigir
tráfico ajeno. No se verificó ninguna de estas reglas hoy; Windows requiere propuesta aparte,
no asumir nftables portable. [Capabilities systemd](https://github.com/systemd/systemd/blob/main/man/systemd.exec.xml).

## 8. Flutter y assetlinks fuera de LAN

Mismo FQDN/origenHTTPS443, Java, WSS y contratos; Flutter no necesita TLS diferente ni UDP
propio. PWA sigue disponible y App Links no es credencial/sesión. Un hostname estable
para el spike limita dominios Android a verificar; múltiples instalaciones/estaciones y
hostnames dinámicos requieren diseño posterior, no se garantiza auto-verificación wildcard.

**Publicar assetlinks.json en HTTPS443 público del mismo FQDN**, sin redirect/login, con
packageName/fingerprint públicos correctos. DNS público resuelve al hosting estático de
asociación, DNS LAN resuelve al Java de juego. Servir idéntico documento también en Java
cuando se implemente4C. Hosting estático público es metadata de instalación, **no segundo
backend de juego**, no relay de input/sesiones y no requiere estar disponible durante partida.
TLS del sitio público independiente del keystore LAN; no compartir claves innecesariamente.

Así Android puede verificar al instalar fuera de LAN con Internet. Instalación/verificación
primera vez totalmente offline no se garantiza. Android debe respetar elección de navegador;
asociación fallida continúa PWA y app puede abrirse manualmente. Verificación suele solicitar
el documento al hostname porHTTPS estándar, por eso443 es relevante.
[Android verificación oficial](https://developer.android.com/training/app-links/verify-applinks).

SplitDNS crea riesgo de cache hacia sitio público al entrar LAN. Sitio público no debe fingir
conexión a Java: página neutral «conéctate a la red del juego», sin backend competitivo; probar
transición exterior→LAN y asociación del mismo enlace. Si no resulta fiable sin configuración
browser, reportar y revisar solución antes de4C. Hoy no publicar assetlinks ni desplegar sitio.

## 9. Costes/dependencias y bloqueos

| Recurso | Coste/dependencia propuesto |
|---|---|
| Dominio controlado | Si existe y se autoriza, posible coste incremental0; compra implica tarifa/renovación del proveedor por aprobar |
| DNS/ACME | Proveedor con TXT/delegación; DNS-01 necesita Internet de preparación. Emisión Let's Encrypt gratuita ([fuente](https://letsencrypt.org/about/)); no se solicita certificado aquí |
| Router | Reusar existente conforme o presupuesto equipo dedicado; precio/modelo sin seleccionar, no inventar cotización |
| Config443 | Trabajo de operador con permisos del SO; sin proxy/licencia/framework adicional, no coste gratuito de mantenimiento supuesto |
| assetlinks público futuro | Hosting estático y certHTTPS; posible plan gratuito sujeto a condiciones, coste no confirmado; sólo4C |
| Demostración | PC Linux, Android Chrome, iPhone Safari y responsable/red física; tiempo de setup/renovación documentado aparte |

No necesidad de nube de juego, cuentas de jugadores ni base de datos remota. Infraestructura
pública de identidad/emisión y futura metadata es dependencia **de preparación**, no requisito
WAN durante gameplay. Ni DNS strict externo ni certificado vencido se resuelven mágicamente
offline. El dominio y acceso administrable a router siguen sin acreditarse: bloquean gate.

## 10. Evaluación y adaptación prevista de #94

| Clasificación | Componente / cambio |
|---|---|
| Reutilizable | Hosting React dentro JAR, assets/manifest/worker local, API no-store, diagnóstico public instance/request counter, UI puntual sin falsa sesión |
| Reutilizable | Config opt-in externa, confirmación IPv4/interfaz, exclusión wildcard/VPN/virtual, paths privados fueraGit, validación PKCS12/password/vigencia, ausencia de deps nuevas |
| Mantener íntegro | Unity supervision/child/EOF/singleton/deadlines, IPC127.0.0.1, framing/token/READY/codecs/corpus; no ampliar parser IPC |
| Modificar | Separar `bindAddress`IPv4 de `publicHostname`; SAN DNS exacto/cadena/sitio sin CA de teléfono; hostname/versionconfig estrictos |
| Sustituir | Setup mkcert/CA móvil por público DNS-01+routerDNS+reserva+DNAT; public origin443 separado de listenPort8443; Java no necesita bind privilegiado |
| Revisar | Health/readiness: httpPort conserva listener8443, no confundirlo con public443. Descriptor/QR futuros usarán publicOrigin validado, no IP o puertoIPC |
| Conservar evidencia |26/26 Java,5/5 React,11 gruposPlayer y salida0/residual0 anteriores mantienen validez de casos ejecutados; no acreditan nuevo trust/DNS/443 |
| Repetir/adaptar tests | SAN DNS correcto/incorrecto, hostname distinto de IP, cert expirado/cadena, config/puerto ocupado, HTTPS origen443, NAT host/LAN/rollback, assets/API/binding y EOF/singleton/Player afectados |
| Nuevos gates físicos | Trust stockChrome/Safari, DNS offline frío/firstload, Wi-Fi + cellular, DNS privado/VPN/DoH, cambio IP/reserva, aislamiento/rebinding, segundo equipo IPC |

Tests con CA efímera continúan para errores de TLS y unidad: no versión privada pública de
certificados de demo ni trust-all. No eliminar tests/infraestructura bien probados; actualizar
sólo expectativas de SAN/origen. No repetir benchmark600s por documentación; si adaptación
posterior afecta IPC realmente, seleccionar regresiones justificadas. #94 no está listo para
merge de nuevo gate aunque CI sea verde. Hoy no se adapta código.

## 11. Pruebas físicas obligatorias y aceptación

Registrar modelos/OS/browser, router/firmware/topología, FQDN/reserva/subred, certificado
público/SAN/vigencia y reglas propias443. Sin keys/tokens/passwords ni datos personales.

1. Android/Chrome e iPhone/Safari de referencia con trust normal, sin CA/perfil/DNS/browser
   settings del jugador: conectar Wi-Fi, URL manual443, cero warnings, secure context.
2. WAN retirada, cache del origen y DNS de laboratorio fríos: primera carga, assets/manifest/
   worker del JAR, health real de misma instancia Java. No usar sólo devtools offline.
3. Refresh/cierre-reapertura, Java detenido sin falso éxito, vuelta Wi-Fi y datos móviles activos;
   registrar qué ruta emplea OS y si requiere confirmar permanencia enWi-Fi sinInternet.
4. Matriz DNS automático/strict/DoH/VPN, sin «arreglar» el jugador; fallos identificados y
   soporte acotado aprobado. Prueba aislada sin ruta celular más stock con cellular activo.
5. DNS/reserva conserva FQDN; reinicio de router y PC y cambio IP controlado actualiza reglas; filtros
   rebinding generales conservados y aislamiento de clientes diagnosticado.
6.443 real desde PC y segundo equipo,8443controlado, IPC sólo127.0.0.1 e inaccesibleLAN; rollback
   no alteraotrosservicios. Unity inicia Java, EOF normal, exit0/cleanup y residual propio 0.
7. Futura4C: verificar instalación App Links fueraLAN, entradaLAN/cache/asociación fallida/PWA,
   mismo enlace/origen/contratos; no es gate que se implemente en3A.

Cualquier plataforma ausente NOT RUN; métricasURL→shell y shell→Java por intento, errores
íntegros, setup aparte. No latencia sensores/gameplay. PKI público/DNS/443 todavía NOT RUN.
No hay hardware/dominio suficiente confirmado para cerrarPASS físico en esta evaluación.

## 12. Decisiones PO y cierre

Aprobar antes de adaptar #94:

- Dominio existente/delegado o presupuesto compra; proveedorDNS y responsable de emisión/
  renovación; margenpropuesto de 14 días y preparación online permitida.
- Router existente conforme o dedicado configurable, modelo/firmware/red y responsable;
  no modificar teléfonos; límites explícitos DNS privado/VPN/DoH sin garantía universal.
- Linux de referencia y DNAT local443→8443, reglaOUTPUT, integración firewall/rollback y
  permisos de operador, sin Java/Unityroot ni capacidad en binarioJava compartido.
- Hostname estable y splitDNS para futura asociaciónpública443; hosting estático assetlinks
  sólo al llegar4C, sin servidorcloud de juego ni duplicaciónbackend.
- Android/iPhone de referencia y procedimiento offline/stockcellular; gate real antes dePASS.

**Estados conservados:** #94 DRAFT;3A IN PROGRESS; 3B/4A/4B/4C NOT STARTED;
Incremento 2 PASS/MERGED;Fase 0 IN PROGRESS;Fase 1 NOT STARTED.
Evaluación documental concluida, arquitectura objetivo aprobada conceptualmente y propuesta
operativa pendiente. Sin runtime, compras, certificados emitidos ni red alterada. Detenerse.
