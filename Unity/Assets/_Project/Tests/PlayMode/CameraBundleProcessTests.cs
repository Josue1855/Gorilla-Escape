using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GorillaEscape.CameraTracking;
using NUnit.Framework;
using UnityEngine.TestTools;
public sealed class CameraBundleProcessTests
{
    [UnityTest]public IEnumerator PackagedPhysicalCameraReachesUnityWithoutDeveloperPaths()
    {
        string root=Environment.GetEnvironmentVariable("GORILLA_TEST_CAMERA_BUNDLE");Assert.IsFalse(string.IsNullOrEmpty(root));
        var paths=CameraRuntimePaths.Resolve(root,_=>null);
        using(var vision=new VisionSupervisor()) {
            var start=vision.StartAsync(paths.Python,paths.Worker,paths.Mode,paths.Fixture,paths.Model,paths.Device,CancellationToken.None);
            var clock=Stopwatch.StartNew();while(!start.IsCompleted&&clock.Elapsed.TotalSeconds<15)yield return null;
            Assert.IsTrue(start.IsCompleted);Assert.IsFalse(start.IsFaulted,start.Exception?.ToString());
            var store=new CameraInputStore();clock.Restart();
            while(store.Accepted<20&&clock.Elapsed.TotalSeconds<10){if(vision.Take(out var frame,out var received))Assert.IsTrue(store.Accept(frame,received));yield return null;}
            var stop=vision.StopAsync();clock.Restart();while(!stop.IsCompleted&&clock.Elapsed.TotalSeconds<8)yield return null;
            Assert.IsTrue(stop.IsCompleted);Assert.IsTrue(vision.CleanupComplete);Assert.IsFalse(vision.Forced);Assert.AreEqual(0,vision.ExitCode);Assert.IsFalse(System.IO.Directory.Exists("/proc/"+vision.Pid));
            Assert.GreaterOrEqual(store.Accepted,20);Assert.AreEqual("physical-webcam",store.Latest.source);Assert.AreEqual(640,store.Latest.width);Assert.AreEqual(480,store.Latest.height);
            UnityEngine.Debug.Log("CAMERA_PACKAGED_SUMMARY "+vision.Summary);
        }
    }
}
