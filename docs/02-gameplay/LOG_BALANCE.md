# Desafío del Tronco / Log Balance

Iteración de desarrollo: 2026-10-20, rama `feature/gameplay-desafio-del-tronco`. La dirección narrativa y este quinto prototipo fueron autorizados por el PO en el encargo de implementación. Las cuatro disciplinas anteriores conservan su propio alcance y estado.

## Historia y objetivo

El personaje controlable es un humano náufrago llegado accidentalmente a la isla de los gorilas. Para escapar debe superar las pruebas que le imponen. En esta prueba cruza un tronco sobre un río; los gorilas son espectadores NPC. La geometría provisional diferencia la figura humana con camisa, brazos extendidos y piernas de los gorilas de torso ancho y brazos largos. No se incorporaron assets externos.

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
- `LogBalanceStage.cs`: humano, tronco, río, orillas, meta, espectadores, cámara y limpieza.
- `LogBalanceSetup.cs`: creación idempotente de la configuración y recursos del escenario.
- `LogBalanceTests.cs` y `LogBalanceFlowTests.cs`: reglas, sensores sintéticos y navegación/flujo.

## Alcance de validación

Consultar [validación de esta iteración](LOG_BALANCE_VALIDATION.md). Una prueba sintética de orientación acredita el adaptador, no la sensación con un teléfono físico. El arte es provisional; no hay animaciones de NPC, competición simultánea, viento, obstáculos, mapas múltiples ni cinemáticas. T019/T020 y aceptación de hardware mantienen su estado propio.
