# Offline LAN y transacciones

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

| Condición | Comportamiento actual | Diseño futuro / prueba |
|---|---|---|
| WAN desconectada, LAN activa | S1 tiene CABIN/WebRTC host y recursos locales; Software/Lab S2 | T019/T020 deben demostrarlo en hardware; backend local puede seguir accesible |
| Backend de jugadores inaccesible | S10 conserva consulta/borrador y protege intento incierto; fallo selectivo de API emulado | Sin cola ni POST automático; prueba física separada pendiente |
| Wi-Fi/LAN perdida | Transporte técnico falla y requiere recuperación manual | No se envía gameplay retrospectivo ni se rehace una partida como si continuara |
| Shell previamente cacheado | S1 y fork S10 tienen SW generado; S10 recarga offline comprobada en Chrome | S4 upstream conserva brecha; móvil/LAN física pendientes; primera visita offline no soportada |

El SW histórico de S4 proponía respaldo ficticio; esa descripción no aplica al laboratorio S10. En S10 Cache Storage sólo sirve shell y el API queda excluido; IndexedDB guarda snapshot/borrador etiquetados. Al volver online se requiere envío explícito. Un error de red/5xx no autoriza reintentar automáticamente. No hay Background Sync ni idempotencia. La falta de IndexedDB de S4 es un dato del upstream anterior, no del fork actual.

Flujo de negocio **PROPUESTO**; sólo encolar operaciones aprobadas. Un resultado oficial necesita outbox Unity/adaptador durable, o una delegación autorizada explícita: no confiar sólo en IndexedDB del teléfono ni dejar que éste fabrique puntuación.

```mermaid
flowchart TD
  A["Operación autorizada con UUID estable"] --> B["Guardar en outbox durable propuesto"]
  B --> C{"Backend accesible"}
  C -->|"No"| D["Pendiente visible / backoff limitado"]
  D --> C
  C -->|"Sí"| E["POST con misma clave y payload"]
  E --> F{"Respuesta durable"}
  F -->|"2xx confirmado"| G["Marcar confirmado conservando recibo"]
  F -->|"Red o 5xx"| D
  F -->|"Validación o conflicto"| H["Error visible / revisión sin duplicar"]
```

Decidir propietario de la cola, capacidad/caducidad, orden por agregado, cierre de sesión con pendientes, conflictos, versión de payload y borrado de caché. `navigator.onLine` es una pista, no una sonda de disponibilidad REST. No cachear respuestas de error como éxito, tokens de admisión ni certificados privados. Background Sync no se presume disponible: prever botón de sincronización y reintento con app abierta, y validar navegadores físicos.
