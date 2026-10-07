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
        public double startupMs, minimumMs, p50Ms, p95Ms, maximumMs, shutdownMs, readersMs;
        public int samples, errors, pid, exitCode;
        public bool cleanExit, forced, cleanupComplete;
        public string instanceId, errorCode, cleanupError;
    }

    /// <summary>One owned child per attempt. Manual recovery only, after complete cleanup.</summary>
    public sealed class JavaProbeSupervisor : IDisposable
    {
        private sealed class Attempt
        {
            public long generation;
            public readonly ProbeSummary result = new ProbeSummary { instanceId = Guid.NewGuid().ToString() };
            public Process child;
            public bool started;
            public TcpClient client;
            public CancellationTokenSource cancellation;
            public Task[] readers = new Task[0];
            public EventHandler exitHandler;
            public string stage = "START";
            public bool observedExit;
        }
        private sealed class Failure : IOException
        {
            public readonly string code;
            public Failure(string code) : base(code) { this.code = code; }
        }
        private readonly object gate = new object();
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly IpcLaunchBudget budget;
        private readonly IpcLifecycle lifecycle = new IpcLifecycle();
        private Attempt current;
        private bool disposed;
        public IpcState State => lifecycle.State;
        public bool CleanupComplete => lifecycle.CleanupComplete;
        public int Launches => budget.Launches;
        public ProbeSummary LastSummary { get; private set; }
        // Same absolute user/product directory is used by Editor Flatpak and host Player.
        public static string DefaultLockDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gorilla-escape", "managed");
        public JavaProbeSupervisor(IpcLaunchBudget launchBudget = null) { budget = launchBudget ?? IpcLaunchBudget.Execution; }
        public Task<ProbeSummary> RunAsync(string java, string jar, CancellationToken token, int sampleCount = 100)
        {
            if (sampleCount < 1 || sampleCount > 1000) throw new ArgumentOutOfRangeException(nameof(sampleCount));
            return Begin(java, jar, token, sampleCount);
        }
        public Task<ProbeSummary> RunLifecycleAsync(string java, string jar, CancellationToken token) => Begin(java, jar, token, 0);
        private Task<ProbeSummary> Begin(string java, string jar, CancellationToken token, int samples)
        {
            Attempt attempt;
            lock (gate)
            {
                if (disposed) throw new ObjectDisposedException(nameof(JavaProbeSupervisor));
                if (budget.Launches >= 3) throw new InvalidOperationException("LAUNCH_LIMIT");
                attempt = new Attempt { generation = lifecycle.Begin() };
                attempt.cancellation = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime.Token);
                current = attempt; LastSummary = attempt.result;
                Log(attempt, "START_REQUESTED");
            }
            return Task.Run(() => Execute(java, jar, samples, attempt));
        }
        public void RequestStop() { lock (gate) { current?.cancellation?.Cancel(); } }
        private async Task<ProbeSummary> Execute(string java, string jar, int samples, Attempt a)
        {
            try { await Operate(java, jar, samples, a); }
            catch (Exception error)
            {
                bool intentional = a.cancellation.IsCancellationRequested && !a.observedExit;
                if (!intentional || !(error is OperationCanceledException || error is IOException || error is ObjectDisposedException))
                {
                    a.result.errorCode = Classify(a, error); a.result.errors++;
                }
            }
            finally
            {
                Move(a, IpcState.STOPPING, "STOP_REQUESTED");
                try { await Cleanup(a); }
                catch (Exception error) { a.result.cleanupError = error is Failure failure ? failure.code : "CLEANUP_FAILED"; }
                bool failed = a.result.errorCode != null || a.result.cleanupError != null || a.result.forced;
                if (a.result.forced && a.result.errorCode == null) a.result.errorCode = "SHUTDOWN_FORCED";
                Move(a, failed ? IpcState.FAILED : IpcState.STOPPED, failed ? "FAILED" : "STOPPED", a.result.cleanupComplete);
                lock (gate)
                {
                    if (a.result.cleanupComplete) { a.cancellation.Dispose(); a.cancellation = null; if (current == a) current = null; if (disposed) lifetime.Dispose(); }
                }
            }
            if (a.result.errorCode != null || a.result.cleanupError != null)
                throw new InvalidOperationException("IPC " + (a.result.errorCode ?? a.result.cleanupError) + ". Comprueba Java/JAR y el diagnóstico; reintenta sólo tras cierre completo.");
            return a.result;
        }
        private async Task Operate(string java, string jar, int samples, Attempt a)
        {
            CancellationToken token = a.cancellation.Token;
            using (var startup = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                startup.CancelAfter(15000);
                var clock = Stopwatch.StartNew();
                token.ThrowIfCancellationRequested();
                if (!Path.IsPathRooted(java) || !File.Exists(java) || !Path.IsPathRooted(jar) || !File.Exists(jar)) throw new Failure("CONFIG_INVALID");
                byte[] random = new byte[32]; using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(random);
                string secret = Convert.ToBase64String(random).TrimEnd('=').Replace('+', '-').Replace('/', '_');
                var start = new ProcessStartInfo { FileName = java, UseShellExecute = false, RedirectStandardInput = true,
                    RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(jar) };
                start.Arguments = "-Djava.net.preferIPv4Stack=true -jar " + Quote(jar) + " --gorilla.ipc.managed=true --server.address=127.0.0.1 --server.port=0 --logging.config=classpath:ipc-logback.xml";
                start.EnvironmentVariables["GORILLA_IPC_INSTANCE"] = a.result.instanceId;
                start.EnvironmentVariables["GORILLA_IPC_TOKEN"] = secret;
                start.EnvironmentVariables["GORILLA_IPC_LOCK_DIR"] = DefaultLockDirectory;
                a.child = new Process { StartInfo = start, EnableRaisingEvents = true };
                a.exitHandler = (sender, args) => {
                    lock (gate)
                    {
                        if (current != a || State == IpcState.STOPPING || CleanupComplete || a.cancellation.IsCancellationRequested) return;
                        a.observedExit = true; a.cancellation.Cancel();
                    }
                };
                a.child.Exited += a.exitHandler;
                budget.Reserve();
                if (!a.child.Start()) throw new Failure("SPAWN_FAILED");
                a.started = true; a.result.pid = a.child.Id;
                if (a.child.HasExited) { a.observedExit = true; a.cancellation.Cancel(); }
                Log(a, "PROCESS_STARTED"); a.stage = "READY";
                var readiness = Task.Run(() => ReadReady(a.child.StandardOutput));
                var stderr = Task.Run(() => Drain(a.child.StandardError));
                a.readers = new Task[] { readiness, stderr };
                using (startup.Token.Register(() => CloseSocket(a)))
                {
                    string json = await Deadline(readiness, Remaining(clock), startup.Token);
                    var ready = JsonUtility.FromJson<IpcReady>(json);
                    if (ready == null || ready.ipcVersion != 1 || ready.instanceId != a.result.instanceId || ready.pid != a.result.pid
                        || ready.ipcPort < 1 || ready.ipcPort > 65535 || ready.httpPort < 1 || ready.httpPort > 65535) throw new Failure("READY_INVALID");
                    var stdout = Task.Run(() => Drain(a.child.StandardOutput));
                    a.readers = new Task[] { stderr, stdout };
                    Move(a, IpcState.CONNECTING, "READY_RECEIVED"); a.stage = "CONNECT";
                    a.client = new TcpClient(AddressFamily.InterNetwork) { NoDelay = true };
                    await Deadline(a.client.ConnectAsync(IPAddress.Loopback, ready.ipcPort), Math.Min(2000, Remaining(clock)), startup.Token);
                    using (NetworkStream stream = a.client.GetStream())
                    using (token.Register(() => CloseSocket(a)))
                    {
                        stream.ReadTimeout = 2000; stream.WriteTimeout = 2000;
                        string connection = Guid.NewGuid().ToString(); int sequence = 1;
                        a.stage = "HANDSHAKE";
                        Exchange(stream, secret, connection, sequence++, a, Math.Min(2000, Remaining(clock)), startup.Token);
                        Remaining(clock); a.result.startupMs = clock.Elapsed.TotalMilliseconds;
                        startup.CancelAfter(Timeout.Infinite);
                        Move(a, IpcState.RUNNING, "FIRST_PONG"); a.stage = "RUNNING";
                        var timings = new List<double>(samples);
                        int total = samples == 0 ? int.MaxValue : samples + 9;
                        for (int i = 0; i < total; i++)
                        {
                            // One request/reader, no accumulation. Sustained mode only uses 1 Hz liveness.
                            await Task.Delay(samples == 0 ? 1000 : 20, token);
                            token.ThrowIfCancellationRequested();
                            if (sequence == int.MaxValue) throw new Failure("SEQUENCE_EXHAUSTED");
                            double duration = Exchange(stream, null, connection, sequence++, a, 2000, token);
                            if (samples != 0 && i >= 9) timings.Add(duration);
                        }
                        if (samples != 0)
                        {
                            timings.Sort(); a.result.samples = timings.Count;
                            a.result.minimumMs = timings[0]; a.result.p50Ms = timings[(int)Math.Ceiling(timings.Count * .50) - 1];
                            a.result.p95Ms = timings[(int)Math.Ceiling(timings.Count * .95) - 1]; a.result.maximumMs = timings[timings.Count - 1];
                        }
                    }
                }
            }
        }
        private static double Exchange(NetworkStream stream, string secret, string connection, int sequence, Attempt a, int milliseconds, CancellationToken token)
        {
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                deadline.CancelAfter(milliseconds);
                using (deadline.Token.Register(() => CloseSocket(a)))
                {
                    var clock = Stopwatch.StartNew();
                    string payload = secret == null ? "{}" : "{\"launchToken\":\"" + secret + "\"}";
                    IpcFrameCodec.Write(stream, "{\"ipcVersion\":1,\"type\":\"PING\",\"instanceId\":\"" + a.result.instanceId
                        + "\",\"connectionId\":\"" + connection + "\",\"sequence\":" + sequence + ",\"payload\":" + payload + "}");
                    var pong = JsonUtility.FromJson<IpcPong>(IpcFrameCodec.Read(stream));
                    if (pong == null || pong.ipcVersion != 1 || pong.type != "PONG" || pong.instanceId != a.result.instanceId
                        || pong.connectionId != connection || pong.sequence != sequence) throw new Failure("HANDSHAKE_FAILED");
                    deadline.Token.ThrowIfCancellationRequested(); return clock.Elapsed.TotalMilliseconds;
                }
            }
        }
        private static string Classify(Attempt a, Exception error)
        {
            if (a.started && a.child.HasExited)
                return a.child.ExitCode == 73 ? "ALREADY_RUNNING" : a.child.ExitCode == 74 ? "LOCK_UNAVAILABLE" : "JAVA_EXITED";
            if (error is Failure failure) return failure.code;
            if (a.stage == "READY") return error is EndOfStreamException ? "READY_MISSING" : error is TimeoutException || error is OperationCanceledException ? "READY_TIMEOUT" : "READY_INVALID";
            if (a.stage == "CONNECT") return error is TimeoutException || error is OperationCanceledException ? "CONNECT_TIMEOUT" : "CONNECT_FAILED";
            if (a.stage == "HANDSHAKE") return "HANDSHAKE_FAILED";
            if (a.stage == "RUNNING") return "CONNECTION_LOST";
            return "SPAWN_FAILED";
        }
        private async Task Cleanup(Attempt a)
        {
            CloseSocket(a);
            var shutdown = Stopwatch.StartNew();
            if (a.started)
            {
                a.child.StandardInput.Close();
                bool exited = await Task.Run(() => a.child.WaitForExit(10000));
                if (!exited)
                {
                    a.result.forced = true;
                    try { a.child.Kill(); } catch (InvalidOperationException) { if (!a.child.HasExited) throw; }
                    exited = await Task.Run(() => a.child.WaitForExit(2000));
                }
                a.result.shutdownMs = shutdown.Elapsed.TotalMilliseconds;
                if (!exited) throw new Failure("CHILD_RESIDUAL");
                a.result.exitCode = a.child.ExitCode;
                if (a.stage == "READY" && a.result.exitCode == 73) a.result.errorCode = "ALREADY_RUNNING";
                if (a.stage == "READY" && a.result.exitCode == 74) a.result.errorCode = "LOCK_UNAVAILABLE";
                a.result.cleanExit = !a.result.forced && a.result.exitCode == 0;
            }
            var readers = Stopwatch.StartNew();
            Task all = Task.WhenAll(a.readers);
            try { await Deadline(all, 2000, CancellationToken.None); }
            catch { if (!all.IsCompleted) throw new Failure("CLEANUP_FAILED"); /* readiness already classified */ }
            a.result.readersMs = readers.Elapsed.TotalMilliseconds;
            if (a.started)
            {
                a.child.Exited -= a.exitHandler;
                a.child.StandardOutput.Dispose(); a.child.StandardError.Dispose(); a.child.StandardInput.Dispose();
                if (a.result.exitCode != 0 && a.result.errorCode == null && !a.result.forced) a.result.errorCode = "JAVA_EXITED";
            }
            a.child?.Dispose(); a.client?.Dispose(); a.result.cleanupComplete = true;
            Log(a, a.result.cleanExit ? "SHUTDOWN_CLEAN" : a.result.forced ? "SHUTDOWN_FORCED" : "CHILD_EXITED");
        }
        private void Move(Attempt a, IpcState next, string name, bool cleaned = false)
        {
            lock (gate) { if (lifecycle.Move(a.generation, next, cleaned)) Log(a, name); }
        }
        private void Log(Attempt a, string name)
        {
            UnityEngine.Debug.Log("{\"component\":\"ipc-unity\",\"event\":\"" + name + "\",\"state\":\"" + State
                + "\",\"instanceId\":\"" + a.result.instanceId + "\",\"generation\":" + a.generation + ",\"pid\":" + a.result.pid
                + ",\"exitCode\":" + a.result.exitCode + ",\"shutdownMs\":" + a.result.shutdownMs.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ",\"readersMs\":" + a.result.readersMs.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"forced\":" + (a.result.forced ? "true" : "false")
                + ",\"launches\":" + Launches + ",\"cleanupComplete\":" + (a.result.cleanupComplete ? "true" : "false")
                + ",\"errorCode\":\"" + (a.result.errorCode ?? "") + "\",\"cleanupError\":\"" + (a.result.cleanupError ?? "") + "\"}");
        }
        private static void CloseSocket(Attempt a) { try { a.client?.Close(); } catch (ObjectDisposedException) { } }
        private static int Remaining(Stopwatch clock)
        {
            int value = 15000 - (int)clock.ElapsedMilliseconds;
            if (value <= 0) throw new TimeoutException("STARTUP_TIMEOUT"); return value;
        }
        private static string Quote(string path)
        {
            if (path.IndexOf('"') >= 0 || path.EndsWith("\\", StringComparison.Ordinal)) throw new Failure("CONFIG_INVALID"); return "\"" + path + "\"";
        }
        private static string ReadReady(StreamReader reader)
        {
            var line = new StringBuilder(); int value;
            while ((value = reader.Read()) != -1)
            {
                if (value == '\n')
                {
                    string text = line.ToString().TrimEnd('\r'); line.Clear();
                    if (text.StartsWith("GORILLA_IPC_READY ", StringComparison.Ordinal)) return text.Substring(18);
                }
                else { if (line.Length >= 4096) throw new InvalidDataException("STDOUT_LIMIT"); line.Append((char)value); }
            }
            throw new EndOfStreamException("READY_MISSING");
        }
        private static void Drain(StreamReader reader) { var chunk = new char[1024]; while (reader.Read(chunk, 0, chunk.Length) > 0) { } }
        private static async Task Deadline(Task action, int milliseconds, CancellationToken token)
        {
            if (await Task.WhenAny(action, Task.Delay(milliseconds, token)) != action) { token.ThrowIfCancellationRequested(); throw new TimeoutException("DEADLINE"); }
            await action;
        }
        private static async Task<T> Deadline<T>(Task<T> action, int milliseconds, CancellationToken token) { await Deadline((Task)action, milliseconds, token); return await action; }
        public void Dispose() { lock (gate) { if (disposed) return; disposed = true; lifetime.Cancel(); if (current == null) lifetime.Dispose(); } }
    }
}
