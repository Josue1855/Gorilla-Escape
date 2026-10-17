# Secuencia de entregas

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

| Orden | Resultado esperado | Dependencia / gate |
|---|---|---|
| P0 | Revisar los incrementos backend/PWA ya implementados y acordar propietarios de Projects | Stack SQLite/JDBC aprobado; PR backend #14 y PWA fork #1 abiertos |
| P0 | Completar T019 y después T020 sin detener el desarrollo paralelo autorizado | QA física, hardware y evidencia auténtica |
| P0 | Acordar interfaz entre UI/UX y cliente técnico existente | Unity/Red + UI/UX; no duplicar lifecycle |
| P0 | Vertical slice High Striker y controles por disciplina | Framework mínimo, calibración, reglas y tests de scoring |
| P1 | GET/POST de jugadores ya verificados; integrar tras revisión y ampliar dominio por incrementos | S9/S10: SQLite/React reales; sesiones/resultados todavía propuestos |
| P1 | Completar Bowling, Throw y Slice | Autoridad Unity, criterios por minijuego, pruebas físicas |
| P1 | Laboratorio WAN sin Internet y backend inaccesible | Test matrix y persistencia disponible |
| P2 | Sincronización durable y retención aprobadas | Idempotencia/seguridad y almacenamiento local probado |
| P2 | Aceptación de torneo y publicación | Cuatro juegos, integración, evidencia académica y gates físicos |

Esta secuencia es PROPUESTO salvo alcance y autorización de inicio S3/DEC-031. No asigna duraciones, fechas, responsables personales ni estados remotos. Los milestones históricos de Unity conservan sus nombres; no se reescribieron para aparentar cuatro juegos implementados.
