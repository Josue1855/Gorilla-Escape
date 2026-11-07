# Revisión de mecánicas y preparación de subida — 5 de noviembre de 2026

## Cambios y alcance

- **Gorilla Smash**: Puntuación determinista por potencia, precisión y sincronía; retirada la autorización sin hardware y la potencia de teclado inventada.
- **Coconut Throw**: Puntuación y apuntado usan la muestra aceptada. Vuelo analítico y punto de impacto exacto sin dependencia de FPS.
- **Coconut Bowling**: La fuerza de curva comprueba la posición de la bola. Se mantienen el paso fijo, las bonificaciones y los reintentos.
- **Gorilla Boxing**: Autorización de golpes, cobertura, esquiva, caída y reloj revisados; empate y resultados corregidos en la pasada anterior.
- **Jungle Baseball**: Contador de lanzamientos corregido; velocidad de la pelota ajustada para que el vuelo corresponda a la distancia mostrada.
- **Jungle Drums**: Mismo margen de tiempo al juzgar el golpe y al caducar la nota, sin la discrepancia de 20 ms anterior.
- **Jungle Slice**: Revisados corte barrido, extremos del segmento, límite de combo, objetos peligrosos y cierre de ronda.
- **Jungle Keeper**: Revisadas zonas de atajada, manos/torso, caducidad de tiros y contabilización de objetos ya resueltos.
- **Gorilla Whack**: Revisados objetivo activo, radio de acierto, autorización, enfriamiento, escapes y límite del combo.
- **Jungle Balance**: Revisados subpasos fijos, inclinación, caída, victoria y resultados. Las referencias de modelos se repararon en la pasada anterior.

El azar de aparición de objetos, ataques rivales y efectos visuales se conserva como parte de esos juegos. Se retiró el azar añadido directamente a la calidad de un mismo golpe o lanzamiento. La predicción visual de la mano no determina la puntuación ni el ángulo de lanzamiento del coco.

## Validación

- C# puro: **171/171**; incluye puntuación repetible, sincronía real, trayectoria a 10, 15, 30, 60, 120 y 240 FPS, alcance de béisbol y entradas no finitas.
- Unity 6000.6.0f1: **229/229 Edit Mode**, **13/13 Play Mode**. Incluye regresiones que ejecutan el controlador de boliche y comprueban el contador real de béisbol. [Resultados](unity.txt).
- PWA: **95/95**, compilación correcta.
- Flutter: analizador sin problemas, **26/26** pruebas.
- La copia aislada se inició cuando el editor dejó de responder. El editor original se recuperó y completó las pruebas; la copia aislada no se cuenta como evidencia de aprobación.

## Límites y estado de versión

Se revisaron reglas, implementación y fases de los diez juegos, y se ejecutaron los botones reales de reintento y menú. Las fases visuales son fixtures simuladas; no equivalen a diez partidas físicas. El usuario indicó que no tiene teléfono disponible para la aceptación final.

Siguen pendientes la validación física de sensores/latencias, varios jugadores, iOS y la investigación del cierre nativo tras inspección con automatización de Windows. No se declara resuelto ese cierre ni se presenta la candidata como versión 1.0 aprobada. [Informe visual anterior](../V1-Review-20261104/README.md).

Se suben fuentes, metadatos, pruebas y evidencia curada. Compilaciones, runtimes, copias aisladas, cachés y registros crudos permanecen locales y excluidos de Git. La copia local de pruebas se conserva tras el bloqueo de su limpieza automática; no forma parte de la aplicación ni del commit.

La candidata local de Windows se recompiló correctamente con las nuevas mecánicas: 0 errores, 11 avisos; [resultado](windows-build.txt). Las compilaciones no se suben a Git.
