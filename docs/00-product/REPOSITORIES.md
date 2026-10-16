# Repositorios y acceso

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

| Proyecto | Repositorio y revisión observada | Acceso y conclusión |
|---|---|---|
| Unity y conectividad técnica | [https://github.com/Josue1855/Gorilla-Escape](https://github.com/Josue1855/Gorilla-Escape), `develop` `7a750826c5565cbafdec983c5988c4cd5657d72a` | Lectura/escritura/admin del repositorio confirmados por API; cambios documentales en rama de tarea |
| PWA UI/UX | [https://github.com/Quintana-117/Dise-o_pwa](https://github.com/Quintana-117/Dise-o_pwa), `main` `ffbec2260dbad3f36743cffc2309cb206f7dba9a` | `pull=true`, `push=false`, `triage=false`, `admin=false`; inspección y build en copia temporal, sin modificaciones remotas |
| PWA Backend | [Josue1855/Gorilla-Escape-Backend](https://github.com/Josue1855/Gorilla-Escape-Backend), `main` `e76371097f4bcd881fa081211ec8d0d78e308c0b` | Privado, lectura/escritura/admin confirmados. Tres Markdown iniciales; no código, esquema, endpoints ni tests aún |

El `Server/` del monorepo es un servicio Spring Boot de conectividad, no evidencia de un backend empresarial persistente. `PWA/` ya contiene el cliente técnico conectado a ese servicio; el repositorio UI/UX separado aún no lo integra. El nuevo backend es creación propia del equipo; no se reutilizó ningún repositorio académico externo.

El checkout original tenía cambios locales en código y configuración. Se conservó intacto. Se reutilizó el worktree limpio adjunto, se actualizó `origin/develop` y se creó `docs/architecture-project-sync`. No se publican sus rutas privadas ni datos de credenciales.

Project histórico confirmado: [Gorilla Escape — Product Development](https://github.com/users/Josue1855/projects/1). Véase [auditoría de gestión](../05-management/PROJECTS_AND_ISSUES.md).

Actualización S8: el backend ya tiene develop como rama por defecto e integración, creada desde la base documental inicial. El trabajo futuro parte de develop en ramas de tarea y vuelve mediante PR a develop; main sólo recibe promociones de entrega aprobadas. [Política obligatoria](../03-development/GIT_WORKFLOW.md#regla-obligatoria-confirmada-por-el-po).
