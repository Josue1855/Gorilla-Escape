# PWA UI/UX, laboratorio y cliente técnico

Última sincronización: 2026-10-16. Fuentes S9/S10 y evidencia fechada; [inventario](../00-product/SOURCE_INVENTORY.md). Trabajo documentado en ramas/PR, no integrado automáticamente.

| Base | Versiones y función | Evidencia / límite |
|---|---|---|
| S1 PWA/ monorepo | React19.3.0/Vite8.3.2; cliente Gorilla Core, sensores, RTC y lifecycle | S2 Software/Lab; físico final pendiente |
| S4 upstream UI/UX | React18.3.1, demo App/AvatarList y JSONPlaceholder | Sólo main y READ; no se modificó; S5 detectó dist incompleto |
| S10 fork de laboratorio | React18.3.1/Vite6.4.4/plugin4.7.0; /lab.html consume backend real | [PR #1](https://github.com/Josue1855/Dise-o_pwa/pull/1), siete tests/build/audit PASS; integración al upstream pendiente |

El laboratorio tiene Service Worker generado desde assets compilados, manifest público e icono SVG. Cache Storage sólo contiene shell; API/POST quedan fuera. IndexedDB conserva snapshot y borrador. La primera visita requiere red; recarga offline del shell y consulta local se verificó en Chrome. No hay competidores de rescate ni sincronización ficticia en /lab.html.

POST sólo por acción explícita. La reconexión no envía el borrador y un intento incierto queda protegido tras recargar. No hay Background Sync ni idempotencia backend. navigator.onLine indica conectividad, no disponibilidad de REST. [Evidencias y tres capturas DevTools pendientes](https://github.com/Josue1855/Gorilla-Escape-Backend/tree/3635186/docs/laboratorio).

/index.html conserva la demo de mando: botones/energía cambian estado local y AvatarList usa JSONPlaceholder. Eso no acredita transporte al juego ni identidad persistente. La integración visual con el cliente técnico sigue planificada, con un propietario de sensores/timers/canales y teardown. No sustituir el runtime del monorepo por el laboratorio. [Plan](../03-development/INTEGRATION_PLAN.md).
