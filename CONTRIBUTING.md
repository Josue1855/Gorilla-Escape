# Trabajar en Gorilla Escape

1. Leer el [índice vigente](docs/README.md), el [estado verificable](docs/00-product/DEVELOPMENT_STATUS.md) y las [decisiones actuales](docs/00-product/ARCHITECTURE_DECISIONS.md). Consultar reglas y especificación histórica con sus enmiendas posteriores.
2. Seguir [TEAM_START](docs/TEAM_START.md) para herramientas, clonado, build y tests. Usar develop como base compartida y origen de nuevas ramas de tarea.
3. Seleccionar una Issue con alcance de la fase actual, aceptación clara y dependencias conocidas. Coordinar responsable y archivos compartidos antes de empezar.
4. Crear una rama pequeña feature/mobile-..., feature/gameplay-..., feature/camera-..., feature/ui-..., fix/... o docs/.... No trabajar directamente en main/develop. El inicio de juegos/clientes está autorizado; su aceptación física y la publicación conservan los criterios pendientes.
5. Validar los componentes y consumidores de contratos afectados, revisar diff y actualizar progreso/tests/documentación. Registrar limitaciones reales de Unity o hardware.
6. Abrir PR con Issue relacionada, alcance, evidencia y riesgos. Revisión de otro integrante y checks aplicables antes de integrar. Promoción develop → main requiere aceptación de entrega.

El repositorio público se puede clonar sin invitación. Para publicar una rama en este repositorio se requiere permiso de escritura; sin él se puede trabajar localmente y proponer mediante un fork. Los cargos del equipo no otorgan permisos GitHub automáticamente. Los usernames/invitaciones aún deben acordarse; no compartir credenciales.

Java/React compilan y sus pruebas de base están disponibles. Unity requiere licencia/importación/build para aceptar sus cambios; los validadores de metadatos no sustituyen al editor. La cámara y los teléfonos requieren hardware real y no se marcan aprobados mediante mocks.

La organización vigente distingue Unity, PWA UI/UX y PWA Backend; consultar [colaboración entre proyectos](docs/03-development/CONTRIBUTING.md) e [índice único](docs/README.md). El monorepo conserva el runtime técnico existente. Preservar .meta, lockfiles y configuración; no publicar outputs de build ni assets comerciales sin derechos. La arquitectura vigente está en [SYSTEM_CONTEXT](docs/01-architecture/SYSTEM_CONTEXT.md); docs/MONOREPO_PLAN.md conserva antecedentes.
