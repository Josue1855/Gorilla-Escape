# Arquitectura actual y objetivo

Última sincronización: 2026-10-16. Fuentes S9/S10 y evidencia fechada; [inventario](../00-product/SOURCE_INVENTORY.md). Trabajo documentado en ramas/PR, no integrado automáticamente.

Hay dos flujos implementados con alcances distintos: canal técnico Software/Lab S1/S2 y laboratorio de persistencia S9/S10 en ramas para revisión. La incorporación al producto distribuido sigue pendiente.

```mermaid
flowchart LR
  Phone["Teléfono: físico final pendiente"] --> Core["PWA técnica del monorepo"]
  Core <-->|"HTTPS bootstrap y WebRTC LAN"| Java["Java local supervisado"]
  Java <-->|"IPC loopback autenticado"| Unity["Unity Foundation: autoridad"]
  Vision["Proceso Vision PC"] -->|"CameraInput"| Unity
  UI["PWA laboratorio del fork"] -->|"GET y POST players"| Proxy["Proxy local de laboratorio"]
  Proxy --> Backend["Spring Boot JDBC"]
  Backend --> DB[("SQLite local")]
  UI --> IDB[("Snapshot y borrador IndexedDB")]
```

El Java técnico aloja el cliente RTC y administra conectividad; no se le atribuye el esquema SQL del backend independiente. El proxy Node loopback4173 y backend8090 son herramientas ejecutadas en el laboratorio, no un nuevo supervisor ni distribución final. SQLite no expone puerto a LAN. Unity calcula reglas y resultados; REST de jugadores no transporta controles.

Integraciones pendientes:

```mermaid
flowchart LR
  UI["UI de entrega por acordar"] -.->|"Adaptador técnico"| Java["Java y WebRTC existentes"]
  Java <-->|"IPC existente"| Unity["Unity: reglas y resultados"]
  Unity -.->|"Publicador autorizado con outbox"| Backend["Backend independiente"]
  Backend -.->|"Modelo de competición futuro"| DB[("SQLite: jugadores ya implementados")]
```

Outbox, autorización, sesiones/resultados/rankings e instalador autocontenido requieren tareas posteriores. [Contratos](API_CONTRACTS.md), [red](NETWORK_AND_IPC.md) y [offline](OFFLINE_STRATEGY.md) conservan estas fronteras.
