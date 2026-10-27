# Adaptador Jungle Party ↔ Fase 0

`IPortableInputSource` conserva la interfaz consumida por los juegos de Saúl. `MotionSession` es una fachada de gameplay; no crea salas, conexiones HTTP ni procesos.

`PortableServiceHost` administra una sesión mediante `JavaProbeSupervisor`, `VisionSupervisor`, `PhoneInputStore`, `InputFusion` y `PlayerLock`. Los supervisores restaurados desde `develop` conservan su implementación: no se reescribieron sus protocolos, presupuestos de lanzamiento ni limpieza. La interfaz y los puntos de entrada de diagnóstico impiden arrancar propietarios duplicados.

`JungleInputAdapter` convierte contratos Shared aceptados en DTO de compatibilidad locales a Unity. El flujo utiliza el jugador 1, descarta muestras incompletas/antiguas/duplicadas, conserva unidades y aplica una sola reflexión horizontal a las coordenadas no espejadas de cámara. Sólo entrega poses con asociación válida y alineación del teléfono/cámara. La falta de una mano mantiene su indicador de detección falso.

Los botones oficiales usan la superficie virtual de contacto: `x=0, y=0.5` para confirmar/lanzar y `x=1, y=0.5` para recentrar; `pressed` representa la pulsación. Se cuenta un flanco observado por pulsación, no cada muestra sostenida. No es un nuevo protocolo de red ni una garantía de entrega de acciones de torneo: continúa siendo latest-only y debe probarse físicamente.

El QR proviene de `QaAdmissionWire` por IPC y se dibuja en Unity; no se publica una URL de admisión ni se consulta un generador externo. La interfaz antigua `PublicControllerUrl` devuelve una cadena vacía por compatibilidad; no se utiliza para conectar.

La recuperación es manual, después de esperar `ShutdownAsync` y comprobar la limpieza. Al salir se espera el cierre de los supervisores. El arranque en Editor exige opt-in y está deshabilitado en batch mode. Para reutilizarlo se necesitan también Shared y el runtime oficial en `_Project`, no basta copiar esta carpeta.

Consulta [la integración y sus pendientes](../../../docs/03-development/SAUL_PHASE0_INTEGRATION.md).
