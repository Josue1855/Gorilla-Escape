# Revisión pendiente: reglas y catálogo

Registro preparado el 30 de octubre de 2026 para revisar con el responsable del producto. Las opciones de esta página no son decisiones ni cambian los criterios de GitHub.

## Juegos del torneo

La enmienda académica DEC-026 enumera Smash, Throw, Bowling y Slice. El issue #4 también registra como juegos actuales Slice, Bowling, Desafío del Tronco y Whac-a-Mole; el menú de esta rama contiene además otras demos y Portero de la Jungla. El usuario indicó que el catálogo del torneo **todavía no está definido**. Antes de cerrar #66 o #104 hay que decidir qué juegos integran el torneo y cómo se reconcilia esa elección con la entrega académica.

## Coconut Slice

| Tema | Implementación actual en esta rama | Criterios que necesitan revisión |
| --- | --- | --- |
| Duración | 45 segundos | #62 indica 60 segundos |
| Control | Cámara; el controlador activa `SetCameraOnly(true)` | #36 describe fusión de teléfono y cámara |
| Fallos | Tres vidas, objetos peligrosos y un objeto de pérdida inmediata | #62 describe penalización de puntos por objeto negativo |
| Puntuación | Fruta, bonus de combo limitado y multiplicador de precisión | #62 indica fruta +100, multicorte +50 y negativo -250; #37 describe puntuación hasta 10000 |
| Turnos | La escena actual tiene su ciclo individual | #37 describe tres intentos por jugador, de 1 a 4 |

La revisión debe acordar duración, controles, puntuación, penalizaciones, intentos y turnos, y registrar si el alcance académico exige conservar otro conjunto de criterios. No tratar la presencia de una mecánica como aprobación de su regla.

## Bowling

El controlador actual usa diez frames, strike/spare y bolas extra en el último frame. [La ficha de Bowling](../02-gameplay/COCONUT_BOWLING.md) también describe diez frames; #58 menciona tres rondas. Se debe confirmar si se busca boliche convencional de diez frames o un formato distinto de tres rondas. Las pruebas `BowlingScoringTests` cubren el comportamiento actual y no anticipan la decisión.

## Criterios para registrar el acuerdo

Para cada juego: duración o cantidad de intentos; dispositivos y controles; inicio, pausa y recuperación; puntuación, errores y empate; condiciones de finalización; número de jugadores y turnos. Actualizar después los issues y la fuente de requisitos aplicable de manera explícita, conservando la historia y la aceptación física pendiente.
