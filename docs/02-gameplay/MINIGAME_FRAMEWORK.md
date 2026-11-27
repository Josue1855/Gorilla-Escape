# Marco común de minijuegos

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Estado real S1: foundation/input/sesiones técnicas disponibles, sin implementación de los cuatro juegos. Estado de sus requisitos S3: PLANIFICADO. Interfaces descritas aquí: PROPUESTO; no son clases existentes.

Flujo común propuesto: preparar partida → asociar humanos/slots → explicar controles/calibrar → listo → intento/acción → resolver física/reglas Unity → feedback → siguiente turno → resultado final inmutable → publicación diferida autorizada. Estados desconectado/pausa/abortado deben preservar autoridad y evitar adjudicar intentos sin acción válida.

Separar adaptador de PhoneInput/CameraInput del modelo de reglas, simulación Unity y presentación. La entrada expresa intención, no puntos. Cada juego define rulesVersion, unidad, límites y eventos de salida; sólo Unity emite resultado final. No modificar Gorilla Protocol para meter reglas sin revisión productor/consumidor y pruebas Shared.

| Disciplina PLANIFICADA | Especificación única | Unidad / aceptación clave |
|---|---|---|
| Bowling | [Coconut Bowling](COCONUT_BOWLING.md) | Puntos oficiales, 10 frames, bonus y décimo frame |
| High Striker | [Gorilla Smash](GORILLA_SMASH.md) | Puntuación por altura/fuerza calibrada; mazo y campana |
| Shot Put | [Coconut Throw](COCONUT_THROW.md) | Mejor distancia válida en metros, tres intentos |
| Slice | [Jungle Slice](JUNGLE_SLICE.md) | Puntos, trayectorias/combo/penalizaciones |

Criterio transversal propuesto: repetir fixtures de intención y verificar reglas deterministas; no exigir determinismo bit a bit a toda física Unity sin diseño específico. Registrar límites/resultado de falta/empate/cancelación/desconexión. Una demo visual o un test de transporte no acepta ningún minijuego. Las pruebas de reglas, interacción y sensor físico se diseñan por separado. [Entradas y personajes](INPUT_AND_CHARACTERS.md).
