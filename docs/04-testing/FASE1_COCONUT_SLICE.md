# Fase 1: recorrido vertical Coconut Slice

El 27 de octubre de 2026 el usuario eligió avanzar la fase 1 con uno de los juegos existentes. Se prioriza **Coconut Slice**, ya presente en Unity y en el alcance académico anterior. Tronco, Whack-a-Mole y Bowling son los otros juegos existentes; Boxing, Baseball y Drums son demos según el usuario.

La épica [#2](https://github.com/Josue1855/Gorilla-Escape/issues/2) refleja esta prioridad. La feature #35 y las tareas #36–#39 ahora describen el recorrido vertical de Coconut Slice y conservan su alcance anterior de Smash como historial. La disciplina completa se sigue en [#61](https://github.com/Josue1855/Gorilla-Escape/issues/61) y #62–#64 en fase 3; coordinar ambas para evitar duplicaciones y reconciliar criterios. El cambio no demuestra cumplimiento de Smash, Throw ni de los requisitos académicos previos; los requisitos externos siguen sujetos a verificación.

## Criterios del primer recorrido

- Conectar el teléfono y activar la cámara cuando el juego la requiera; informar y recuperarse de fallos sin inventar entrada.
- Explicar los controles, comenzar, detectar cortes válidos, puntuar, finalizar, mostrar resultado y reiniciar sin puntajes ni objetos residuales.
- Verificar las reglas reales y decidir la discrepancia con #61/#62: el controlador tiene vidas, obstáculos, bonificación de combo y duración de 45 s, mientras esos issues describen 60 s y otra puntuación. No declarar equivalencia sin decisión o adaptación.
- Ejecutar pruebas de lógica y PlayMode sobre el loop, probar con teléfono y cámara físicos, y registrar condiciones, versión, resultados y límites.
- Mantener la fase abierta hasta integrar los cambios, revisar resultados y cumplir los criterios acordados. Las pruebas automáticas del 27 de octubre en `REVISION_PROJECT_1_2026-10-27.md` son evidencia parcial, no aceptación del juego.

## Comprobación posterior del 27 de octubre

- Se corrigió el orden del límite de tiempo para no aceptar un nuevo corte ni generar objetos cuando la ronda ya terminó. El final por quedarse sin vidas ahora indica derrota; el reinicio limpia los avisos y los tiempos de corte anteriores.
- Unity 6000.6.0f1, EditMode en modo batch: 175/175 PASS. Compilación estática de scripts: PASS. Contratos C# independientes: 104/104 PASS. No son una prueba específica del ciclo completo de Coconut Slice.
- PlayMode batch sin configurar las rutas de fixtures: 9 PASS, 17 FAIL, 3 omitidas. Entre las causas observadas figuran variables ausentes para Java, JAR, clientes y bundle de cámara; no se puede concluir que todos los fallos se deban exclusivamente al entorno. Esta batería no está aprobada y debe repetirse con sus fixtures configurados.
- Las reglas de 45 s, vidas y combo no se han sustituido por las reglas históricas de 60 s y scoring de #61/#62. La decisión y la aceptación de dichas reglas siguen pendientes. No se ha acreditado una partida con cámara y teléfono físicos ni un build final.

## Avance de pruebas de Coconut Slice

- Unity 6000.6.0f1 EditMode: 183/183 PASS en el árbol local con cambios sin confirmar. Incluye pruebas específicas de geometría de segmentos, límite del bono por combo y vencimiento de la ronda. No prueban la partida completa ni dispositivos físicos.
- Ensayo Android API 36/Chrome 133: el primer intento falló al abrir Chrome en frío; el segundo falló porque el harness esperaba la antigua pantalla de diagnóstico en `/`. Tras añadir una espera acotada y usar `?view=health`, el tercer intento pasó arranque, carga del diagnóstico web, respuesta Java, recarga, reinicio del navegador, fallo al detener Java y rechazo de un certificado no confiable. Limpieza PASS. Los informes permanecen en `/tmp/gorilla-e1-android/`; esta prueba usa localhost/ADB y no valida QR, conexión pública, sensores, tracking físico ni una partida de Coconut Slice.

## Recuperación de seguimiento — 28 de octubre

El adaptador de visión asigna identificadores espaciales temporales: después de perder todos los sujetos puede generar un identificador nuevo, y la asociación anterior no lo adopta automáticamente. Se añadió una acción explícita en la pausa para volver a detectar al jugador; esta descarta la asociación anterior, exige nuevos cuadros y vuelve a establecer una línea base sin movimientos autorizados. Un escenario sintético comprueba que una persona distinta no sustituye automáticamente a la anterior y que no se reproduce una acción tras el reinicio. Es una recuperación manual, no una prueba de identidad ni de seguimiento humano fiable. Quedan pendientes pruebas físicas de oclusión, recuperación y error/latencia con teléfono y cámara.
