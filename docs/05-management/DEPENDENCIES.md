# Dependencias entre equipos

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

| Productor → consumidor | Entregable requerido | Bloqueo / forma de seguimiento |
|---|---|---|
| PO → Backend | Repo oficial, owner, scope y acceso lectura | S7 confirmado: [https://github.com/Josue1855/Gorilla-Escape-Backend](https://github.com/Josue1855/Gorilla-Escape-Backend); stack de jugadores aprobado/implementado S9; acordar colaboradores y distribución final |
| Red/Unity → UI/UX | Cliente técnico, identidad/epoch, estados y contratos | Existe S1; integración S4 pendiente; UI-01/04 |
| Unity → UI/UX | Reglas/intenciones/eventos de cada disciplina | PLANIFICADO; UI-02 no puede inventar scoring |
| Arte → Unity/UI/UX | Humanos/NPC y recursos propios aprobados | Alcance validado, assets finales no verificados |
| Backend → UI/UX | API/versiones/errores/persistencia | S9/S10: GET/POST jugadores integrados en fork; revisión/incorporación upstream pendientes |
| Unity → Backend | Resultados oficiales, rulesVersion y autorización | Publicador/outbox PROPUESTO; no aceptar scores de teléfono sin autoridad |
| Backend ↔ UI/UX | Idempotencia, estado pendiente/confirmado y reconciliación | Snapshot/borrador S10 implementado; idempotencia/outbox automática siguen propuestos |
| QA física → aceptación | T019 y T020 con ejecutable identificado | BLOQUEADO; desarrollo paralelo autorizado |
| Mantenedor S4 → gestión | develop upstream y revisión del fork; owner Project propio | Upstream sólo READ/main; solicitud al mantenedor, sin asignar permisos por cuenta propia |

No hay ciclo obligatorio para empezar: el fork ya corrigió empaquetado y probó jugadores; Unity puede implementar reglas y UI preparar el adaptador técnico. La aceptación extremo a extremo sí depende de los tres. Enlace cruzado propuesto: issue consumidora referencia URL real productora y versión del contrato; una incidencia se cierra en su repositorio, no se reflejan Done falsos en otro Project.
