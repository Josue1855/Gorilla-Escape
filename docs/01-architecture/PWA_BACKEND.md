# Backend de jugadores: implementación y límites

Última sincronización: 2026-10-16. Fuentes S9/S10 y evidencia fechada; [inventario](../00-product/SOURCE_INVENTORY.md). Trabajo documentado en ramas/PR, no integrado automáticamente.

Repositorio privado [Gorilla-Escape-Backend](https://github.com/Josue1855/Gorilla-Escape-Backend), [PR #14](https://github.com/Josue1855/Gorilla-Escape-Backend/pull/14) integrado en develop; evidencia funcional original 3635186 y estado actual en S12. El PO aprobó SQLite/JDBC para este incremento; [ADR acotado](https://github.com/Josue1855/Gorilla-Escape-Backend/blob/3635186/docs/adr/ADR-001-PLAYERS-APPROVAL.md). El PR #12 de evaluación amplia fue cerrado sin merge; no confundirlo con la aprobación acotada del PO ni con la implementación integrada del PR #14.

Implementado: Spring Boot4.1.1/Java21, JDBC parametrizado, SQLite JDBC3.53.2.1 y Flyway12.4.0; módulos players/api/application/domain/infrastructure, persistencia y HTTP. GET /api/players paginado, POST validado con201 tras COMMIT, Problem Details sanitizado, OpenAPI servido. No JPA, PostgreSQL, Docker, cuentas, partidas ni rankings.

SQLite vive fuera del JAR en directorio por usuario. Conexión exclusivamente local sin puerto SQL; pool de una conexión, journal DELETE, synchronous FULL, FK activas, lock de instancia y migración V1 validada. Pruebas de reinicio, corrupción preservada, schema futuro rechazado y concurrencia. [CI Linux/Windows: 17 tests PASS](https://github.com/Josue1855/Gorilla-Escape-Backend/actions/runs/38989509375); harness adicional Linux PASS.

La PWA del fork S10 ya consume el contrato mediante proxy local: [dossier SQL/HTTP/UI](https://github.com/Josue1855/Gorilla-Escape-Backend/tree/3635186/docs/laboratorio). No se incorpora todavía al supervisor ni instalador del videojuego. Backend HTTP loopback8090 por defecto, sin auth/TLS; LAN opt-in sólo para laboratorio aislado. La fachada HTTPS autorizada, respaldo/restore, runtime distribuido y supervisión final permanecen pendientes. No modificar supervisor sin auditoría.

Unity sigue autoridad de resultados. Mapeo RTC a UUID durable, publicación autorizada, idempotencia y entidades de competición siguen propuestos en [modelo](DATABASE_DESIGN.md) y [contratos](API_CONTRACTS.md). POST de jugadores no es idempotente: no reintentar automáticamente tras una respuesta perdida.
