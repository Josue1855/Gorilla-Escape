# Jungle Party / Gorilla Escape — integración de Saúl con Fase 0

Estado: **integración de código; aceptación física pendiente**. Esta rama conserva las escenas y recursos del alpha de Saúl e incorpora `develop` y el runtime de Fase 0. No se ha integrado esta rama hacia `develop` ni `main`.

## Proyecto y versión

Abrir esta carpeta `Unity/` con **Unity 6000.6.0f1**, la versión ya utilizada por las escenas de Saúl. No se degradaron las escenas a 6000.3.23f1. El pin histórico de Fase 0 era 6000.3.23f1; aceptar ambas versiones en el validador de metadatos no demuestra compatibilidad del motor.

Se conservan `Connection`, `MainMenu`, `BananaBowling`, `GorillaBoxing`, `JungleBaseball`, `JungleDrums`, `CoconutSlice` y `WhackAMole`, junto con `LogBalance` (Desafío del Tronco), además del Bootstrap técnico restaurado en `Assets/_Project/Scenes/Bootstrap/`.

## Conexión vigente

Los juegos consumen `MotionSession`, que delega en `PortableServiceHost`. Este componente utiliza `JavaProbeSupervisor` y `VisionSupervisor` de Fase 0. El teléfono utiliza la PWA oficial, WebRTC y el Java supervisado; Unity recibe contratos Shared por IPC autenticado en loopback. La cámara utiliza el adaptador oficial y sus pipes privados.

Los scripts antiguos `CameraTracker/`, `MotionRelay/` y `PREPARAR_MODULO.ps1` se conservan como referencia del alpha. **El flujo migrado no los ejecuta**, no prepara dependencias por PowerShell ni crea su propio túnel. El QR se dibuja localmente a partir de los módulos enviados por Java; no utiliza servicios externos de generación de QR.

## Desarrollo local

1. Ejecutar `npm --prefix PWA ci`, `npm --prefix PWA run build` y `Server/mvnw -B -f Server/pom.xml verify` desde la raíz del repositorio.
2. Preparar los runtimes y variables de Fase 0. Para el editor puede usarse `tools/integration/run-jungle-editor.sh /ruta/al/Editor/Unity`. Requiere JDK 21 instalado sólo en la máquina de desarrollo; la distribución final usa el Java empaquetado.
3. Para cámara, proporcionar el paquete oficial `Vision` mediante `GORILLA_PACKAGE_ROOT` o configurar las variables de laboratorio documentadas en [integración](../docs/03-development/SAUL_PHASE0_INTEGRATION.md). La cámara no usa el rastreador antiguo del alpha.
4. Usar **Gorilla Escape → Play Gorilla Escape**. Este comando prepara las rutas locales de Java/JAR, registra las diez escenas y abre `Assets/Scenes/Connection.unity` antes de activar Play. **Open Bootstrap** abre solamente el diagnóstico técnico. También puede abrirse Connection manualmente usando el launcher; el arranque físico exige opt-in y permiso del test runner. Los tests y la edición ordinaria no arrancan servicios.
5. Autorizar la red local si el sistema lo solicita. Usar Paso 1 para preparación del certificado cuando corresponda y Paso 2 para unirse. El código aleatorio de cuatro letras del prototipo ya no se usa.
6. Conceder permisos de sensores en la PWA. Confirmar/lanzar y recentrar se transmiten como zonas de contacto dentro del contrato oficial. El alpha utiliza el jugador 1; no anuncia soporte multijugador migrado.

No abrir simultáneamente la sesión técnica Bootstrap y la sesión de juegos: cada flujo tiene su propietario y la integración impide iniciar un segundo supervisor cuando el diagnóstico está activo.

## InputFusion en los siete juegos

Las mecánicas consumen ahora una muestra fusionada tipada y requieren autorización de gesto. La pérdida de seguimiento pausa tiempo y física; el ratón y el teléfono sin cámara no son vías alternativas de puntuación. [Reglas por juego, rearme y pruebas](../docs/02-gameplay/JUNGLE_FUSED_INPUT.md).

## Validación y límites

[Resultados, comandos reproducibles y pendientes](../docs/03-development/SAUL_PHASE0_INTEGRATION.md). La compilación completa/importación en Unity, las pruebas físicas de controles y la distribución Windows están pendientes. La cámara oficial conserva las limitaciones Windows de Fase 0; esta migración no las resuelve ni declara un instalador final.

Corrección del punto de entrada y verificaciones: [detalle](../docs/03-development/SAUL_PRODUCT_ENTRY.md).

El séptimo juego conserva el modelo físico y el náufrago del tronco, y reutiliza la selva, los gorilas y el tema de Saúl. [Integración, créditos y prueba pendiente](../docs/02-gameplay/LOG_BALANCE_SAUL.md).

## Cloudflared del paquete de Fase 0

El menú del Editor y `run-jungle-editor.sh` reutilizan `Unity/Builds/FoundationLinux` si contiene Vision y el helper de firewall. Cloudflared se busca en el paquete seleccionado, en `Tools/cloudflared` del repositorio o en aquel paquete preparado; una ruta explícita `GORILLA_CLOUDFLARED_PATH` tiene prioridad. Se mantiene `AUTO` salvo configuración explícita. Java verifica el SHA-256 fijado antes de ejecutarlo; Play no descarga ni instala herramientas.

En esta PC ya existe el binario revisado dentro del paquete preparado. Un checkout nuevo necesita los artefactos de distribución: el helper existente `python3 tools/t063/package-cloudflared.py /ruta/al/binario-revisado /ruta/al/paquete` incluye binario, licencia y manifiesto. No se versionan copias de los ejecutables generados. Este soporte de empaquetado es Linux amd64; no certifica Windows.

[Comprobación real y límite de conectividad observado](../docs/evidence/saul-log-balance-2026-10-27/cloudflared.md). Que el proceso se registre no garantiza HTTPS público disponible. AUTO conserva la vía LAN si la comprobación externa falla.
