# Estado verificable del producto

Nota de la rama Jungle Party (2026-10-26): se preservan ocho escenas del alpha de Saúl y se adapta su entrada al transporte oficial de Fase 0. Este código aún no está integrado hacia `develop`. Los estados históricos siguientes pertenecen al baseline y no certifican estos minijuegos. [Matriz de integración](../03-development/SAUL_PHASE0_INTEGRATION.md).

Última revisión: 2026-10-26; evidencia histórica S9/S10 y estado de integración S12. Fuentes: S1, S2, S3, S9 y S10; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Este es el registro vigente. Los resultados de tests se conservan en [evidencia](../04-testing/EVIDENCE_INDEX.md); S9/S10 añaden pruebas reales del backend y PWA del fork. Esta sincronización sólo verifica documentación; no repite ni cambia resultados históricos.

| Componente / alcance | Estado | Evidencia y límite |
|---|---|---|
| Unity Foundation Linux | IMPLEMENTADO Y VERIFICADO | S1: Scripts/Core, UI/MainMenuController; tests EditMode/PlayMode; S2 build Linux. No significa torneo jugable |
| Shared, IPC, lifecycle y hardening Linux | IMPLEMENTADO Y VERIFICADO | S1: Shared/CSharp/Runtime, JavaProbeSupervisor, ManagedProbe, ProbeCodec; corpus y tests de proceso; S2 |
| HTTPS, QR y WebRTC local, Software/Lab | IMPLEMENTADO Y VERIFICADO | S1: connectivity, hosting, mobile; BootstrapRtcLabTest y S2: cuatro peers nativos; arranque público intermitente |
| Firewall acotado y recuperación, Software/Lab | IMPLEMENTADO Y VERIFICADO | S1: FirewallPreflight, tools/connectivity/firewall.py, test_firewall.py; S2: 11 pruebas. Permisos físicos pendientes |
| Captura móvil y lifecycle, Software/Lab | IMPLEMENTADO Y VERIFICADO | S1: PWA/src/input y controller, PWA/tests; S2 69/69. No acredita sensores físicos |
| LAN físico del candidato final, T019 | BLOQUEADO | S2 y issue #33 abierto: faltan pruebas físicas completas y evidencia final de iPhone/cuatro teléfonos |
| Empaquetado de referencia | IMPLEMENTADO Y VERIFICADO | S1: tools/t020/package.py y S2; sólo PC de desarrollo Linux |
| PC limpia T020 y cierre E0/F05 | BLOQUEADO | Issues #34/#1/#31 abiertos; PC física separada y T019 pendientes |
| PWA UI/UX: React, botones, GET de demostración | IMPLEMENTADO SIN VERIFICAR | S4: App.jsx/AvatarList.jsx; build ejecutado en auditoría. Sin prueba de interacción/GET en navegador |
| PWA del fork: build y tests | IMPLEMENTADO Y VERIFICADO | S10: siete tests, build Vite6.4.4 y npm audit PASS; no certifica instalación móvil |
| PWA del fork: SW/manifest/cache/IndexedDB | IMPLEMENTADO Y VERIFICADO | S10: shell compilado, snapshot y borrador conservados tras recarga offline en Chrome; upstream S4 aún sin integrar |
| UI/UX independiente ↔ runtime Java | PLANIFICADO | S3; no cliente RTC ni importación del adaptador actual en S4 |
| Repositorio backend y enlace histórico al Project | IMPLEMENTADO Y VERIFICADO | Snapshot S7: [repositorio privado](https://github.com/Josue1855/Gorilla-Escape-Backend), main `e76371097f4bcd881fa081211ec8d0d78e308c0b`; API confirma asociación al Project #1 |
| Backend jugadores: API y persistencia | IMPLEMENTADO Y VERIFICADO | S9: GET/POST /api/players, migración y SQLite real; 17 tests por plataforma Linux/Windows y harness PASS; PR #14 integrado a develop del backend; revisión remota S12 |
| Backend: stack aprobado para jugadores | IMPLEMENTADO Y VERIFICADO | S9: Boot4.1.1/Java21, SQLite JDBC3.53.2.1/Flyway12.4.0; sin JPA ni PostgreSQL |
| Cola durable/idempotencia de negocio | PROPUESTO | S10 tiene snapshot/borrador IndexedDB, no cola transaccional ni idempotencia; envío manual e intento incierto protegido |
| Minijuegos Unity del alpha de Saúl | IMPLEMENTADO SIN VERIFICAR | Rama Jungle Party: escenas y mecánicas preservadas; adaptador oficial implementado; importación/compilación Unity, controles y aceptación física pendientes |

Etiquetas permitidas: IMPLEMENTADO Y VERIFICADO (código + prueba/log con alcance), IMPLEMENTADO SIN VERIFICAR (código sin prueba concluyente), EN DESARROLLO (parcial comprobado), PLANIFICADO (decisión autorizada), PROPUESTO (por decidir), BLOQUEADO (impedimento conocido), DESCONOCIDO (evidencia insuficiente). No se asigna EN DESARROLLO a juegos sólo por existir issues abiertos.

Un ensayo físico anterior comunicado por el usuario no acredita la matriz del nuevo ejecutable. Un build, una API falsa, un ACK técnico o un `navigator.onLine=true` tampoco acreditan integración del producto.

El laboratorio React/SQLite del fork está implementado y verificado en escritorio: GET2 → POST201/SQL3 → offline/recarga → POST manual online201/SQL4. [Dossier y límites](../04-testing/PWA_TEST_LAB.md). Tres capturas literales de DevTools y pruebas físicas pendientes. El upstream de Hiram no se modificó; backend y laboratorio del fork ya están integrados en sus develop. [Reconciliación S12](SOURCE_INVENTORY.md#s12--reconciliación-del-pr-130-2026-10-26).
