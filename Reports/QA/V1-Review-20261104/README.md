# Gorilla Escape — revisión para candidata 1.0

Estado: candidata local, pendiente de aceptación física y de resolver el cierre nativo observado durante automatización. Revisión iniciada el 4 y continuada el 5 de noviembre de 2026. El usuario indicó que no tiene teléfono disponible durante esta revisión. No se publica ni se declara una versión 1.0 aprobada.

## Cobertura de juegos

Se cargaron los diez juegos y se revisaron sus tres fases principales, con 30 capturas. Se activaron los botones reales de reintento y regreso al menú. Las fases y puntuaciones se forzaron deliberadamente para revisar todas las pantallas sin dispositivos; esto no acredita diez partidas físicas completadas.

- **GorillaSmash**: Instrucciones, intento activo, resultados, reintento y menú; fondo ajustado al encuadre.
- **CoconutThrow**: Instrucciones, apuntado, resultados, reintento y menú; perímetro tropical y materiales.
- **BananaBowling**: Conexión, apuntado, resultados, otra partida y menú; resultados disponibles al perder señal.
- **CoconutSlice**: Introducción, partida, resultado, otra ronda y menú; introducción independiente de la pausa.
- **LogBalance**: Preparación, cruce, victoria, otro intento y menú; mensaje final prioritario y referencias de prefabs reparadas.
- **GorillaBoxing**: Combate, caída, resultado, revancha y menú; empate correcto y rostro dentro del encuadre.
- **JungleBaseball**: Preparación, lanzamiento, resultado, otra partida y menú; bate compuesto y público Meshy.
- **JungleDrums**: Preparación, partida, resultado, repetir y menú; djembes procedurales y entorno tropical.
- **WhackAMole**: Introducción, partida, resultado, otra ronda y menú; fondo cubre el encuadre.
- **JungleKeeper**: Introducción, partida, resultado, otra ronda y menú; variantes propias del modo arquero.

## Correcciones comunes

- Los resultados permanecen accesibles tras perder señal de cámara o teléfono. La puerta de acciones sigue exigiendo señal fresca al volver a jugar.
- Se conservan sala y preparación al cambiar de modo; el splash conserva el mismo personaje Meshy de los juegos.
- Se repararon las referencias serializadas de los prefabs del tronco y del náufrago. El respaldo exclusivo del editor ocultaba el problema.
- El modo de cámara de laboratorio se valida como un nombre permitido, separado de las rutas a archivos.
- Los fondos se ajustan a la cámara y se revisaron materiales ausentes o no compatibles en las escenas activas.

## Pruebas y compilación

- Play Mode final: **13/13**; incluye los diez juegos, botones de reintento/salida, pausa, audio interrumpido, splash, continuidad de sala y cámara contraída bajo los botones del menú.
- Edit Mode completo: **220/220**, sin omisiones, incluido el catálogo de recursos.
- Contratos C#: **159/159**.
- Windows: compilación sin errores, 13 escenas registradas, unos 406 MB antes de añadir runtimes auxiliares. Véase [build.txt](build.txt).
- La primera compilación emitió 498 avisos: 487 de shaders de paquetes y 11 de C#. La recompilación posterior emitió 7 y el empaquetado limpio incremental final emitió 0; el número menor no significa que se corrigieran las advertencias de los paquetes. Se conservan [avisos iniciales](build-warnings-initial.txt) y [avisos finales](build-warnings.txt).
- Servidor: segunda ejecución completa correcta, 165 pruebas, 0 fallos, 0 errores y 15 omitidas. El fallo inicial por tiempo de arranque se conserva en el registro local `server-build.log`; la ejecución correcta, en el registro local `server-recheck.log`. Resumen versionado: [server-summary.json](server-summary.json).
- PWA y Flutter: evidencia de la revisión anterior en [UI/UX](../UI-UX-20261104/README.md); no se volvieron a modificar durante esta pasada.

La primera ejecución de la nueva revisión detectó el bloqueo de salida de boliche y las referencias rotas. El primer empaquetado del servidor agotó el límite de arranque de un proceso de estrés mientras se compilaba Unity y se copiaban runtimes. Se conserva el fallo inicial y se vuelve a ejecutar la suite sin ampliar el límite ni omitir esa prueba.

## Evidencia

[Resultados iniciales de Unity](results.txt), [última pasada de presentación](final-play.txt), [inventario de fases y botones](inventory.txt), [cobertura estructurada](coverage.json), [contratos C#](csharp.txt). Las 30 capturas llevan el nombre del juego y de la fase. Algunas muestran valores sintéticos, no resultados de jugadores.

## Comprobación fuera del editor

El ejecutable empaquetado arrancó su servidor local y se observó el menú con imagen real de cámara. La revisión visual detectó símbolos sin glifo y una vista previa que tapaba opciones; se retiraron los emojis decorativos y la cámara entra contraída. La prueba de Play Mode comprueba que el control queda debajo de los botones.

Las capturas de los intentos con ventana oculta resultaron negras; no son evidencia visual válida y están identificadas en `standalone/hidden-attempt.txt`. El intento batch no pudo capturar imágenes. No se cuentan como comprobaciones aprobadas.

Al cerrar la ventana inspeccionada se registró un cierre nativo con pila de UnityPlayer y `UiaDisconnectAllProviders`. Registro: [visible-player-exit.log](standalone/visible-player-exit-summary.txt). No se atribuye de forma concluyente al código del juego ni se declara resuelto. Unity documenta una incidencia relacionada con el cierre tras interacción de clientes de automatización en sus [notas oficiales de 6000.6.0f1](https://unity.com/releases/editor/whats-new/6000.6.0f1); esa referencia no demuestra que nuestra incidencia sea idéntica.

## Pendiente para aprobar 1.0

- Reproducir y resolver o aislar con evidencia el cierre nativo al salir, con y sin cliente de automatización.

- Completar los diez juegos con cámara y teléfono físicos; comprobar permiso, calibración, recuperación, pausa, resultados y salida.
- Historia y Fiesta con cambios de modo, fin de torneo, empate y pérdidas de señal reales; 2–4 jugadores.
- Comprobar sensores y navegador en Android e iOS, WAN si se distribuye esa modalidad, y ejecución en una PC sin herramientas de desarrollo.
- Rendimiento sostenido y audio percibido en el hardware objetivo. Las capturas y el banco de audio no sustituyen esas mediciones.
- La candidata local no incluye aceptación de licencias de distribución ni aprobación para publicar tiendas/instaladores.

Cambios locales sin subir a Git ni desplegar.

La candidata está en `Builds/Review-1.0/GorillaEscape.exe`, junto a los runtimes y el servidor/PWA actualizados. No se incluye aprobación para distribuirla como versión final.

Actualización posterior de mecánicas: [revisión del 5 de noviembre](../Mechanics-20261105/README.md). La candidata anterior fue recompilada con esas correcciones; véase el informe más reciente.
