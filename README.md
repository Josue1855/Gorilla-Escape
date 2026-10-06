# Gorilla Escape: Gorilimpiadas

Party game físico local para 1–4 personas. La PC arbitra cinco pruebas; los teléfonos sirven de controles y una webcam aporta contexto corporal. El gameplay previsto funciona sin Internet.

## Arranque del equipo

[TEAM_START.md](docs/TEAM_START.md) explica herramientas, comandos, estructura, responsables y pruebas. La base compartida del equipo es develop. Las ramas de tarea parten de develop y las entregas revisadas llegan a main.

| Ruta | Tecnología | Estado |
|---|---|---|
| Unity/ | Unity 6000.3.23f1 + C# | Foundation Linux de desarrollo validada; PC de presentación pendiente |
| Server/ | Java 21 + Spring Boot 4.1.1 + Maven wrapper | Host local, health, DTOs y frontend empaquetado; build/tests PASS |
| PWA/ | React 19.3.0 + Vite 8.3.2, Node 24 para desarrollo | Shell, health y caché de interfaz; build/tests PASS |
| Shared/ | Fixtures JSON y DTOs C# independientes del motor | Contrato v1 compartido entre consumidores |

El Product Owner aprobó cambiar ASP.NET Core por Java y añadir React: [DEC-002](docs/DECISIONS.md). Modular Monolith, PC autoritativa, WebSocket/JSON y MediaPipe/OpenCV siguen vigentes. No hay base de datos, cuentas ni servicios cloud obligatorios. WebSocket del juego, sensores, cámara y minijuegos todavía no están implementados. No hay build jugable ni métricas físicas verificadas.

Consulta también [CONTRIBUTING.md](CONTRIBUTING.md) para empezar una tarea y entregar un PR.

## Documentación y seguimiento

- [Reglas](docs/DEVELOPMENT_RULES.md) y [especificación v4 enmendada](docs/Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md).
- [Progreso](docs/DEVELOPMENT_PROGRESS.md), [pruebas](docs/TEST_REPORT.md) y [decisiones](docs/DECISIONS.md).
- [Workflow de ramas/revisión](docs/WORKFLOW.md) y [arquitectura vigente del monorepo](docs/MONOREPO_PLAN.md).
- [Investigación técnica](docs/TECHNOLOGY_RESEARCH.md): las recomendaciones .NET anteriores quedaron sustituidas por DEC-002.
- [Project](https://github.com/users/Josue1855/projects/1), [configuración](docs/github/PROJECT_SETUP.md) y [backlog](docs/github/BACKLOG_STRUCTURE.md).

## Fases y calidad

Fase 0 (base/Spike) → Fase 1 (Gorilla Smash) → Fase 2 (sistemas reutilizables) → Fase 3 (otros minijuegos) → Fase 4 (torneo) → Fase 5 (polish) → Fase 6 (QA/release). Sólo se trabaja el alcance solicitado; no se inicia automáticamente otra fase.

CI ejecuta validación documental y build/tests Java/React. No se presenta como validación Unity. main/develop requieren PR aprobado y los checks management-validation y java-react-foundation. Ningún PR o Issue se cierra automáticamente por este arranque.
