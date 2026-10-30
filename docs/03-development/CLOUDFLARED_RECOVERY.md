# Cloudflared: recuperación y tiempos del juego

Fecha: 2026-10-27. Rama `feature/saul-jungle-party-alpha`, PR [#131](https://github.com/Josue1855/Gorilla-Escape/pull/131), sin merge automático.

Por solicitud del usuario se incorporan selectivamente los ajustes de conectividad del commit `44890bedc738d143a079422929502ae3e40448dd` de `feature/gameplay-desafio-del-tronco`. No se fusiona aquella rama completa ni su descubrimiento LAN, menú o transporte antiguo.

| Ajuste | Valor vigente |
| --- | --- |
| Presupuesto de arranque completo | 30 000 ms por defecto; `GORILLA_TUNNEL_STARTUP_MS` admite de 100 a 60 000 ms |
| Conexión/solicitud de comprobación HTTPS | Hasta 3 000 ms por intento, acotado por el presupuesto restante |
| Pausa entre comprobaciones de arranque fallidas | Hasta 500 ms, acotada por el presupuesto restante |
| Vigilancia después del arranque | Comprobación tras 3 s y separación de 3 s después de cada comprobación; declara fallo tras 4 fallos HTTPS consecutivos |
| Recuperación AUTO tras fallo o pérdida | Próximo intento a partir de 20 s, al consultar estado, si no hay jugadores y terminó la limpieza |
| Registro de conexiones | Cuenta conexiones por `connIndex`; duplicados no suman, pérdida de una conexión conserva las restantes |
| Diagnóstico | Intentos, último código HTTP, conexiones y cadena de tipos de excepción; advertencia estructurada sin mensajes/URLs/payloads de excepción |

Un HTTP satisfactorio reinicia el contador de fallos consecutivos. La pérdida de todas las conexiones registradas o la muerte del proceso invalida el túnel; la tolerancia de cuatro fallos se aplica a las comprobaciones HTTPS. No se confunde un registro Cloudflare con disponibilidad pública: sigue exigiéndose HTTP 200, proceso vivo, registro y origen temporal válido.

## Supervisión y alcance

La auditoría previa identificó los cambios en `CloudflaredSupervisor`, `ConnectionBootstrap` y `ConnectivityService`, sus tests, los contratos de QR y el cierre de procesos. La adaptación mantiene la arquitectura existente: Java es dueño del hijo Cloudflared; el destino sigue siendo exclusivamente `PublicIngress` en loopback con las rutas permitidas. Unity y Shared no cambian. Los controles continúan por WebRTC LAN, sin pasar por el túnel.

El listener restringido se conserva durante AUTO para que el siguiente intento tenga un destino válido. En CABIN explícito se cierra. CABIN no ejecuta Cloudflared y CLOUDFLARED forzado no pasa silenciosamente a LAN ni reintenta automáticamente. Sin listener disponible no hay un intento alternativo contra otro puerto.

Los reintentos se ejecutan en el trabajador existente, de uno en uno; una limpieza pendiente o incompleta impide lanzar otro proceso. Mientras hay jugadores se difieren. No expulsan jugadores, cambian épocas, reinician partidas ni reemplazan el QR ya emitido. Un NUEVO QR explícito selecciona el origen disponible. Al cerrar se cancela la recuperación pendiente y se limpian los hijos propios. La política DNS de la JVM, el pin SHA-256, TLS y la selección automática de protocolo se conservan.

El intento inicial puede ocupar 30 s, o hasta 60 s si se configura: es el coste de esperar más por HTTPS público. Una red que impida el acceso público puede seguir fallando pese al aumento de tiempo; el funcionamiento LAN no depende de Internet.

## Pruebas

`ConnectivityRecoveryTest` cubre con reloj monotónico controlado el límite de 20 s (incluyendo origen negativo del reloj), jugadores activos, limpieza incompleta, modos explícitos, cierre, recuperación tras pérdida y conservación del listener/QR. Incluye fixtures de proceso para desconexión parcial, registro duplicado, cuatro fallos consecutivos con rearme al recibir éxito y diagnóstico sin contenido sensible. No espera 20 s reales ni exige Internet para probar la política.

```sh
Server/mvnw -B -f Server/pom.xml -Dtest=ConnectivityTest,ConnectivityRecoveryTest,DnsRetryPolicyTest test
Server/mvnw -B -f Server/pom.xml verify
```

El test externo existente `RealQuickTunnelTest` utiliza ahora el presupuesto normal de 30 s. Su opt-in con `GORILLA_REAL_TUNNEL` sigue separado de CI determinista y de la aceptación física del teléfono. Los registros anteriores de timeout a 15 s son históricos; no se reinterpretan como un resultado de este incremento.

[Resultados ejecutados y ensayo HTTPS real](../evidence/cloudflared-recovery-2026-10-27/README.md): batería general aprobada y 35 pruebas focalizadas/externas sin fallos; HTTPS público 200 observado.
