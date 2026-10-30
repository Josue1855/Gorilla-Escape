# Entrada fusionada de los siete juegos

Revisión: 2026-10-27. Rama `feature/saul-jungle-party-alpha`. Sustituye la entrega separada de movimiento/pose del incremento `095995f`.

## Arquitectura

```text
Shared → Java supervisado → IPC loopback autenticado → PhoneInputStore
                                                       │
Cámara → VisionSupervisor → CameraInputStore → InputFusion.EvaluateLatest
                                                       │
                                      Gameplay/Input/JungleGameplayInput
                                                       │
                              JungleFusedFrame → MotionSession → controlador
                                                       │
                                          autorización de gesto → mecánica
```

InputFusion sigue siendo infraestructura técnica: no reconoce golpes, calcula puntuaciones ni decide resultados deportivos. Su nueva operación `EvaluateLatest` exige el par actual; la evaluación histórica `Evaluate` permanece disponible para QA y conserva su selección anterior. Esto evita retener una pareja antigua de desfase mínimo durante una acción nueva.

Las reglas semánticas viven en `Scripts/Gameplay/Input/`, sin dependencias de UnityEngine. `JungleGameplayInput` entrega una muestra tipada que reúne teléfono, pose, velocidades, secuencias, revisión y acciones reconocidas. Las proyecciones sólo tienen setters internos; los juegos no reciben por separado cadenas JSON mutables.

`PhoneInputStore` conserva la API original. Se separaron su almacenamiento/validación y la proyección a tipos Unity en archivos parciales; `CameraInput` también se extrajo como contrato independiente del motor. Los supervisores y los contratos Shared no cambian.

## Condiciones comunes

- Jugador activo 1, misma sesión/dispositivo/época y sujeto asociado por PlayerLock.
- Teléfono y cámara actuales: antigüedad máxima 250 ms, tolerancia de futuro 20 ms y desfase máximo 100 ms.
- Teléfono ACTIVE con gravedad, rotación, orientación y orientación de pantalla disponibles y finitas. La ausencia de orientación de pantalla no se interpreta como un cero observado.
- Cámara TRACKING, confianza mínima 0.6 y muñecas requeridas visibles, con presencia/visibilidad mínimas 0.6.
- Velocidad de muñecas derivada del tiempo de captura de cámara, no del FPS de render. Una recuperación, nueva época, ronda o escena crea una línea base sin velocidad ni acción.
- Descarte conservador de saltos superiores a 12 unidades normalizadas/s. Es un límite de prototipo sujeto a calibración física, no una medida certificada de velocidad humana.
- Reconocimiento con rearme en reposo, histéresis y separación temporal; un pico sostenido no se convierte en golpes repetidos al cumplirse un temporizador.
- Autorización de cada acción sólo una vez para su revisión, con comprobación de frescura al consumirla. Una revisión anterior, una acción de otro juego o una máscara combinada no puede autorizarse.
- Sin colas de gestos: un gesto fuera de la ventana de juego se descarta. No se aplica más tarde al aparecer una pelota o una cabeza.

La intensidad `PhonePower` conserva la heurística del alpha —magnitud angular más magnitud de gravedad por siete— para mantener las mecánicas. No representa vatios ni fuerza física calibrada. Los umbrales siguientes son parámetros de software del prototipo, pendientes de ajuste con mediciones reales.

## Matriz de mecánicas

| Juego | Seguimiento requerido | Gesto autorizado y consumidor |
| --- | --- | --- |
| Banana Bowling | Muñeca derecha + teléfono alineado | Pico de intensidad >330 y velocidad derecha >0.35; rearme por debajo de 180/0.25, separación 600 ms. `LaunchBall` requiere la autorización Throw; conserva trayectoria y puntuación de bolos. |
| Gorilla Boxing | Ambas muñecas + teléfono alineado | Izquierda: velocidad >0.72; derecha: velocidad >0.72 e intensidad >215. Rearme en reposo y separación 340 ms; se conserva la regla de combate entre golpes. La recuperación tras derribo también exige movimiento derecho y pulso válido. |
| Jungle Baseball | Muñeca derecha + teléfono alineado | Intensidad >260 y velocidad >0.55, rearme por debajo de 155/0.25. `TrySwing` exige Pitching y autorización Swing; una velocidad anterior no valida un impulso posterior. |
| Jungle Drums | Ambas muñecas + teléfono alineado | Descenso izquierdo >0.62; derecho >0.62 con intensidad >225. Rearme y separación 200 ms; cada mano puede autorizarse una vez en el mismo par. No hay penalizaciones por golpear antes del inicio ni después del final. |
| Coconut Slice | Ambas muñecas + teléfono alineado | Segmentos de cámara nuevos con velocidad >0.72. Una autorización por mano/revisión y separación geométrica por mano. Se elimina la puntuación mediante ratón y no se dibuja un segmento atravesando una pérdida de seguimiento. |
| Gorilla Whack | Muñeca derecha + teléfono alineado | Descenso derecho >0.72 **y** impulso del teléfono >235 según la heurística propia del alpha. Se elimina la alternativa OR y el apuntado de emergencia sólo por teléfono; se conserva la comprobación espacial de la cabeza. |

| Desafío del Tronco | Ambas muñecas + teléfono alineado | Autorización Balance por revisión válida; inclinación gamma relativa al neutro observado (≥8 muestras distintas, ≥450 ms estables), sin pulsos ni teclado. La pose valida asociación y presencia; no se inventa una fuerza de cámara. |

Las acciones de esta capa autorizan mecánicas locales. No crean resultados persistidos de torneo ni sustituyen el protocolo/registro oficial de resultados futuro. El `OfficialActionLedger` existente de Fase 0 conserva su implementación y no se llena con cada frame de cámara.

## Pérdida y recuperación

`MotionSession` es el propietario de la pausa técnica de gameplay. Ejecuta el muestreo antes de los controladores y adquiere una pausa (`Time.timeScale=0`) cuando falta un par válido. Así se detienen física, cuentas atrás, notas, apariciones y penalizaciones. Los controladores también verifican disponibilidad antes de actualizar sus mecánicas.

La pausa conserva la escala anterior, no reanuda una pausa ajena y se libera al volver al menú o destruir la sesión. La comunicación y la adquisición continúan con reloj no escalado, por lo que pueden recuperar el seguimiento mientras la simulación está detenida. Un aviso explica cómo recuperar sensores/manos.

La vuelta exige asociación válida y una línea base nueva. Los gestos de pulso necesitan observar reposo antes de rearmarse. Una bola que ya había sido lanzada reanuda su física; la recuperación no devuelve indebidamente el estado a Aim ni crea otro lanzamiento. Un corte no une la última mano visible con la posición reaparecida.

Se retiraron las ramas sin uso de tótems/equilibrio/karate del controlador combinado antiguo. El tronco se integra ahora desde su rama independiente mediante su propio modelo y controlador, sin reactivar aquel código. Las escenas y recursos de los seis juegos de Saúl se conservan. [Diseño del séptimo juego](LOG_BALANCE_SAUL.md).

## Verificación reproducible

```sh
dotnet run --project tools/integration/jungle-phase0/JunglePhase0.Checks.csproj --configuration Release
python3 tools/integration/validate_jungle_gameplay.py
python3 tools/integration/compile_jungle_phase0.py --editor-data /ruta/Unity/Editor/Data --project-scripts
python3 tools/integration/compile_jungle_phase0.py --editor-data /ruta/Unity/Editor/Data --project-scripts --player
python3 tools/validate_foundation.py
python3 tools/github/validate_management.py
```

El harness ejecuta el InputFusion, almacenes, asociación, proyección, reconocimiento y autorización reales contra fixtures sintéticos. Incluye los siete perfiles, desconexión, manos perdidas, cambio de época, pares obsoletos, datos parciales, duplicados, rearme, pausa y 10 000 muestras con almacenamiento acotado. El validador comprueba que las siete escenas guardadas referencian los controladores migrados y que no hay rutas de puntuación por ratón/JSON/transporte directo. Ambos forman parte de CI Foundation. El harness incluye además física del tronco y calibración desde frames fusionados.

Las comprobaciones C# usan las DLL reales importadas de Unity/glTFast para compilar los scripts de runtime, gameplay y Editor. Esto no ejecuta el motor, sus coroutines, la física ni sus assets. [Resultados de esta revisión](../evidence/saul-gameplay-fusion-2026-10-27/README.md).

## Ensayo físico pendiente

En Unity abierto mediante Hub, arrancar con **Gorilla Escape → Play Gorilla Escape**. Preparar el paquete Vision oficial y conectar un teléfono real. Para cada juego:

1. Verificar reposo sin puntuación y el gesto de su fila de la matriz; confirmar que no se duplica por sostener el pico.
2. Tapar la cámara o esconder la muñeca requerida durante una acción. Deben detenerse física y tiempo sin nuevos golpes, cortes, strikes, notas falladas, escapes ni daño al jugador por seguimiento perdido.
3. Recuperar las manos: no debe existir un lanzamiento/golpe/corte fantasma. Volver al reposo y realizar un nuevo gesto.
4. Repetir con teléfono desconectado, navegador suspendido, dos sujetos dentro de ROI, nueva ronda y cambio de juego. Comprobar la restitución del tiempo al volver al menú.
5. En bolos, tapar la cámara con una bola en movimiento y verificar que continúa la misma bola al recuperar, sin volver a Aim. En Drums y Slice, verificar acciones simultáneas de ambas manos. En Slice, verificar que el ratón no puntúa.

Estos pasos no se marcaron ejecutados ni se fabricaron capturas. La licencia Student mostrada por el usuario en Hub no fue reconocida por el intento de EditMode desde terminal (salida 198); la validación nativa y física sigue pendiente. Tampoco se declara completada la distribución de cámara Windows.
