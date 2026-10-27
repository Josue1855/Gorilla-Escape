# Desafío del Tronco en la selva de Saúl

Fecha: 2026-10-27. Rama: `feature/saul-jungle-party-alpha`. Séptimo juego del menú; revisión en el PR [#131](https://github.com/Josue1855/Gorilla-Escape/pull/131), sin merge automático.

## Mecánica e integración

Se recuperan selectivamente el modelo físico, náufrago, tronco y presentación del juego de equilibrio de `feature/gameplay-desafio-del-tronco` (`bcb4fc4`). El archivo `LogBalanceModel.cs` se conserva sin cambios: subpasos de 10 ms, avance, inercia lateral, rotación por peso/pisadas, viento, dificultad progresiva, avisos y resultados terminales. No se fusiona el transporte ni el menú antiguos.

`LogBalanceGameController` vive junto a los controladores de Saúl. Consume exclusivamente `MotionSession` y frames fusionados con autorización `Balance`. Exige teléfono activo y ambas muñecas asociadas, frescas y alineadas. La cámara comprueba asociación/presencia; la inclinación gamma del teléfono dirige el equilibrio. No se inventa una fuerza de cámara ni se lee el teléfono por otra vía.

Antes de comenzar observa ≥8 muestras de teléfono distintas durante ≥450 ms, con variación ≤2 grados, pantalla vertical y sin huecos superiores a 350 ms. Calcula el neutro real y convierte ±25 grados respecto al neutro en el comando normalizado. La preparación conserva el tiempo detenido. Desconexión, seguimiento perdido o cambio de época invalidan el control y exigen nuevo neutro. Durante una partida la simulación mantiene su estado y reanuda después de recuperar/calibrar; no crea otro intento. Pausar, perder foco o abrir créditos también detiene la simulación local.

Las reglas y calibración son independientes de UnityEngine. Shared, IPC loopback, JavaProbeSupervisor y VisionSupervisor conservan sus contratos y supervisión. No se añaden servidores, puertos ni instalaciones. El resultado es local; no publica un torneo al backend.

## Recursos visuales

| Elemento | Selección |
| --- | --- |
| Tronco y náufrago | Prefabs/FBX originales de equilibrio; mismos GUID y rig del personaje |
| Material del tronco | `Assets/Materials/BananaBowling/Tronco.mat` de Saúl, asignado en la escena |
| Palmas y arbustos | Modelos existentes en `Resources/Jungle/NewModels`, con sus texturas |
| Gorilas espectadores | Modelo y textura existentes de `Resources/Jungle/GorillaBowling`; pose procedural sobre su rig |
| Fondo y suelo | `bowling_waterfall_background` y `bowling_jungle_ground` existentes |
| Menú e interfaz | `JungleUITheme`, madera/turquesa/dorado y hojas de Saúl; cuadrícula de siete opciones |

`LogBalanceArtCatalog` centraliza las rutas reutilizadas; no se duplican los recursos de la selva. Los únicos nuevos binarios son los dos FBX y tres texturas originales del tronco/personaje, almacenados mediante Git LFS. Los seis juegos anteriores conservan sus escenas y arte. El escenario libera materiales/mallas propios y restaura cámara/ambiente al salir o fallar la preparación.

Los créditos de Trunk (gashtan) y Castaway (katarzynaszemla), CC BY 4.0, están incluidos y accesibles desde CRÉDITOS en el juego: `Resources/LogBalance/CREDITS.txt`. Incluyen enlaces y adaptaciones. Los recursos de Saúl mantienen su procedencia en el repositorio; esta integración no declara licencias nuevas para ellos.

## Validación y aceptación

[Resultados reproducibles](../evidence/saul-log-balance-2026-10-27/README.md). Los checks de código, dependencias de assets y fixtures no ejecutan el motor. La inspección visual, importación completa y ensayo con cámara/teléfono quedan pendientes dentro de Unity abierto desde Hub.

1. Abrir **Gorilla Escape → Play Gorilla Escape**, conectar mediante la PWA oficial y entrar en DESAFÍO DEL TRONCO. Verificar las siete opciones del menú.
2. Comprobar náufrago sobre la superficie del tronco, textura, riberas, vegetación, gorilas y fondo sin materiales rosados; verificar créditos y retorno al menú.
3. Mantener el teléfono vertical quieto hasta Control listo. Comenzar: cuenta atrás, inclinaciones a ambos lados, contrainclinación para frenar, avisos y caída; reiniciar y completar el cruce.
4. Tapar cámara, ocultar una muñeca, suspender el navegador y desconectar teléfono. Deben congelarse progreso, cuenta atrás y resultado. Recuperar y recalibrar: debe continuar el mismo intento sin saltos ni entradas antiguas.
5. Probar pausa, pérdida de foco, créditos y cambio de juego; comprobar que se restauran cámara, ambiente y escala de tiempo al volver al menú.
6. Repetir en distintas resoluciones y realizar una prueba breve de los seis juegos anteriores.

No se registran estos pasos como ejecutados. La licencia Student mostrada en Hub no fue reconocida por la invocación de terminal anterior; no se ha cambiado ni intentado eludir la licencia.
