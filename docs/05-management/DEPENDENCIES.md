# Dependencias entre equipos

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

| Productor → consumidor | Entregable requerido | Bloqueo / forma de seguimiento |
|---|---|---|
| PO → Backend | Repo oficial, owner, scope y acceso lectura | S7 confirmado: [https://github.com/Josue1855/Gorilla-Escape-Backend](https://github.com/Josue1855/Gorilla-Escape-Backend); falta acordar colaboradores/stack |
| Red/Unity → UI/UX | Cliente técnico, identidad/epoch, estados y contratos | Existe S1; integración S4 pendiente; UI-01/04 |
| Unity → UI/UX | Reglas/intenciones/eventos de cada disciplina | PLANIFICADO; UI-02 no puede inventar scoring |
| Arte → Unity/UI/UX | Humanos/NPC y recursos propios aprobados | Alcance validado, assets finales no verificados |
| Backend → UI/UX | API/versiones/errores/persistencia | BLOQUEADO; API-01/02 |
| Unity → Backend | Resultados oficiales, rulesVersion y autorización | Publicador/outbox PROPUESTO; no aceptar scores de teléfono sin autoridad |
| Backend ↔ UI/UX | Idempotencia, estado pendiente/confirmado y reconciliación | Diseño PROPUESTO; OFF-02/03 dependen de contrato |
| QA física → aceptación | T019 y T020 con ejecutable identificado | BLOQUEADO; desarrollo paralelo autorizado |
| Mantenedor S4 → gestión | Write de issues y owner Project confirmado | BLOQUEADO; no se crearon recursos |

No hay ciclo obligatorio para empezar: UI puede corregir empaquetado y preparar el adaptador mientras Unity implementa reglas y se prepara el backend nuevo. La aceptación extremo a extremo sí depende de los tres. Enlace cruzado propuesto: issue consumidora referencia URL real productora y versión del contrato; una incidencia se cierra en su repositorio, no se reflejan Done falsos en otro Project.
