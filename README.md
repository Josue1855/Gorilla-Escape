# Gorilla Escape: Gorilimpiadas

Guía de entrada revisada el 2026-10-20 contra `develop`. La revisión documental no cambia el alcance de la evidencia ni cierra pruebas pendientes.

**Base del equipo:** `develop`. El PO autoriza iniciar el desarrollo de juegos, PWA y cliente Flutter en ramas de tarea desde esta base, mientras se completa la validación física pendiente. [Cómo empezar y límites](docs/TEAM_START.md#arranque-del-equipo-autorizado-2026-10-16).

**Aceptación pendiente:** Fase 0/E0, F05, T019 y T020 siguen abiertos. Esta autorización de desarrollo paralelo no declara aprobadas las pruebas físicas ni una entrega jugable. El inicio de implementación está autorizado; el cierre y la publicación conservan sus gates.

Party game físico local previsto para 1–4 personas humanas. El alcance actual contempla Coconut Bowling, Gorilla Smash/High Striker, Coconut Throw/Shot Put y Jungle Slice; los teléfonos sirven de controles y la webcam PC aporta contexto. Los minijuegos aún no están implementados.

**Documentación vigente:** [índice único](docs/README.md), [estado verificable](docs/00-product/DEVELOPMENT_STATUS.md) y [organización de los tres proyectos](docs/00-product/REPOSITORIES.md). La PWA UI/UX independiente contiene una demo aún sin integrar; el [repositorio del backend](https://github.com/Josue1855/Gorilla-Escape-Backend) ya está creado, con base documental y API/BD pendientes de implementación.

## Arranque del equipo

[TEAM_START.md](docs/TEAM_START.md) explica herramientas, comandos, estructura, responsables y pruebas. La base compartida del equipo es develop. Las ramas de tarea parten de develop y sus PR vuelven a develop; main recibe sólo promociones de entregas validadas y aprobadas.

| Ruta | Tecnología | Estado |
|---|---|---|
| Unity/ | Unity 6000.3.23f1 + C# | Foundation Linux de desarrollo validada; PC de presentación pendiente |
| Server/ | Java 21 + Spring Boot 4.1.1 + Maven wrapper | Host técnico local, HTTPS, admisión, señalización RTC e IPC; validado Software/Lab |
| PWA/ | React 19.3.0 + Vite 8.3.2, Node 24 para desarrollo | Control RTC, permisos, captura de sensores y lifecycle; validado Software/Lab |
| Shared/ | Fixtures JSON y DTOs C# independientes del motor | Contrato v1 compartido entre consumidores |

El Product Owner aprobó cambiar ASP.NET Core por Java y añadir React: [DEC-002](docs/DECISIONS.md). Modular Monolith y Unity autoritativa se conservan. WebRTC DataChannel es el transporte móvil seleccionado por DEC-010; Java administra transporte/sesión técnica y el IPC permanece en 127.0.0.1. PhoneInput, CameraInput, Player Lock, alineación y Fusion infrastructure están implementados/validados Software/Lab. Los eventos móviles de laboratorio son sintéticos/replay; no validan sensores físicos. No hay minijuegos ni build jugable. El runtime técnico auditado no tiene cuentas ni base de datos; no requiere servicios cloud obligatorios. [Estado consolidado y deuda física](docs/00-product/DEVELOPMENT_STATUS.md).

Consulta también [CONTRIBUTING.md](CONTRIBUTING.md) para empezar una tarea y entregar un PR.

## Antecedentes y seguimiento histórico

- [Reglas](docs/DEVELOPMENT_RULES.md) y [especificación v4 enmendada](docs/Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md).
- [Progreso](docs/DEVELOPMENT_PROGRESS.md), [pruebas](docs/TEST_REPORT.md) y [decisiones](docs/DECISIONS.md).
- [Workflow de ramas/revisión](docs/WORKFLOW.md) y [arquitectura histórica del monorepo](docs/MONOREPO_PLAN.md).
- [Investigación técnica](docs/TECHNOLOGY_RESEARCH.md): las recomendaciones .NET anteriores quedaron sustituidas por DEC-002.
- [Project](https://github.com/users/Josue1855/projects/1), [configuración](docs/github/PROJECT_SETUP.md) y [backlog](docs/github/BACKLOG_STRUCTURE.md).

## Fases y calidad

Fase 0 (base/Spike) → Fase 1 (Gorilla Smash) → Fase 2 (sistemas reutilizables) → Fase 3 (otros minijuegos) → Fase 4 (torneo) → Fase 5 (polish) → Fase 6 (QA/release). Sólo se trabaja el alcance solicitado; no se inicia automáticamente otra fase.

CI ejecuta validación documental y build/tests Java/React. No se presenta como validación Unity. main/develop requieren PR y los checks management-validation y java-react-foundation. Ningún PR o Issue se cierra automáticamente por este arranque.
