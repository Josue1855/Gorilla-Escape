# Riesgos y mitigación propuesta

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

| Prioridad | Riesgo comprobado o pendiente | Impacto / mitigación |
|---|---|---|
| P0 | Backend no localizado | No validar REST/BD; obtener repo y baseline antes de fijar stack |
| P0 | Dos PWA con diferente stack y sólo una conectada al runtime | Sustituir cliente técnico por demo rompería input; aprobar adaptador/fuente de entrega |
| P0 | QA física final y PC limpia sin cerrar | No prometer torneo/cero configuración universal; ejecutar T019/T020 |
| P0 | Permiso S4 sólo lectura y owner Project indefinido | No se pueden publicar issues propias; mantenedor/owner ejecuta plan |
| P0 | S4 dist sin SW/iconos | Offline/instalación no demostrados; reparar en tarea UI propia y probar dist |
| P1 | Dos autoridades de puntuación | Unity oficial, backend valida y UI consulta; autorizar publicador |
| P1 | Replay de POST o pérdida de cola | UUID estable, hash, UNIQUE, transacción y recibo durable; pruebas concurrentes |
| P1 | Identidad RTC confundida con humano persistente | Mapeo explícito; FKs compuestas por sesión y autorización |
| P1 | Relojes/sensores físicos y calibración no acreditados | No convertir raw motion/RTT en fuerza/latencia E2E sin medición |
| P1 | Reglas faltantes (Smash intentos/curva; empates; Slice; unidades Throw) | Aprobación de PO y fixtures antes de cerrar juego |
| P1 | Bootstrap público intermitente y trust local de primera visita | Fallback explícito, evidencias reales de permisos/firewall y recuperación |
| P1 | Instalación npm S4 reporta vulnerabilidades | Tarea de análisis de advisories/alcance; no aplicar actualizaciones forzadas aquí |
| P2 | Históricos usados como estado actual | Índice único, registro de estado y discrepancias con SHA |
| P2 | Retención/licencias/distribución no concluidas | Decisiones explícitas y evidencias previas a publicación; no inventar certificación |

[Próximos pasos](NEXT_STEPS.md) contiene acciones y preguntas abiertas; [evidencia](../04-testing/EVIDENCE_INDEX.md) delimita qué se probó.
