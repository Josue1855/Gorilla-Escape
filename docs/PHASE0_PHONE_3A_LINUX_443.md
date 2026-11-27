# 3A — Procedimiento Linux HTTPS 443 → Java 8443

**Preparado, NO EJECUTADO.** La aprobación autoriza software y este procedimiento, no cambios de router, firewall permanente, compras ni emisión pública. Cualquier aplicación temporal de reglas requiere autorización operativa posterior. No es una receta automática ni prueba de compatibilidad Windows.

## Inventario y precondiciones

Operador registra distribución, interfaz física LAN, IPv4 reservada, subred real, router/DHCP/DNS, FQDN controlado, configuración externa y responsable del rollback. Ejemplo documental: `wlan0`, `192.168.1.10`, `192.168.1.0/24`; **reemplazar y revisar**, nunca asumir estos valores. Router resuelve el FQDN a esa IPv4 sin WAN; no AAAA ficticio, aislamiento que impida teléfono→PC ni port forwarding WAN/UPnP.

Inspección previa (sólo lectura):

```bash
ip -4 address show
ip -4 route show
ss -ltnp
systemctl is-active ufw firewalld nftables
sudo nft -a list ruleset
sudo ufw status verbose
```

Ejecutar sólo las consultas aplicables al firewall instalado. Guardar inventario/reglas en archivo privado externo; sanitizar al publicar. Buscar listeners existentes 443/8443, reglas NAT/output previas, administradores concurrentes y prioridad de hooks. Un servicio en 443 podría quedar oculto por DNAT: detener la preparación y resolver el conflicto, no interceptarlo. Comprobar que no existe ya la tabla propuesta; nunca reemplazar una tabla ajena. No activar un segundo gestor de firewall encima del existente.

Preparar certificado público/clave/fullchain/PKCS12 vigentes fuera de Git, con configuración y password privados. Java usa UID normal, sin root, `setcap` ni cambios de `ip_unprivileged_port_start`. Unity conserva el hijo/EOF. Sin certificado público todavía se permite exclusivamente PKI efímera de test, que no acredita confianza de teléfono.

## Reglas propuestas para revisión

Archivo externo `gorilla-3a.nft`, contenido **a adaptar**, todavía no aplicado:

```nft
 table ip gorilla_3a {
   chain ingress_https {
     type nat hook prerouting priority dstnat; policy accept;
     iifname "wlan0" ip saddr 192.168.1.0/24 ip daddr 192.168.1.10 tcp dport 443 counter dnat to 192.168.1.10:8443 comment "Gorilla-3A incoming HTTPS"
   }
   chain host_https {
     type nat hook output priority dstnat; policy accept;
     ip saddr 192.168.1.10 ip daddr 192.168.1.10 tcp dport 443 counter dnat to 192.168.1.10:8443 comment "Gorilla-3A PC HTTPS"
   }
 }
```

DNAT conserva la IPv4 seleccionada; no `redirect` que pueda elegir otra dirección primaria. La regla OUTPUT es necesaria porque una conexión originada en la PC no atraviesa PREROUTING. No toca loopback IPC, otras interfaces, destinos, puertos, IPv6 ni tráfico WAN. No habilita forwarding global.

**Interacción con filtrado:** INPUT recibe normalmente el destino ya traducido 8443. Una regla que permite sólo 443 puede no bastar. Revisar el gestor activo, prioridades y política real. Si necesita permiso, proponer en su cadena INPUT existente una excepción limitada a interfaz/subred/IP/8443, conexión DNAT y destino original 443; preservar las demás reglas. Una cadena independiente con ACCEPT no garantiza permiso frente a DROP en otra cadena. No deshabilitar UFW/firewalld, no abrir 8443 globalmente ni añadir excepciones genéricas. Si no puede integrarse con claridad, detener y presentar la regla concreta al operador.

Tras aprobación operativa, validar sintaxis con `sudo nft --check --file <archivo externo>`, guardar snapshot privado y aplicar una sola transacción mediante `sudo nft --file <archivo externo>`. Esto **no se ha ejecutado**. Comprobar antes que tabla no existe; no hacer `flush ruleset`. No instalar unidad de arranque ni persistencia todavía.

## Verificación necesaria después de aplicación autorizada

1. Lanzar Unity con `GORILLA_MOBILE_CONFIG` externo. READY conserva `httpPort=8443`, IPC `127.0.0.1:<efímero>`. Listeners del PID hijo: exclusivamente IPv4 LAN:8443 y loopback IPC. No se espera listener Java 443: el kernel traduce ese puerto. Revisar UID del PID y cero capacidades globales del ejecutable Java.
2. PC: `curl --noproxy '*' https://<FQDN>/api/health` con confianza de sistema normal, sin `-k`, sin CA de test ni `--resolve` para el gate real. Root/assets/manifest/SW deben funcionar. Esto prueba OUTPUT y DNS operativo. Acceder directamente a 8443 no sustituye esta prueba.
3. Android/iPhone físicos, stock: misma URL sin puerto, confianza pública sin instalación de CA ni DNS manual, health real. Registrar contadores NAT y solicitud Java, no passwords/tokens. La prueba desde PC no acredita PREROUTING.
4. WAN desconectada, LAN viva: primera carga del origen sin cache, health, refresh/reapertura; registrar ruta real con datos móviles activos y prueba separada de aislamiento de WAN. DNS privado/VPN se registran como compatibilidad observada, sin exigir cambiar su configuración al jugador.
5. Segundo dispositivo: TCP IPC rechazado; interfaz externa/otra subred/otro destino no debe recibir esta redirección. Verificar que firewall efectivo impide accesos fuera de alcance y que no surgió HTTP paralelo.
6. Cerrar Unity: EOF, Java exit0, cleanup y cero hijo residual. Regla NAT no es un servicio y permanece hasta rollback; Java cerrado debe hacer fallar health, no mostrar éxito cacheado.

## Rollback y diagnóstico

Después de aplicación autorizada: `sudo nft delete table ip gorilla_3a` elimina **sólo la tabla propia**. Eliminar también exclusivamente la excepción de filtrado añadida mediante el gestor original, usando identificador/handle anotado. No restaurar ciegamente un snapshot ni borrar cambios concurrentes. Comparar inventario previo/posterior, contadores y listeners; verificar que 443 dejó de traducirse. Snapshot es respaldo para revisión, no autorización de reescribir toda la red.

Cambio de IPv4/interfaz/subred exige revisión coordinada de reserva, DNS, configuración Java y reglas antes del relanzamiento. FQDN estable no exige renovar leaf por un mero cambio de IP. Reboot: estas reglas no son persistentes; registrar ausencia/reaplicación autorizada, nunca declarar preparación reproducible sin probarlo. Puerto ocupado, conflicto NAT, DNS incorrecto, TLS inválido y firewall bloqueado se diagnostican por separado. Logs/counters limitados; no captura de secretos ni dump del entorno.

## Estado

Procedimiento preparado; infraestructura y reglas **NOT RUN**. DNS/router/HTTPS443 público/Android/iPhone físicos pendientes. #94 DRAFT; 3A IN PROGRESS; 3B/4A/4B/4C NOT STARTED; Fase0 IN PROGRESS; Fase1 NOT STARTED.
