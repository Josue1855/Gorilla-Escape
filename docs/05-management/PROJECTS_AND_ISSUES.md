# Auditoría y propuesta de Projects V2

> Snapshot histórico de la auditoría S1–S8. Para el backend/PWA implementados después, consultar [S9/S10](../00-product/SOURCE_INVENTORY.md#s9--backend-de-jugadores-aprobado-e-implementado), [estado actual](../00-product/DEVELOPMENT_STATUS.md) y [sincronización](../00-product/DOCUMENTATION_SYNC.md). Los conteos y ausencia de implementación descritos abajo corresponden al momento de esa auditoría.

Última revisión: 2026-10-16. Fuentes: S1, S3, S4 y S6; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

## Fase 7A: observado

S6: cuenta autenticada Josue1855. El [Project principal #1](https://github.com/users/Josue1855/projects/1) es privado, abierto, título «Gorilla Escape — Product Development», 95 items/21 campos; se consultó sólo en lectura. Su [vista de referencia](https://github.com/users/Josue1855/projects/1/views/1) sigue sin modificación. Consulta de Projects visibles de Josue1855: sólo ese Project; Quintana-117: cero; projectsV2 enlazados a Dise-o_pwa: cero. Esto descarta equivalentes **visibles**, no Projects privados inaccesibles.

S4 no tiene issues, PR ni milestones en las listas accesibles al auditar; sus etiquetas son las genéricas y accessibility. S1: 95 issues (68 abiertos, 27 cerrados), 26 PR accesibles/26 merged; milestones históricos de fases permanecen abiertos. No se copiaron 88/95 issues ni sus estados.

Permiso real S4: lectura sí, push/triage/admin no. Propietario previsto del Project nuevo aún no confirmado: se propone Josue1855, o Quintana-117 si el equipo decide y otorga acceso. No se deduce propiedad de un Project a partir de ser PO.

## Actualización posterior S7: repositorio backend agregado

La instrucción posterior explícita del PO autorizó crear Gorilla-Escape-Backend y vincularlo al Project #1. Ambas operaciones fueron ejecutadas y verificadas por consulta posterior. El Project ahora tiene dos repositorios enlazados y conserva 95 items/21 campos/10 vistas; no se cambiaron issues ni estados. Esto no crea el Project independiente de PWA UI/UX ni resuelve sus permisos de escritura. Fuente/commit del nuevo backend: [S7](../00-product/SOURCE_INVENTORY.md#s7--creación-posterior-autorizada-del-backend).

## Configuración PROPUESTA

Título: **Gorilla Escape — PWA UI/UX**. Seguimiento independiente; Unity mantiene Project #1. El backend S7 tiene repositorio propio bajo Josue1855 y quedó vinculado al Project #1 por nueva instrucción explícita del PO; el seguimiento independiente detallado sigue por acordar. No se crea otro Project Unity.

| Campo | Tipo / valores candidatos |
|---|---|
| Status | Single select: Backlog, Ready, In Progress, In Review, Blocked, Done |
| Priority | Single select P0, P1, P2; urgencia, no fase ni grado académico |
| Type | Single select Epic, Task, Bug, Test, Documentation; no presumir Issue Types habilitados |
| Iteration | Iteration; fechas/cadencia tras Sprint Planning real |
| Assignees | Campo nativo, sólo cuentas elegibles confirmadas |
| Epic / Dependency | Parent issue/subissues del mismo repo si se habilita; URLs cruzadas para bloqueos de otros repos; texto de apoyo sin inventar soporte nativo de dependencia |
| Target date | Date sólo si se acuerda fecha real; sin rellenar fechas inventadas |

| Vista | Layout y filtro propuestos |
|---|---|
| Overview | Tabla del backlog propio |
| Kanban | Board agrupado por Status |
| Roadmap | Roadmap; fechas sólo cuando existan |
| Sprint | Tabla/board, iteration:@current cuando se configure |
| By Epic | Tabla agrupada por Epic disponible |
| By Assignee | Tabla agrupada por Assignees |
| Bugs | Tabla, Type Bug |
| Testing | Tabla, Type Test |
| Academic Deliverables | Tabla de tareas LAB-01..10 mediante etiqueta acordada |

Automatizaciones PROPUESTAS: nuevos items→Backlog, issue cerrada→Done sólo con criterios/evidencias, PR vinculado sin autoaceptar pruebas físicas, revisión de blockers y archivado tras entrega. Confirmar las opciones realmente disponibles en Settings/Workflows. No hay ninguna automatización configurada por esta tarea, ni se promete API de workflow no inspeccionada.

## Fase 7B: BLOQUEADO, sin recursos remotos nuevos

Faltan propietario confirmado y permiso de escritura de issues en S4; lectura de Projects no demuestra permiso de crear en otra cuenta. No se solicita privilegio de administrador ni token nuevo. Se necesita que un mantenedor cree las issues o conceda Write al colaborador acordado, y un owner autorizado cree/configure el Project. Repetir búsqueda de duplicados justo antes de ejecutar.

Plan reproducible (comandos para un operador ya autorizado; **no ejecutados**, no son prueba de recursos creados). `gh` debe estar instalado/autenticado; en este ambiente se usó su ruta local instalada.

```bash
# Auditoría y guardas; confirmar propietario con el equipo antes de crear.
gh api repos/Quintana-117/Dise-o_pwa --jq '.permissions'
gh project list --owner Josue1855 --format json
gh project list --owner Quintana-117 --format json
gh issue list --repo Quintana-117/Dise-o_pwa --state all --limit 200
# Detener si push=false, owner sin acordar o equivalente visible.
# Sólo para owner confirmado Josue1855 y escritura ya otorgada:
gh project create --owner Josue1855 --title 'Gorilla Escape — PWA UI/UX' --format json
# Registrar número/ID/URL devueltos; usar el número real, nunca el #1 histórico.
```

Después de la creación, con PROJECT_NUMBER tomado del recibo real:

```bash
gh project view "$PROJECT_NUMBER" --owner Josue1855 --format json
gh project field-list "$PROJECT_NUMBER" --owner Josue1855 --format json
# Reusar campos existentes, no duplicar Title/Status/Assignees.
gh project field-create "$PROJECT_NUMBER" --owner Josue1855 --name Priority --data-type SINGLE_SELECT --single-select-options P0,P1,P2
gh project field-create "$PROJECT_NUMBER" --owner Josue1855 --name Type --data-type SINGLE_SELECT --single-select-options Epic,Task,Bug,Test,Documentation
# Guardar body completo desde EPICS_AND_BACKLOG; no usar una issue vacía.
gh issue create --repo Quintana-117/Dise-o_pwa --title '[UI-01] Integrar controlador técnico' --body-file /ruta/al/body-revisado.md
# ISSUE_URL se toma de la respuesta; verificar por consulta posterior.
gh issue view "$ISSUE_URL" --json number,title,state,url,body
gh project item-add "$PROJECT_NUMBER" --owner Josue1855 --url "$ISSUE_URL" --format json
gh project item-list "$PROJECT_NUMBER" --owner Josue1855 --format json
```

Configurar Status, Iteration, vistas/agrupaciones y Workflows con UI del Project o API compatible, previa lectura de fields/views reales. S6 confirmó que existe CreateProjectV2ViewInput con projectId/name/layout/configuration; eso **no** prueba que se hayan creado vistas ni que todas las agrupaciones/automatizaciones sean escribibles por API. Registrar recibo y consulta posterior de cada campo/vista/issue/item, IDs, URL, conteos y errores; si una operación falla, no continuar como si existiera.

No ejecutar `tools/github/configure_project.py` ni `configure_views.py` del monorepo para el Project PWA: su estado apunta al Project histórico. Los cuerpos propuestos y dependencias están en [backlog](EPICS_AND_BACKLOG.md).
