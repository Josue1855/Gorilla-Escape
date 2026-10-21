# Documentación de Gorilla Escape: Gorilimpiadas

Última revisión de navegación y arranque: 2026-10-20. Índice único de la documentación vigente. El código y la evidencia fechada prevalecen sobre descripciones históricas. Se auditaron Unity/UI y se creó/verificó el tercer repositorio oficial del backend; su runtime y BD siguen sin implementar.

Comenzar por [estado verificable](00-product/DEVELOPMENT_STATUS.md), [informe de auditoría](00-product/AUDIT_REPORT.md) y [siguientes acciones](05-management/NEXT_STEPS.md). Las etiquetas, límites de verificación y decisiones están centralizados; los documentos especializados contienen su detalle único.

## Para comenzar una tarea

1. Leer el estado verificable y las decisiones vigentes enlazados arriba.
2. Seguir [TEAM_START](TEAM_START.md) y [preparación y ejecución](03-development/SETUP_AND_RUN.md).
3. Consultar la arquitectura y la ficha del juego o componente que se modificará.
4. Crear una rama desde `develop` y entregar un PR hacia `develop`; `main` recibe promociones validadas.

El inicio de juegos y clientes está autorizado. La aceptación física de Fase 0 continúa pendiente. Las fechas de los informes describen su evidencia original; esta revisión de guías no los convierte en pruebas nuevas.

## Producto, fuentes y decisiones

- [Registro vigente de decisiones](00-product/ARCHITECTURE_DECISIONS.md)
- [Informe de auditoría y control de cambios](00-product/AUDIT_REPORT.md)
- [Estado verificable del producto](00-product/DEVELOPMENT_STATUS.md)
- [Producto y lectura inicial](00-product/OVERVIEW.md)
- [Repositorios y acceso](00-product/REPOSITORIES.md)
- [Secuencia de entregas](00-product/ROADMAP.md)
- [Alcance y precedencia](00-product/SCOPE.md)
- [Inventario y trazabilidad de fuentes](00-product/SOURCE_INVENTORY.md)

## Arquitectura y contratos

- [API real y contratos de negocio propuestos](01-architecture/API_CONTRACTS.md)
- [Modelo relacional propuesto](01-architecture/DATABASE_DESIGN.md)
- [Red, contratos Shared y supervisión](01-architecture/NETWORK_AND_IPC.md)
- [Offline LAN y transacciones](01-architecture/OFFLINE_STRATEGY.md)
- [Backend de persistencia: frontera y desconocidos](01-architecture/PWA_BACKEND.md)
- [PWA UI/UX y cliente técnico](01-architecture/PWA_UI_UX.md)
- [Arquitectura actual y objetivo](01-architecture/SYSTEM_CONTEXT.md)
- [Unity y autoridad del juego](01-architecture/UNITY.md)

## Gameplay planificado

- [Coconut Bowling](02-gameplay/COCONUT_BOWLING.md)
- [Coconut Throw / Shot Put](02-gameplay/COCONUT_THROW.md)
- [Gorilla Smash / High Striker](02-gameplay/GORILLA_SMASH.md)
- [Entradas, humanos y gorilas](02-gameplay/INPUT_AND_CHARACTERS.md)
- [Jungle Slice](02-gameplay/JUNGLE_SLICE.md)
- [Marco común de minijuegos](02-gameplay/MINIGAME_FRAMEWORK.md)

## Desarrollo e integración

- [Convenciones que preservan los límites](03-development/CODING_CONVENTIONS.md)
- [Colaboración entre proyectos](03-development/CONTRIBUTING.md)
- [Ramas, PR y validaciones](03-development/GIT_WORKFLOW.md)
- [Plan de integración de los tres equipos](03-development/INTEGRATION_PLAN.md)
- [Preparación y ejecución verificable](03-development/SETUP_AND_RUN.md)

## Pruebas y laboratorio

- [Índice de evidencias](04-testing/EVIDENCE_INDEX.md)
- [Laboratorio PWA, REST y BD](04-testing/PWA_TEST_LAB.md)
- [Resultados de auditoría y discrepancias](04-testing/RESULTS_AND_GAPS.md)
- [Matriz de casos requeridos](04-testing/TEST_CASE_MATRIX.md)
- [Estrategia de pruebas y aceptación](04-testing/TEST_STRATEGY.md)

## Gestión independiente

- [Dependencias entre equipos](05-management/DEPENDENCIES.md)
- [Backlog propio propuesto para PWA UI/UX](05-management/EPICS_AND_BACKLOG.md)
- [Próximos pasos operativos](05-management/NEXT_STEPS.md)
- [Auditoría y propuesta de Projects V2](05-management/PROJECTS_AND_ISSUES.md)
- [Riesgos y mitigación propuesta](05-management/RISKS.md)

## Historia preservada

Los documentos anteriores permanecen en sus rutas originales; no deben interpretarse como estado actual. [Inventario completo con hashes](00-product/SOURCE_INVENTORY.md) incluye todos los Markdown de la base, contratos y READMEs. [Historial de decisiones](DECISIONS.md), [pruebas](TEST_REPORT.md), [progreso](DEVELOPMENT_PROGRESS.md), [v4](Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md) y [configuración histórica de GitHub](github/PROJECT_SETUP.md) conservan trazabilidad. El nuevo alcance define cuatro disciplinas y tres proyectos; no se importan ni documentan migraciones de prototipos descartados.
