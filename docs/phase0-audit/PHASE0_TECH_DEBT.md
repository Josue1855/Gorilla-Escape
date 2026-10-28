# Inventario de Deuda Técnica — Phase 0
**Proyecto:** Gorilla Escape: Gorilimpiadas  
**Fecha:** 2026-10-14  
**Clasificación de Fases de Corrección:**
- **FIX BEFORE E0:** Requisitos bloqueantes para dar por cerrada la Fase 0.
- **FIX BEFORE PHASE 1:** Deuda arquitectónica que debe resolverse antes de empezar a programar la lógica de gameplay.
- **FIX DURING PHASE 2:** Mejoras de robustez y rendimiento durante la consolidación de minijuegos y torneos.
- **POST-MVP:** Características avanzadas no requeridas para la entrega académica.

---

## 1. Matriz Resumen de Deuda Técnica

| ID | Título de la Deuda | Componente | Severidad | Costo si se Ignora | Fase Recomendada |
|---|---|---|:---:|---|---|
| **DEBT-01** | Stop-and-Wait ACK en WebRTC Motion Channel | PWA Client | **HIGH** | Congelamientos de 2 segundos en el control por pérdida de paquetes Wi-Fi. | **FIX BEFORE PHASE 1** |
| **DEBT-02** | Acoplamiento implícito en build de Server Maven sin compilar PWA | Server Build | **HIGH** | Builds rotos y fallos 404 en CI o en clones limpios del repositorio. | **FIX BEFORE E0** |
| **DEBT-03** | Falta de ventana temporal rodante para detección de gestos | Unity Input | **HIGH** | Cada minijuego reinventará su propio analizador temporal en `Update()`. | **FIX BEFORE PHASE 1** |
| **DEBT-04** | Falta de abstracción para Fallback de entrada (Teclado/Botón) | Unity Input | **HIGH** | Incumplimiento del requisito académico T060/#104; imposible jugar sin teléfono. | **FIX BEFORE PHASE 1** |
| **DEBT-05** | Consulta síncrona a disco (`File.Exists`) en Update de Unity | Unity Lab | **MEDIUM** | Micro-stuttering y caídas de frames en el render principal. | **FIX BEFORE PHASE 1** |
| **DEBT-06** | Límite estricto de 4096 bytes en frame IPC | IPC Protocol | **MEDIUM** | Descarte o rotación forzada de telemetría de 4 jugadores en sesiones densas. | **FIX DURING PHASE 2** |
| **DEBT-07** | Búsqueda de objetos en jerarquía (`FindFirstObjectByType`) en Update | Unity Lab | **MEDIUM** | Desperdicio de CPU de 1–2 ms por frame en la búsqueda del runner de IPC. | **FIX BEFORE PHASE 1** |
| **DEBT-08** | Asignaciones de memoria (GC Allocations) en hot path de IPC y Fusion | Unity Core | **MEDIUM** | GC Spikes periódicos cada pocos segundos durante el juego activo. | **FIX DURING PHASE 2** |
| **DEBT-09** | Código C# hiper-condensado y sin formateo estándar | Unity Scripts | **MEDIUM** | Dificultad extrema para que otros estudiantes lean o modifiquen el código. | **FIX DURING PHASE 2** |
| **DEBT-10** | Ausencia del paquete oficial de UI en `manifest.json` | Unity Packages | **MEDIUM** | Bloqueo al diseñar HUDs deportivos, menús y pantallas de torneo. | **FIX BEFORE PHASE 1** |
| **DEBT-11** | Pruebas Físicas T019 no ejecutadas en teléfonos reales | QA / Testing | **BLOCKER** | Cierre prematuro de E0 sin validar latencia óptica real ni Safari/Chrome móvil. | **FIX BEFORE E0** |
| **DEBT-12** | Validación en PC Limpia T020 no ejecutada | Release / QA | **BLOCKER** | Incertidumbre sobre dependencias de sistema y ejecución sin SDK instalado. | **FIX BEFORE E0** |
| **DEBT-13** | Falta de tracking simultáneo de múltiples personas en cámara | Python Vision | **LOW** | Ninguno en el MVP (las Gorilimpiadas son por turnos). Aplica a Post-MVP. | **POST-MVP** |

---

## 2. Fichas Detalladas de Deuda Técnica (Formato Why / Evidence / Risk / Cost / When)

### DEBT-01: Stop-and-Wait ACK en WebRTC Motion Channel
- **WHY:** El canal `motion` de WebRTC fue configurado como no confiable y desordenado (`maxRetransmits: 0`) para actuar como un canal UDP de baja latencia. Sin embargo, en el código cliente de la PWA se introdujo un patrón de promesa que espera un `ACK` del servidor antes de permitir el siguiente envío, con un timeout rígido de 2000 ms.
- **EVIDENCE:** [mobileClient.js](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/PWA/src/input/mobileClient.js#L101-L109):
  ```javascript
  const timer = setTimeout(() => { reject(Error('ACK deadline')); }, 2000);
  ...
  flush() {
    this.inFlight = true;
    this.send('MOTION_SAMPLE', ...).finally(() => { this.inFlight = false; this.flush(); });
  }
  ```
- **RISK IF NOT FIXED:** Si se pierde un paquete UDP o un ACK en la red Wi-Fi (algo habitual en entornos con múltiples teléfonos), la transmisión de movimiento del jugador se detiene durante 2 segundos completos. Para un juego deportivo en tiempo real como *Gorilla Smash* o *Jungle Slice*, esto arruina la jugabilidad.
- **COST:** Muy bajo (1 a 2 horas de refactorización en `mobileClient.js` para desacoplar el envío de movimiento de la resolución de promesas).
- **WHEN TO FIX:** **FIX BEFORE PHASE 1**.

---

### DEBT-02: Acoplamiento implícito en build de Server Maven sin compilar PWA
- **WHY:** `Server/pom.xml` copia los recursos estáticos desde `../PWA/dist` hacia `static/` durante el empaquetado del JAR. Sin embargo, Maven no invoca `npm run build` en la carpeta `PWA`, ni verifica si la carpeta existe.
- **EVIDENCE:** [pom.xml](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Server/pom.xml#L17):
  ```xml
  <resource><directory>../PWA/dist</directory><targetPath>static</targetPath></resource>
  ```
  Al ejecutar `mvn test` en un clon limpio del repositorio sin haber compilado manualmente la PWA, Maven imprime `[INFO] skip non existing resourceDirectory .../PWA/dist` y los tests `ServerIntegrationTest.servesPackagedReactShell` y `MobileHttpsTest` fallan con error HTTP 404.
- **RISK IF NOT FIXED:** Integración continua rota, fallos de compilación intermitentes entre miembros del equipo que clonen el repositorio, y riesgo de empaquetar un JAR sin interfaz web.
- **COST:** Bajo (agregar un script orquestador en la raíz o configurar `frontend-maven-plugin`).
- **WHEN TO FIX:** **FIX BEFORE E0**.

---

### DEBT-03: Falta de ventana temporal rodante para detección de gestos
- **WHY:** En Phase 0 se construyó la infraestructura de transporte y almacenamiento inmediato (`PhoneInputStore`), la cual almacena únicamente la muestra más reciente (`latest`) de cada jugador. No se incluyó una capa de análisis temporal de movimiento.
- **EVIDENCE:** [PhoneInputStore.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Unity/Assets/_Project/Scripts/Input/PhoneInputStore.cs#L8):
  ```csharp
  private readonly Dictionary<int,PhoneInputWire> latest=new Dictionary<int,PhoneInputWire>();
  ```
  Para calcular la potencia de un golpe (*Smash*), la trayectoria de un lanzamiento (*Throw*) o el ángulo de un corte (*Slice*), se requiere calcular derivadas y picos en ventanas de 200 a 500 milisegundos.
- **RISK IF NOT FIXED:** Cada desarrollador de minijuego programará buffers temporales ad-hoc en el `Update()` de su minijuego, provocando código duplicado, bugs de sincronización y dificultad para calibrar umbrales globales.
- **COST:** Medio (1–2 días de desarrollo para crear `GestureRecognitionEngine` con tests en EditMode).
- **WHEN TO FIX:** **FIX BEFORE PHASE 1**.

---

### DEBT-04: Falta de abstracción para Fallback de entrada (Teclado/Botón)
- **WHY:** El requisito académico GOR-01, SRS-GOR-05 y la tarea T060/#104 exigen explícitamente que el usuario pueda calibrar, probar y jugar todo el torneo mediante un control alternativo accesible si el teléfono o la cámara fallan, sin requerir otro dispositivo ni omitir reglas oficiales.
- **EVIDENCE:** Actualmente no existe ninguna interfaz de entrada en Unity. Todo el flujo asume `PhoneInputStore` poblado por el socket IPC de Java.
- **RISK IF NOT FIXED:** Incumplimiento directo de los criterios de evaluación del Academic MVP (IDGS102N). Si un teléfono falla durante la presentación ante el docente, el juego queda bloqueado.
- **COST:** Medio (diseñar `IPlayerInputSource` y su implementación `KeyboardFallbackInputSource`).
- **WHEN TO FIX:** **FIX BEFORE PHASE 1**.

---

### DEBT-05: Consulta síncrona a disco (`File.Exists`) en Update de Unity
- **WHY:** Para permitir inyectar comandos de prueba durante el desarrollo de la fusión de cámara, se programó una lectura directa de un archivo temporal en disco dentro del ciclo de actualización por frame.
- **EVIDENCE:** [CameraFusionLabRunner.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Unity/Assets/_Project/Scripts/CameraTracking/CameraFusionLabRunner.cs#L48-L49):
  ```csharp
  string path=Environment.GetEnvironmentVariable("GORILLA_FUSION_COMMAND");
  if(!string.IsNullOrEmpty(path)&&File.Exists(path)) try{
    if(new FileInfo(path).Length<=1024){
      var c=JsonUtility.FromJson<Command>(File.ReadAllText(path));
  ```
- **RISK IF NOT FIXED:** Operaciones síncronas de sistema de archivos en el hilo principal de renderizado provocan caídas esporádicas de frames (frame drops) y aumentan la latencia visual percibida.
- **COST:** Mínimo (eliminar o envolver bajo bandera de depuración estricta de laboratorio).
- **WHEN TO FIX:** **FIX BEFORE PHASE 1**.

---

### DEBT-06: Límite estricto de 4096 bytes en frame IPC
- **WHY:** Se eligió un tamaño de buffer de 4 KB en `IpcFrameCodec` para mantener acotada la memoria en pruebas unitarias tempranas.
- **EVIDENCE:** [IpcFrameCodec.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Shared/CSharp/Runtime/IpcFrameCodec.cs#L9):
  ```csharp
  public const int MaximumBytes = 4096;
  ```
  Al conectar 4 teléfonos transmitiendo todos los ejes de aceleración, aceleración con gravedad, giroscopio, orientación y toques, junto con los metadatos del lobby, el JSON generado en Java excede los 3500 bytes. Esto obligó a implementar una rotación forzada en Java:
  [ManagedProbe.java](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Server/src/main/java/com/gorillaescape/server/ipc/ManagedProbe.java#L103):
  ```java
  pongPayload.set("phoneInputs", mobile.inputs.drain(Math.min(3500, ProbeCodec.MAX_FRAME - overhead - 20)));
  ```
- **RISK IF NOT FIXED:** Descarte de muestras o aumento artificial de latencia por tener que enviar inputs rotados en múltiples frames en vez de en un solo tick.
- **COST:** Bajo (cambiar la constante a 64 KB tanto en C# como en Java y actualizar pruebas de límites).
- **WHEN TO FIX:** **FIX DURING PHASE 2**.

---

### DEBT-07: Búsqueda de objetos en jerarquía (`FindFirstObjectByType`) en Update
- **WHY:** En `CameraFusionLabRunner.cs`, el componente busca el objeto de IPC en la escena mediante la API de búsqueda global de Unity en cada frame.
- **EVIDENCE:** [CameraFusionLabRunner.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Unity/Assets/_Project/Scripts/CameraTracking/CameraFusionLabRunner.cs#L38, #L51):
  ```csharp
  if(fusion==null){var ipc=FindFirstObjectByType<IpcProbeRunner>(); ...}
  var owner=FindFirstObjectByType<IpcProbeRunner>();
  ```
- **RISK IF NOT FIXED:** Desperdicio innecesario de tiempo de CPU en el hilo principal buscando en el árbol de GameObjects 60 veces por segundo.
- **COST:** Mínimo (cachear la referencia en una variable privada durante la inicialización).
- **WHEN TO FIX:** **FIX BEFORE PHASE 1**.

---

### DEBT-08: Asignaciones de memoria (GC Allocations) en hot path de IPC y Fusion
- **WHY:** En cada frame de IPC (50 Hz), se crean nuevos arrays para leer el frame (`new byte[4]`, `new byte[size]`) y nuevos strings UTF8 en [IpcFrameCodec.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Shared/CSharp/Runtime/IpcFrameCodec.cs#L13, #L16). Igualmente, `InputFusion.Evaluate()` genera nuevas instancias de `InputFusionFrame` y listas temporales.
- **EVIDENCE:** Análisis estático de código en `IpcFrameCodec.cs` e `InputFusion.cs`.
- **RISK IF NOT FIXED:** Generación continua de basura en memoria administrada (Garbage Collection pressure). Aunque en sesiones cortas no es evidente, en sesiones de torneo de 15 minutos provocará pausas de GC perceptibles.
- **COST:** Medio (implementar buffer reutilizable y structs en lugar de clases para la evaluación de fotogramas).
- **WHEN TO FIX:** **FIX DURING PHASE 2**.

---

### DEBT-09: Código C# hiper-condensado y sin formateo estándar
- **WHY:** Varios archivos fueron escritos en un formato ultra-compacto (múltiples sentencias en una sola línea, sin saltos ni sangría estándar), presumiblemente para reducir el recuento de líneas o evitar paginación durante desarrollo previo.
- **EVIDENCE:** [VisionSupervisor.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Unity/Assets/_Project/Scripts/CameraTracking/VisionSupervisor.cs), [InputFusion.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Unity/Assets/_Project/Scripts/Input/Fusion/InputFusion.cs), [PhoneInputStore.cs](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Unity/Assets/_Project/Scripts/Input/PhoneInputStore.cs).
- **RISK IF NOT FIXED:** Fricción severa para el trabajo colaborativo. Errores de lógica introducidos por mala interpretación de sentencias ocultas en la misma línea.
- **COST:** Bajo (aplicar formateo estándar con `csharpier` o `dotnet-format`).
- **WHEN TO FIX:** **FIX DURING PHASE 2**.

---

### DEBT-10: Ausencia del paquete oficial de UI en `manifest.json`
- **WHY:** La fundación técnica de Unity se centró en la validación de scripts sin render visual de producción.
- **EVIDENCE:** [manifest.json](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/Unity/Packages/manifest.json) solo contiene el test-framework y el módulo de json serialization.
- **RISK IF NOT FIXED:** No se pueden crear componentes `Canvas`, botones, textos o paneles para los minijuegos ni para el flujo de torneo hasta que no se instale el paquete correspondiente.
- **COST:** Mínimo (agregar `"com.unity.ugui": "2.0.0"` al manifest).
- **WHEN TO FIX:** **FIX BEFORE PHASE 1**.

---

### DEBT-11 y DEBT-12: Pruebas Físicas T019 y PC Limpia T020 pendientes
- **WHY:** Las pruebas de Phase 0 se han completado con éxito en entornos de laboratorio (LAB, emuladores y fixtures automatizados), pero la acreditación física ante dispositivos y hardware real sigue formalmente abierta.
- **EVIDENCE:** [PHASE0_FINAL_EXIT_REPORT.md](file:///home/josuegalvan/Documentos/Repos/Gorilla-Escape-audit/docs/PHASE0_FINAL_EXIT_REPORT.md): *Final Exit Audit: BLOCKED / NOT PASS. E0/#1 OPEN. Phase0 IN PROGRESS. Phase1 BLOCKED / NOT STARTED.*
- **RISK IF NOT FIXED:** No se pueden certificar las diferencias de hardware móvil (iPhone con iOS Safari vs Android con Chrome), la estabilidad de la antena Wi-Fi local bajo interferencia ni la ausencia de dependencias de desarrollo en el paquete distribuible.
- **COST:** Requiere acceso al hardware físico designado por el equipo docente/estudiantil.
- **WHEN TO FIX:** **FIX BEFORE E0** (Condición mandataria de cierre de Fase 0).
