# Laboratorio PWA, REST y BD

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Estado del flujo real PWA UI/UX → backend → BD: **BLOQUEADO** por backend sin implementación y falta de integración. Todos los resultados de ejecución del laboratorio: **PENDIENTE DE EJECUCIÓN**. Los códigos siguientes trazan el encargo PO S3; no reemplazan una rúbrica académica externa que no fue proporcionada.

| ID / entregable | Procedimiento requerido y esperado | Evidencia disponible / faltante |
|---|---|---|
| LAB-01 Repositorio final | README con commits, diagrama AS-IS/TO-BE y enlaces de los tres proyectos | S1/S4/S7 y esta documentación; tres repositorios enlazados desde backend; falta README cruzado en S4 |
| LAB-02 Scripts reproducibles | Confirmar motor; preparar esquema/datos ficticios y limpieza idempotente en entorno aislado; segunda ejecución coherente | No scripts oficiales BD confirmados; nombres propuestos `scripts/lab/prepare`, `seed`, `cleanup` con extensión según motor |
| LAB-03 BD antes/después | Guardar SELECT de conteos/filas/constraints antes y tras POST; IDs coincidentes | Falta BD real, esquema y consultas ejecutadas |
| LAB-04 Logs GET/POST y JVM | Capturar requestId, método/ruta/HTTP, duración con unidad y memoria JVM antes/durante/después en ambiente declarado | Sin aplicación ni logs/métricas backend; no mezclar HTTP 201 con filas insertadas o SQLSTATE |
| LAB-05 GET renderizado | Preparar registros; GET de participantes reales; comparar filas de BD, respuesta y lista de UI | S4 tiene sólo demo JSONPlaceholder; falta GET del dominio y captura |
| LAB-06 Network | Capturar método/URL sanitizada/status, timings, payload, tamaño y headers de GET y POST | Faltan capturas auténticas y mediciones |
| LAB-07 Application | Manifest/SW activo/scope y recursos cacheados; IndexedDB/cola sólo cuando exista; reconexión confirma operación | Código fuente SW/manifest; S5 detecta dist incompleto; no IndexedDB ni prueba de cola |
| LAB-08 Console | Registrar errores/advertencias durante carga, fallo, reintento y sincronización; no esconder fallos | Sin ejecución browser en este encargo |
| LAB-09 POST único | Crear operación autorizada; repetir misma clave en secuencia y concurrencia; una fila; payload distinto→conflicto sin modificación | Diseño candidato, falta API/constraints/test ejecutado |
| LAB-10 Ambiente y fallos | Identificar versiones/commit/hardware, datos ficticios, pasos/esperado/observado; preservar intentos fallidos | Inventario S1/S4; ambiente backend y ejecución académica faltan |

Escenario **A**: cortar sólo Internet WAN manteniendo Wi-Fi/LAN y backend local. Verificar GET/POST locales y control WebRTC; registrar disponibilidad por servicio, no sólo bandera online. Primera visita sin recursos previamente instalados es un caso separado, no un éxito inferido.

Escenario **B**: mantener LAN y detener exclusivamente el backend de prueba autorizado. GET muestra indisponibilidad; POST no presenta éxito hasta COMMIT confirmado. Si existe cola, registrar pendiente, reiniciar backend, reenviar la misma clave, confirmar una sola fila y vaciar sólo la operación confirmada. Si no existe cola, registrar limitación y fallo esperado de disponibilidad sin inventar sincronización.

Ficha por ejecución: runId, fecha/zona, repo/branch/SHA, SO/hardware/navegador, servidor/motor/versiones, origen, condición WAN/LAN/backend, datos iniciales, pasos, esperado, observado **PENDIENTE DE EJECUCIÓN**, resultado, evidencia, incidencia y responsable real. No rellenar duraciones, memoria ni capturas con ejemplos simulados. Sanitizar headers sin autorización/cookies y no exportar un HAR con credenciales.

Capturas planificadas: listado, BD antes/después, consola Spring, Network GET/POST, Application manifest/SW/cache/IndexedDB si existe, Console fallo/recuperación. Logs estructurados y consultas complementan capturas; no afirmar ausencia de errores sin revisar la ventana completa. La ejecución y los scripts vivirán en sus repositorios oficiales, mediante tareas separadas.
