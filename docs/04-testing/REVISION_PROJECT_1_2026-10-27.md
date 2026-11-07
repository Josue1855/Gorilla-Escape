# Revisión de Project 1 — 27 de octubre de 2026

Se contrastó el [Project 1](https://github.com/users/Josue1855/projects/1) y sus issues abiertos con la rama local `feature/saul-jungle-party-alpha`, base `40accec`, con cambios sin confirmar. Los resultados corresponden a ese árbol de trabajo, no certifican `main` ni una entrega integrada. La lista del tablero es jerárquica: los hijos pueden estar ocultos bajo cada épica.

## Resultado de cierre

No se identificó una fase abierta que cumpla todos sus criterios de aceptación y su definición de terminado. No se cambiaron estados ni se cerraron issues. Las cinco features #9, #13, #18, #23 y #28 de fase 0 ya aparecen cerradas; su aceptación anterior tiene el alcance y las excepciones descritas en `docs/PHASE0_FINAL_EXIT_REPORT.md`.

| Fase | Decisión | Evidencia y trabajo pendiente |
| --- | --- | --- |
| 0 — Base, #1 y #31 | Mantener abiertas | #33 requiere cuatro teléfonos, 20 conexiones y reconexiones y medidas físicas; #34 requiere una PC limpia. Las pruebas de software no sustituyen esas pruebas. |
| 1 — Slice Smash, #2 y #35–#39 | Mantener abiertas | Acreditar reglas, tutorial, bucle y recuperación; #39 exige 50 gestos, 100 movimientos pequeños, latencia, jugadores externos y aceptación. |
| 2 — Sistemas, #3 y #40–#48 | Mantener abiertas | Hay pruebas de entrada y fusión, pero faltan evidencias integradas de los criterios completos de sesión, calibración, puntuación y persistencia. #42 exige calibración física <30 s; #44 exige comparación de jitter y retraso. |
| 3 — Minijuegos, #4 y #49–#64 | Mantener abiertas | Verificar cada criterio y jugar con entrada real. #58 exige tres rondas; el controlador actual contiene una tabla de diez frames. #62 especifica 60 segundos y reglas de puntuación diferentes de la mecánica descrita para Coconut Slice; no asumir equivalencia. |
| 4 — Torneo, #5 y #65–#71, #104 | Mantener abiertas | Acreditar torneo de cuatro disciplinas con jugadores, puntos, desempate, campeón y reinicio; UI móvil, accesibilidad y control alternativo requieren sus criterios completos. |
| 5 — Pulido, #6 y #72–#75 | Mantener abiertas | Faltan mediciones de rendimiento y balance y experiencia de jugadores externos; una implementación visual no acredita game feel. |
| 6 — QA y entrega, #7 y #76–#82 | Mantener abiertas | Falta matriz de aceptación completa, resolución de defectos, freeze y build final reproducible desde un tag aprobado. #127 sigue abierto por CI WebRTC. |
| Futuro, #8 y #83–#88, #106 | Mantener abiertas | Opciones posteriores y deuda de validación humana no se convierten en aceptación del MVP por pruebas simuladas. |

El tablero académico menciona Smash, Throw, Bowling y Slice, mientras que el menú de la rama actual ofrece Bowling, Boxing, Baseball, Drums, Coconut Slice, Whack-a-Mole y Log Balance. El informe histórico menciona Archery para #53–#56, pero esos issues hoy figuran como Gorilla Smash: reconciliar decisiones y criterios antes de dar por equivalentes los juegos o editar el alcance.

## Pruebas ejecutadas en esta revisión

- PWA: `npm test`, 88/88; `npm run build`, PASS.
- Contratos C# independientes de Unity: `dotnet run --project tools/integration/jungle-phase0/JunglePhase0.Checks.csproj`, 104/104. No son pruebas de Unity ni de hardware.
- Servidor: `./mvnw -q -Dtest=ConnectivityTest,JoinQrPocTest test`, 28/28.
- Router: `node --test tools/cloudflare-worker/tests/router.test.mjs`, 2/2.
- `python3 tools/validate_foundation.py`: PASS, incluidos 426 GUID de Unity únicos.

La prueba previa de esta sesión `PublicRoomRtcLiveTest` pasó 1/1 con túnel real y carga de fuentes, pero no se volvió a ejecutar durante esta revisión ni acredita sensores físicos, cámara o una partida completa. No se ejecutaron hoy pruebas físicas, Unity PlayMode, build de Unity, aceptación por jugadores externos ni CI remoto. No se han revisado individualmente todos los criterios de todas las tareas: la tabla señala las razones suficientes para no cerrar las épicas y los siguientes controles prioritarios.

## Siguiente paso

Confirmar el catálogo y las reglas vigentes del MVP frente al tablero, integrar y revisar los cambios pendientes, y ejecutar las matrices físicas de #33/#34 y de los juegos aplicables. Registrar fecha, versión, commit, dispositivo, procedimiento, resultados y límites antes de cerrar cada tarea.
