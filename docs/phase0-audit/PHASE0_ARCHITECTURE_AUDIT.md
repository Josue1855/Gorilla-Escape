# Auditoría Técnica Independiente de Arquitectura — Phase 0
**Proyecto:** Gorilla Escape: Gorilimpiadas  
**Roles del Auditor:** Senior Software Architect · Lead Unity Game Developer · Backend/Networking Architect · QA/Performance Engineer · Security/Privacy Reviewer · Release/Build Engineer  
**Fecha:** 2026-10-14  
**Baseline Auditado:** `de54ff069b214015d83d722bc037258aeece7903` (rama `develop`, snapshot post-PR #110) en worktree aislado `audit/phase0-architecture`  
**Objetivo Central:** Determinar si la arquitectura actual de Phase 0 es suficientemente sólida, mantenible, escalable y adecuada para construir encima los cuatro minijuegos del Academic MVP (*Gorilla Smash*, *Coconut Throw*, *Coconut Bowling*, *Jungle Slice*) sin tener que rehacer la base.

---

## 1. Resumen Ejecutivo (Respuestas a las 16 Preguntas Clave)

1. **¿La Phase 0 está bien diseñada?**  
   **SÍ, en sus fundamentos de distribución y autoridad de estado.** La decisión de mantener a Unity como autoridad única de reglas, puntuación e idempotencia mediante [OfficialActionLedger.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Unity/Assets/_Project/Scripts/Input/OfficialActionLedger.cs), mientras Java se limita a señalización/transporte local ([TechnicalSession.java](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Server/src/main/java/com/gorillaescape/server/protocol/TechnicalSession.java)) y Python a visión desacoplada ([worker.py](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/tools/spikes/camera/worker.py)), es técnicamente impecable y evita la trampa clásica de lógica partida o "split-brain".

2. **¿Hay algún error arquitectónico serio?**  
   **SÍ, dos errores de diseño que requieren corrección antes de escalar:**
   - **Mecanismo Stop-and-Wait sobre WebRTC no confiable:** En [mobileClient.js](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/PWA/src/input/mobileClient.js#L108-L109), el envío de `MOTION_SAMPLE` se bloquea esperando un `ACK` con un timeout de 2000 ms (`ACK deadline`). Dado que el canal `motion` es UDP/no confiable (`maxRetransmits: 0`), si un paquete o su ACK se pierde por jitter o pérdida de paquetes Wi-Fi normal, la emisión de movimiento del móvil queda completamente congelada durante 2 segundos.
   - **Límite rígido de 4096 bytes en frame IPC:** En [IpcFrameCodec.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Shared/CSharp/Runtime/IpcFrameCodec.cs#L9), el buffer máximo es de 4 KB. Con 4 jugadores transmitiendo aceleración, giroscopio y estado de lobby simultáneamente, el payload JSON roza el límite y obligó al servidor a recortar o rotar datos ([ManagedProbe.java](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Server/src/main/java/com/gorillaescape/server/ipc/ManagedProbe.java#L103)).

3. **¿Existe algo que obligaría a rehacer la arquitectura durante gameplay?**  
   **NO a nivel estructural, pero SÍ a nivel de capa de entrada:**  
   `PhoneInputStore` y `InputFusion` solo exponen el *último fotograma* (`latest`). Los 4 minigames (*Smash, Throw, Bowling, Slice*) dependen de reconocer gestos en ventanas temporales (200–500 ms de historial para calcular pico de aceleración, arco de lanzamiento o plano de corte). Si no se introduce una capa de `GestureDetector` / `MotionHistoryBuffer`, cada minijuego tendrá que reinventar su propio buffer temporal, generando duplicación y dispersión de lógica.

4. **¿Unity está correctamente aislado de Java/PWA/Vision?**  
   **SÍ a nivel de contratos de datos.** `GorillaEscape.Contracts.asmdef` tiene `noEngineReferences: true`, y los contratos C# ignoran por completo los sockets, HTTP o MediaPipe.  
   **RISK menor en runner de laboratorio:** `CameraFusionLabRunner` consulta el disco (`File.Exists`) dentro de `Update()`, lo cual debe ser eliminado de los flujos de producción.

5. **¿Unity sigue siendo autoridad real?**  
   **SÍ.** La evidencia en [OfficialActionLedgerTests.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Unity/Assets/_Project/Tests/EditMode/OfficialActionLedgerTests.cs) y [OfficialActionProcessTests.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Unity/Assets/_Project/Tests/PlayMode/OfficialActionProcessTests.cs) demuestra que ninguna acción se vuelve oficial por recibir un sensor o un evento WebRTC; solo cuando Unity ejecuta `Actions.TryBegin()` y `Actions.Confirm()`.

6. **¿InputFusion es suficientemente extensible?**  
   **PARCIALMENTE.** En [InputFusion.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Unity/Assets/_Project/Scripts/Input/Fusion/InputFusion.cs#L73-L74), la propiedad `Eligible` exige que tanto el móvil como la cámara estén alineados (`Aligned`). Si una disciplina solo requiere móvil (o si la cámara está degradada), `Eligible` se evalúa como `false`. Debe desacoplarse la elegibilidad por disciplina para permitir modos solo-teléfono, solo-cámara o híbridos.

7. **¿El sistema podrá soportar cuatro minijuegos?**  
   **SÍ**, siempre que se implemente una máquina de estados de juego (`GameState / MinigameController`) y un buffer de análisis de gestos.

8. **¿Podrá soportar 1–4 jugadores?**  
   **SÍ en móvil; NO en cámara simultánea.** La arquitectura de `TechnicalSession` y `PhoneInputStore` maneja perfectamente 4 slots concurrentes con epochs independientes. Sin embargo, `PlayerLock` y `VisionSupervisor` están explícitamente diseñados para 1 jugador activo por turno en cámara (`num_poses=2` en MediaPipe, ROI central única). Esto cumple con el diseño por turnos de las Gorilimpiadas, pero no permite 4 personas jugando simultáneamente frente a la cámara.

9. **¿Podrá soportar torneo completo?**  
   **SÍ.** No hay dependencias de sesión única destructiva en `OfficialActionLedger`. Soporta múltiples intentos y acumulación de resultados si se añade la capa de puntuación del torneo (F40).

10. **¿Podrá soportar fallback (teclado/botón sin dispositivo)?**  
    **Actualmente NO sin refactorizar la inyección.** Hoy en día, los datos solo entran vía `PhoneInputStore.Accept(PhoneInputWire)`. Para soportar el requerimiento académico de fallback accesible sin teléfono (T060/#104), se debe abstraer la entrada detrás de una interfaz `IPlayerInputProvider`.

11. **¿La arquitectura es demasiado compleja?**  
    **Es densa y estricta, pero NO innecesariamente compleja.** No hay microservicios externos ni bases de datos. Los 3 procesos (Unity, Java, Python) responden a restricciones tecnológicas reales de la plataforma (WebRTC y Spring en Java; MediaPipe/OpenCV maduro en Python; render y física en Unity).

12. **¿Qué simplificarías?**  
    - Eliminar el stop-and-wait con ACK en el DataChannel de movimiento en la PWA.
    - Ampliar el frame IPC a 16 KB o 64 KB para evitar micro-optimizaciones y rotaciones forzadas.
    - Formatear el código C# que actualmente está hiper-condensado en una sola línea por método.

13. **¿Qué NO tocarías?**  
    - [OfficialActionLedger.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Unity/Assets/_Project/Scripts/Input/OfficialActionLedger.cs): La máquina de estados de acciones oficiales es sólida y exhaustivamente probada.
    - [TechnicalSession.java](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Server/src/main/java/com/gorillaescape/server/protocol/TechnicalSession.java): El manejo de admisiones efímeras, reconexión y epochs es robusto.
    - La arquitectura de hilos de la cámara en Python (`CapturePump` + `LatestFrameSlot` en [pipeline.py](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/tools/spikes/camera/pipeline.py)).

14. **¿Qué debe corregirse antes de cerrar E0?**  
    - Ejecutar las pruebas físicas T019 (1–4 teléfonos reales) y T020 (PC limpia física) que siguen `NOT RUN`.
    - Resolver el acoplamiento de build entre PWA y Server en [pom.xml](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Server/pom.xml#L17) (Server asume que `PWA/dist` ya fue compilado previamente).

15. **¿Qué debe corregirse antes de iniciar Phase 1?**  
    - Eliminar el timeout de 2 segundos en el envío de movimiento de la PWA.
    - Diseñar la interfaz de abstracción de entrada (`IPlayerInputProvider`) para habilitar el fallback (T060).
    - Exponer un buffer temporal de muestras en Unity para detección de gestos de los minijuegos.

16. **¿Qué puede esperar a Phase 2 / Post-MVP?**  
    - Tracking simultáneo de múltiples personas frente a la cámara (DEC-027 / #106).
    - Minijuego *Jungle Archery* (DEC-026).
    - Soporte multiplataforma para Windows/macOS (Phase 0 es estrictamente Linux x86_64).

---

## 2. Mapa Arquitectónico Real del Sistema

```mermaid
graph TD
    subgraph "Cliente Móvil (PWA React / Browser)"
        Sensor[SensorCapture 50Hz] --> Normalizer[normalizeMotion]
        Normalizer --> Client[MobileClient]
        Client -->|RTCDataChannel: motion (UDP, unack)| WebRTC_Net[WebRTC PeerConnection]
        Client -->|RTCDataChannel: control (TCP-like)| WebRTC_Net
    end

    subgraph "Backend Local (Java 21 / Spring Boot)"
        WebRTC_Net --> MobileRuntime[MobileRuntime / dev.onvoid.webrtc]
        MobileRuntime --> PhoneInputHub[PhoneInputHub (4 latest slots)]
        MobileRuntime --> TechnicalSession[TechnicalSession (Epoch, Admission, Lobby)]
        PhoneInputHub --> ManagedProbe[ManagedProbe (TCP 127.0.0.1)]
        TechnicalSession --> ManagedProbe
    end

    subgraph "Tracking Visual (Python 3.12 / venv)"
        Webcam[(V4L2 Webcam)] --> CapturePump[CapturePump Thread (30 FPS)]
        CapturePump --> LatestSlot[LatestFrameSlot (Drop if slow)]
        LatestSlot --> MediaPipe[MediaPipe PoseLandmarker (26.7 FPS)]
        MediaPipe --> WorkerStdout[worker.py Pipe Stdout]
    end

    subgraph "Motor de Juego (Unity 6 / Linux x86_64)"
        ManagedProbe -->|TCP Socket / Length-Prefix Frame| JavaSupervisor[JavaProbeSupervisor]
        JavaSupervisor --> PhoneStore[PhoneInputStore]
        JavaSupervisor --> LobbyBridge[LobbyAuthorityBridge]
        
        WorkerStdout -->|Pipe JSON Line| VisionSupervisor[VisionSupervisor]
        VisionSupervisor --> CameraStore[CameraInputStore]
        
        PhoneStore --> OfficialLedger[OfficialActionLedger (Single Authority)]
        PhoneStore --> Fusion[InputFusion]
        CameraStore --> Fusion
        Fusion --> PlayerLock[PlayerLock (Turn Subject Tracking)]
        
        OfficialLedger --> GameLoop[Minigames / Tournament Loop (Phase 1+)]
        Fusion --> GameLoop
    end
```

### Tabla de Responsabilidades y Trust Boundaries

| Componente | Responsabilidad | Trust Boundary | Fuente de Verdad (Source of Truth) | Estado |
|---|---|---|---|---|
| **PWA (React)** | Capturar sensores inerciales y toques en el móvil a máx 50 Hz. Normalizar sin inventar ejes. | Cliente no confiable (Untrusted). | Raw Sensor Events del hardware móvil. | **GOOD** |
| **Java Server** | Señalización local, WebRTC, admisiones QR de un solo uso, control de epochs. | Proceso local host (Loopback/LAN). | Estado de transporte, sockets y sesiones técnicas. | **GOOD** |
| **Python Vision** | Captura V4L2 en hilo dedicado, inferencia MediaPipe, exportación de landmarks JSON por pipe. | Proceso hijo local sin red. | Detección espacial efímera de landmarks. | **GOOD** |
| **Unity Runtime** | Evaluación de físicas, confirmación de acciones deportivas, cálculo de puntuaciones, reglas de torneo. | Proceso maestro del sistema. | **Autoridad única oficial de gameplay, score, turnos y torneos.** | **GOOD** |

---

## 3. Análisis de Principios Arquitectónicos

### 3.1 Source of Truth
- **Gameplay, Score y Turnos:** Centralizados de forma estricta en Unity. En [OfficialActionLedger.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Unity/Assets/_Project/Scripts/Input/OfficialActionLedger.cs#L37), se establece: *"Unity authority API: accepts observations, never creates actions from TOUCH/ACK/READY"*. Cada acción requiere una llamada deliberada de gameplay (`TryBegin`), es evaluada contra la frescura del jugador (`Fresh`), y solo se vuelve oficial mediante `Confirm`.
- **Identidad de Sesión y Reconexión:** Manejada por Java mediante `connectionEpoch`. Si un teléfono se desconecta y reconecta, su epoch se incrementa (`epoch + 1`). En Unity, el ledger cancela inmediatamente cualquier acción en estado `CANDIDATE` del epoch anterior, garantizando que acciones viejas no se apliquen tras reconectar.
- **Veredicto:** **GOOD**. No hay evidencia de estado deportivo duplicado en Java ni en la PWA.

### 3.2 Separation of Concerns
- **Puntos Fuertes:**
  - `Shared/CSharp` no tiene dependencias con el motor de Unity (`noEngineReferences: true`).
  - Java no tiene clases ni menciones a minijuegos específicos (no hay conceptos de "Coco", "Bolos" o "Smash").
  - La visión por computadora no envía imágenes a Unity; solo coordenadas normalizadas de puntos corporales.
- **Puntos Débiles (Logic Leakage):**
  - **Sobrecarga en JavaProbeSupervisor:** En [JavaProbeSupervisor.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Unity/Assets/_Project/Scripts/Network/JavaProbeSupervisor.cs), la clase orquesta el proceso del sistema operativo, el socket TCP, el protocolo de PING-PONG, la deserialización de inputs telefónicos, la sincronización del lobby y el cálculo de estadísticas de latencia.
  - **Alineación Rígida en InputFusion:** [InputFusion.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Unity/Assets/_Project/Scripts/Input/Fusion/InputFusion.cs#L74) acopla la frescura de la cámara con la elegibilidad de la entrada general (`Eligible = Aligned`). Si la disciplina no requiere cámara, la entrada de teléfono no debería ser marcada como no elegible.
- **Veredicto:** **RISK** moderado en runners de diagnóstico; **GOOD** en capas de dominio.

### 3.3 Dependency Direction
- Las dependencias apuntan hacia adentro:
  - `Minigames (Futuro)` → `PlayerInput / OfficialActionLedger` → `Contracts (DTOs puros)`.
  - El código de juego no depende de sockets TCP, ni de WebRTC, ni de OpenCV.
- **Riesgo Identificado:** Falta una interfaz mediadora (`IInputSource`) entre el hardware y los consumidores de entrada. Actualmente, `CameraFusionLabRunner` llama directamente a `FindFirstObjectByType<IpcProbeRunner>()`.
- **Veredicto:** **GOOD** en contratos; **RISK** en la falta de abstracción de fallback.

### 3.4 Extensibilidad para los Cuatro Minijuegos
| Minijuego | Requerimiento de Entrada | ¿Soportado por la Arquitectura Actual? | Brecha Identificada |
|---|---|---|---|
| **Gorilla Smash** | Golpe vertical descendente con teléfono + tracking corporal del salto/postura. | **Parcial** | Falta un detector de pico de aceleración en ventana temporal. |
| **Coconut Throw** | Arco de lanzamiento (pitch forward) + botón de suelta táctil + tracking de brazo. | **Parcial** | Falta correlación temporal entre trayectoria de ángulo y evento táctil `touch.pressed = false`. |
| **Coconut Bowling** | Swing bajo péndulo + dirección angular (yaw) + tracking de flexión de rodilla. | **Parcial** | Falta integrador de velocidad angular para inferir trayectoria del bolo. |
| **Jungle Slice** | Gestos rápidos de corte en múltiples ángulos (slash/slice) con aceleración y giroscopio. | **Parcial** | Falta evaluador de plano de corte espacial. |

> [!IMPORTANT]
> **Conclusión de Extensibilidad:** La base de datos cruda ([PlayerInput.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Unity/Assets/_Project/Scripts/Input/PlayerInput.cs)) transporta todos los campos necesarios (`acceleration`, `rotationRate`, `orientation`, `landmarks`). Lo que **falta** es una capa reutilizable de reconocimiento de gestos (`GestureRecognitionEngine`) en Unity. Sin ella, los desarrolladores de gameplay tenderán a programar heurísticas dispersas en el `Update()` de cada minijuego.

### 3.5 Overengineering (Evaluación de Complejidad)

| Componente | Justificación | Clasificación | Motivo |
|---|---|---|---|
| **Enmarcado IPC de 4096 bytes** | Limita artificialmente el tamaño de mensaje y causa cuellos de botella con 4 jugadores. | **SIMPLIFY** | Aumentar a 64 KB y simplificar código de fragmentación. |
| **Stop-and-wait ACK en WebRTC motion** | Obliga a esperar respuesta en un canal UDP no confiable, provocando congelamientos de 2s si se pierde un ACK. | **SIMPLIFY** | Convertir a transmisión continua (fire-and-forget) a 50 Hz con descarte de fotogramas viejos. |
| **Control V4L2 en Python (`camera_controls.py`)** | Guarda y restaura controles de exposición/ganancia de la cámara al arrancar/cerrar. | **KEEP** | Esencial en Linux para evitar autoexposición parpadeante durante tracking. |
| **Lockfiles y Supervisor con PID (`ManagedProcessGuard.java`)** | Evita procesos zombi de Java cuando Unity se cierra inesperadamente. | **KEEP** | Previene puertos ocupados y fugas de memoria en sesiones prolongadas. |
| **Cálculo de telemetría P50/P95 con ventanas de 4096 en caliente** | Arrays dinámicos y ordenamientos frecuentes en hot paths. | **REMOVE LATER** | Reemplazar por contadores acumulativos e histogramas fijos (HDR Histogram). |

---

## 4. Red Flags e Inspección de Código

A continuación se detallan los hallazgos críticos detectados en el código fuente:

```
RED FLAG #1: WebRTC Motion Stop-and-Wait sobre canal no confiable
Archivo: PWA/src/input/mobileClient.js (L108-L109)
Severidad: HIGH
Descripción:
  motion(payload,clientTimestamp=Date.now()){
    this.latest={payload,clientTimestamp};
    if(!this.inFlight) this.flush();
  }
  flush(){
    this.inFlight=true;
    this.send('MOTION_SAMPLE',...).finally(()=>{ this.inFlight=false; this.flush(); });
  }
  send(type, payload) {
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => { reject(Error('ACK deadline')); }, 2000);
      ...
    });
  }
Impacto:
  El canal 'motion' está configurado como maxRetransmits: 0 (UDP). Si el ACK devuelto por Java se pierde en el aire Wi-Fi, la promesa tarda 2000 ms en vencer. Durante 2 segundos, inFlight es true y ningún movimiento nuevo es transmitido, destruyendo la fluidez del control.
```

```
RED FLAG #2: Consulta síncrona a disco en el Update Loop de Unity
Archivo: Unity/Assets/_Project/Scripts/CameraTracking/CameraFusionLabRunner.cs (L48-L49)
Severidad: MEDIUM
Descripción:
  string path=Environment.GetEnvironmentVariable("GORILLA_FUSION_COMMAND");
  if(!string.IsNullOrEmpty(path)&&File.Exists(path)) try{
    if(new FileInfo(path).Length<=1024){
      var c=JsonUtility.FromJson<Command>(File.ReadAllText(path));
      ...
    }
  }
Impacto:
  Llamar a File.Exists() y File.ReadAllText() cada frame en el hilo principal de Unity provoca pausas en el render y picos de frame time.
```

```
RED FLAG #3: FindFirstObjectByType en cada frame de Update()
Archivo: Unity/Assets/_Project/Scripts/CameraTracking/CameraFusionLabRunner.cs (L38, L51)
Severidad: MEDIUM
Descripción:
  var owner=FindFirstObjectByType<IpcProbeRunner>();
  for(int player=1;player<=4;player++) if(owner.PhoneInputs.TryLatest(player,out var p)) fusion.ObservePhone(p);
Impacto:
  En Unity 6, FindFirstObjectByType recorre la jerarquía de la escena. Ejecutarlo 60 veces por segundo es un antipatrón evidente de rendimiento.
```

```
RED FLAG #4: Acoplamiento de Build en Server Maven
Archivo: Server/pom.xml (L17)
Severidad: HIGH
Descripción:
  <resource><directory>../PWA/dist</directory><targetPath>static</targetPath></resource>
Impacto:
  Si se clona el repositorio de cero y se ejecuta 'mvn test' en Server sin haber ejecutado antes 'npm run build' en PWA, Maven omite silenciosamente la carpeta faltante y los tests de integración fallan con error HTTP 404. La compilación de Java no orquesta la construcción de la PWA ni valida su presencia previa.
```

```
RED FLAG #5: Formato de código minificado y falta de documentación interna
Archivos: Unity/Assets/_Project/Scripts/CameraTracking/VisionSupervisor.cs,
          Unity/Assets/_Project/Scripts/Input/Fusion/InputFusion.cs,
          Unity/Assets/_Project/Scripts/Input/PhoneInputStore.cs
Severidad: MEDIUM (Mantenibilidad)
Descripción:
  Varios archivos C# clave tienen clases completas condensadas en 30-80 líneas con múltiples sentencias por línea, sin espacios ni comentarios explicativos.
Impacto:
  Dificulta severamente la lectura, depuración y mantenimiento por parte del equipo universitario o ingenieros que hereden el proyecto.
```

---

## 5. Tabla de Recuperación ante Fallos (Fault Recovery Matrix)

| Falla / Escenario | Detectado por | Respuesta del Sistema | Estado tras Falla | ¿Recupera automáticamente? | ¿Requiere reinicio? |
|---|---|---|---|---|---|
| **Caída del proceso Java** | `JavaProbeSupervisor.cs` (EOF en socket o exitCode del proceso hijo). | Cierra socket, notifica evento `JAVA_EXITED`, limpia canal de entrada y pasa a estado `FAILED`. | `IpcState.FAILED` | **No** (Permite reintento manual sin reiniciar Unity). | No |
| **Caída del proceso Python (Visión)** | `VisionSupervisor.cs` (EOF en stdout pipe o `child.HasExited`). | Restaura controles V4L2 vía subproceso auxiliar, libera cámara y marca estado `FAILED`. | `State = FAILED` | **No** (Desconecta cámara de forma segura). | No |
| **Caída de conexión WebRTC / Wi-Fi** | `mobileClient.js` (evento `close` en DataChannel); Java (`liveness` timeout 5s). | Móvil pasa a `DISCONNECTED`. Java marca cliente `DISCONNECTED`. Unity cancela acciones en curso. | `DISCONNECTED` | **Sí**, al pulsar reconectar en móvil con `resumeToken`. | No |
| **Poner móvil en segundo plano / Bloqueo** | `ControllerSession.js` (`visibilitychange` / `pagehide`). | Detiene sensores, envía mensaje `CLIENT_STATE: suspended`. Java suspende entrada. | `SUSPENDED` | **Sí**, al volver al navegador exige 3 muestras frescas antes de reactivar. | No |
| **Cierre de Unity por el usuario** | `ParentLifetime.java` (monitorea PID padre); `worker.py` (lee EOF en stdin). | Java y Python detectan la pérdida del padre y se auto-terminan inmediatamente sin procesos zombi. | `CLEAN_SHUTDOWN` | N/A | N/A |
| **Cámara ocupada por otro software** | `worker.py` (`capture.isOpened() == false`). | Emite error `CAMERA_UNAVAILABLE` y termina con código de salida limpio. | `FAILED` | No | Requiere liberar el dispositivo. |

---

## 6. Presupuesto de Rendimiento y Latencia

### 6.1 Presupuesto por Fotograma (Frame Budget — 60 FPS = 16.6 ms)

```
[----------- PRESUPUESTO UNITY MAIN THREAD (16.6 ms) -----------]
[ Engine Overhead: 2.0 ms ]
[ Update Input / IPC Ingestion: 1.5 ms ]
[ Physics & Minigame Gameplay: 5.0 ms ]
[ UI & Rendering Submission: 6.0 ms ]
[ GC Margin / Safety Buffer: 2.1 ms ]
```

- **Captura de Cámara:** Hilo independiente a **30 FPS** (33.3 ms).
- **Inferencia MediaPipe:** Hilo independiente a **26.7 FPS** (37.5 ms). No bloquea el render de Unity.
- **Transmisión de Teléfono:** Clamp de sensor a **50 Hz** (20.0 ms).
- **Sondeo IPC Unity-Java:** PING/PONG a **50 Hz** (20.0 ms).

### 6.2 Desglose de Latencia E2E (Movimiento Físico → Respuesta en Pantalla)

| Segmento | Componente | Latencia P50 | Latencia P95 | Notas / Evidencia |
|---|---|---|---|---|
| 1. Sensor Hardware | Sensor móvil + OS Callback | 10 ms | 20 ms | Frecuencia de muestreo nativa del hardware. |
| 2. PWA Normalization | Normalización y serialización JSON | 2 ms | 5 ms | Ejecución en JS main thread móvil. |
| 3. Red Local Wi-Fi | Transporte WebRTC DataChannel | 3 ms | 12 ms | Medición en LAB: ACK RTT P50 = 1.6–3.1 ms; P95 = 2.1–14 ms. |
| 4. Backend Java | Deserialización y buffer en PhoneInputHub | 1 ms | 3 ms | Operación en memoria concurrente. |
| 5. Transporte IPC | Polling TCP PING-PONG (50 Hz) | 10 ms | 20 ms | Jitter inherente al intervalo de 20 ms de sondeo. |
| 6. Unity Update Loop | Recepción en Store y ejecución de gameplay | 8 ms | 16.6 ms | Alineación con fotograma de 60 FPS. |
| 7. Render & Scanout | GPU rendering + VSync + Display | 12 ms | 20 ms | Dependiente del monitor de la PC anfitriona. |
| **TOTAL ESTIMADO E2E** | **Móvil → Pantalla PC** | **~46 ms** | **~96.6 ms** | **Cumple el objetivo RNF-GOR-01 (< 150 ms).** |

> [!CAUTION]
> Si ocurre pérdida de paquetes en el canal de movimiento mientras el stop-and-wait con ACK esté activo, la latencia salta a **> 2000 ms** (timeout de la promesa). Eliminar ese mecanismo es fundamental para asegurar el P95 en redes Wi-Fi reales.

---

## 7. Puntuación de Arquitectura (0 – 10)

| Dimensión | Puntuación | Justificación Técnica |
|---|:---:|---|
| **Arquitectura de Software** | **8.5 / 10** | Excelente delimitación de responsabilidades entre procesos. Unity posee la autoridad absoluta. |
| **Arquitectura Unity** | **6.5 / 10** | Scripts condensados, uso de `FindFirstObjectByType` y `File.Exists` en Update, falta de UI/Input System moderno. |
| **Networking & Protocolos** | **7.5 / 10** | WebRTC y protocolo Gorilla bien estructurados; penalizado por stop-and-wait en movimiento UDP y límite de 4 KB en IPC. |
| **Ciclo de Vida de Procesos** | **9.0 / 10** | Cierre determinista ejemplar. Lockfiles, pipes stdin/stdout con detección de EOF y sin fugas de procesos zombi. |
| **Arquitectura de Entrada** | **8.0 / 10** | `OfficialActionLedger` es de nivel de producción. Penalizado por falta de buffer histórico para gestos y falta de fallback. |
| **Cámara y Visión** | **8.5 / 10** | Desacoplamiento total de captura e inferencia. Caída limpia de fotogramas sin backlog de memoria. |
| **Rendimiento** | **7.5 / 10** | Inferencia a 26.7 FPS y Unity a 60 FPS estables en soak de 30 min. Serialización JSON en hot-paths genera allocations. |
| **Seguridad** | **9.0 / 10** | Tokens de admisión efímeros, certificados IP SAN, TLS local estricto, binds exclusivos a localhost y LAN privada. |
| **Privacidad** | **9.5 / 10** | Impecable. No se almacena video, ni fotos, ni historial biométrico. Solo coordenadas matemáticas efímeras. |
| **Testabilidad** | **8.5 / 10** | 59 tests PWA, 78 tests Java, tests de PlayMode con navegadores reales y pruebas de concurrencia con barreras. |
| **Mantenibilidad** | **6.0 / 10** | Código excesivamente condensado en Unity, falta de formateo estándar y acoplamiento implícito en el build de Maven. |
| **Extensibilidad** | **7.0 / 10** | Soporta 4 juegos conceptualmente, pero obligará a duplicar lógica de gestos si no se construye el motor de análisis. |
| **Empaquetado y Runtime Offline**| **8.5 / 10** | 100% offline, empaquetado reproducible con jlink y virtualenv en Linux. No apto para Windows aún. |
| **Observabilidad** | **8.5 / 10** | Logs estructurados JSON, cálculo de percentiles y telemetría no invasiva. |
| **Recuperación ante Errores** | **8.5 / 10** | Manejo de desconexiones y reconexiones limpio con conservación de puntuaciones confirmadas. |
| **PROMEDIO GLOBAL** | **8.1 / 10** | **Base sólida de nivel ingeniería, con deuda técnica técnica específica a corregir.** |

---

## 8. Clasificación Final de Hallazgos

### 8.1 Must Fix Before E0 (Cierre de Fase 0)
1. **Completar pruebas físicas reales T019:** El exit report actual mantiene T019 como `NOT RUN` para teléfonos físicos reales (iOS Safari y Android Chrome).
2. **Validar ejecución en PC limpia T020:** Ejecutar el paquete generado en una máquina Linux independiente sin SDK ni caches de desarrollo.
3. **Corregir dependencia de build en `Server/pom.xml`:** Hacer que el build de Maven falle con un mensaje explícito o invoque la compilación de la PWA si `PWA/dist` no existe.

### 8.2 Must Fix Before Phase 1 (Inicio de Gameplay)
1. **Eliminar Stop-and-Wait ACK en `mobileClient.js`:** Cambiar el flujo de muestras de movimiento a emisión unidireccional no bloqueante.
2. **Construir `GestureRecognitionEngine` / `MotionHistoryBuffer` en Unity:** Dotar a Unity de un buffer rodante de 500 ms de aceleración y giroscopio para que los 4 minijuegos reconozcan swings, smashes y lanzamientos limpiamente.
3. **Diseñar la interfaz de abstracción `IPlayerInputProvider` (T060 Fallback):** Permitir que las entradas provengan de móviles, de fixtures o de teclado/gamepad local sin alterar los minijuegos.
4. **Desacoplar la elegibilidad en `InputFusion.cs`:** Permitir que una disciplina defina si requiere o no la cámara para considerar la entrada como válida.
5. **Limpiar llamadas síncronas en `CameraFusionLabRunner`:** Eliminar `File.Exists` y `FindFirstObjectByType` del `Update()`.

### 8.3 Should Fix Later (Phase 2 / Hardening)
1. **Ampliar el tamaño máximo de frame IPC a 64 KB.**
2. **Re-formatear y documentar los scripts C# minificados.**
3. **Incorporar paquete formal de UI (`com.unity.ugui` o UI Toolkit) en Unity `manifest.json`.**

---

## 9. Veredicto Final

```
================================================================================
VEREDICTO: B — READY WITH MINOR HARDENING
================================================================================
```

### Justificación Técnica del Veredicto
La arquitectura de Phase 0 **NO TIENE ERRORES ESTRUCTURALES FATALES**. La separación de procesos (Unity maestro, Java transporte, Python visión) es la correcta para las tecnologías elegidas. Unity mantiene la autoridad absoluta sobre las reglas y la puntuación, el protocolo de sesiones es seguro y resistente a desconexiones, la privacidad está garantizada y el rendimiento en laboratorio (30 FPS cámara, 26.7 FPS inferencia, 60 FPS Unity) es estable en sesiones de 30 minutos.

No se le otorga calificación **A (READY)** porque existen dos riesgos técnicos concretos que deben mitigarse antes de programar gameplay:
1. El cuello de botella de stop-and-wait en el canal de movimiento de la PWA.
2. La ausencia de un buffer temporal de gestos y de una abstracción de fallback en Unity.

Ambos puntos son correcciones acotadas que no requieren rehacer la base arquitectónica. Con este endurecimiento menor y la acreditación de las pruebas físicas de T019/T020, el proyecto estará en una posición inmejorable para construir los cuatro minijuegos del Academic MVP.
