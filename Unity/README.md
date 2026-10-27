# Jungle Party / Gorilla Escape — integración de Saúl con Fase 0

Estado: **integración de código; aceptación física pendiente**. Esta rama conserva las escenas y recursos del alpha de Saúl e incorpora `develop` y el runtime de Fase 0. No se ha integrado esta rama hacia `develop` ni `main`.

## Proyecto y versión

Abrir esta carpeta `Unity/` con **Unity 6000.6.0f1**, la versión ya utilizada por las escenas de Saúl. No se degradaron las escenas a 6000.3.23f1. El pin histórico de Fase 0 era 6000.3.23f1; aceptar ambas versiones en el validador de metadatos no demuestra compatibilidad del motor.

Se conservan `Connection`, `MainMenu`, `BananaBowling`, `GorillaBoxing`, `JungleBaseball`, `JungleDrums`, `CoconutSlice` y `WhackAMole`, además del Bootstrap técnico restaurado en `Assets/_Project/Scenes/Bootstrap/`.

## Conexión vigente

Los juegos consumen `MotionSession`, que delega en `PortableServiceHost`. Este componente utiliza `JavaProbeSupervisor` y `VisionSupervisor` de Fase 0. El teléfono utiliza la PWA oficial, WebRTC y el Java supervisado; Unity recibe contratos Shared por IPC autenticado en loopback. La cámara utiliza el adaptador oficial y sus pipes privados.

Los scripts antiguos `CameraTracker/`, `MotionRelay/` y `PREPARAR_MODULO.ps1` se conservan como referencia del alpha. **El flujo migrado no los ejecuta**, no prepara dependencias por PowerShell ni crea su propio túnel. El QR se dibuja localmente a partir de los módulos enviados por Java; no utiliza servicios externos de generación de QR.

## Desarrollo local

1. Ejecutar `npm --prefix PWA ci`, `npm --prefix PWA run build` y `Server/mvnw -B -f Server/pom.xml verify` desde la raíz del repositorio.
2. Preparar los runtimes y variables de Fase 0. Para el editor puede usarse `tools/integration/run-jungle-editor.sh /ruta/al/Editor/Unity`. Requiere JDK 21 instalado sólo en la máquina de desarrollo; la distribución final usa el Java empaquetado.
3. Para cámara, proporcionar el paquete oficial `Vision` mediante `GORILLA_PACKAGE_ROOT` o configurar las variables de laboratorio documentadas en [integración](../docs/03-development/SAUL_PHASE0_INTEGRATION.md). La cámara no usa el rastreador antiguo del alpha.
4. Abrir `Assets/Scenes/Connection.unity` y pulsar Play. El arranque físico en Editor requiere `GORILLA_EDITOR_PHYSICAL_QA=1`; los tests y la edición ordinaria no arrancan servicios.
5. Autorizar la red local si el sistema lo solicita. Usar Paso 1 para preparación del certificado cuando corresponda y Paso 2 para unirse. El código aleatorio de cuatro letras del prototipo ya no se usa.
6. Conceder permisos de sensores en la PWA. Confirmar/lanzar y recentrar se transmiten como zonas de contacto dentro del contrato oficial. El alpha utiliza el jugador 1; no anuncia soporte multijugador migrado.

No abrir simultáneamente la sesión técnica Bootstrap y la sesión de juegos: cada flujo tiene su propietario y la integración impide iniciar un segundo supervisor cuando el diagnóstico está activo.

## Validación y límites

[Resultados, comandos reproducibles y pendientes](../docs/03-development/SAUL_PHASE0_INTEGRATION.md). La compilación completa/importación en Unity, las pruebas físicas de controles y la distribución Windows están pendientes. La cámara oficial conserva las limitaciones Windows de Fase 0; esta migración no las resuelve ni declara un instalador final.
