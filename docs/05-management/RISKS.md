# Riesgos y mitigación propuesta

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

| Prioridad | Riesgo comprobado o pendiente | Impacto / mitigación |
|---|---|---|
| P0 | Implementación en ramas aún no integrada al producto | Revisar PR backend/PWA; no confundir laboratorio SQLite con instalador/supervisor final |
| P0 | Dos PWA con diferente stack y sólo una conectada al runtime | Sustituir cliente técnico por demo rompería input; aprobar adaptador/fuente de entrega |
| P0 | QA física final y PC limpia sin cerrar | No prometer torneo/cero configuración universal; ejecutar T019/T020 |
| P0 | Upstream S4 sólo READ y main; owner Project indefinido | Solicitar develop y revisión del fork; no abrir PR a main ni cambiar accesos sin autorización |
| P0 | Confundir fork S10 verificado con upstream S4 | Brecha de build resuelta sólo en fork; completar revisión y tres capturas DevTools |
| P1 | Dos autoridades de puntuación | Unity oficial, backend valida y UI consulta; autorizar publicador |
| P1 | Replay de POST o pérdida de cola | UUID estable, hash, UNIQUE, transacción y recibo durable; pruebas concurrentes |
| P1 | Identidad RTC confundida con humano persistente | Mapeo explícito; FKs compuestas por sesión y autorización |
| P1 | Relojes/sensores físicos y calibración no acreditados | No convertir raw motion/RTT en fuerza/latencia E2E sin medición |
| P1 | Reglas faltantes (Smash intentos/curva; empates; Slice; unidades Throw) | Aprobación de PO y fixtures antes de cerrar juego |
| P1 | Bootstrap público intermitente y trust local de primera visita | Fallback explícito, evidencias reales de permisos/firewall y recuperación |
| P1 | Dependencias divergentes entre upstream y fork | S10 Vite6.4.4 con audit cero; upstream S4 conserva avisos históricos hasta revisión |
| P2 | Históricos usados como estado actual | Índice único, registro de estado y discrepancias con SHA |
| P2 | Retención/licencias/distribución no concluidas | Decisiones explícitas y evidencias previas a publicación; no inventar certificación |

[Próximos pasos](NEXT_STEPS.md) contiene acciones y preguntas abiertas; [evidencia](../04-testing/EVIDENCE_INDEX.md) delimita qué se probó.
