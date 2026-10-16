# Ramas, PR y validaciones

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Monorepo: `main` es entrega y `develop` integración. Ramas pequeñas desde develop; esta auditoría usa `docs/architecture-project-sync`. La consulta API confirmó checks estrictos management-validation y java-react-foundation, aplicados a administradores, PR obligatorio y force push deshabilitado; aprobaciones mínimas actualmente cero no equivalen a permiso de merge automático.

PWA independiente: rama por defecto `main` confirmada. No hay evidencia de `develop` y no se propone un PR hacia una rama inexistente. Falta push; no se creó PR allí. Backend: flujo DESCONOCIDO.

No reescribir historia, forzar push, cambiar protecciones ni mezclar directamente. El PR documental del monorepo apunta a develop y queda para revisión, sin merge. Esta tarea sólo cambia Markdown. En documentación validar enlaces/anchors, Mermaid y diff; CI existente también ejecuta Java/React y Python según sus workflows, sin modificarlos. Tests Unity completos se requieren cuando cambien sus fuentes, no se declaran reejecutados por un cambio editorial.

[Workflow histórico detallado](../WORKFLOW.md). Las instrucciones de inicio restringido anteriores se interpretan junto a la autorización de desarrollo paralelo de DEC-031 y S3.
