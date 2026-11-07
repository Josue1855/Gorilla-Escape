# Revisión de controles — 5 y 6 de noviembre de 2026

Se implementaron perfiles por juego, simplificación de box/béisbol/tambores, Whack vertical con potencia móvil, precisión de cámara y potencia móvil en Smash, mazo existente compartido, coco existente con escala/textura corregidas y recuperación del control web al desbloquear. Criterio y revisión de los diez juegos: [Controles y mecánicas](../../../docs/03-development/GAMEPLAY_CONTROLS.md).

## Evidencia final

- C# puro: **180/180**. Incluye separación de potencia/precisión, golpe fuera del blanco, tope de potencia/combo de Whack, timing de béisbol, gestos de cámara sin teléfono y rechazo de repetición/entrada caducada.
- PWA: **97/97** y build correcta. Dos regresiones nuevas cubren desbloquear con transporte vivo y con transporte perdido, restaurando sensores sin QR ni nueva pulsación de permisos.
- Flutter: analizador sin incidencias y **26/26**.
- Compilador C# de Unity: los cuatro ensamblados modificados compilan, incluidas las pruebas. Esto no equivale a un build del reproductor ni a ejecutar las pruebas del motor.
- [Captura del coco existente](CoconutThrow-Aiming.png): modelo y textura del catálogo actual; no se creó un coco nuevo.

## Límites

Revisión posterior del 6 de noviembre: la licencia permitió importar/compilar y ejecutar **28/28 EditMode y 1/1 PlayMode** seleccionados. La preparación Windows y Server también se verificaron; la prueba externa de túnel encontró un fallo DNS. [Resultados y alcance actuales](../../../docs/03-development/EDITOR_WINDOWS_PORTABILITY.md). Los registros de licencia siguientes describen el intento anterior, no el resultado de esta revisión posterior.

Antes de la última ampliación se ejecutaron 241 pruebas EditMode; ese resultado no se atribuye al estado final. La primera exploración de escenas detectó un índice antiguo de Whack que se corrigió al cambiar de nueve casillas a tres alturas. El lanzamiento exploratorio completo también incluyó pruebas de procesos sin sus variables de Java/cámara preparadas; sus errores no son evidencia de aceptación del transporte ni de una regresión comprobada. Los registros exploratorios permanecen locales en Unity/Temp/ControlsEarlyEvidence.

La última ejecución nativa y la build Windows quedaron bloqueadas por la licencia de Unity (salida 198 tanto en modo batch como normal; [detalle](unity-license.txt)). No se presenta la build anterior como actualizada. Falta verificar el nuevo mazo/encuadre en el motor, completar las pruebas nativas finales y jugar con teléfono/cámara reales para afinar la potencia, precisión y comodidad. La potencia es una medida de juego derivada de sensores, no una medición de fuerza física en newtons.
