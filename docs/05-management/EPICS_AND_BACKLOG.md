# Backlog propio propuesto para PWA UI/UX

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

**PROPUESTO**: identificadores locales de planificación, no números de issues GitHub. Sin asignación personal ni Sprint/fecha inventados. Todos arrancarán Backlog al crearse; Ready exige dependencias resueltas, contrato y aceptación revisados. Responsables abajo son roles propuestos. No se crean tareas de implementación en esta auditoría.

| Epic local | Objetivo y tareas candidatas |
|---|---|
| E-REACT | Fundamento React: UI-00 inventario/build y UI-01 adaptador |
| E-CONTROL | Controles y navegación: UI-02 disciplinas, UI-03 humanos/accesibilidad |
| E-SESSION | Sesiones/jugadores: UI-04 identidad/estado |
| E-LAN | Integración LAN: UI-05 conexión/lifecycle |
| E-REST | REST real: API-01 contrato y API-02 listado |
| E-SW | Service Worker: OFF-01 empaquetado/cache |
| E-STORAGE | Almacenamiento offline: OFF-02 cola autorizada |
| E-SYNC | Sincronización: OFF-03 reintentos/idempotencia |
| E-QA | QA funcional: QA-01 fallos/contratos |
| E-LAB | Entregables académicos: LAB-01..10 del laboratorio, ejecución QA-02 |
| E-DOC | Documentación propia: DOC-01 README/trazabilidad |

Cada fila siguiente aporta objetivo, alcance, dependencias, aceptación, evidencia, rol y prioridad. Al crear una issue se traslada su fila y se añade enlace al epic real, sin inventar URLs.

| ID / objetivo y alcance | Depende de | Aceptación verificable / evidencia exigida | Rol propuesto / prioridad |
|---|---|---|---|
| UI-00 Reproducir baseline UI/UX; inventariar dependencias y analizar avisos npm sin upgrade automático | Acceso S4 | npm ci/build reproducibles; registrar lock/SHA y riesgos; dist inventariado | UI/UX, P0 |
| UI-01 Adaptar UI al cliente técnico existente; decidir fuente de entrega | UI-00, acuerdo Red/Unity | Un intento produce input del slot real; cero listeners/canales extra tras navegación; tests y traza sanitizada | UI/UX + Red, P0 |
| UI-02 Diseñar cuatro controles por disciplina y feedback real | UI-01, reglas Unity | Apuntado/potencia/efecto; golpe; ángulo/lanzamiento; trayectoria Slice revisados por productor/consumidor; grabación de prueba o capturas sin datos privados | UI/UX + Unity, P1 |
| UI-03 Corregir humanos/NPC y revisar accesibilidad móvil | S3, arte aprobado | No confunde avatar NPC con jugador; teclado/foco/contraste/tamaño y permisos revisados en dispositivos objetivo; reporte | UI/UX + Arte/QA, P1 |
| UI-04 Identidad y estado de sesión | UI-01, contrato de identidad | No usa ID JSONPlaceholder como PlayerId; preserva slot/epoch por reconnect; pruebas de rechazo cruzado | UI/UX + Red, P0 |
| UI-05 Conectar por LAN y cleanup | UI-01/UI-04, T019 coordinado | JOIN real, permisos denegados, suspensión, desconexión/reconnect manual y sin emisión oculta; evidencia física separada de emulador | UI/UX + QA, P0 |
| API-01 Acordar contrato REST con backend confirmado | S9 jugadores aprobado/implementado; autorización y contrato de competición pendientes | Métodos/DTO/roles/errores/idempotencia aprobados y versionados; no Swagger ficticio | Backend + UI/UX, P0 |
| API-02 Reemplazar listado demo por GET de participantes reales | S9/S10 jugadores verificados en fork; incorporación upstream pendiente | BD→respuesta→lista coinciden; carga/error/vacío y backend inaccesible; LAB-03/05/06 | UI/UX + Backend, P1 |
| OFF-01 Empaquetar SW/manifest/iconos y cache del dist | UI-00, fuente de entrega | Build contiene recursos del manifest y SW; precache sólo emitidos; segunda carga sin WAN; evidencia Application/Network | UI/UX, P0 |
| OFF-02 Diseñar/implementar cola IndexedDB sólo de operaciones autorizadas | API-01, ownership/outbox aprobado | Pendientes sobreviven recarga; límites/migración/borrado definidos; no tokens ni resultados falsos; tests/storage inspectado | UI/UX + Backend/Unity, P1 |
| OFF-03 Reintentar y confirmar sin duplicados | OFF-02, idempotencia backend | Misma clave con concurrencia escribe una vez; conflicto distinto payload visible; OFF-02 y LAB-09 | UI/UX + Backend, P1 |
| QA-01 Suite de UI/transporte/errores con alcance explícito | UI-01/04, contratos | Estado red/backend separado; fixtures y asserts de cleanup/identidad; fallos conservados | QA + UI/UX, P1 |
| QA-02 Ejecutar LAB-01..10 y escenarios A/B | API-02, OFF-01; OFF-02/03 si aprobados | Cada requisito con ambiente/pasos/esperado/observado/evidencia auténtica; GET/POST/BD/logs/JVM y DevTools | QA + tres equipos, P1 |
| DOC-01 README propio y referencias cruzadas | Baselines confirmados | Setup/versiones/estado real; demo delimitada, diagramas/enlaces válidos y evidencia fechada | UI/UX, P1 |

Trabajo paralelo Unity propuesto: framework mínimo, reglas High Striker/Shot Put/Bowling/Slice y salida autoritativa; usar issues reales existentes como base antes de crear duplicados. Backend S9: repositorio, motor SQLite/JDBC, jugadores GET/POST y scripts ya implementados. Sigue propuesto el modelo de competición, roles/publicador e integración de recibos. Los Projects respectivos conservarán tareas propias con enlaces cruzados, no copias con estados independientes.

Definition of Done propuesta: PR revisado e integrado por flujo normal, pruebas requeridas y evidencia ligada al SHA; cierre de una tarea técnica no cierra automáticamente aceptación de producto ni gates físicos.

## Avance posterior S9/S10

API-01/02 para jugadores y empaquetado shell ya implementados/probados en el fork (PR #1), no incorporados al upstream. OFF-02 sólo cubre snapshot/borrador; OFF-03 idempotente sigue propuesto. La tabla anterior es backlog de producto, no estados remotos Done. Backend se sigue en Project #2, issues #3/#4/#10 parciales y #13 en revisión. Ver [laboratorio](../04-testing/PWA_TEST_LAB.md).
