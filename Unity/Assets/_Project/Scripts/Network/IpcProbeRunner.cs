using GorillaEscape.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GorillaEscape.Network
{
    /// <summary>Opt-in development diagnostics; no scenes or game session authority.</summary>
    public sealed class IpcProbeRunner : MonoBehaviour
    {
        private JavaProbeSupervisor supervisor;
        public GorillaEscape.Input.PhoneInputStore PhoneInputs => supervisor?.PhoneInputs;
        private CancellationTokenSource cancellation;
        private Task run;
        private bool quitting, allowQuit;
        private string message = "Iniciando backend local…";
        private bool sustained, previousBackground;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GORILLA_IPC_JAVA"))
                && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GORILLA_IPC_JAR"))) return;
            var host = new GameObject("IPC development diagnostics");
            DontDestroyOnLoad(host); host.AddComponent<IpcProbeRunner>();
        }
        private void Start()
        {
            previousBackground = Application.runInBackground; Application.runInBackground = true;
            supervisor = new JavaProbeSupervisor();
            if(Environment.GetEnvironmentVariable("GORILLA_PHONE_INPUT")=="1"){
                supervisor.HttpReady+=port=>Debug.Log("PHONE_LAB_HTTP_READY "+port);
                supervisor.PhoneInputReceived+=sample=>{
                    if(supervisor.PhoneInputs.TryPhoneInput(sample.playerId,out var phone))
                        Debug.Log("PHONE_INPUT_OBSERVED "+JsonUtility.ToJson(new PhoneObservation(phone.ProtocolSample)));
                };
            }
            sustained = Environment.GetEnvironmentVariable("GORILLA_IPC_MODE") == "lifecycle";
            Application.wantsToQuit += WantsToQuit;
            StartAttempt();
        }
        [Serializable] private sealed class PhoneObservation {
            public int playerId;public long sequence,epoch,clientTimestamp,serverReceiveTimestamp,unityReceiveTimestamp;
            public string source,connectionState;public bool accelerationXPresent;public float accelerationX;
            public PhoneObservation(PhoneInputWire s){playerId=s.playerId;sequence=s.sequence;epoch=s.connectionEpoch;clientTimestamp=s.clientTimestamp;serverReceiveTimestamp=s.serverReceiveTimestamp;unityReceiveTimestamp=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();source=s.quality.source;connectionState=s.connectionState;accelerationXPresent=s.acceleration!=null&&s.acceleration.hasX;accelerationX=s.acceleration==null?0:s.acceleration.x;}
        }
        private void StartAttempt()
        {
            cancellation?.Dispose(); cancellation = new CancellationTokenSource();
            run = Execute();
        }
        private async Task Execute()
        {
            int exit = 0;
            try
            {
                string java = Environment.GetEnvironmentVariable("GORILLA_IPC_JAVA"), jar = Environment.GetEnvironmentVariable("GORILLA_IPC_JAR");
                var result = await (sustained ? supervisor.RunLifecycleAsync(java, jar, cancellation.Token) : Environment.GetEnvironmentVariable("GORILLA_IPC_MODE") == "benchmark" ? supervisor.RunBenchmarkAsync(java, jar, cancellation.Token) : supervisor.RunAsync(java, jar, cancellation.Token));
                if (Environment.GetEnvironmentVariable("GORILLA_IPC_MODE") == "benchmark" || Environment.GetEnvironmentVariable("GORILLA_IPC_MEASURE") == "1") Debug.Log("IPC_2C_RESULT " + JsonUtility.ToJson(result));
                if (!sustained) Debug.Log("IPC_2A_SMOKE " + JsonUtility.ToJson(new Smoke(result)));
                message = "Backend detenido. Recursos cerrados.";
            }
            catch (Exception)
            {
                exit = 1; message = Message(supervisor.LastSummary?.errorCode ?? supervisor.LastSummary?.cleanupError);
                Debug.Log("IPC_DIAGNOSTIC " + message);
            }
            if (!sustained && !quitting && Environment.GetEnvironmentVariable("GORILLA_IPC_SMOKE_EXIT") == "1" && supervisor.CleanupComplete)
            {
                allowQuit = true; Release(); Application.Quit(exit);
            }
        }
        private void OnGUI()
        {
            if (!sustained || supervisor == null) return;
            bool canRetry = !quitting && supervisor.State == IpcState.FAILED && supervisor.CleanupComplete && supervisor.Launches < 3;
            bool canStop = !quitting && (supervisor.State == IpcState.STARTING || supervisor.State == IpcState.CONNECTING || supervisor.State == IpcState.RUNNING);
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.R && canRetry) { StartAttempt(); Event.current.Use(); }
                if (Event.current.keyCode == KeyCode.S && canStop) { supervisor.RequestStop(); Event.current.Use(); }
            }
            var area = new Rect(20,20,640,180);
            GUI.DrawTexture(area, Texture2D.blackTexture, ScaleMode.StretchToFill, false);
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label("Backend local: " + supervisor.State + " | Lanzamientos: " + supervisor.Launches + "/3");
            GUILayout.Label(supervisor.State == IpcState.RUNNING ? "Backend conectado; supervisión activa." : message);
            GUI.enabled = canRetry;
            if (GUILayout.Button("Reintentar backend local (R)")) StartAttempt();
            GUI.enabled = canStop;
            if (GUILayout.Button("Detener (S)")) supervisor.RequestStop();
            GUI.enabled = true; GUILayout.EndArea();
        }
        private static string Message(string code)
        {
            switch (code)
            {
                case "ALREADY_RUNNING": return "Ya existe un backend administrado. Cierra la otra ejecución antes de reintentar.";
                case "LOCK_UNAVAILABLE": return "No se pudo reservar la ejecución. Revisa los permisos de la carpeta de datos.";
                case "READY_TIMEOUT": case "READY_MISSING": return "El backend no quedó listo a tiempo o terminó antes de estar listo.";
                case "PROTOCOL_INVALID": case "READY_INVALID": case "HANDSHAKE_FAILED": return "El backend no respondió con la identificación esperada.";
                case "CONNECT_FAILED": case "CONNECT_TIMEOUT": return "No se pudo conectar con el backend local.";
                case "JAVA_EXITED": return "El backend local se cerró inesperadamente.";
                case "CONNECTION_LOST": return "Se perdió la conexión con el backend local.";
                case "SHUTDOWN_FORCED": return "El backend no se cerró a tiempo; se terminó únicamente su ejecución propia.";
                case "CHILD_RESIDUAL": case "CLEANUP_FAILED": return "No se completó el cierre. Reintento bloqueado.";
                default: return "No se pudo iniciar el backend. Comprueba las rutas Java/JAR y el diagnóstico.";
            }
        }
        private bool WantsToQuit()
        {
            if (allowQuit) return true;
            if (supervisor.CleanupComplete && (run == null || run.IsCompleted))
            {
                allowQuit = true; Release(); return true;
            }
            if (!quitting) { quitting = true; supervisor.RequestStop(); }
            return false;
        }
        private void Update()
        {
            // Completed tasks only: no wait on Unity's frame thread or nested quit callback.
            if (!quitting || allowQuit || run == null || !run.IsCompleted) return;
            if (!supervisor.CleanupComplete) { message = "Cierre incompleto; consulta el diagnóstico."; quitting = false; return; }
            allowQuit = true; Release(); Application.Quit();
        }
        private async void OnDestroy()
        {
            supervisor?.RequestStop();
            if (run != null) await run;
            Release(); Application.runInBackground = previousBackground;
        }
        private void Release()
        {
            Application.wantsToQuit -= WantsToQuit;
            supervisor?.Dispose(); cancellation?.Dispose();
        }
        [Serializable] private sealed class Smoke
        {
            public double startupMs, minimumMs, p50Ms, p95Ms, maximumMs;
            public int samples, errors, pid, exitCode; public bool cleanExit;
            public Smoke(ProbeSummary r) { startupMs=r.startupMs; minimumMs=r.minimumMs; p50Ms=r.p50Ms; p95Ms=r.p95Ms;
                maximumMs=r.maximumMs; samples=r.samples; errors=r.errors; pid=r.pid; exitCode=r.exitCode; cleanExit=r.cleanExit; }
        }
    }
}
