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
    [UnityTest] public IEnumerator PhoneFlowWorksWithQaHiddenAndDoesNotStartEditorServices()
    {
        var previous=Environment.GetEnvironmentVariable("GORILLA_QA_MENU");
        Environment.SetEnvironmentVariable("GORILLA_QA_MENU","0");
        var host=new GameObject("Product phone flow fixture");
        try{
            var menu=host.AddComponent<MainMenuController>();yield return null;
            Assert.False(menu.PlayEnabled);menu.OpenPhone();
            Assert.True(menu.InPhone);Assert.True(menu.Runtime.PhoneActive);
            Assert.False(menu.InTests);Assert.False(menu.Runtime.TestsActive);
            Assert.False(menu.Runtime.ServicesEnabled);Assert.True(menu.Runtime.PhoneFailed);
            menu.OpenTests();Assert.False(menu.InTests);
            menu.Back();Assert.False(menu.InPhone);Assert.False(menu.Runtime.PhoneActive);
            Assert.True(menu.Runtime.CleanupComplete);
        }finally{UnityEngine.Object.Destroy(host);Environment.SetEnvironmentVariable("GORILLA_QA_MENU",previous);}
        yield return null;
    }

    [UnityTest] public IEnumerator ProductBootstrapRemainsOptInInsideTestRunner()
    {
        var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(MainMenuController.BootstrapScenePath,UnityEngine.SceneManagement.LoadSceneMode.Additive);
        var bootstrap=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(MainMenuController.BootstrapScenePath);
        try
        {
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(bootstrap);
            MainMenuController.TryLaunchForCurrentScene();
            Assert.IsNull(UnityEngine.Object.FindFirstObjectByType<MainMenuController>(),"Even the product scene must not auto-launch inside a test run");
        }
        finally { UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous); }
        yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(bootstrap);
    }

    [UnityTest] public IEnumerator NonProductSceneDoesNotAutoLaunchAndEditorBackendStaysIdle()
    {
        MainMenuController.TryLaunchForCurrentScene();
        Assert.IsNull(UnityEngine.Object.FindFirstObjectByType<MainMenuController>());
        var host=new GameObject("Explicit editor navigation fixture");
        var menu=host.AddComponent<MainMenuController>();
        bool previousBackground=Application.runInBackground;
        try
        {
            yield return null;
            Assert.False(menu.Runtime.ServicesEnabled);
            Assert.That(menu.Runtime.JavaStatus,Does.StartWith("JAVA NOT RUNNING"));
            Assert.That(menu.Runtime.CameraStatus,Does.StartWith("CAMERA NOT RUNNING"));
            Assert.True(menu.Runtime.CleanupComplete);
            menu.OpenTests();Assert.True(menu.InTests);
            var closing=menu.Runtime.ShutdownAsync();
            while(!closing.IsCompleted)yield return null;
            Assert.False(closing.IsFaulted);
            Assert.True(menu.Runtime.CleanupComplete);
            Assert.False(menu.Runtime.TestsActive);
            Assert.AreSame(closing,menu.Runtime.ShutdownAsync());
            // Prevent Update requesting Stop Play Mode inside the test runner.
            menu.Runtime.enabled=false;
        }
        finally { UnityEngine.Object.Destroy(host); }
        yield return null;
        Assert.AreEqual(previousBackground,Application.runInBackground);
    }

    [UnityTest] public IEnumerator InheritedPhysicalQaFlagNeverOverridesTestRunner()
    {
        string before=Environment.GetEnvironmentVariable("GORILLA_EDITOR_PHYSICAL_QA");
        GameObject host=null;
        try
        {
            Environment.SetEnvironmentVariable("GORILLA_EDITOR_PHYSICAL_QA","1");
#if UNITY_EDITOR
            Assert.True(UnityEditor.SessionState.GetBool("GorillaEscape.Product.TestRun",false));
#endif
            host=new GameObject("Inherited physical flags inside tests");var menu=host.AddComponent<MainMenuController>();
            yield return null;
            Assert.False(menu.Runtime.ServicesEnabled);Assert.True(menu.Runtime.CleanupComplete);
            menu.OpenPhone();menu.Runtime.InstallNetwork();menu.Runtime.ConfigurePhone();
            yield return null;
            Assert.That(menu.Runtime.JavaStatus,Does.StartWith("JAVA NOT RUNNING"));
#if UNITY_EDITOR
            Assert.True(UnityEditor.SessionState.GetBool("GorillaEscape.Product.TestRun",false));
#endif
            Assert.True(menu.Runtime.CleanupComplete);
        }
        finally{if(host!=null)UnityEngine.Object.Destroy(host);Environment.SetEnvironmentVariable("GORILLA_EDITOR_PHYSICAL_QA",before);}
        yield return null;
    }

    [UnityTest] public IEnumerator DestroyBeforeFirstFrameDoesNotRequireBackendInitialization()
    {
        var host=new GameObject("Short-lived editor navigation fixture");
        host.AddComponent<MainMenuController>();
        UnityEngine.Object.DestroyImmediate(host);
        yield return null;
        LogAssert.NoUnexpectedReceived();
    }

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
