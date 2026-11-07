# Qué está terminado y qué avanzar sin equipo físico

Revisión: 30 de octubre de 2026. Rama local `feature/saul-jungle-party-alpha`, HEAD `c894bfda5b588d729dcaa9da27128299c21bc433`, con numerosos cambios staged, unstaged y archivos nuevos. Esta revisión analiza el árbol local; no certifica develop ni una release. No se modificaron issues, estados del Project ni requisitos.

## Fuentes y límites

- Se consultaron en vivo las dos páginas de issues del repositorio mediante el conector GitHub: **99 issues, 27 cerrados y 72 abiertos**. Estos conteos son del repositorio; no se presentan como conteos de tarjetas del Project.
- El [Project 1](https://github.com/users/Josue1855/projects/1) devolvió 404 en el navegador sin sesión. El conector disponible permite consultar issues, PR y Actions, pero no los campos de Projects. No se verificaron columnas, tarjetas borrador, asignaciones ni Status del tablero.
- [PR #131](https://github.com/Josue1855/Gorilla-Escape/pull/131): abierto, sin merge, hacia develop, HEAD c894bfd. Foundation, Management, dependency audit y cross-repository integration están verdes para ese commit. No validan los cambios locales posteriores.
- Se leyeron criterios actuales de las tareas, controladores Unity, límites de entrada, presentación PWA, workflows y documentación de aceptación. No se ejecutó hoy Unity EditMode/PlayMode, build Unity ni pruebas físicas.

## Trabajo que ya puede reconocerse como terminado

| Issues cerrados | Resultado reconocido | Límite |
| --- | --- | --- |
| #9–#18 | Decisiones, inventario, baseline, monorepo, host Java, contratos y CI inicial | Base técnica, no aceptación del videojuego actual |
| #19–#27 | Contexto seguro/QR, shell PWA, sensores, offline/lifecycle, RTC, lobby, PhoneInput y reconexión | Alcance de aceptación Software/Lab documentado en cada issue |
| #28–#30 | Cámara, CameraInput, Player Lock y fusión de infraestructura | La deuda humana diferida sigue en #106 |
| #32 | Instrumentación de métricas | No demuestra por sí misma que se cumplan objetivos físicos |
| #111, #114, #115, #116 | Menú/centro QA, conectividad AUTO, motion sin stop-and-wait y arranque de producto | #132 conserva endurecimiento público y aceptación posterior |

Son 27 issues contando features y tareas: no son 27 entregables independientes ni un porcentaje de avance. No volver a implementarlos ni reabrirlos únicamente porque la fase padre sigue abierta. E0/#1 y F05/#31 siguen abiertos por aceptación pendiente.

## Candidatos de software: trabajo concreto para llegar al cierre

La existencia de código no basta para cerrar. Cada fila requiere criterios completos, pruebas, evidencia ligada a una revisión, PR revisado e integración. Las dependencias abiertas se conservan; implementar en paralelo no las satisface automáticamente.

| Prioridad | Issues | Lo observado | Trabajo realizable sin teléfonos/cámara físicos |
| --- | --- | --- | --- |
| 1 | #127, fallo CI WebRTC | Hay varias ejecuciones Foundation verdes, pero el workflow prepara PulseAudio y no se encontró `kDummyAudio` en el código del servidor | Resolver o justificar formalmente el criterio de audio dummy en tests; investigar EOF IPC por separado; conservar varias ejecuciones Ubuntu y logs. Un verde aislado o tests Windows no permiten cerrar |
| 1 | #36–#38 y #61–#63, Coconut Slice | Controlador, geometría, resultados, reinicio, feedback y entrada existen. Reglas actuales incompatibles entre sí | Conciliar reglas vigentes; probar scoring, cortes, negativos, combos, timeout, reinicio, pausa y 1–4 turnos según alcance aprobado; tutorial/READY y PlayMode. Coordinar las dos series para evitar duplicación |
| 1 | #58–#59, Bowling | Hay controlador con 10 frames, pinos, spin y lógica strike/spare | Resolver 3 rondas vs 10 frames; aislar/verificar scoring con 0, 300, strikes/spares y bonus finales; doble lanzamiento/pino; tutorial, resultados, reinicio y turnos en PlayMode |
| 2 | #41, sesión y jugadores | MotionSession y lobby proporcionan infraestructura, pero no acreditan una sesión de torneo completa | Máquina de estados 1–4, cierre de admisión al iniciar, turnos, pausa, reconexión y recalibración sólo entre intentos; pruebas de transiciones. Depende de #39 para cierre según issue actual |
| 2 | #47, Gorilla Points | No se encontró implementación de GP/Sudden Smash en los scripts de producto revisados | 10/7/5/3 por puesto; caso solitario; desempates por primeros/segundos/terceros y desempate final conforme al catálogo acordado; fixtures deterministas. No requiere sensores |
| 2 | #48, resultados y persistencia | Hay exportación QA; no equivale a configuración/volumen JSON del producto con recuperación | Guardado local, archivo inválido/ausente, resultado oficial sin anticipación, logs y escritura limitada; pruebas de recuperación e idempotencia del resultado |
| 2 | #45, fusión reutilizable | Hay código y cobertura de caducidad, pérdida de tracking, asociación, cooldown y acciones únicas | Completar trazabilidad criterio→prueba y recorrido cámara sintética→fusión→input en Unity. No convertirlo en prueba humana; respetar dependencia #44 |
| 3 | #70, UI móvil | PWA con controles por juego, reconexión y presentación desde Unity; cambios locales aún sin integrar | Completar WAITING con turno/prueba/intento/posición/siguiente; YOUR_TURN, resultados, pausa y reconexión coherentes; pruebas de escena vieja, espectador y reconexión. Cierre condicionado al torneo #66 |
| 3 | #66–#67, torneo y menús | Menú de juegos y navegación existen; no acreditan tabla GP, campeón y secuencia oficial | Implementar secuencia acordada, continuidad 1–4/GP, clasificación, desempate, campeón y reinicio limpio; narrativa y Fiesta Próximamente. Primero conciliar catálogo y completar scoring |
| 3 | #69 y #71, arte/audio/accesibilidad | Hay modelos, audio y UI; no equivalen a cuatro avatares completos ni al AudioMixer requerido | Inventario/licencias, avatares sin ventajas, animaciones, mixer MUSIC/SFX/UI/CHARACTER/AMBIENCE, volumen, instrucciones y señales además de color; comprobar flujos en Unity |
| 3 | #104, control alternativo | Sólo cámara y sólo teléfono son alternativas que todavía requieren dispositivo | Crear alternativa visible de teclado/ratón en Player para calibrar/probar y completar el catálogo/torneo oficial, sin depender de Editor. Debe pasar por reglas/scoring normales |

No hay evidencia suficiente en esta auditoría para recomendar el cierre inmediato de un issue abierto. Sí hay candidatos a completar sin comprar ni conectar hardware adicional. La matriz distingue trabajo ejecutable ahora de aceptación final.

## Discrepancias que hay que resolver primero

1. **Slice:** #37 aún pide tres intentos por jugador y score hasta 10000; #62 pide 60 s, +100 fruta, +50 multicorte y -250 negativo; el controlador actual fija `RoundDuration = 45f`, tres vidas y `SetCameraOnly(true)`. #36 conserva patrón descendente y fusión teléfono+cámara. No cambiar el código ni marcar AC para hacerlos coincidir sin una decisión explícita de reglas.
2. **Bowling:** #58 pide tres rondas; código usa diez frames y `docs/02-gameplay/COCONUT_BOWLING.md` también pide diez frames. Hay contradicción entre fuentes, no simplemente una función faltante.
3. **Catálogo:** #4 registra Slice, Bowling, Tronco y Whac-a-Mole; Boxing/Baseball/Drums son demos. El menú local ya incluye además Portero de la Jungla. #66/#104 siguen exigiendo Smash, Throw, Bowling y Slice; no asumir que los nuevos juegos satisfacen los académicos por equivalencia.
4. **Documentación:** README y documentos antiguos dicen que no hay minijuegos, Flutter o integración de ciertas funciones, mientras esta rama ya contiene implementaciones. Actualizar entradas de estado y conservar los reportes históricos con su fecha, sin reescribir resultados anteriores.
5. **Integración:** cambios sin confirmar y archivos nuevos no están cubiertos por los checks del PR. Agruparlos por alcance verificable antes de presentarlos como entregados.

## Trabajo físico que debe permanecer pendiente

| Issues | Motivo |
| --- | --- |
| #33 | 1 y 4 controles, 20 conexiones/reconexiones, permisos, bloqueo, fallos y latencia reales |
| #34 | PC limpia separada, sin Editor/SDK, recursos, arranque y cierre; además depende de #33 |
| #39, #52, #56, #60, #64 | Entrada real, recuperación y aceptación de las disciplinas correspondientes |
| #42 | Se puede programar y probar calibración sintética, pero <30 s con persona/dispositivo sigue pendiente |
| #44 | Normalización/filtros probables por software; no presentar comparación sintética como jitter/lag físico |
| #73, #75, #77, #78 | Balance humano, incorporación, objetivos simultáneos de rendimiento, gestos y experiencia externa |
| #81–#82 | Preparar docs, licencias, manifiesto y hashes ahora; build final/PC limpia/aceptación y publicación permanecen pendientes |
| #106 | Player Lock con humanos, deuda diferida; replay no sustituye aceptación humana |
| #132, #133, #134 | Pueden completarse software, reglas y pruebas; sus AC actuales también exigen dispositivos/jugadores e integración |

Para mostrar avance real en tareas mixtas, registrar por separado implementación/pruebas de software y aceptación física. Si se decide descomponerlas en subtareas, conservar la aceptación física en la tarea padre y sus dependencias; no eliminar criterios para producir cierres.

Las tareas documentales #84/#85/#87/#88 no requieren hardware, pero son roadmap P1/P2 y no solucionan el P0. No priorizarlas para aumentar el contador de Done. #89 pertenece a un entorno Linux/Flatpak concreto: una ejecución Windows correcta no la resuelve.

## Verificación ejecutada en esta revisión

| Comando | Resultado y alcance |
| --- | --- |
| `npm test` en PWA | 93/93, sin fallos ni omitidas |
| `npm run build` en PWA | PASS, dist y service worker generados |
| `dotnet run --project tools/integration/jungle-phase0/JunglePhase0.Checks.csproj` | 120/120, contratos/lógica C# independientes del motor; no PlayMode |
| `node --test tools/cloudflare-worker/tests/router.test.mjs` | 2/2 |
| `python tools/validate_foundation.py` | PASS, incluidos 700 GUID meta únicos |
| `mvnw.cmd -q -Dtest=MobileUiCommandsTest,MobileNavigationRtcLabTest,ConnectivityTest,LocalTrustTest,ParentLifetimeTest test` en Server | Salida 0; 34 tests ejecutados, 0 fallos/errores, 1 omitido: MobileNavigationRtcLabTest. No se ejecutó toda la suite Java ni se acredita RTC navegador integrado por ese test omitido |

Entorno web local: Node **26.13.0**; package.json pide Node >=24 <25. El resultado local se conserva con ese límite y debe repetirse en Node 24 para evidencia del entorno soportado. Unity configurado en 6000.6.0f1; no se lanzó ni se compiló durante esta auditoría. Los logs de fallos simulados del servidor no se interpretan como fallo de tests; los resultados anteriores provienen de Surefire.

## Orden recomendado de avance

1. Conciliar catálogo/reglas y actualizar trazabilidad de #36–#38/#58/#62/#66/#104; revisar cambios locales y PR #131.
2. Completar Coconut Slice como primer recorrido: scoring, ciclo, tutorial y regresión Unity con entradas sintéticas. Mantener #39 físico pendiente.
3. Completar #127 en Ubuntu y los sistemas compartidos #41/#47/#48; cubrir los criterios de fusión ya implementados con evidencia.
4. Terminar scoring/ciclo Bowling y las partes de software de Tronco/Whac; registrar reglas actuales y pruebas, sin aceptar físicamente #133/#134.
5. Integrar torneo, estados móviles, accesibilidad y control alternativo de teclado/ratón para poder recorrer el producto sin hardware adicional.
6. Preparar paquete reproducible y matriz de aceptación. Después ejecutar únicamente los escenarios físicos pendientes cuando haya equipos disponibles.

Objetivo: llegar a software integrado y verificable, con una lista corta de aceptación física pendiente. No declarar Fase 0, MVP ni entrega final completos por tener pruebas de laboratorio verdes.
