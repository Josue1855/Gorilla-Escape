# Recuperación de Cloudflared — validación

Fecha: 2026-10-27. Rama `feature/saul-jungle-party-alpha`. [Cambios y parámetros](../../03-development/CLOUDFLARED_RECOVERY.md).

| Comprobación ejecutada | Resultado |
| --- | --- |
| Maven verify completo | PASS: 125 casos, 121 ejecutados, 4 omitidos por requisitos de entorno, 0 fallos/errores |
| ConnectivityTest + ConnectivityRecoveryTest + DnsRetryPolicyTest + RealQuickTunnelTest | PASS: 35 ejecutados, sin omisiones, fallos ni errores |
| Pruebas nuevas de recuperación | 13: temporización, jugadores, limpieza, cierre, modos, QR/listener, conexiones parciales/duplicadas, cuatro fallos y diagnóstico |
| Foundation/metadatos y Management | PASS local |
| Ensayo externo HTTPS real | PASS: arranque 7577 ms y HTTP 200 de la PWA |
| Rutas privadas expuestas por el túnel | Las nueve verificadas devolvieron 404; admisión inválida devolvió 400 |
| Limpieza del ensayo externo | Completa, sin procesos cloudflared residuales |
| Juego real Unity + teléfono | NOT RUN en este incremento |

[Reporte real sin hostname ni credenciales](public-30s.json). [Resumen y hashes de fuentes](validation.json). Los logs completos de Maven se conservan en el respaldo local `cloudflared-recovery-20261027` fuera de Git. La respuesta pública 200 se midió en esta ejecución: no se atribuye automáticamente a un único parámetro ni garantiza disponibilidad en todas las redes. Los timeouts del ensayo anterior a 15 s permanecen como evidencia histórica.

Reproducción: los comandos focalizado/completo y las condiciones del ensayo están en el documento de parámetros. Para incluir la prueba externa se requiere opt-in y un reporte de salida nuevo:

```sh
GORILLA_REAL_TUNNEL="$PWD/Unity/Builds/FoundationLinux/Tools/cloudflared" GORILLA_REAL_TUNNEL_REPORT=/ruta/nueva/public-30s.json Server/mvnw -B -f Server/pom.xml -Dtest=ConnectivityTest,ConnectivityRecoveryTest,DnsRetryPolicyTest,RealQuickTunnelTest test
```

El test levanta su propio listener restringido y túnel temporal, y los cierra al terminar. No reinicia la sesión de juego existente. El siguiente arranque normal usa los parámetros nuevos; esta prueba no deja un túnel permanente.
