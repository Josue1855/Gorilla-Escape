# PWA UI/UX y cliente técnico

Última revisión: 2026-10-16. Fuentes: S1, S2, S3, S4 y S5; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Hay dos bases distintas:

| Base | Versiones verificadas | Función observada |
|---|---|---|
| S1 `PWA/` monorepo | React/ReactDOM 19.3.0, Vite 8.3.2, plugin 6.1.2, Node >=24 <25 | Gorilla Core: admisión, permisos por gesto, sensores, RTC, lifecycle, shell offline; evidencia S2 |
| S4 UI/UX independiente | package.json rangos React ^18.3.1, Vite ^5.4.2, plugin ^4.3.1; lock resuelve React/DOM 18.3.1, Vite 5.4.21, plugin 4.7.0 | App.jsx D-pad/háptica/energía local; AvatarList.jsx GET JSONPlaceholder; sin transporte al juego |

No se actualizan versiones por esta auditoría. `navigator.onLine` informa conectividad de navegador, no disponibilidad de Java o backend. Avatares/energía/acciones de la demo no representan identidades técnicas ni reglas aprobadas. Las etiquetas de «gorila atleta» deben reconciliarse con humanos participantes.

S4 tiene registro `/sw.js` en index.html, manifest e instrucciones cache-first. La build Vite ejecutada S5 produce HTML/assets/manifest procesado, **sin dist/sw.js ni iconos**. El SW fuente precachea `/src/*.jsx`, rutas ausentes del dist. Por ello build PASS no equivale a PWA offline instalable. Revisar empaquetado, precache de assets emitidos y alcance antes de prometer offline. No se reparó código.

No se encontró IndexedDB, POST persistente, cola transaccional, cliente RTC ni tests automatizados en S4; `npm test` no está definido. El monorepo genera un SW de shell desde assets compilados y evita cachear APIs; tampoco aporta una cola durable de negocio.

PLANIFICADO: acordar en qué repositorio vive la UI de entrega y cómo incorporar el adaptador técnico sin duplicar eventos/timers/identidades. Criterios: [integración](../03-development/INTEGRATION_PLAN.md) y [laboratorio](../04-testing/PWA_TEST_LAB.md).
