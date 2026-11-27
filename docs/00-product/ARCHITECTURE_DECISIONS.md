# Registro vigente de decisiones

Última revisión: 2026-10-26; evidencia histórica S9/S10 y estado de integración S12. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Los registros históricos [DECISIONS.md](../DECISIONS.md) conservan fechas y condiciones originales. Este registro concentra la lectura vigente y las propuestas nuevas, sin reutilizar números DEC históricos.

| ID de auditoría | Estado | Decisión y procedencia |
|---|---|---|
| A-01 | PLANIFICADO | Tres proyectos coordinados (Unity/UI/UX/backend), seguimiento independiente; encargo PO S3. Repositorios Unity/UI/UX/backend confirmados; backend independiente implementado S9 e integrado en su develop, ver S12 |
| A-02 | IMPLEMENTADO Y VERIFICADO | Unity autoridad; Java transporte/sesión técnica; Shared contratos puros; IPC loopback supervisado. DEC-005, S1/S2 |
| A-03 | IMPLEMENTADO Y VERIFICADO | WebRTC DataChannel para input; REST de señalización separado. DEC-010 de transporte, S1/S2 |
| A-04 | IMPLEMENTADO Y VERIFICADO | Un QR JOIN normal, origen congelado y recuperación explícita; firewall acotado. DEC-031, PR #121, S2; alcance Software/Lab |
| A-05 | PLANIFICADO | Humanos participantes, cuatro disciplinas definidas en S3; no combate ni tiro al blanco. Implementación Unity pendiente |
| A-06 | PROPUESTO | Publicador autorizado de resultados Unity → backend; PWA no puede declarar puntuación oficial. Credencial del adaptador pendiente |
| A-07 | PROPUESTO | UUID de negocio, transactionId estable, hash canónico y transacción atómica para resultados/intentos; ver diseño BD |
| A-08 | PROPUESTO | IndexedDB para operaciones permitidas, reintento limitado y acuse durable; no guardar tokens RTC ni telemetría masiva |
| A-09 | APROBADO E IMPLEMENTADO PARA JUGADORES | S9: SQLite/JDBC/Boot4.1.1/Java21/Flyway; aprobación expresa PO. Sin ORM/JPA; hosting e instalador final pendientes |

DEC-010 aparece dos veces en la historia; la sección titulada «Mobile Transport» identifica la decisión de transporte. Las condiciones antiguas que impedían iniciar Fase 1 fueron enmendadas por autorización posterior de trabajo paralelo, sin cerrar aceptación física. Las propuestas A-06 a A-08 necesitan aprobación de ambos productores/consumidores antes de programarse.

A-10 — IMPLEMENTADO Y VERIFICADO, S7: por instrucción posterior del PO se creó el repositorio privado oficial del backend y se vinculó al Project #1. Esta autorización permite únicamente esa asociación del Project existente; no cambia sus campos/vistas/issues ni implementa el backend.

A-11 — PLANIFICADO como política obligatoria confirmada por el PO el 2026-10-16: develop es la base de integración de todos los proyectos; trabajo en ramas de tarea y PR hacia develop. main queda reservada para promoción de una entrega validada y aprobada. El backend y el fork PWA ya usan develop como base de integración; la adopción en UI/UX requiere al mantenedor por falta de Write.

A-12 — IMPLEMENTADO Y VERIFICADO en S10: snapshot/borrador IndexedDB y shell cacheado del laboratorio; reconexión sin reenvío automático, intento incierto protegido. No equivale a la outbox idempotente propuesta A-08. Laboratorio integrado en develop del fork (PR #7); incorporación al upstream original pendiente.
