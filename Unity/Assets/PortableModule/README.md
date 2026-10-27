# Entrada Jungle Party sobre Fase 0

`IPortableInputSource` entrega un `JungleFusedFrame` tipado y permite autorizar acciones por revisión. Se eliminan las lecturas JSON separadas de teléfono/pose. `MotionSession` mantiene la sesión entre escenas y es el propietario de la pausa técnica al perder entrada válida.

`PortableServiceHost` reutiliza los supervisores oficiales, PhoneInputStore, PlayerLock e InputFusion. Consulta `EvaluateLatest` y entrega su resultado a las reglas puras de `Scripts/Gameplay/Input/`; no conserva una ruta de gameplay que lea directamente el teléfono. Los supervisores no se reescriben.

Cada uno de los siete juegos exige las muñecas que necesita y un teléfono alineado. Las velocidades usan el tiempo de captura, los pulsos requieren rearme y una revisión no autoriza dos veces la misma acción. Los controladores verifican la autorización antes de cambiar su mecánica/puntuación. Los cortes continuos reciben una autorización por mano y por muestra de cámara nueva.

El modo actual usa al jugador 1. Compartir la infraestructura no declara soporte multijugador migrado, aceptación física ni instalador terminado. Los umbrales se mantienen como parámetros de prototipo pendientes de calibración.

El QR se dibuja localmente desde los módulos enviados por Java en IPC autenticado. Los botones de la PWA usan el grupo touch del contrato existente para confirmar/recentrar; no generan por sí mismos golpes deportivos. La recuperación de procesos sigue siendo manual y exige limpieza completa. El Editor necesita opt-in y permiso del test runner; no se inicia hardware en batch mode.

Para reutilizar este adaptador se requieren Shared, `_Project` y los runtimes oficiales; no basta copiar esta carpeta. [Arquitectura, matriz por juego, pruebas y aceptación pendiente](../../../docs/02-gameplay/JUNGLE_FUSED_INPUT.md).
