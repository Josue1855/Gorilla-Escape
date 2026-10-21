# Estado verificable del producto

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Actualización de desarrollo 2026-10-20, rama `feature/gameplay-desafio-del-tronco`: se añade [Desafío del Tronco](../02-gameplay/LOG_BALANCE.md), quinto prototipo autorizado con náufrago humano y gorilas NPC. Su [validación](../02-gameplay/LOG_BALANCE_VALIDATION.md) se registra por separado; no cambia la aceptación pendiente de hardware ni convierte las cuatro disciplinas anteriores en implementadas.

Este es el registro vigente. Los resultados de tests se conservan en [evidencia](../04-testing/EVIDENCE_INDEX.md); no fueron reejecutados por esta auditoría salvo el build de la PWA independiente y las validaciones documentales.

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
| PWA UI/UX: build de producción | IMPLEMENTADO Y VERIFICADO | S4/S5: npm ci y npm run build terminan correctamente; no valida instalación PWA |
| PWA UI/UX: SW/manifest/cache | IMPLEMENTADO SIN VERIFICAR | S4: archivos presentes, pero build no entrega sw.js ni iconos; véase brechas |
| UI/UX independiente ↔ runtime Java | PLANIFICADO | S3; no cliente RTC ni importación del adaptador actual en S4 |
| Repositorio backend y enlace al Project | IMPLEMENTADO Y VERIFICADO | S7: [repositorio privado](https://github.com/Josue1855/Gorilla-Escape-Backend), main `e76371097f4bcd881fa081211ec8d0d78e308c0b`; API confirma asociación al Project #1 |
| PWA Backend: API y persistencia | PLANIFICADO | S7 sólo contiene README y dos documentos; no aplicación, endpoints, BD, tests ni CI |
| Backend: versiones, BD real, JPA y motor | DESCONOCIDO | Decisiones pendientes; no se fijan versiones sin manifiestos |
| Cola durable/idempotencia de negocio | PROPUESTO | Diseño documental; no IndexedDB encontrado en ninguno de los dos clientes |
| Cuatro minijuegos Unity | PLANIFICADO | S3; inventario S1 sin escenas/clases de minijuegos y PlayEnabled=false |

Etiquetas permitidas: IMPLEMENTADO Y VERIFICADO (código + prueba/log con alcance), IMPLEMENTADO SIN VERIFICAR (código sin prueba concluyente), EN DESARROLLO (parcial comprobado), PLANIFICADO (decisión autorizada), PROPUESTO (por decidir), BLOQUEADO (impedimento conocido), DESCONOCIDO (evidencia insuficiente). No se asigna EN DESARROLLO a juegos sólo por existir issues abiertos.

Un ensayo físico anterior comunicado por el usuario no acredita la matriz del nuevo ejecutable. Un build, una API falsa, un ACK técnico o un `navigator.onLine=true` tampoco acreditan integración del producto.
