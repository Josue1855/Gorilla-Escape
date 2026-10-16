# Próximos pasos operativos

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

## P0

1. Equipo backend/PO: usar Gorilla-Escape-Backend recién creado (S7), acordar responsables/colaboradores y flujo desde main, stack, motor y esquema. Crear el repositorio no acepta el laboratorio.
2. UI/UX/Red/Unity: acordar fuente de entrega y adaptador sobre cliente técnico S1, preservando lifecycle/Shared/IPC. Decidir cómo resolver las diferencias de versiones sin upgrades por inercia.
3. Mantenedor UI/UX: confirmar owner del Project y ejecutar plan de gestión con Write autorizado. No tocar Project Unity #1 ni duplicar issues.
4. UI/UX: abrir tarea para SW/manifest/iconos de dist y prueba offline real; no vender la demo como integración.
5. QA/PO: ejecutar T019 final (iPhone/cuatro teléfonos, permisos/firewall, sensores/latencias/cleanup), luego T020 en otra PC. Desarrollo paralelo de juegos/clientes sigue autorizado.

## P1

6. Unity/PO: cerrar decisiones de reglas por minijuego y crear framework/vertical slice en tareas independientes; no hay juegos implementados que aceptar.
7. Backend/Unity/UI/UX: aprobar REST/BD/autoridad de publicación, mapeo de identidad y transactionId; ejecutar GET/POST/SQL e idempotencia.
8. QA: ejecutar LAB-01..10 con escenarios WAN caída y backend inaccesible; registrar observaciones, capturas, logs/memoria y fallos auténticos.
9. UI/UX: analizar avisos de dependencias y accesibilidad sin mezclar upgrades con documentación.

## P2

10. Acordar retención, cola/outbox y reconciliación; completar cuatro disciplinas y aceptación del torneo; publicar sólo con gates satisfechos.

Preguntas abiertas: ¿Quién mantiene el backend nuevo y qué colaboradores deben tener acceso? ¿Quién será owner del Project UI/UX y qué cuentas tienen Write? ¿Cuál frontend será empaquetado con Java? ¿Qué credencial autoriza al publicador Unity? ¿Qué reglas/curvas/empates se aprueban? ¿Qué navegadores/PC son objetivo? ¿Qué política de retención y fechas de Sprint se acuerdan? No se sustituyen respuestas por supuestos.

Esta lista es operativa, no un reporte de tareas ejecutadas. Los recursos remotos nuevos y las verificaciones de este encargo se registran en [informe de auditoría](../00-product/AUDIT_REPORT.md).
