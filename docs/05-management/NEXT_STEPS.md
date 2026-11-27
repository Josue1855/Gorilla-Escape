# Próximos pasos operativos

Última sincronización: 2026-10-16. Fuentes S9/S10 y evidencia fechada; [inventario](../00-product/SOURCE_INVENTORY.md). Trabajo documentado en ramas/PR, no integrado automáticamente.

1. Usar los develop actualizados del backend (PR #14 integrado) y del fork PWA (PR #7 integrado). Revisar la sincronización documental en [PR #130](https://github.com/Josue1855/Gorilla-Escape/pull/130) antes de integrarla al monorepo. main no recibe trabajo de tarea.
2. Mantenedor upstream UI/UX: habilitar develop y revisar incorporación desde el fork. No se necesita asumir Write para abrir un PR entre forks cuando exista la base correcta.
3. Completar tres capturas literales DevTools Network/Application/Console siguiendo [laboratorio](../04-testing/PWA_TEST_LAB.md); SQL, GET/POST/UI y offline emulado ya ejecutados. Garantizar acceso del docente al backend privado o paquete de evidencias.
4. Ejecutar T019 final y T020 en PC separada; escenarios físicos WAN/LAN/backend siguen pendientes. No sustituirlos por emulación Chrome.
5. Acordar frontend de entrega y adaptador técnico preservando sensores/lifecycle/Shared/IPC. El laboratorio no reemplaza el mando conectado.
6. Unity/PO: framework y cuatro disciplinas en tareas independientes, decisiones de reglas y scoring. Backend: publicación autorizada, mapeo RTC/UUID, sesiones/resultados, idempotencia y outbox antes de automatizar reintentos.
7. Completar empaquetado autocontenido/JRE, supervisión auditada, backup/restore, retención, licencias y aceptación final Windows/Linux. No añadir instalaciones SQL manuales para el usuario.

El stack SQLite/JDBC para jugadores ya fue aprobado e implementado; no se repite como bloqueo abierto. Preguntas aún abiertas: propietario del Project UI/UX, credencial del publicador, frontend empaquetado, reglas faltantes, equipos/navegadores objetivo y retención. Sin fechas/assignments inventados.
