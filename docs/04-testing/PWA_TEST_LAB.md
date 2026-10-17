# Laboratorio PWA, REST y SQLite

Última sincronización: 2026-10-16. Fuentes S9/S10 y evidencia fechada; [inventario](../00-product/SOURCE_INVENTORY.md). Trabajo documentado en ramas/PR, no integrado automáticamente.

El flujo React → Spring Boot → SQLite **está implementado y probado en el fork de laboratorio**, no en el upstream ni en el paquete final del videojuego. [Dossier con procedimientos y evidencia](https://github.com/Josue1855/Gorilla-Escape-Backend/tree/3635186/docs/laboratorio). Fuentes S9/S10. La rúbrica entregada por el PO pide repositorio/documentación y evidencias de SQL, GET/POST, UI, Network/Application/Console y offline.

| ID / entregable | Estado observado | Evidencia / faltante |
|---|---|---|
| LAB-01 Repositorio final | Documentación y código en ramas para revisión | Backend PR #14 y fork PWA PR #1; upstream/main sin cambios. Backend privado: acceso del docente necesario |
| LAB-02 Scripts | EJECUTADO para jugadores | scripts/sql/seed.sql, query.sql y cleanup.sql; migración V1 real |
| LAB-03 BD antes/después | EJECUTADO | SQL2 → POST201/SQL3 → envío manual posterior/SQL4; UUID coincidente e integridad ok |
| LAB-04 Logs GET/POST/JVM | EJECUTADO | backend-application.log: requestId/status/durationMs/heap y affectedRows. Heap no es RSS; HTTP no es código SQL |
| LAB-05 GET renderizado | EJECUTADO en Chrome | Capturas02/03/07/11 y registros Network; primera página máximo100 |
| LAB-06 Network | PARCIAL | Eventos reales CDP con headers/timings/bytes; falta captura literal de pestaña |
| LAB-07 Application | PARCIAL | SW activated, caché compilada e IndexedDB verificados; recarga offline real emulada. Falta captura literal de pestaña |
| LAB-08 Console | PARCIAL | console.json auténtico, errores offline esperados y recuperación; falta captura literal de pestaña |
| LAB-09 POST único/idempotencia futura | Registro validado EJECUTADO; replay idempotente NO IMPLEMENTADO | Dos envíos explícitos produjeron dos UUID/filas. Intento incierto bloqueado; sin retry automático. Idempotencia concurrente de resultados es criterio futuro, no requisito de SQLite ya satisfecho |
| LAB-10 Ambiente/fallos | EJECUTADO con límites | Chrome escritorio, build/JDK/SQLite/tests identificados, datos ficticios, API inaccesible emulada, validación400 sin escritura; teléfono y LAN/WAN físicos pendientes |

[CI backend Linux/Windows](https://github.com/Josue1855/Gorilla-Escape-Backend/actions/runs/38989509375): 17 tests por plataforma, build y harness Linux PASS. [CI PWA](https://github.com/Josue1855/Dise-o_pwa/actions/runs/38989521358): siete tests, build y npm audit PASS. Son resultados de los commits citados en S9/S10; esta sincronización documental no los transforma en nuevas pruebas físicas.

Escenario A pendiente: cortar sólo WAN conservando LAN/backend local, probar servicios y WebRTC de forma independiente. El caso ya ejecutado desactiva red del navegador con emulación DevTools y verifica shell/consulta/borrador locales; no equivale al corte físico de WAN.

Escenario B pendiente físico: LAN activa con backend detenido. El caso ya ejecutado bloquea selectivamente la ruta API con red disponible y verifica que no haya falso éxito ni reenvío al recargar. No existe cola con Background Sync: reconexión avisa, consulta y envío requieren acción explícita. POST /api/players no es idempotente.

Para completar la parte visual de la rúbrica faltan tres capturas literales de DevTools Network/Application/Console; seguir instrucciones del dossier. Los JSON de CDP son registros auténticos, no imágenes de esas pestañas. No se eludió el bloqueo de páginas internas del navegador ni se fabricaron capturas. Mantener logs/evidencias fechados y sanitizados; no exponer cookies, autorizaciones o BD reales. No se envió la tarea a la plataforma universitaria.
