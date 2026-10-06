# Trabajar en Gorilla Escape

1. Leer las [reglas de desarrollo](docs/DEVELOPMENT_RULES.md) y la [especificación v4 enmendada](docs/Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md).
2. Seguir [TEAM_START](docs/TEAM_START.md) para herramientas, clonado, build y tests. Usar develop como base compartida y origen de nuevas ramas de tarea.
3. Seleccionar una Issue con alcance de la fase actual, aceptación clara y dependencias conocidas. Coordinar responsable y archivos compartidos antes de empezar.
4. Crear una rama pequeña feature/mobile-..., feature/gameplay-..., feature/camera-..., feature/ui-..., fix/... o docs/.... No trabajar directamente en main/develop ni avanzar una fase sin solicitud.
5. Validar los componentes y consumidores de contratos afectados, revisar diff y actualizar progreso/tests/documentación. Registrar limitaciones reales de Unity o hardware.
6. Abrir PR con Issue relacionada, alcance, evidencia y riesgos. Revisión de otro integrante y checks aplicables antes de integrar. Promoción develop → main requiere aceptación de entrega.

El repositorio público se puede clonar sin invitación. Para publicar una rama en este repositorio se requiere permiso de escritura; sin él se puede trabajar localmente y proponer mediante un fork. Los cargos del equipo no otorgan permisos GitHub automáticamente. Los usernames/invitaciones aún deben acordarse; no compartir credenciales.

Java/React compilan y sus pruebas de base están disponibles. Unity requiere licencia/importación/build para aceptar sus cambios; los validadores de metadatos no sustituyen al editor. La cámara y los teléfonos requieren hardware real y no se marcan aprobados mediante mocks.

Todo proyecto propio permanece en este monorepo. Preservar .meta, lockfiles y configuración; no publicar outputs de build ni assets comerciales sin derechos. La estructura y las dependencias están en docs/MONOREPO_PLAN.md.
