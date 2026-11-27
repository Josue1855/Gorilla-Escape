# Preflight técnico y operativo de 3A

**Resultado: software disponible; preparación de infraestructura y gate físico pendientes.** Sólo lectura y documentación. No autoriza ejecutar el plan siguiente. Fecha: 2026-10-07 UTC / 2026-10-06 America/Chihuahua.

PR [#94](https://github.com/Josue1855/Gorilla-Escape/pull/94) verificado **OPEN / DRAFT**, head exacto `034ab44aa5b27c1812fc1b5f9f3cc8f7593debf1`, rama `feature/mobile-phase0-lan-https-3a`. Checks `management-validation` y `java-react-foundation` SUCCESS para ese head. Sin merge ni cambios de runtime en este preflight.

## 1. Fuentes y alcance

Revisados [v4](Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md), [reglas](DEVELOPMENT_RULES.md), [decisiones](DECISIONS.md), [progreso](DEVELOPMENT_PROGRESS.md), [viabilidad](PHASE0_PHONE_3A_FEASIBILITY.md), [validación](PHASE0_PHONE_3A_VALIDATION.md), [Linux443](PHASE0_PHONE_3A_LINUX_443.md), [rediseño](PHASE0_PHONE_LAN_ONBOARDING_REDESIGN.md) y configuración Java real. Sus registros históricos conservan estados de su momento; la precisión posterior del PO y el procedimiento vigente prevalecen sobre la CA móvil histórica o textos Proposed anteriores.

Mantener Unity autoritativa y supervisora; Java único backend local; PWA universal y Flutter Android opcional futuro. FQDN/TLS público, DNS administrado, 443→8443 e IPC loopback ya aprobados como arquitectura. Complejidad en operador/infraestructura, nunca instalación CA, DNS manual o excepción TLS del jugador. En 3A se abre URL manual; QR/sesiones corresponden a 3B, WSS a 4A y sensores a 4B.

## 2. Hallazgos comprobados en esta PC

Consultas sin sudo ni elevación: `/etc/os-release`, interfaces/rutas IPv4, NetworkManager, resolved, estados systemd, sockets TCP, UFW config legible, versión nft/iptables, módulos cargados, UID/capacidades y procesos Java. Sin SSID/MAC, credenciales ni inventario de servicios ajenos en este documento.

| Elemento | Observación comprobada | Límite |
|---|---|---|
| SO / kernel | Pop!_OS 24.04 LTS; kernel `7.1.5-76070105-generic` | Referencia Linux actual; no Windows validado |
| Wi-Fi | `wlp0s20f3`, conectada | No identifica modelo/propietario del router |
| IPv4 / subred | `10.1.125.17/22`; red `10.1.124.0/22` | Observación temporal, no reserva DHCP demostrada |
| Gateway | `10.1.124.1`, ruta default DHCP | No prueba acceso administrativo ni que sea el AP físico |
| Ethernet | `enp0s31f6`, unavailable | Existe interfaz; no enlace disponible comprobado |
| Gestor | NetworkManager y systemd-resolved activos | No modificar perfiles ni resolv.conf |
| DNS PC | Stub `127.0.0.53`; dos resolvers privados anunciados por Wi-Fi, fuera de `10.1.124.0/22` | Direcciones exactas omitidas por minimizar datos; no hay override FQDN confirmado |
| Hotspot | NetworkManager anuncia AP=yes | No demuestra AP estable, DHCP/DNS custom ni cliente+AP simultáneos |
| Firewall gestor | UFW enabled, active/exited; config `ENABLED=yes` | Estado del servicio/config, **no verificación del filtrado efectivo** |
| Otros gestores | firewalld y nftables.service inactive | Inactive no significa ausencia de reglas kernel |
| Herramientas / backend | nftables1.0.9; iptables1.8.10 `(nf_tables)`; módulo `nf_tables` cargado | No se inspeccionó ni probó NAT efectivo |
| Privilegios | UID normal1000; umbral puertos no privilegiados1024 | Bind443 necesita preparación de operador; no reducir umbral |
| Binario Java21 usado | `getcap` no reporta capacidades de archivo | No conceder capacidades; un Java futuro debe comprobar UID/CapEff propios |
| Puertos | `ss -ltnp`: ningún listener TCP443 o8443 en IPv4/IPv6 | Snapshot; no garantiza ausencia de redirección NAT o carrera posterior |
| Java ahora | Ningún proceso `java` observado | No se lanzó Java/Unity en este preflight; exposición activa no se puede medir |

**Inferencia limitada sobre DNS:** resolvers fuera de subred requieren ruta, pero pueden pertenecer a una red privada interna; no demuestra dependencia de Internet ni de WAN. Tampoco demuestra que sigan accesibles o resuelvan el FQDN con WAN retirada. El gateway no anuncia por sí solo un DNS local autoritativo probado. No se desconectó ninguna red ni se vació cache para investigar.

### Lecturas impedidas por permisos

- `nft list ruleset`: `Operation not permitted (you must be root)` y `netlink: Error: cache initialization failed: Operation not permitted`.
- `ufw status verbose`: `ERROR: You need to be root to run this script`.
- `/etc/ufw/user.rules`, `before.rules`, `after.rules`: no legibles por usuario actual.

Se detuvieron esas comprobaciones sin sudo, sin reintento privilegiado ni cambio de permisos. **Falta autorización para lectura administrativa** de ruleset nft/UFW, o que el operador obtenga un resumen sanitizado. Necesario identificar políticas INPUT/OUTPUT, NAT443 existente, prioridades, tablas propias/ajenas, compatibilidad de la excepción y rollback. No afirmar «firewall permite443», «puertos accesibles», «sin conflictos NAT» o «reglas preparadas compatibles» a partir de estos snapshots.

## 3. Qué ya tenemos y qué falta

[Software/evidencia del candidato](evidence/phone-lan-fqdn-3a-2026-10-06/java-results.json): Java30/30, React5/5, build/JAR, Unity→Java HTTPS DNS y regresión Player11 grupos PASS; cierre normal y propios residuales0. Son resultados anteriores conservados, **no suites reejecutadas en este preflight**. Certificados efímeros, SNI DNS y ruta de test a IP no prueban confianza pública/DNS router/443 ni teléfono.

Config real separa `address` IPv4 física, `hostname` SAN DNS exacto, `publicOrigin=https://<hostname>` (443) y `port` interno default8443. Config/PKCS12/password externos y privados; cadena/vigencia/PKIX, TLS1.2/1.3, fallo explícito y sin fallback HTTP móvil. Unity conserva READY (`httpPort` interno), hijo/EOF/singleton/cleanup y recuperación limitada. IPC permanece127.0.0.1; perfil de desarrollo sin config móvil conserva HTTP loopback, que no es alternativa móvil de producto.

| Requisito | Disponibilidad real |
|---|---|
| Código/artefactos y procedimientos de 3A | Disponibles, evidencia histórica PASS |
| FQDN real controlado / acceso TXT o delegación DNS-01 | No confirmado; ejemplo `.test` de automatización no sirve como dominio público |
| Certificado público leaf+chain+clave+PKCS12 vigente | No emitido para esta demo; no buscar/copiar secretos privados |
| Responsable y proceso de renovación previa online | Pendiente; leer notBefore/notAfter real, no asumir vigencia fija |
| Router con DNS A local persistente y reserva DHCP | Modelo/firmware/admin/capacidad no confirmados |
| Resolución offline sin ajustes del jugador | NOT RUN |
| Excepción443→8443 PREROUTING + OUTPUT y filtrado compatible | Procedimiento disponible, no aplicado; lectura administrativa bloqueada |
| Router sin exposición WAN/UPnP ni aislamiento incompatible | No comprobado; no acceso al router en esta tarea |
| Android/Chrome e iPhone/Safari físicos | Modelos/versiones/participación sin confirmar; NOT RUN |

DNS-01 permite validar un dominio sin exponer el servidor de juego públicamente; requiere control TXT/delegación y conectividad durante preparación. API con permisos acotados favorece renovación reproducible; credenciales nunca en Java/Git/logs. No emitir ni adquirir nada aquí. [Let's Encrypt DNS-01](https://letsencrypt.org/docs/challenge-types/).

## 4. Infraestructura existente y alternativas

| Alternativa | Coste / complejidad / estabilidad | Seguridad y portabilidad | Decisión condicionada |
|---|---|---|---|
| A Router actual administrable | Menor coste incremental y trabajo si ya tiene DNS local, reserva y control WAN | Depende firmware, permisos, aislamiento y red; no adivinar funciones. Config reproducible/exportable necesaria | **Primero evaluar y reutilizar** si cumple todos los requisitos |
| B Router dedicado configurable disponible | Requiere equipo/responsable; si ya existe, no presupone compra. Separa demo de servicios ajenos y reduce dependencia del escritorio | DHCP/DNS persistente, Wi-Fi protegido y administración offline; permite PC por Ethernet cuando disponible. Hardware por confirmar | Referencia preferida si A no cumple o no es administrable; justificar cualquier compra después |
| C Hotspot Linux administrado | AP=yes da candidato sin compra; más pruebas de driver, radio, suspensión, DHCP/DNS y firewall | Puede cortar Wi-Fi actual; no portable automáticamente a otra PC/SO. No activar ahora. IPv4/shared altera forwarding y asigna red | Fallback sólo tras autorización y prueba física; no atajo ya demostrado |

Los tres deberían dar al jugador la misma experiencia: elegir Wi-Fi y abrir enlace HTTPS, sin tocar DNS/CA. Ninguno garantiza cualquier VPN/DNS privado estricto. NetworkManager shared gestiona red y forwarding; su disponibilidad no demuestra funcionamiento offline personalizado en esta PC. [NetworkManager IPv4](https://networkmanager.dev/docs/api/latest/settings-ipv4.html).

**Recomendación:** conservar arquitectura router DHCP/DNS + FQDN público + certificado previo + DNAT local restringido. Antes de comprar, identificar/router actual y demostrar sus funciones. Hoy no hay evidencia suficiente para afirmar que la infraestructura existente basta, ni para afirmar que deba reemplazarse.

## 5. Seguridad y riesgos abiertos

| Requisito/riesgo | Evaluación / condición necesaria |
|---|---|
| TLS público | Software valida SAN/cadena/vigencia, pero admitir root efímero en pruebas no acredita issuer público. Operador verifica confianza stock Android/iOS y cadena servida real; no CA instalada ni excepción TLS |
| DNS / rebinding | Registro A exacto local; no AAAA falso. Conservar protección global; excepción específica sólo si se demuestra necesaria y se autoriza |
| DNS privado / DoH / VPN | Pueden sustituir resolución/rutas o bloquear LAN; registrar caso no soportado sin exigir cambios al jugador. No interceptar TLS. Inferencia técnica, no prueba ejecutada |
| Wi-Fi sin WAN / datos móviles | OS puede usar cellular o no mantener LAN; verificar stock y ruta real, no sólo página cacheada. iOS actual puede usar Connectivity Assist; versiones anteriores Wi-Fi Assist |
| Firewall / NAT | Reglas efectivas desconocidas. INPUT ve normalmente8443 tras DNAT; ACCEPT independiente no vence DROP ajeno. Auditar UFW/nft sin reemplazar gestor ni deshabilitar filtros |
| WAN / UPnP / forward público | No se configuraron; ausencia efectiva en router no inspeccionado sigue pendiente. No exponer backend público para DNS-01 |
| Privilegios | Java normal8443; operador prepara443 por separado. Sin root Java, setcap global, nuevo daemon/proxy ni cambio de umbral global |
| Secretos | No archivos de PKI encontrados en inventario visible del repositorio; configuración privada fueraGit. Esto no es auditoría exhaustiva de todo historial Git. Nunca publicar passwords/tokens/keys ni archivos operativos sin sanitizar |
| IP/expiry/reinicio | Reserva DHCP y FQDN estables; revisar DNS/config/reglas cuando IP cambia. Renovación previa y reloj correcto, no dependencia de ACME durante partida |
| Recuperación | 3A valida recarga manual/volver a Wi-Fi/relanzamiento explícito limitado de Unity; no auto-reconnect infinito ni nuevo lifecycle. Java apagado no debe producir health cacheado exitoso |
| Cloud durante gameplay | Assets/API Java locales; preparación DNS-01 online y futura metadata App Links no son transporte de juego. Offline real sigue pendiente |

Referencias: [Google DNS privado/VPN](https://developers.google.com/speed/public-dns/docs/using), [Apple Connectivity Assist](https://support.apple.com/en-us/127686), [Netfilter NAT](https://netfilter.org/projects/nftables/manpage.html). No modificar teléfonos para convertir un fallo de compatibilidad en PASS.

## 6. Plan mínimo posterior, NO EJECUTADO

1. **Completar inventario:** operador confirma red propia/permiso del titular, router/modelo/firmware/admin, dispositivos/versiones y ventana de demo. Obtener lectura administrativa sanitizada UFW/nft y verificar nuevamente conflictos443/8443 antes de cualquier cambio.
2. **Elegir red:** validar A; si falla, evaluar B disponible; C sólo con ensayo autorizado. Requisitos: DHCP con reserva PC, DNS local exacto FQDN accesible sin upstream, LAN teléfono→PC, sin captive portal/config DNS cliente, Wi-Fi protegido, gestión local y WAN desconectable. No desconectar red compartida de terceros para probar.
3. **Identidad pública:** confirmar FQDN controlado y permiso TXT/delegación DNS-01, proveedor/issuer/responsable. Tras autorización separada, emitir/convertir/validar pública chain y preparar renovación antes de demo. Sin secretos en argumentos/Git, archivos600/directorio700. Ventana de vigencia cubre evento; margen14d sigue propuesta operativa, no criterio cambiado unilateralmente.
4. **Configuración de operador:** IPv4 reservada/interfaz/subred reales, hostname y origen canónico443, listen8443, paths externos. Datos actuales son snapshot, no valores definitivos de la demo. Revisar SAN/issuer/cadena/reloj/confianza, sin arrancar Java root.
5. **Redirección:** con autorización explícita, aplicar procedimiento [Linux443](PHASE0_PHONE_3A_LINUX_443.md) adaptado a UFW efectivo: interfaz/origen/subred/destino443→mismaIPv4:8443, OUTPUT PC acotado y excepción de filtrado mínima si hace falta. Precheck/snapshot/tabla propia/handles y rollback antes de aplicar. No persistencia ni WAN/UPnP sin autorización; `nft --check` privilegiado también quedó pendiente.
6. **Arranque:** Unity inicia el único Java normal, READY y PONG, listeners exactamenteLAN8443 + loopbackIPC; sin HTTP paralelo. PC al FQDN443 con confianza sistema normal verifica DNS/OUTPUT; teléfono verifica PREROUTING. Sin `-k`, `--resolve`, CA de test ni DevTools-offline para aceptación real.
7. **Validación:** matriz siguiente en ambas plataformas; evidencias sanitizadas, resultados individuales y fallos. API instancia/contador demuestra acceso al Java real, no CDN ni sólo cache. Separar primera carga, refresh/reapertura y setup; no métricas gameplay.
8. **Cierre y rollback:** Unity EOF → Java exit0 → cleanup/residuales0. Retirar sólo tabla/reglas de esta demo y restaurar cambios de router/perfil autorizados con inventario/backup propios; comparar antes/después sin sobrescribir cambios ajenos. Registrar resultado antes de proponer cierre3A.

### Rollback preparado

Hoy no hay cambios que revertir. Si se autoriza la ejecución: anotar tabla/handles/reglas propias, backup router y valores DNS/reserva antes; eliminar sólo tabla propia y excepción por gestor original, nunca `flush ruleset` o restauración global ciega. No borrar certificados privados necesarios por iniciativa propia; su retirada segura compete al operador. Prueba de apagado mantiene regla NAT hasta rollback explícito: ausencia de Java/listener no elimina reglas. Restaurar WAN de laboratorio y perfiles sólo si esta intervención los cambió, sin tocar red/servicios ajenos.

## 7. Matriz física reproducible

Todas las celdas físicas siguientes **NOT RUN**. Registrar Android modelo/OS/Chrome e iPhone modelo/iOS/Safari, firmware/router, topología, FQDN/IP/reserva, public leaf/issuer/vigencia/fingerprint, artefactos y reglas sanitizadas. No simuladores como sustituto.

| Caso | Android + Chrome | iPhone + Safari | Qué puede automatizarse / qué exige físico |
|---|---|---|---|
| TLS y URL pública | HTTPS FQDN443 sin warnings ni CA/DNS manual | Igual, sin perfiles/trust manual | Código/cadena/SNI test ya PASS; confianza pública/stock físico obligatorio |
| Primera carga sin cache | Navegador/origen de laboratorio limpio; shell/assets/SW/health | Igual | JAR/localidad comprobables; DNS frío y primera carga LAN física obligatorios; no pedir limpiar browser al jugador |
| WAN ausente | Wi-Fi viva, WAN retirada con permiso; health de instancia Java | Igual | Corte real/router/rutas físicos; no DevTools offline ni cache como prueba |
| Datos móviles activos | Repetir stock; registrar selección de ruta y llegada a Java | Repetir, registrar Connectivity/Wi-Fi Assist según versión | Ensayo físico separado de laboratorio sin salida celular; no exigir apagar datos como paso universal |
| Health/refresh/reapertura | ≥10 health y5 reaperturas; contador crece, API no-store | Igual | Servidor testable; UX/browser/SW físico |
| DNS privado | Automático y, si ya hay equipo de laboratorio preparado, estricto; no cambiar jugador | Registrar DNS estándar y cualquier resolver/perfil existente | Resolución/ruta físico; no forzar ajustes. Perfil especial de ensayo requiere consentimiento |
| VPN / DoH | Registrar configuración existente, alcance LAN/fallo | Igual | Físico; no instalar VPN ni interceptar HTTPS para completar matriz |
| Wi-Fi perdida / vuelta | Volver a red y comprobar manualmente health sin falsa sesión | Igual | Acciones físicas; no auto-reconnect/protocolo de sesión aún |
| Sensores | Sólo documentar futuro requisito de permiso; no solicitar ni implementar | Igual, gesto/permiso futuro por validar en4B | **DEFERRED 4B**, no gate de lectura sensores3A; secure context sí gate3A |
| Exposición | TCP IPC rechazado desde segundo dispositivo; HTTPS443 accesible sólo segmento autorizado | Repetir/plataforma de segundo equipo documentada | Listeners PC automatizables; alcance remoto y filtrado físico obligatorios |
| Cierre Unity/Java | Health falla≤5s; EOF/cierre/cleanup propios0 | Igual | EOF/exit local automatizable; error visible real desde teléfono obligatorio |
| Reinicio/reserva/rollback | Misma URL tras preparación autorizada; sin reglas/servicios residuales ajenos | Igual | Router/PC reales; no cambios de IP ni reboot ejecutados ahora |

DNS estricto/VPN arbitraria pueden impedir LAN sin posibilidad de reparación por infraestructura; soporte de dispositivos/configuraciones de referencia debe quedar explícito. PWA universal significa cliente sin app obligatoria, no garantía contra toda política de red externa. No excluir fallos ni marcar N/A como PASS; casos diferidos y configuraciones ausentes se identifican separadamente.

## 8. Información y autorizaciones que necesita el operador

**Información, sin secretos:** FQDN disponible/titular y proveedorDNS, acceso TXT/delegación confirmado; router modelo/firmware y permiso administrativo del dueño; capacidad DNS local/reserva DHCP/control WAN y red de laboratorio autorizada; Android/iPhone/versiones/navegadores y participantes. Responsable de certificado/renovación y ventana de vigencia/demo. No enviar passwords/router key/API tokens/PKCS12/private key al chat.

**Autorizaciones separadas, no solicitadas ni ejercidas por este preflight:**

- Lectura administrativa UFW/nft (o resumen sanitizado producido por operador); actualmente impedida por permisos.
- Preparación del router/DHCP/DNS y ventana de corte WAN en red controlada.
- Emisión pública DNS-01 y manejo local privado del certificado; compra sólo si se prueba ausencia de alternativa y se aprueba presupuesto.
- Reglas temporales concretas443/OUTPUT/filtrado y rollback revisados contra UFW real; ninguna persistencia general autorizada.
- Ensayo físico y condiciones de laboratorio para distinguir WAN de datos móviles; perfiles especiales DNS/VPN sólo en dispositivos de prueba consentidos, nunca configuración exigida al jugador.

## 9. Cierre de 3A y estado

Aceptar3A únicamente con Android/Chrome **e** iPhone/Safari físicos identificados: certificado público confiable, FQDN443 correcto, cero CA/perfiles/DNS manual, primera carga y health real sin WAN, refresh/reapertura, ausencia de falsa respuesta cacheada, IPC inaccesible desde LAN, exposición acotada, Unity/Java exit0 y propios residuales0. DNS offline y rollback reproducibles deben estar demostrados. No asumir compatibilidad universal ni declarar DONE por suites verdes.

**Conclusión:** PC/software listos para preparación condicionada; infraestructura existente todavía no acreditada. Evaluar router actual antes de comprar; router administrado conforme ofrece menor complejidad al jugador. Bloqueos reales: dominio/certificado, funciones/permisos router, lectura/reglas efectivas de firewall y teléfonos físicos. El plan está preparado, no ejecutado.

#94 DRAFT;3A IN PROGRESS;3B/4A/4B/4C NOT STARTED; Incremento2 PASS/MERGED; Fase0 IN PROGRESS; Fase1 NOT STARTED. Preflight concluido; detenerse sin infraestructura ni siguiente bloque.
