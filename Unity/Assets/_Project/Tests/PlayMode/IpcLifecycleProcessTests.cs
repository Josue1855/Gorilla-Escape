using System;
using System.Collections;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GorillaEscape.Network;
using NUnit.Framework;
using UnityEngine.TestTools;

public class IpcLifecycleProcessTests
{
    private static string Java => Environment.GetEnvironmentVariable("GORILLA_TEST_JAVA");
    private static string Jar => Environment.GetEnvironmentVariable("GORILLA_TEST_JAR");
    private static JavaProbeSupervisor Supervisor() { Assert.That(Java,Is.Not.Null,"Real Java required"); Assert.That(Jar,Is.Not.Null); return new JavaProbeSupervisor(new IpcLaunchBudget()); }
    private static IEnumerator Wait(Task task, int seconds=25)
    {
        var clock=Stopwatch.StartNew(); while(!task.IsCompleted && clock.Elapsed.TotalSeconds<seconds) yield return null;
        Assert.That(task.IsCompleted,Is.True,"Task deadline / cleanup failure");
    }
    private static IEnumerator Running(JavaProbeSupervisor s,Task task)
    {
        var clock=Stopwatch.StartNew(); while(s.State!=IpcState.RUNNING && !task.IsCompleted && clock.Elapsed.TotalSeconds<18) yield return null;
        Assert.That(s.State,Is.EqualTo(IpcState.RUNNING),task.Exception?.ToString());
    }
    private static object Attempt(JavaProbeSupervisor s) => typeof(JavaProbeSupervisor).GetField("current",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(s);
    private static T Owned<T>(JavaProbeSupervisor s,string field) => (T)Attempt(s).GetType().GetField(field).GetValue(Attempt(s));
    private static void Clean(JavaProbeSupervisor s)
    {
        Assert.That(s.CleanupComplete,Is.True); Assert.That(s.LastSummary.cleanupComplete,Is.True);
        Assert.That(System.IO.Directory.Exists("/proc/"+s.LastSummary.pid),Is.False,"Own child residual");
    }
    [UnityTest] public IEnumerator DeathManualRetryAndThreeLaunchLimit()
    {
        using(var s=Supervisor()) {
            string previous=null; EventHandler lateExit=null;
            for(int i=0;i<3;i++) {
                var task=s.RunLifecycleAsync(Java,Jar,CancellationToken.None); yield return Running(s,task);
                Assert.That(s.LastSummary.instanceId,Is.Not.EqualTo(previous)); previous=s.LastSummary.instanceId;
                if(lateExit!=null) { lateExit(null,EventArgs.Empty); Assert.That(s.State,Is.EqualTo(IpcState.RUNNING)); }
                lateExit=Owned<EventHandler>(s,"exitHandler");
                Owned<Process>(s,"child").Kill(); yield return Wait(task);
                Assert.That(task.IsFaulted,Is.True); Assert.That(s.LastSummary.errorCode,Is.EqualTo("JAVA_EXITED"));
                Assert.That(s.State,Is.EqualTo(IpcState.FAILED)); Clean(s);
            }
            Assert.That(s.Launches,Is.EqualTo(3)); Assert.Throws<InvalidOperationException>(()=>s.RunLifecycleAsync(Java,Jar,CancellationToken.None));
        }
    }
    [UnityTest] public IEnumerator CancelDuringRunningClosesRealChild()
    {
        using(var s=Supervisor()) { var task=s.RunLifecycleAsync(Java,Jar,CancellationToken.None); yield return Running(s,task);
            s.RequestStop(); s.RequestStop(); yield return Wait(task); Assert.That(task.IsFaulted,Is.False,task.Exception?.ToString());
            Assert.That(s.State,Is.EqualTo(IpcState.STOPPED)); Assert.That(s.LastSummary.exitCode,Is.Zero); Clean(s); }
    }
    [UnityTest] public IEnumerator CancelDuringStartupClosesRealChild()
    {
        using(var s=Supervisor()) { var task=s.RunLifecycleAsync(Java,Jar,CancellationToken.None);
            var clock=Stopwatch.StartNew(); while(s.LastSummary.pid==0 && !task.IsCompleted && clock.Elapsed.TotalSeconds<5) yield return null;
            Assert.That(s.LastSummary.pid,Is.GreaterThan(0)); s.RequestStop(); yield return Wait(task);
            Assert.That(task.IsFaulted,Is.False,task.Exception?.ToString()); Assert.That(s.State,Is.EqualTo(IpcState.STOPPED)); Clean(s); }
    }
    [UnityTest] public IEnumerator TcpLossStopsLivingOwnedJava()
    {
        using(var s=Supervisor()) { var task=s.RunLifecycleAsync(Java,Jar,CancellationToken.None); yield return Running(s,task);
            Assert.That(Owned<Process>(s,"child").HasExited,Is.False); Owned<TcpClient>(s,"client").Close(); yield return Wait(task);
            Assert.That(s.LastSummary.errorCode,Is.EqualTo("CONNECTION_LOST")); Assert.That(s.State,Is.EqualTo(IpcState.FAILED)); Clean(s);
            var retry=s.RunLifecycleAsync(Java,Jar,CancellationToken.None); yield return Running(s,retry); s.RequestStop(); yield return Wait(retry); Clean(s); }
    }
    [UnityTest] public IEnumerator SingletonRefusesSecondRealChildWithoutHarmingOwner()
    {
        using(var a=Supervisor()) using(var b=Supervisor()) {
            var owner=a.RunLifecycleAsync(Java,Jar,CancellationToken.None); yield return Running(a,owner);
            var second=b.RunLifecycleAsync(Java,Jar,CancellationToken.None); yield return Wait(second);
            Assert.That(b.LastSummary.errorCode,Is.EqualTo("ALREADY_RUNNING")); Clean(b);
            Assert.That(a.State,Is.EqualTo(IpcState.RUNNING)); Assert.That(Owned<Process>(a,"child").HasExited,Is.False);
            a.RequestStop(); yield return Wait(owner); Clean(a);
        }
    }
    [UnityTest] public IEnumerator RealFixtureReadyTimeoutHasBoundedCleanup()
    {
        using(var s=Supervisor()) {
            string helper=Environment.GetEnvironmentVariable("GORILLA_TEST_FIXTURES")+"/no-ready.jar";
            var clock=Stopwatch.StartNew(); var task=s.RunLifecycleAsync(Java,helper,CancellationToken.None); yield return Wait(task);
            Assert.That(s.LastSummary.errorCode,Is.EqualTo("READY_TIMEOUT")); Assert.That(clock.Elapsed.TotalSeconds,Is.InRange(14.5,20)); Clean(s);
        }
    }
    [UnityTest] public IEnumerator RealFixtureTcpRefusedIsDifferentiated()
    {
        using(var s=Supervisor()) {
            string helper=Environment.GetEnvironmentVariable("GORILLA_TEST_FIXTURES")+"/connect-refused.jar";
            var task=s.RunLifecycleAsync(Java,helper,CancellationToken.None); yield return Wait(task);
            Assert.That(s.LastSummary.errorCode,Is.EqualTo("CONNECT_FAILED")); Clean(s);
        }
    }
}
