# Arte low poly y personaje de Desafío del Tronco

Actualización 2026-10-20, exclusivamente en `feature/gameplay-desafio-del-tronco`. Sustituye el escenario y humano geométricos de la primera iteración; mantiene reglas, sensores y calibración.

## Recursos proporcionados

- `trunk.glb`: tronco de la prueba, ajustado al recorrido y anchura del juego.
- `low_poly_trees.glb`: variedades de árboles y roca del pack, separadas en prefabs para distribuirlas en las orillas.
- `Vagabundo.glb`: humano náufrago Castaway, con tres mallas skinned, texturas, rig original de 65 huesos y acción Take 001.

Los originales se conservan byte a byte en `Unity/Assets/_Project/Art/LogBalance/Sources/`. El manifiesto en `Resources/LogBalance/asset-manifest.json` registra hashes, materiales y atribuciones incluidas en los GLB. Los [créditos](../../Unity/Assets/_Project/Art/LogBalance/ATTRIBUTION.md) se mantienen también en el juego. No se descargaron otros modelos.

## Preparación reproducible

Unity usa derivados FBX, texturas y prefabs preparados en el editor, sin cargar o convertir GLB durante una partida. No se añadió un paquete externo glTF. Se habilitó únicamente el módulo integrado de animación `com.unity.modules.animation@1.0.0` mediante Package Manager; manifest y lockfile quedaron sincronizados.

Para regenerar derivados desde la raíz del repositorio, con Blender 4.0.2:

```sh
blender -b --python tools/art/prepare_log_balance.py -- /ruta/absoluta/al/repositorio
```

Después, abrir Unity y ejecutar **Gorilla Escape → Log Balance → Rebuild Art Prefabs**. El editor crea materiales mates, conserva texturas, prepara el rig y guarda los prefabs y sus referencias. Los archivos ya están versionados: no hay que ejecutar estos pasos para jugar ni retocar YAML o GUID a mano.

Los árboles comparten mallas/materiales de sus prefabs. El escenario usa una distribución reproducible, orillas irregulares, rocas y vegetación baja de caras planas. El corredor central y los desembarcos se mantienen libres. Los NPC provisionales también usan geometría facetada.

## Movimiento del náufrago

Se conserva el esqueleto y los pesos originales. La primera pose de la acción importada prepara la postura base; `LogBalanceCharacter` aplica movimientos procedurales aditivos, sin root motion:

- Caminata: alternancia de muslos, rodillas y tobillos, con pequeño movimiento del cuerpo.
- Equilibrio: inclinación global y compensación de torso/cabeza; brazos que se abren más al acercarse al límite.
- Caída: cuerpo que sale del tronco y baja, con piernas y brazos reaccionando.
- Victoria: brazos levantados y postura de celebración.
- Pausa/reinicio: se restablece la pose base y se evita acumular rotaciones entre frames.

La simulación existente sigue siendo dueña de distancia, tiempo, caída y victoria. Las animaciones no dan puntos ni cambian el balance. El apoyo vertical toma muestras de los triángulos del tronco importado, sin añadir un sistema de físicas o alterar el protocolo móvil.

Es una primera animación procedural para el vertical slice; no incluye captura de movimiento, un Animator Controller complejo, ragdoll ni IK avanzada. La caminata y las reacciones pueden refinarse conservando el mismo rig.

## Validación

Consultar el registro final al pie de este documento. La validación física del teléfono sigue independiente del cambio de arte.

Resultado final de esta actualización:

| Comprobación | Resultado |
|---|---|
| EditMode completo | 183/183 PASS, 0 omitidas; incluye recursos, rig y deformación real de la malla |
| PlayMode completo | 31/31 PASS, 0 omitidas; navegación, intentos, pausa, reconexión, Java/RTC/cámara existentes |
| Build Linux Development | Succeeded, 0 errores y 0 advertencias |
| Validadores foundation/gestión | PASS; metadatos, contratos, enlaces y backlog conservados |
| Originales proporcionados | SHA-256 idénticos a los archivos recibidos |
| Prueba gráfica del Player final | Caminata, equilibrio, victoria en 24 s/18 m, reinicio y caída con inclinación extrema |

Capturas del ejecutable con los modelos suministrados: [caminata](../evidence/log-balance-low-poly-2026-10-20/walking.png), [victoria](../evidence/log-balance-low-poly-2026-10-20/victory.png) y [caída](../evidence/log-balance-low-poly-2026-10-20/fall.png). La prueba gráfica usó teclado; no acredita sensores físicos.

Cambios principales: `LogBalanceStage.cs`, `LogBalanceController.cs`, nuevo `LogBalanceCharacter.cs`, preparación de assets en `LogBalanceAssetSetup.cs` y `tools/art/prepare_log_balance.py`, pruebas `LogBalanceArtTests.cs`, módulo integrado de animación, originales/derivados/prefabs/materiales/texturas/metadatos, créditos y documentación. Las reglas de equilibrio y el transporte de teléfono no cambiaron.

Pendientes: refinar la calidad de poses/contacto si se desea una animación final, sustituir los gorilas NPC provisionales por modelos propios y validar sensación con teléfono físico. No se incorporaron ragdoll, obstáculos ni nuevos mapas. Los gates físicos de la base siguen independientes.

## Pasto adicional proporcionado

`low_poly_grass_pack.glb` se conserva en Sources y aporta cuatro variantes (`Grass_00` a `Grass_03`). La licencia y autor Anskar proceden del GLB y figuran en los créditos. Los pequeños grupos se distribuyen de forma reproducible, con variación de escala y giro, junto a la vegetación existente. Se preservan huecos entre grupos, el acceso al tronco, la meta y el espacio de los espectadores; no modifica el orden ni la distribución de los árboles.

Para preparar sólo este recurso sin regenerar los FBX anteriores:

```sh
blender -b --python tools/art/prepare_log_balance.py -- /ruta/absoluta/al/repositorio --asset low_poly_grass_pack
```

El comando de editor `LogBalanceAssetSetup.CreatePrefabs` acepta `--log-balance-source low_poly_grass_pack` en batch; el menú normal sigue preparando todos los recursos del manifiesto.

Validación de la incorporación del pasto: EditMode 183/183 PASS, flujo PlayMode 2/2 PASS, build Linux Succeeded con 0 errores/advertencias, validadores foundation/gestión PASS y revisión gráfica del ejecutable. [Captura con pasto](../evidence/log-balance-low-poly-2026-10-20/grass.png). Las reglas y controles del juego permanecen sin cambios.
