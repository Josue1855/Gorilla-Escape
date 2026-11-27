# Gorilla Escape — pruebaunion

Copia de integración para Windows y Unity **6000.6.0f1**.

## Abrir y jugar

1. Cierra otras sesiones del juego para liberar la cámara y el puerto local.
2. En Unity Hub agrega la carpeta `pruebaunion`, abre `Assets/Scenes/Connection.unity` y pulsa **Play**.
3. Escanea el QR en el celular, toca **ACTIVAR MOVIMIENTO** y concede los permisos del navegador.
4. Pulsa **CONFIRMAR** para entrar. Apunta con el celular y confirma para elegir un juego. Puedes usar el ratón en el menú.

Unity inicia el rastreador y Cloudflare desde esta carpeta. El enlace HTTPS requiere Internet y cambia cuando se reinicia el túnel.

## Juegos

| Juego | Control durante la partida |
| --- | --- |
| Banana Bowling | Celular |
| Boxeo con gorila | Celular y cámara |
| Jungle Baseball | Celular y cámara |
| Jungle Drums | Cámara y apoyo del celular |
| Coconut Slice | Cámara |
| Whack-a-Mole | Cámara |
| Coconut Throw | Celular y cámara |
| Log Balance | Inclinación del celular |
| Jungle Keeper | Cámara, manos y torso |

El menú y las tipografías se adaptaron de V2. Las seis primeras escenas provienen de Definitivo. Coconut Throw, Log Balance y Jungle Keeper usan la misma sesión de sensores que los demás juegos.

## Comprobaciones

`UnionValidation.Validate` abre las escenas importadas y las escenas clave de Definitivo, verifica las fuentes y los modelos de Whack-a-Mole y construye el escenario de Log Balance. El registro está en `IntegrationValidation.log`.
