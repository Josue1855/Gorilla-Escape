# Gorilla Escape — Uso del Project por el equipo

**Product Owner:** Josue. **Fuente:** v4 §§39/48/49/50/51; [WORKFLOW](../WORKFLOW.md).
**Project:** [Gorilla Escape — Product Development](https://github.com/users/Josue1855/projects/1).

## Roles y permisos

Josue prioriza y acepta el producto. Organization Owner administra la organización; este repo pertenece por ahora a la cuenta personal Josue1855, sin organización creada. CODEOWNER orienta revisión por rutas, Assignee ejecuta la tarea y Reviewer revisa el cambio. Son responsabilidades diferentes, aunque una persona pueda tener más de una. No se generó CODEOWNERS sin usernames reales.

Principales de la v4: Josue producto, Erik proceso, Saul arte/UI, Arturo Unity/gameplay, Hiram móvil/red, Austin cámara. No representan exclusividad. Ningún Issue del backlog fue asignado automáticamente. El equipo acuerda Assignee, estimación y capacidad durante Sprint Planning.

## Estados

| Estado | Entrada y salida |
|---|---|
| Backlog | Todo trabajo nuevo; sin promesa de ejecución |
| Ready | Cumple §48: objetivo, responsable, prioridad, dependencias, aceptación, diseño y tamaño adecuados |
| In Progress | Trabajo iniciado en rama de tarea de la fase autorizada |
| In Review | PR completo y pruebas disponibles, esperando revisión técnica |
| Validation | Integrado en develop; pruebas funcionales/físicas e integración pendientes |
| PO Review | Evidencia disponible; Josue revisa resultado y alcance |
| Done | DoD §49 satisfecha y aceptación pertinente registrada; después cerrar Issue como completed |

Una dependencia nativa identifica blocked-by/blocking; blocked sirve para búsqueda global y la vista Blocked. El responsable retira blocked cuando los bloqueantes estén resueltos. No se automatizó esta label ni se promete que se actualice sola.

Critical/High/Medium/Low representa urgencia de planificación. P0/P1/P2 representa alcance de la especificación, y 0–6 representa fase de implementación. El alcance está en el cuerpo; Phase y Priority están en el Project. Nunca desarrollar P1/P2 con P0 pendiente.

## Sprint Planning

1. Josue ordena Product Backlog y resuelve needs-decision; Erik comprueba dependencias/capacidad.
2. Seleccionar tareas de la fase autorizada, acordar Assignee y puntos; estimar tareas ejecutables, sin sumar otra vez Epics/Features.
3. Completar Ready y asignar Iteration únicamente al trabajo elegido.
4. Usar Current Sprint/Sprint Board; registrar evidencia y bloqueos durante la semana.
5. Al cierre revisar Done aceptado, pendientes y aprendizaje; no mover automáticamente todo lo incompleto al siguiente Sprint.

Hay una iteración semanal vacía desde el 5 de octubre; no asigna automáticamente un Sprint ni responsables. El usuario sí autorizó la base de Fase 0, registrada en DEC-002 y TEAM_START. Las próximas semanas y fechas de entregas se deciden por capacidad real. Milestone agrupa aceptación de fase; Sprint agrupa una semana de trabajo. Target Release v1.0 sólo identifica el MVP previsto, no una release publicada ni fecha prometida.

## Ramas y PR

Una tarea → rama temporal desde develop → PR a develop → revisión → integración → Validation → PO Review cuando corresponda. main conserva entregas validadas. No ramas por persona ni permanentes por minijuego/plataforma. Los cambios de protocolo requieren revisión conjunta de productor y consumidor.

Enlazar Issues con Closes #N cuando el PR realmente resuelva el trabajo y añadir también el enlace en Development del Issue si hace falta. GitHub interpreta keywords de cierre en relación con la rama por defecto: un merge a develop no debe darse por cierre automático del Issue. Al promover a main comprobar qué Issues se cerrarán y que ya cumplan DoD. [Documentación de enlace](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/linking-a-pull-request-to-an-issue).

La automatización de PR merged pone Done al elemento PR si está en el Project; no prueba aceptación del Issue. La de Issue closed pone Done al Issue, por lo que sólo cerrar como completed después de aceptación. Cancelados/duplicados/not planned deben documentarse y retirarse del Project activo o corregir su estado manualmente para que no cuenten como trabajo completado. No se configuró cierre automático al mover una tarjeta a Done ni por aprobar código.

## Operación cotidiana

My Work usa assignee:@me; PO Review usa Status, no Assignee=Josue. Bugs usa type:bug como fallback. El formulario Feature/Task requiere elegir tipo; si Issue Types no están disponibles, ajustar type:feature o type:task durante triage. El tipo elegido en el formulario no modifica labels dinámicamente por sí solo.

Las plantillas solicitan Objective, Context, Scope, Out of Scope, Acceptance Criteria, Dependencies, Technical Notes, Tests, Evidence Required y Definition of Done. Rellenarlas no convierte automáticamente un Issue en Ready: hace falta revisión y planificación.

Para un bug real registrar reproducción y esperado/observado antes de priorizar. needs-device-test exige teléfonos/webcam reales con versiones registradas. No adjuntar secretos ni grabaciones de cámara del juego. Un spike termina con evidencia y decisión, aunque la alternativa evaluada se descarte.

## Base disponible para trabajo

[TEAM_START](../TEAM_START.md) contiene clonado y herramientas. La base compartida es develop; crear ramas pequeñas desde ella y enviar los PR a develop. No se crean ramas permanentes por integrante ni se asignan tareas automáticamente.
