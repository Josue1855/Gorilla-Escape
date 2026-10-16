# Registro vigente de decisiones

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Los registros históricos [DECISIONS.md](../DECISIONS.md) conservan fechas y condiciones originales. Este registro concentra la lectura vigente y las propuestas nuevas, sin reutilizar números DEC históricos.

| ID de auditoría | Estado | Decisión y procedencia |
|---|---|---|
| A-01 | PLANIFICADO | Tres proyectos coordinados (Unity/UI/UX/backend), seguimiento independiente; encargo PO S3. La separación física del backend aún no está confirmada |
| A-02 | IMPLEMENTADO Y VERIFICADO | Unity autoridad; Java transporte/sesión técnica; Shared contratos puros; IPC loopback supervisado. DEC-005, S1/S2 |
| A-03 | IMPLEMENTADO Y VERIFICADO | WebRTC DataChannel para input; REST de señalización separado. DEC-010 de transporte, S1/S2 |
| A-04 | IMPLEMENTADO Y VERIFICADO | Un QR JOIN normal, origen congelado y recuperación explícita; firewall acotado. DEC-031, PR #121, S2; alcance Software/Lab |
| A-05 | PLANIFICADO | Humanos participantes, cuatro disciplinas definidas en S3; no combate ni tiro al blanco. Implementación Unity pendiente |
| A-06 | PROPUESTO | Publicador autorizado de resultados Unity → backend; PWA no puede declarar puntuación oficial. Credencial del adaptador pendiente |
| A-07 | PROPUESTO | UUID de negocio, transactionId estable, hash canónico y transacción atómica para resultados/intentos; ver diseño BD |
| A-08 | PROPUESTO | IndexedDB para operaciones permitidas, reintento limitado y acuse durable; no guardar tokens RTC ni telemetría masiva |
| A-09 | DESCONOCIDO | Motor, ORM, versión, hosting y repositorio del backend; no se adopta PostgreSQL/JPA por inferencia |

DEC-010 aparece dos veces en la historia; la sección titulada «Mobile Transport» identifica la decisión de transporte. Las condiciones antiguas que impedían iniciar Fase 1 fueron enmendadas por autorización posterior de trabajo paralelo, sin cerrar aceptación física. Las propuestas A-06 a A-08 necesitan aprobación de ambos productores/consumidores antes de programarse.
