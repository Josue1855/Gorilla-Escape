# Cloudflared: arranque del alpha y prueba externa

Fecha: 2026-10-27. La implementación del supervisor y el empaquetador ya estaban en develop. El paquete local generado `Unity/Builds/FoundationLinux/Tools/cloudflared` también existía: la revisión inicial de la raíz del repositorio no bastaba para concluir que faltaba el ejecutable.

Se comprobó su SHA-256: `d33ff2d14475178d2012c2c56beba87389ac5ded27649519f198a7d3134a99db`, igual al pin del supervisor. El proceso Java de la sesión existente tenía `GORILLA_PACKAGE_ROOT` apuntando al paquete y modo AUTO. No se reinició ni interrumpió esa sesión.

Se corrigió una diferencia real: el launcher de shell fijaba el repositorio como paquete mientras el menú del Editor prefería la distribución preparada con Vision/firewall. Ambos reutilizan ahora esa distribución y seleccionan explícitamente el Cloudflared en ubicaciones conocidas si no se proporcionó una ruta. Se conservan las prioridades de configuración, AUTO/CABIN, verificación Java, presupuesto, limpieza y transporte LAN. No se modificaron los supervisores, no se inicia el MotionRelay antiguo y no hay descarga durante Play.

Validación local: cinco pruebas de selección/configuración del launcher PASS, sintaxis shell PASS y comprobación de tipos del Editor contra Unity 6000.6 PASS. CI Foundation incorpora las cinco pruebas. Son fixtures de configuración, no ejecuciones de Unity.

## Prueba externa real

Se ejecutó el test existente `RealQuickTunnelTest`, con opt-in explícito, contra el binario revisado. Levantó su propio PublicIngress de laboratorio, separado de la partida, y cerró sus procesos al terminar. [Reporte real](cloudflared-live-report.json):

- Integridad del binario: PASS.
- Registro de conexión Cloudflare: observado (`registered: true`).
- HTTPS público: no confirmado, `lastHttpsStatus: 0`, `HttpConnectTimeoutException`.
- Resultado: BLOCKED / `TUNNEL_HTTPS_UNAVAILABLE`, no PASS.
- Limpieza: completa; el laboratorio no deja un túnel huérfano.

La prueba no demuestra que el servicio público sea inaccesible en todas las redes ni diagnostica la causa de ese timeout. No se debilitaron TLS, la comprobación HTTP 200, el pin ni los límites para convertir el fallo externo en éxito. Tampoco constituye una prueba con teléfono. La sesión normal intentará el túnel durante el siguiente arranque; si la comprobación pública falla seguirá disponible LAN por AUTO.

Reproducción (Linux; el reporte debe usar una ruta de salida nueva):

```sh
GORILLA_REAL_TUNNEL="$PWD/Unity/Builds/FoundationLinux/Tools/cloudflared" GORILLA_REAL_TUNNEL_REPORT=/ruta/nueva/reporte.json Server/mvnw -B -f Server/pom.xml -Dtest=RealQuickTunnelTest test
python3 -m unittest discover -s tools/integration -p 'test_jungle_launcher.py' -v
sh -n tools/integration/run-jungle-editor.sh
```
