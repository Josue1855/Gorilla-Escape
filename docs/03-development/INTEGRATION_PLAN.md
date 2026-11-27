# Plan de integración de los tres equipos

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Todo el plan es PROPUESTO excepto las responsabilidades y alcance autorizados S3.

1. UI/UX y red acuerdan la fuente de entrega: frontend independiente vs ensamblado desde monorepo. Comparar manifests/locks; no copiar dependencias ni sustituir el cliente funcional por la demo.
2. Definir adaptador de UI sobre el cliente técnico ya existente: asociación de slots, permisos, conexión, error, reconexión manual y teardown. Un solo propietario de sensores/timers/canales por dispositivo. Probar fuera del navegador de escritorio.
3. Unity define estados/intenciones/eventos por minijuego; Shared cambia sólo cuando el contrato lo requiere, con versión, fixtures y pruebas cruzadas. Touch raw no equivale a trayectoria deportiva ya integrada.
4. Usar el repositorio backend creado S7 y aprobar modelo/REST/autorización. Mantener sesión RTC separada de sesión de competición persistente. Definir mapeo a UUID del humano y rulesVersion.
5. Añadir, en otra tarea, publicador de resultados autorizado con outbox e idempotencia. La indisponibilidad de BD no debe duplicar resultados ni producir una segunda autoridad de puntuación.
6. Ejecutar laboratorio GET/render/POST/BD, fallos/replays y dos escenarios offline. Sólo después enlazar evidencias y actualizar el estado.

Gates: contrato revisado por ambos lados, build de cada consumidor, pruebas de lifecycle sin listeners duplicados, rechazo de resultado no autorizado y de participante ajeno, reintentos concurrentes sin duplicación, resultados de Unity coincidentes con consulta. Dependencias/entregables externos se enlazan por URLs reales tras crearlos; no se simulan números de issues.
