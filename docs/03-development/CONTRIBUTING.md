# Colaboración entre proyectos

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Tomar una tarea verificable y coordinar cambios de contrato con productor y consumidor. En monorepo, [CONTRIBUTING raíz](../../CONTRIBUTING.md) se conserva como antecedente operativo; la separación en tres proyectos de S3 enmienda su antigua exigencia de mantener todo trabajo en un único repositorio.

Unity: gameplay, escenas, simulación y resultados. UI/UX: interacción, estados y presentación móvil. Backend: dominio persistente, API y transacciones cuando se identifique. Red/Shared: compatibilidad y lifecycle actual. QA/PO: evidencia y aceptación; los roles propuestos no conceden permisos GitHub.

Cada PR indica issue, fuente de decisión, alcance, contratos afectados, pruebas ejecutadas y limitaciones. No mezclar cambios de paquete/escena con documentación. Evitar editar simultáneamente una escena/prefab; conservar .meta y lockfiles. Actualizar [estado único](../00-product/DEVELOPMENT_STATUS.md) sólo con evidencia enlazada. No reutilizar assets/código de prototipos descartados.

Sin permiso push en S4, preparar propuesta y pedir al mantenedor una rama/PR o acceso de colaborador; no publicar automáticamente un fork ni copiar todo el backlog Unity. El backend no se asigna por deducir el responsable de Java técnico.
