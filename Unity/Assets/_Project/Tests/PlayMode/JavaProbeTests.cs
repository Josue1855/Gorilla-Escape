using System;
using System.Collections;
using System.Threading;
using GorillaEscape.Network;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class JavaProbeTests
{
    [UnityTest] public IEnumerator StartsRealJavaAndClosesOwnedChild()
    {
        string java = Environment.GetEnvironmentVariable("GORILLA_TEST_JAVA");
        string jar = Environment.GetEnvironmentVariable("GORILLA_TEST_JAR");
        if (string.IsNullOrEmpty(java) || string.IsNullOrEmpty(jar)) Assert.Ignore("Real Java/JAR development paths not configured; integration NOT RUN.");
        using (var supervisor = new JavaProbeSupervisor(new IpcLaunchBudget()))
        {
            var task = supervisor.RunAsync(java,jar,CancellationToken.None);
            while (!task.IsCompleted) yield return null;
            Assert.That(task.IsFaulted, Is.False, task.Exception?.ToString());
            var result = task.Result;
            Assert.That(result.samples, Is.EqualTo(100)); Assert.That(result.errors, Is.Zero);
            Assert.That(result.cleanExit, Is.True); Assert.That(result.exitCode, Is.Zero);
            Assert.That(System.IO.Directory.Exists("/proc/"+result.pid) && UnityEngine.Application.platform==RuntimePlatform.LinuxEditor, Is.False);
            Debug.Log($"IPC_2A_PLAYMODE startupMs={result.startupMs:F3} min={result.minimumMs:F3} p50={result.p50Ms:F3} p95={result.p95Ms:F3} max={result.maximumMs:F3} samples={result.samples} errors={result.errors} pid={result.pid} cleanExit={result.cleanExit}");
        }
    }
    [UnityTest] public IEnumerator MissingExecutableIsRecoverable()
    {
        using (var supervisor = new JavaProbeSupervisor(new IpcLaunchBudget()))
        {
            var task = supervisor.RunAsync("/missing-java", "/missing-jar", CancellationToken.None);
            while (!task.IsCompleted) yield return null;
            Assert.That(task.IsFaulted, Is.True);
            StringAssert.Contains("Comprueba Java/JAR", task.Exception.ToString());
        }
    }
}
