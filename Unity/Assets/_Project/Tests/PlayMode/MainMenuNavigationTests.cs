using System;
using System.Collections;
using GorillaEscape.UI;
using GorillaEscape.QA;
using GorillaEscape.CameraTracking;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MainMenuNavigationTests
{
    [UnityTest] public IEnumerator NavigationStaysAvailableWhenTechnicalServicesFail()
    {
        var names=new[]{"GORILLA_QA_MENU","GORILLA_QA_OVERLAY","GORILLA_IPC_JAVA","GORILLA_IPC_JAR"};
        var old=Array.ConvertAll(names,Environment.GetEnvironmentVariable);
        GameObject host=null;
        try
        {
            Environment.SetEnvironmentVariable(names[0],"1");
            Environment.SetEnvironmentVariable(names[2],"/nonexistent-java");
            Environment.SetEnvironmentVariable(names[3],"/nonexistent-jar");
            host=new GameObject("Navigation test");
            var menu=host.AddComponent<MainMenuController>();
            yield return null;
            Assert.False(menu.InTests);Assert.False(menu.PlayEnabled);Assert.False(menu.Runtime.TestsActive);
            menu.OpenTests();Assert.True(menu.InTests);Assert.True(menu.Runtime.TestsActive);
            menu.Back();Assert.False(menu.InTests);Assert.False(menu.Runtime.TestsActive);
            Environment.SetEnvironmentVariable(names[0],"0");menu.OpenTests();Assert.False(menu.InTests);
            Environment.SetEnvironmentVariable(names[0],"1");menu.OpenTests();menu.Back();
        }
        finally
        {
            if(host!=null)UnityEngine.Object.Destroy(host);
            for(int i=0;i<names.Length;i++)Environment.SetEnvironmentVariable(names[i],old[i]);
        }
        yield return null;
    }
    [UnityTest] public IEnumerator PreviewControlCreatesAndClosesOnlyItsLoopbackListener()
    {
        string previous=Environment.GetEnvironmentVariable("GORILLA_QA_OVERLAY");
        Environment.SetEnvironmentVariable("GORILLA_QA_OVERLAY","1");
        var paths=CameraRuntimePaths.Resolve(Environment.GetEnvironmentVariable("GORILLA_TEST_CAMERA_BUNDLE"),_=>null);
        using(var vision=new VisionSupervisor())
        {
            CameraPreviewClient preview=null;
            try
            {
                var start=vision.StartAsync(paths.Python,Environment.GetEnvironmentVariable("GORILLA_TEST_CAMERA_SCRIPT"),"physical",paths.Fixture,paths.Model,paths.Device,CancellationToken.None);
                var clock=System.Diagnostics.Stopwatch.StartNew();while(!start.IsCompleted&&clock.Elapsed.TotalSeconds<15)yield return null;
                Assert.IsTrue(start.IsCompleted);Assert.IsFalse(start.IsFaulted,start.Exception?.ToString());Assert.AreEqual(0,vision.PreviewPort);
                for(int repeat=0;repeat<2;repeat++)
                {
                    vision.SetQaPreview(true);clock.Restart();while(vision.PreviewControlPending&&clock.Elapsed.TotalSeconds<5)yield return null;
                    Assert.IsFalse(vision.PreviewControlPending);int port=vision.PreviewPort;Assert.Greater(port,0);
                    preview=new CameraPreviewClient();preview.Start(port);byte[] jpeg=null;clock.Restart();
                    while(jpeg==null&&clock.Elapsed.TotalSeconds<5){jpeg=preview.Take();yield return null;}
                    Assert.IsTrue(CameraPreviewClient.ValidJpeg(jpeg));Array.Clear(jpeg,0,jpeg.Length);
                    preview.Dispose();clock.Restart();while(!preview.Run.IsCompleted&&clock.Elapsed.TotalSeconds<3)yield return null;Assert.IsTrue(preview.Run.IsCompleted);preview=null;
                    vision.SetQaPreview(false);clock.Restart();while(vision.PreviewControlPending&&clock.Elapsed.TotalSeconds<5)yield return null;
                    Assert.IsFalse(vision.PreviewControlPending);Assert.AreEqual(0,vision.PreviewPort);
                    using(var socket=new System.Net.Sockets.TcpClient())Assert.Throws<System.Net.Sockets.SocketException>(()=>socket.Connect(System.Net.IPAddress.Loopback,port));
                }
                var stop=vision.StopAsync();clock.Restart();while(!stop.IsCompleted&&clock.Elapsed.TotalSeconds<10)yield return null;
                Assert.IsTrue(vision.CleanupComplete);Assert.IsTrue(vision.ControlsRestored);
            }
            finally
            {
                preview?.Dispose();System.Threading.Tasks.Task.Run(()=>vision.StopAsync()).GetAwaiter().GetResult();
                Environment.SetEnvironmentVariable("GORILLA_QA_OVERLAY",previous);
            }
        }
    }
    [UnityTest] public IEnumerator AutomatedFrameMeasurementCannotChangePreviewRoute()
    {
        var names=new[]{"GORILLA_QA_MENU","GORILLA_QA_OVERLAY","GORILLA_QA_AUTOMATED_MEASUREMENT","GORILLA_T018_MEASURE","GORILLA_IPC_JAVA","GORILLA_IPC_JAR"};
        var old=Array.ConvertAll(names,Environment.GetEnvironmentVariable);GameObject host=null;
        try
        {
            Environment.SetEnvironmentVariable(names[0],"1");Environment.SetEnvironmentVariable(names[2],"1");Environment.SetEnvironmentVariable(names[3],"1");
            Environment.SetEnvironmentVariable(names[4],"/nonexistent-java");Environment.SetEnvironmentVariable(names[5],"/nonexistent-jar");
            host=new GameObject("Automated measurement test");var menu=host.AddComponent<MainMenuController>();yield return null;
            menu.OpenTests();Assert.IsTrue(menu.Runtime.MeasuringFrames);menu.Back();Assert.IsTrue(menu.InTests);
            Environment.SetEnvironmentVariable(names[2],null);menu.Back();Assert.IsFalse(menu.InTests);
        }
        finally
        {
            if(host!=null)UnityEngine.Object.Destroy(host);
            for(int i=0;i<names.Length;i++)Environment.SetEnvironmentVariable(names[i],old[i]);
        }
        yield return null;
    }
}
