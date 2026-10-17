# Convenciones que preservan los límites

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

No se modifica ni introduce código en esta auditoría. Se conserva el stack y convenciones existentes; consultar assemblies y módulos S1 antes de diseñar nuevas interfaces.

- C#: contratos Shared serializables sin UnityEngine; gameplay separado de input raw, UI y red; no I/O bloqueante en frame; preservar disponibilidad, epoch y freshness.
- Java técnico: servicios con ownership de recursos y cierre acotado; mensajes/diagnósticos limitados; no introducir scoring o SQL incidental en el transporte.
- React: lifecycle único, cleanup de listeners/timers, permisos por gesto, errores visibles y estados diferenciados; `navigator.onLine` no es readiness del juego.
- Backend candidato: DTO explícito, validación y autorización en servidor, transacciones por agregado, claves de idempotencia estables; los detalles ORM/motor esperan decisión.
- Pruebas: nombres por comportamiento, fixtures sin datos reales, distinguir sintético/emulador/físico; no guardar tokens/SDP/fotogramas ni telemetría extensa.

Versionar contratos y reglas; no usar TypeScript como si ya existiera en los clientes inspeccionados, que usan JavaScript/JSX. Las propuestas de tipado/formato requieren tarea independiente y acuerdo; no añadir una herramienta por documentarla.
