# Gorilla Escape — Configuración GitHub

**Fecha:** 2026-10-06. Configuración del repositorio público y Project privado; base técnica de Fase 0 publicada, Spike pendiente.

## Destino y comprobaciones previas

- [Repositorio público Josue1855/Gorilla-Escape](https://github.com/Josue1855/Gorilla-Escape).
- [Project Gorilla Escape — Product Development](https://github.com/users/Josue1855/projects/1), personal y privado; enlazado al repositorio.
- Cuenta GitHub verificada: Josue1855, permisos admin/maintain/push/triage/pull en el repo.
- GitHub CLI oficial ejecutada temporalmente fuera del repo; autenticación en keyring. Scope project añadido con autorización explícita del usuario. No tokens en archivos versionados.
- Project número 1 conservado y enlazado al repositorio actual; se reutilizan campos, vistas e iteración.
- main y develop comparten la base inicial. Los nuevos PR de tareas apuntan a develop.

La organización no se creó: no hay nombre/equipo confirmado y una cuenta personal permite esta preparación. No invitar personas ni conceder acceso usando nombres incompletos.

## Campos configurados

| Campo | Tipo / valores |
|---|---|
| Status | Backlog, Ready, In Progress, In Review, Validation, PO Review, Done |
| Priority | Critical, High, Medium, Low |
| Iteration | Semana de 7 días; una iteración vacía desde 2026-10-05 |
| Phase | 0, 1, 2, 3, 4, 5, 6, Futuro |
| Area | Mobile/PWA, Network, Server, Protocol, Unity/Core, Gameplay, Camera/Tracking, UI/Art, CI/QA, Documentation |
| Story Points | Number; sin estimaciones automáticas |
| Risk | Critical, High, Medium, Low; sin valor inventado |
| Target Release | v1.0 — MVP; futuros sin release comprometida |

Se reutiliza Status nativo. Priority y Risk siguen vacíos para evaluación del PO/equipo. Los IDs reales se guardan en [project_state.json](project_state.json). No confundir P0/P1/P2 de alcance con Priority o Phase.

## Vistas creadas y filtros guardados

| Vista | Layout | Filtro |
|---|---|---|
| Product Backlog | Table | is:issue |
| Current Sprint | Table | is:issue iteration:@current |
| Sprint Board | Board | is:issue iteration:@current |
| My Work | Table | is:issue assignee:@me |
| PO Review | Table | is:issue status:"PO Review" |
| Blocked | Table | is:issue label:blocked |
| Bugs | Table | is:issue label:"type:bug" |
| By Area | Table | is:issue |
| Roadmap | Roadmap | is:issue label:"type:epic" |
| Releases | Table | is:issue -phase:Futuro |

Se crearon por GraphQL createProjectV2View/updateProjectV2View, con filtros persistidos. No se afirma que las vistas sean imposibles por API: el esquema actual sí permite crearlas. La API REST consultada para fields/views GET devolvió 404; GraphQL permitió completar creación/nombres/filtros/columnas visibles. By Area agrupada por Area y Releases por Milestone guardadas en UI; Sprint Board muestra las siete columnas de Status. Roadmap no tiene fechas inventadas.

No quedan vistas ni agrupaciones pendientes. Para cambiar agrupación: abrir vista → View → Group by → campo → Save view → confirmar Save en el diálogo. Sprint Board usa Status como columnas. No introducir fechas de roadmap ficticias: PO debe acordar hitos antes de asignarlas. Roadmap existe, pero no promete un cronograma cargado. Current Sprint/My Work/Bugs/PO Review pueden estar vacíos correctamente.

## Labels, tipos, hitos y jerarquía

Convención fallback type:epic, type:feature, type:task, type:bug, type:spike, type:documentation: no hay organización para administrar Issue Types. Labels adicionales blocked, needs-device-test, needs-decision, security, performance, breaking-protocol, documentation. Labels area:* se justifican por búsqueda global fuera del Project; no se duplican Priority/Phase en labels.

Siete milestones derivados de §50, Fase 0–6, sin due dates nuevos. Futuro no tiene milestone ni fecha de release. No se creó release vacía ni tag de un producto no construido.

Backlog, jerarquía nativa, dependencias y cantidades reales: [BACKLOG_STRUCTURE.md](BACKLOG_STRUCTURE.md). [publish_receipt.json](publish_receipt.json) conserva IDs/URLs para evitar duplicados y auditar la publicación. No usar jerarquía parent como si fuera automáticamente una dependencia blocking.

## Automatizaciones nativas configuradas

- Item added → Backlog.
- Auto-add de Issues abiertos de Gorilla-Escape.
- Auto-add de sub-issues (nativa).
- Item closed → Done, limitado a Issue; cierre exige evidencia/aceptación por proceso.
- Pull request merged → Done para el PR agregado al Project; no equivale a aceptación de su Issue.
- Item reopened → Backlog, para volver a triage.

Auto-close por mover estado, auto-archive y aprobación de código → Done permanecen desactivadas. No bots, PAT en Actions ni servicios externos. Configuración verificada en interfaz; aún no se realizó una prueba de cierre/merge con Issue de prueba porque no corresponde marcar trabajo P0 terminado. [Automatizaciones GitHub](https://docs.github.com/en/issues/planning-and-tracking-with-projects/automating-your-project/using-the-built-in-automations).

## Archivos y repetición segura

- Issue Forms: Feature/Task, Bug y Technical Spike; blank issues desactivados.
- PR template con los campos solicitados.
- Management validation: workflow real de validación documental/backlog, checkout fijado a SHA, permiso contents:read, sin tests ficticios de Unity o hardware.
- tools/github/configure_project.py: reutiliza Project y campos; no reemplaza opciones pobladas silenciosamente.
- tools/github/configure_views.py: reutiliza nombres de vistas.
- tools/github/publish_backlog.py: identifica Issues por marker estable, preserva cuerpos/metadata existentes y reutiliza relaciones. Completa sólo metadata ausente; no restablece valores editados por PO. No usarlo para vaciar intencionadamente un campo.
- tools/github/validate_management.py: validación local sin dependencias externas.

Estos scripts están acotados a Josue1855/Gorilla-Escape y Project 1; revisar constantes si el destino cambia. Usan gh autenticado, no leen ni imprimen tokens. Para repetir, ejecutar desde raíz con gh instalado; GH_BINARY puede apuntar a una instalación oficial. La manifest es semilla de preparación: tras arrancar seguimiento, GitHub es fuente de Status, asignaciones, Sprint y discusión; no sincronizar en sentido inverso sin diseñarlo.

## Pendientes de gobierno

- Usuarios GitHub de integrantes, invitaciones y permisos acordados.
- Protecciones main/develop activas tras autorizar el usuario el repositorio público: un PR, una aprobación de otro revisor, checks management-validation y java-react-foundation, rama actualizada y conversaciones resueltas; aplican a administradores. Sin force push ni deletion. Resultado en [protection_result.json](protection_result.json).
- Fechas/hitos, estimaciones y selección Sprint por PO, sin rellenar semanas automáticamente.
- Resolver discrepancias 42/50, confirmación de resultado y slow motion P1/§26 mediante T001.
- Revisar naming y calendario v4: no se reescribieron fechas antiguas.

Objetivo GITHUB PROJECT MANAGEMENT READY condicionado a los pendientes reales listados; no confundir preparación de tablero con producto terminado.

## Reporte de configuración verificada

1. Project 1 creado, privado y enlazado al repo público.
2. Ocho campos solicitados configurados; Priority/Risk/Story Points vacíos para planificación.
3. Diez vistas creadas con filtros guardados; agrupaciones Area/Milestone comprobadas y Board por Status.
4. Ninguna vista pendiente. Fechas de roadmap y planificación Sprint quedan para PO.
5. 23 labels gestionadas: seis tipos fallback, diez áreas y siete señales. Se conservaron labels predeterminadas GitHub sin usarlas como Priority.
6. Siete milestones Fase 0–6, sin fechas nuevas.
7. Ocho Epics.
8. Veintiuna Features.
9. Cuarenta y ocho Tasks, seis Spikes y cinco Documentation. Cero Bugs inventados.
10. Ochenta vínculos parent/sub-issue y 86 dependencias blocked-by verificadas por API.
11. Seis automatizaciones nativas activas, con límites de cierre/aceptación documentados.
12. Tres Issue Forms, config, PR template, workflow de validación y herramientas de preparación/auditoría; incluidos en la base publicada.
13. Permisos admin y project disponibles. Protecciones activas en main/develop; limitación previa resuelta al hacerlo público.
14. Pendientes: usuarios/permisos del equipo, prioridades/estimaciones/Sprint y aclaraciones de producto. Fase 0 en progreso, sin gameplay.
15. Issues creados por fase: 0=27, 1=6, 2=10, 3=17, 4=8, 5=5, 6=8, Futuro=7; total 88.

Auditoría: [verification.json](verification.json). La importación se reanudó tras conflictos temporales con auto-add; terminó sin duplicados y preservando metadatos existentes. Configuración API/archivos no equivale a ejecución de sensores/cámara ni pruebas de producto.

## Integración revisable

La base técnica y el gobierno están incluidos en main/develop. Los nuevos PR de tareas apuntan a develop. Build y tests Java/React disponibles mediante java-react-foundation, además de management-validation. Unity continúa pendiente de licencia; no cerrar issues por la mera publicación de la base.
