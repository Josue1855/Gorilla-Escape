using System;
using System.Collections;
using System.IO;
using System.Threading;
using GorillaEscape.Network;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
public class IpcHardeningProcessTests
{
    private IEnumerator InvalidFixture(string name,string expected)
    {
        string java=Environment.GetEnvironmentVariable("GORILLA_TEST_JAVA"),folder=Environment.GetEnvironmentVariable("GORILLA_TEST_FIXTURES");
        if(string.IsNullOrEmpty(java)||string.IsNullOrEmpty(folder))Assert.Ignore("JVM fixture paths missing: NOT RUN");
        using(var s=new JavaProbeSupervisor(new IpcLaunchBudget())) {
            var task=s.RunAsync(java,Path.Combine(folder,name),CancellationToken.None);
            while(!task.IsCompleted)yield return null;
            Assert.That(task.IsFaulted,Is.True);Assert.That(s.State,Is.EqualTo(IpcState.FAILED));
            Assert.That(s.LastSummary.errorCode,Is.EqualTo(expected));Assert.That(s.CleanupComplete,Is.True);
            Assert.That(s.LastSummary.exitCode,Is.Zero);Assert.That(s.LastSummary.cleanExit,Is.True);
            if(Application.platform==RuntimePlatform.LinuxEditor)Assert.That(Directory.Exists("/proc/"+s.LastSummary.pid),Is.False);
        }
    }
    [UnityTest] public IEnumerator DuplicatePongFromTestJvmFailsAndCleansUp() {return InvalidFixture("pong-duplicate.jar","PROTOCOL_INVALID");}
    [UnityTest] public IEnumerator DuplicateReadyFromTestJvmFailsBeforeConnect() {return InvalidFixture("ready-duplicate.jar","READY_INVALID");}
}
