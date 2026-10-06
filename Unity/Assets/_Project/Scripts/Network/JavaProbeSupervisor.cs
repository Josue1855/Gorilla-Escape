using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GorillaEscape.Contracts;
using UnityEngine;

namespace GorillaEscape.Network
{
    public sealed class ProbeSummary
    {
        public double startupMs, minimumMs, p50Ms, p95Ms, maximumMs;
        public int samples, errors, pid, exitCode;
        public bool cleanExit;
        public string instanceId;
    }

    /// <summary>One owned Java child. No reconnect/restart or game state.</summary>
    public sealed class JavaProbeSupervisor : IDisposable
    {
        private Process child;
        private TcpClient client;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly object gate = new object();
        private bool started;
        private bool processStarted;
        private string stage = "START";
        public async Task<ProbeSummary> RunAsync(string javaPath, string jarPath, CancellationToken cancellation,
                                                int sampleCount = 100)
        {
            lock (gate) { if (started) throw new InvalidOperationException("IPC_ALREADY_STARTED"); started = true; }
            if (sampleCount < 1 || sampleCount > 1000) throw new ArgumentOutOfRangeException(nameof(sampleCount));
            var summary = new ProbeSummary {instanceId = Guid.NewGuid().ToString()};
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, lifetime.Token))
            {
                Exception failure = null;
                string failureStage = null;
                try
                {
                    await Task.Run(async () => await Probe(javaPath, jarPath, sampleCount, summary, linked.Token), linked.Token);
                }
                catch (Exception error)
                {
                    summary.errors++;
                    failure = error; failureStage = stage;
                }
                finally
                {
                    try { await StopAsync(summary); }
                    catch (Exception cleanup) { if (failure == null) { failure = cleanup; failureStage = stage; } }
                }
                if (failure != null) throw new InvalidOperationException("No se pudo completar IPC (" + failureStage + "). Comprueba Java/JAR y el cierre del proceso.", failure);
            }
            return summary;
        }
        private async Task Probe(string javaPath, string jarPath, int sampleCount, ProbeSummary summary, CancellationToken token)
        {
            using (var startupDeadline = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                startupDeadline.CancelAfter(15000);
                CancellationToken startupToken = startupDeadline.Token;
                if (!Path.IsPathRooted(javaPath) || !File.Exists(javaPath) || !Path.IsPathRooted(jarPath) || !File.Exists(jarPath))
                    throw new FileNotFoundException("IPC_PATHS");
                byte[] random = new byte[32]; using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(random);
                string secret = Convert.ToBase64String(random).TrimEnd('=').Replace('+', '-').Replace('/', '_');
                var start = new ProcessStartInfo {
                    FileName = javaPath, UseShellExecute = false, RedirectStandardInput = true,
                    RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(jarPath)
                };
                // Unity's profile does not assume modern ProcessStartInfo.ArgumentList.
                start.Arguments = "-Djava.net.preferIPv4Stack=true -jar " + Quote(jarPath) + " --gorilla.ipc.managed=true --server.address=127.0.0.1 --server.port=0 --logging.config=classpath:ipc-logback.xml";
                start.EnvironmentVariables["GORILLA_IPC_INSTANCE"] = summary.instanceId;
                start.EnvironmentVariables["GORILLA_IPC_TOKEN"] = secret;
                var startup = Stopwatch.StartNew();
                child = new Process {StartInfo = start};
                if (!child.Start()) throw new IOException("IPC_SPAWN");
                processStarted = true;
                summary.pid = child.Id;
                Log("STARTED", summary);
                stage = "READY";
                var outputReader = child.StandardOutput; var errorReader = child.StandardError;
                Task<string> readiness = Task.Run(() => ReadReady(outputReader, startupToken), startupToken);
                Task stderr = Task.Run(() => Drain(errorReader), CancellationToken.None);
                drainTasks = new Task[] {readiness, stderr};
                // Cancellation closes a blocked socket; no network wait occurs on the Unity frame thread.
                using (startupToken.Register(() => { try { client?.Close(); } catch (ObjectDisposedException) {} }))
                {
                    string readyJson = await Deadline(readiness, 15000, startupToken);
                    IpcReady ready = JsonUtility.FromJson<IpcReady>(readyJson);
                    if (ready == null || ready.ipcVersion != 1 || ready.instanceId != summary.instanceId || ready.pid != child.Id
                        || ready.ipcPort < 1 || ready.ipcPort > 65535 || ready.httpPort < 1 || ready.httpPort > 65535)
                        throw new InvalidDataException("IPC_READY");
                    Task stdout = Task.Run(() => Drain(outputReader));
                    drainTasks = new[] {stderr, stdout};
                    client = new TcpClient(AddressFamily.InterNetwork) {NoDelay = true};
                    stage = "CONNECT";
                    await Deadline(client.ConnectAsync(IPAddress.Loopback, ready.ipcPort), Math.Min(2000, Remaining(startup)), startupToken);
                    using (NetworkStream stream = client.GetStream())
                    {
                        stream.ReadTimeout = 2000; stream.WriteTimeout = 2000;
                        string connection = Guid.NewGuid().ToString();
                        var timings = new List<double>(sampleCount);
                        stage = "PING_PONG";
                        for (int sequence = 1; sequence <= sampleCount + 10; sequence++)
                        {
                            token.ThrowIfCancellationRequested();
                            if (sequence == 1) stream.ReadTimeout = Math.Min(2000, Remaining(startup));
                            var clock = Stopwatch.StartNew();
                            string payload = sequence == 1 ? "{\"launchToken\":\"" + secret + "\"}" : "{}";
                            string ping = "{\"ipcVersion\":1,\"type\":\"PING\",\"instanceId\":\"" + summary.instanceId
                                + "\",\"connectionId\":\"" + connection + "\",\"sequence\":" + sequence + ",\"payload\":" + payload + "}";
                            IpcFrameCodec.Write(stream, ping);
                            var pong = JsonUtility.FromJson<IpcPong>(IpcFrameCodec.Read(stream));
                            if (pong == null || pong.ipcVersion != 1 || pong.type != "PONG" || pong.instanceId != summary.instanceId
                                || pong.connectionId != connection || pong.sequence != sequence) throw new InvalidDataException("IPC_PONG");
                            clock.Stop();
                            if (sequence == 1) { Remaining(startup); startupDeadline.CancelAfter(Timeout.Infinite); summary.startupMs = startup.Elapsed.TotalMilliseconds; Log("CONNECTED", summary); }
                            if (sequence > 10) timings.Add(clock.Elapsed.TotalMilliseconds);
                            stream.ReadTimeout = 2000;
                            await Task.Delay(20, token);
                        }
                        timings.Sort(); summary.samples = timings.Count;
                        summary.minimumMs = timings[0]; summary.p50Ms = timings[(int)Math.Ceiling(timings.Count * .50) - 1];
                        summary.p95Ms = timings[(int)Math.Ceiling(timings.Count * .95) - 1]; summary.maximumMs = timings[timings.Count - 1];
                    }
                    // Streams are observed during StopAsync; their readers drain until child EOF.
                    drainTasks = new[] {stderr, stdout};
                }
            }
        }
        private Task[] drainTasks = new Task[0];
        private static int Remaining(Stopwatch clock)
        {
            int remaining = 15000 - (int)clock.ElapsedMilliseconds;
            if (remaining <= 0) throw new TimeoutException("IPC_STARTUP_TIMEOUT"); return remaining;
        }
        private static string Quote(string path)
        {
            if (path.IndexOf('"') >= 0 || path.EndsWith("\\", StringComparison.Ordinal)) throw new ArgumentException("IPC_PATH");
            return "\"" + path + "\"";
        }
        private static string ReadReady(StreamReader reader, CancellationToken token)
        {
            var line = new StringBuilder(); int value;
            while ((value = reader.Read()) != -1)
            {
                token.ThrowIfCancellationRequested();
                if (value == '\n')
                {
                    string text = line.ToString().TrimEnd('\r'); line.Clear();
                    if (text.StartsWith("GORILLA_IPC_READY ", StringComparison.Ordinal)) return text.Substring(18);
                }
                else { if (line.Length >= 4096) throw new InvalidDataException("IPC_STDOUT_LIMIT"); line.Append((char)value); }
            }
            throw new EndOfStreamException("IPC_NO_READY");
        }
        private static void Drain(StreamReader reader)
        {
            var chunk = new char[1024]; while (reader.Read(chunk, 0, chunk.Length) > 0) {}
        }
        private static async Task Deadline(Task action, int milliseconds, CancellationToken token)
        {
            if (await Task.WhenAny(action, Task.Delay(milliseconds, token)) != action) { token.ThrowIfCancellationRequested(); throw new TimeoutException("IPC_DEADLINE"); }
            await action;
        }
        private static async Task<T> Deadline<T>(Task<T> action, int milliseconds, CancellationToken token)
        {
            await Deadline((Task)action, milliseconds, token); return await action;
        }
        private async Task StopAsync(ProbeSummary summary)
        {
            stage = "STOP";
            client?.Close();
            if (child == null) return;
            if (!processStarted) { child.Dispose(); child = null; return; }
            try
            {
                child.StandardInput.Close();
                bool exited = await Task.Run(() => child.WaitForExit(10000));
                summary.cleanExit = exited;
                if (!exited) { child.Kill(); exited = await Task.Run(() => child.WaitForExit(2000)); }
                if (!exited) throw new TimeoutException("IPC_CHILD_RESIDUAL");
                summary.exitCode = child.ExitCode;
                summary.cleanExit &= summary.exitCode == 0;
                if (drainTasks.Length > 0)
                {
                    try { await Deadline(Task.WhenAll(drainTasks), 2000, CancellationToken.None); }
                    catch (IOException) { /* Readiness failure is reported by RunAsync. */ }
                    catch (OperationCanceledException) { /* Reader cancelled; child exit already confirmed. */ }
                }
                Log(summary.cleanExit ? "SHUTDOWN_CLEAN" : "SHUTDOWN_FORCED", summary);
                if (!summary.cleanExit) throw new IOException("IPC_SHUTDOWN_NOT_CLEAN");
            }
            finally { child.Dispose(); child = null; client?.Dispose(); client = null; }
        }
        private static void Log(string name, ProbeSummary value)
        {
            UnityEngine.Debug.Log("{\"component\":\"ipc-unity\",\"event\":\"" + name + "\",\"instanceId\":\"" + value.instanceId + "\",\"pid\":" + value.pid + "}");
        }
        public void Dispose() { lifetime.Cancel(); }
    }
}
