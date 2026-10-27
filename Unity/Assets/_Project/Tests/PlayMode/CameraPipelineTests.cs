using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GorillaEscape.CameraTracking;
using NUnit.Framework;
using UnityEngine.TestTools;
public sealed class CameraPipelineTests {
 static string Env(string name)=>Environment.GetEnvironmentVariable("GORILLA_TEST_CAMERA_"+name);
 static IEnumerator Wait(Task task,int timeout=16){var c=Stopwatch.StartNew();while(!task.IsCompleted&&c.Elapsed.TotalSeconds<timeout)yield return null;Assert.That(task.IsCompleted,Is.True,"vision deadline");}
 static Task Start(VisionSupervisor s,string mode,CancellationToken token)=>s.StartAsync(Env("PYTHON"),Env("SCRIPT"),mode,Env("FIXTURE"),"","/dev/nonexistent",token);
 static void Clean(VisionSupervisor s){Assert.That(s.CleanupComplete,Is.True);Assert.That(System.IO.Directory.Exists("/proc/"+s.Pid),Is.False);}
 [UnityTest]public IEnumerator ReplayChildFramesReachStoreAndEofReleases(){using(var s=new VisionSupervisor()){var t=Start(s,"replay",CancellationToken.None);yield return Wait(t);Assert.That(t.IsFaulted,Is.False,t.Exception?.ToString());var store=new CameraInputStore();var clock=Stopwatch.StartNew();while(store.Accepted<15&&clock.Elapsed.TotalSeconds<5){if(s.Take(out var f,out var stamp))Assert.That(store.Accept(f,stamp),Is.True);yield return null;}Assert.That(store.Accepted,Is.GreaterThanOrEqualTo(15));Assert.That(store.Latest.source,Is.EqualTo("replay"));var stop=s.StopAsync();yield return Wait(stop);Assert.That(s.ExitCode,Is.Zero);Assert.That(s.Forced,Is.False);Clean(s);}}
 [UnityTest]public IEnumerator UnavailableAndVisionFailureCleanWithoutAffectingJava(){foreach(var mode in new[]{"unavailable","failure"})using(var s=new VisionSupervisor()){var t=Start(s,mode,CancellationToken.None);yield return Wait(t);if(!t.IsFaulted){var c=Stopwatch.StartNew();while(s.State!="FAILED"&&c.Elapsed.TotalSeconds<3){s.Poll();yield return null;}}var stop=s.StopAsync();yield return Wait(stop);Assert.That(s.Error,Is.Not.Null);Assert.That(s.ExitCode,Is.EqualTo(2));Clean(s);}}
 [UnityTest]public IEnumerator CancelStartupIsBoundedAndOwnChildStops(){using(var s=new VisionSupervisor())using(var token=new CancellationTokenSource()){var t=Start(s,"silent",token.Token);token.Cancel();yield return Wait(t);Assert.That(t.IsCanceled,Is.True);Assert.That(s.Error,Is.EqualTo("CAMERA_CANCELLED"));Assert.That(s.Forced,Is.False);Clean(s);}}
 [UnityTest]public IEnumerator StartupDeadlineAndInvalidFramesAreBounded(){foreach(var mode in new[]{"silent","malformed","oversized"})using(var s=new VisionSupervisor()){var t=Start(s,mode,CancellationToken.None);yield return Wait(t);if(!t.IsFaulted){var c=Stopwatch.StartNew();while(s.State!="FAILED"&&c.Elapsed.TotalSeconds<3){s.Poll();yield return null;}}var stop=s.StopAsync();yield return Wait(stop);Assert.That(s.Error,Is.Not.Null);Assert.That(s.Forced,Is.False);Clean(s);}}
}
