# Desafío del Tronco / Log Balance

Iteración de desarrollo: 2026-10-20, rama `feature/gameplay-desafio-del-tronco`. La dirección narrativa y este quinto prototipo fueron autorizados por el PO en el encargo de implementación. Las cuatro disciplinas anteriores conservan su propio alcance y estado.

## Historia y objetivo

El personaje controlable es un humano náufrago llegado accidentalmente a la isla de los gorilas. Para escapar debe superar las pruebas que le imponen. En esta prueba cruza un tronco sobre un río; los gorilas son espectadores NPC. El personaje usa el modelo Vagabundo/Castaway suministrado por el PO; el tronco y el ambiente reutilizan sus GLB trunk y low_poly_trees. Los espectadores usan el gorila low poly proporcionado y reaccionan con poses sobre su esqueleto original. Consultar [arte y animación](LOG_BALANCE_ART.md) para el rig, derivados y créditos.

Cuenta regresiva de tres segundos, avance automático y equilibrio lateral. Inclinar a la izquierda mueve el balance a la izquierda; inclinar a la derecha lo mueve a la derecha. Corregir hacia el centro evita la caída. El recorrido predeterminado es de 18 m a 0,75 m/s: unos 24 segundos de cruce, además de la cuenta regresiva.

## Abrir y probar con teclado

1. Abrir `Unity/` con Unity **6000.3.23f1** y licencia válida; esperar la importación.
2. Usar **Gorilla Escape → Play Gorilla Escape**.
3. Elegir **DESAFÍO DEL TRONCO · PROTOTIPO**. El torneo general sigue separado de esta entrada.
4. Pulsar **PROBAR CON TECLADO**, luego **COMENZAR** o Espacio.
5. Usar A/flecha izquierda y D/flecha derecha. Mantener una dirección extrema permite comprobar la caída. Corregir suavemente permite llegar a la meta.
6. Revisar tiempo sobrevivido, metros, porcentaje y resultado; **OTRO INTENTO** o R limpia el intento. **VOLVER** o Escape regresa al menú.

El teclado sólo está disponible en Editor y Development Build, mediante selección explícita; no sustituye silenciosamente un teléfono desconectado. El foco perdido pausa y requiere reanudar.

## Probar con teléfono

Se conserva PWA → WebRTC → Java → IPC → `PhoneInputStore`; no hay otro backend ni cambios al protocolo. Usar la preparación del [producto integrado](../03-development/SETUP_AND_RUN.md) y el [contrato de conexión](../PHASE0_PHONE_CONNECTION_FINAL.md).

1. Preparar los runtimes de Java/cámara y la conectividad del producto. Para Editor, los servicios físicos exigen el opt-in existente `GORILLA_EDITOR_PHYSICAL_QA=1`, Bootstrap, rutas de Java/JAR válidas y un entorno compatible. No se anula la protección del Test Runner.
2. Desde el menú, **CONECTAR TELÉFONO**; escanear el QR JOIN y completar conexión y permisos de sensores. Resolver permisos de firewall/confianza con la recuperación existente, sin ignorar errores TLS.
3. Volver y entrar al prototipo. Seleccionar el slot 1–4 correspondiente al teléfono admitido; el MVP procesa sólo ese jugador por intento.
4. Mantener el teléfono **vertical, con su parte superior arriba** y en una posición cómoda. Pulsar **CALIBRAR** o C en la PC; se guarda el gamma neutro de esta conexión. Pulsar **COMENZAR**.
5. Inclinar lateralmente. Una muestra de otro jugador/sesión/dispositivo/epoch no puede mover el intento seleccionado. Datos ausentes, orientación no disponible o más de 350 ms sin recepción pausan el cruce y la cuenta regresiva.
6. Al recuperar datos, **REANUDAR**. Cambiar de conexión exige calibrar de nuevo; cada intento nuevo también exige calibración. Girar la pantalla a horizontal bloquea el control: volver a vertical antes de continuar.

El adaptador conserva el `PhoneInputWire` validado y usa gamma en grados; no fabrica el quaternion que `TryPhoneInput` declara ausente. El giro horizontal del teléfono no está soportado en este MVP. La cámara no es un requisito para calcular este equilibrio; su supervisor existente conserva su lifecycle y no se duplica.

## Reglas y configuración

`Assets/_Project/Resources/LogBalanceSettings.asset` es un ScriptableObject editable en Inspector, cargado al abrir el prototipo. Si falta puede crearse con **Gorilla Escape → Log Balance → Create Default Settings**. No hay que crear o conectar escenas/prefabs manualmente; la escena Bootstrap existente aloja la prueba y su escenario se crea y libera al entrar/salir.

Parámetros: cuenta regresiva, longitud, velocidad, duración máxima, grados para tilt completo, dead zone, smoothing, fuerza de control, perturbación, inestabilidad, dificultad, clamp, umbral/gracia de caída y freshness. Los cambios se aplican al abrir un nuevo prototipo. La duración máxima debe permitir completar longitud/velocidad.

La lógica usa pasos de 10 ms, zona muerta reescalada, suavizado exponencial y perturbación determinista gradual. La perturbación no es viento ni un sistema de obstáculos. El balance se limita a ±1 por defecto; superar ±0,85 durante 0,65 s causa caída. Volver dentro del umbral limpia ese tiempo de riesgo. Caída prevalece si coincide con meta en el mismo paso; un resultado terminal no puede cambiar a otro. Durante una pausa no avanza el tiempo ni la distancia. Los frames de más de 100 ms se limitan para evitar saltos bruscos del control.

`LogBalanceResult` es un resultado inmutable del intento en memoria: jugador, victoria/caída, segundos, distancia, progreso y motivo. No existía un sistema común de resultados deportivos en esta base; no se usa `OfficialActionLedger` como puntuación ni se introduce persistencia/API/GP paralela. Publicación al backend y torneo quedan pendientes de integración acordada.

## Componentes

- `LogBalanceModel.cs`: reglas y resultado independientes de Unity/physics.
- `LogBalanceTilt.cs`: adaptación del input validado, calibración y freshness.
- `LogBalanceSettings.cs`: configuración ScriptableObject.
- `LogBalanceController.cs`: intento, selección de control/slot, pausa, UI y reinicio.
- `LogBalanceStage.cs`: selva low poly con modelos suministrados, superficie de apoyo del tronco, espectadores, cámara y limpieza.
- `LogBalanceCharacter.cs`: poses del rig original para caminata, equilibrio, caída y victoria.
- `LogBalanceSetup.cs`: creación idempotente de la configuración y recursos del escenario.
- `LogBalanceTests.cs` y `LogBalanceFlowTests.cs`: reglas, sensores sintéticos y navegación/flujo.

## Alcance de validación

Consultar [validación de esta iteración](LOG_BALANCE_VALIDATION.md). Una prueba sintética de orientación acredita el adaptador, no la sensación con un teléfono físico. El arte low poly y la animación procedural del humano están integrados; no hay competición simultánea, viento, obstáculos, mapas múltiples ni cinemáticas. T019/T020 y aceptación de hardware mantienen su estado propio.

## Arranque del Player con Cloudflare

En Linux, ejecutar `./run-game.sh` desde la raíz. Requiere el Player Development actual en `Unity/Builds/FoundationLinux`, el JAR construido y un paquete integrado con Java, Vision y `Tools/cloudflared`. Configurar `GORILLA_RUNTIME_ROOT` a ese paquete, o crear el enlace local `~/.gorilla-escape/runtime`. El lanzador verifica el binario fijado y entrega su ruta al supervisor existente.

Cloudflare comienza durante el arranque de los servicios, antes de abrir CONECTAR TELÉFONO. El modo predeterminado del lanzador es AUTO: arranca Cloudflare y, si falla la comprobación HTTPS, recupera la conexión HTTPS local. Para uso sin Internet se puede elegir `GORILLA_CONNECTION_MODE=CABIN ./run-game.sh`; AUTO permite la recuperación local existente. El QR se entrega después de comprobar HTTPS público. El túnel se cierra con el juego; los datos del control siguen por WebRTC en la misma LAN.

### Apertura directa del ejecutable

El Player configura sus servicios al arrancar: busca `Java/runtime/bin/java`, `Java/server.jar`, `Vision`, `Tools/cloudflared` y `tools/setup-firewall.sh` junto al ejecutable. Inicia Java y el túnel supervisado sin abrir el panel del teléfono ni ejecutar un script. AUTO es el valor predeterminado: intenta Cloudflare desde el arranque y conserva el QR local cuando HTTPS público no queda disponible. Una configuración explícita CLOUDFLARED o CABIN tiene prioridad. La credencial local se genera en memoria y no se guarda en archivos. La navegación del Editor conserva su aislamiento.

Para preparar la carpeta Development local una sola vez después del build/JAR: `./run-game.sh --prepare-only` con el runtime configurado arriba. Después abrir directamente `Unity/Builds/FoundationLinux/GorillaEscape.x86_64`. Los enlaces de runtimes son una comodidad local y no sustituyen al empaquetado de distribución mediante `tools/t020/package.py`.

Validación del arranque directo: 184/184 EditMode, 3/3 flujos PlayMode y build Linux sin errores ni advertencias. Al ejecutar el Player sin lanzador ni variables GORILLA externas se observó Java RUNNING, cámara lista y el proceso cloudflared iniciado automáticamente. La comprobación pública HTTPS en esta red no se confirmó dentro del presupuesto del supervisor; iniciar el proceso no acredita una conexión física móvil ni disponibilidad WAN.

## Tronco giratorio y pruebas desde Unity Editor

El tronco suministrado gira sobre su eje longitudinal a 18 grados por segundo durante el cruce. Su posición permanece centrada entre las orillas. El apoyo del personaje se calcula contra los triángulos del modelo ya girados. La rotación usa el tiempo del intento: se detiene al pausar o terminar y vuelve a cero al reiniciar. Las reglas y sensores de equilibrio conservan su comportamiento.

Desde Unity Hub abrir este proyecto (`Unity/`), esperar la importación y usar **Gorilla Escape → Play Tests with Phone**. El comando abre Bootstrap y el centro de pruebas con Java, cámara y Cloudflare preparados desde la carpeta Development local. Requiere preparar esa carpeta con `./run-game.sh --prepare-only` como se indica arriba. No hay que definir variables en una terminal para iniciar el editor. Al detener Play se restauran las variables previas; el Test Runner conserva el bloqueo de servicios físicos. Para probar únicamente el juego con teclado sigue disponible **Play Gorilla Escape**.

Validación de esta iteración: 184/184 EditMode y 4/4 flujos PlayMode, incluida rotación real del tronco, pausa y reinicio. Build Linux Succeeded con 0 errores y 0 advertencias. La conexión física de iPhone y HTTPS pública siguen requiriendo comprobación en la red real.

El Play normal del Editor en Bootstrap también prepara automáticamente los servicios físicos si existe el runtime local completo. No se activan en batch, Test Runner ni otras escenas. Si faltan recursos o Java falla, CONECTAR TELÉFONO muestra el problema; no permanece en «Preparando conexión…» por servicios apagados.

Validación de la corrección del QR: 185/185 EditMode en el editor y build Linux sin errores ni advertencias. Prueba real de arranque sin teléfono: Java RUNNING, firewall READY y QR AVAILABLE después de recuperar la conexión local. La prueba externa de Cloudflare quedó BLOCKED por HttpConnectTimeoutException/TUNNEL_HTTPS_UNAVAILABLE, con hash válido y túnel registrado; no se declara HTTPS público ni iPhone físico aprobado. El botón REINTENTAR también vuelve a iniciar el modo de conexión si el bootstrap falló.
