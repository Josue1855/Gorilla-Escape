# Estrategia de pruebas y aceptación

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Separar pruebas de contratos/lifecycle, reglas Unity, integración UI/UX, persistencia real y hardware. La compilación no demuestra comportamiento integrado; un emulador no demuestra frecuencia de sensor físico y un GET health no demuestra consulta SQL.

Nivel actual: S2 conserva validación Software/Lab del monorepo. S5 aporta un build de S4, sin navegador. Este encargo ejecuta revisión de fuentes y validaciones documentales, sin repetir la batería Unity ni alterar tests. La CI normal del PR ofrece una nueva comprobación del software sin cambios de producción.

La [matriz](TEST_CASE_MATRIX.md) define pruebas requeridas por alcance. [Laboratorio](PWA_TEST_LAB.md) cubre los diez entregables; [índice](EVIDENCE_INDEX.md) distingue registros históricos y ejecuciones de esta auditoría. No hay capturas/logs nuevos de BD ni GET/POST de negocio.

T019/#33, T020/#34, F05/#31 y E0/#1 continúan abiertos. QA física debe identificar ejecutable/commit, hardware, SO/navegador, LAN, N, fallos, métricas y cleanup. Cualquier evidencia incompleta se registra PENDIENTE DE EJECUCIÓN; no cerrar por descripción de IA.
