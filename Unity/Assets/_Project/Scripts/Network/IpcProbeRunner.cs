using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GorillaEscape.Network
{
    /// <summary>Opt-in development probe, configured by launch environment, no scene edits.</summary>
    public sealed class IpcProbeRunner : MonoBehaviour
    {
        private JavaProbeSupervisor supervisor;
        private CancellationTokenSource cancellation;
        private Task run;
        private bool finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GORILLA_IPC_JAVA"))
                && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GORILLA_IPC_JAR"))) return;
            var host = new GameObject("IPC 2A development probe");
            DontDestroyOnLoad(host); host.AddComponent<IpcProbeRunner>();
        }
        private void Start()
        {
            supervisor = new JavaProbeSupervisor(); cancellation = new CancellationTokenSource();
            Application.wantsToQuit += WantsToQuit;
            run = Execute();
        }
        private async Task Execute()
        {
            int exit = 0;
            try
            {
                var result = await supervisor.RunAsync(Environment.GetEnvironmentVariable("GORILLA_IPC_JAVA"),
                    Environment.GetEnvironmentVariable("GORILLA_IPC_JAR"), cancellation.Token);
                Debug.Log("IPC_2A_SMOKE " + JsonUtility.ToJson(new Smoke(result)));
            }
            catch (Exception) { exit = 1; Debug.LogError("IPC 2A no pudo completarse. Comprueba rutas Java/JAR y disponibilidad local; puedes cerrar y volver a intentarlo."); }
            finally
            {
                finished = true; Application.wantsToQuit -= WantsToQuit; supervisor.Dispose(); cancellation.Dispose();
            }
            if (Environment.GetEnvironmentVariable("GORILLA_IPC_SMOKE_EXIT") == "1") Application.Quit(exit);
        }
        private bool WantsToQuit()
        {
            if (finished) return true;
            cancellation.Cancel(); _ = FinishQuit(); return false;
        }
        private async Task FinishQuit() { await run; Application.Quit(); }
        private void OnDestroy() { if (!finished) cancellation?.Cancel(); }
        [Serializable] private sealed class Smoke
        {
            public double startupMs, minimumMs, p50Ms, p95Ms, maximumMs;
            public int samples, errors, pid, exitCode; public bool cleanExit;
            public Smoke(ProbeSummary r) { startupMs=r.startupMs; minimumMs=r.minimumMs; p50Ms=r.p50Ms; p95Ms=r.p95Ms;
                maximumMs=r.maximumMs; samples=r.samples; errors=r.errors; pid=r.pid; exitCode=r.exitCode; cleanExit=r.cleanExit; }
        }
    }
}
