using System;
using System.Collections;
using System.IO;
using System.Threading;
using GorillaEscape.Network;
using NUnit.Framework;
using UnityEngine.TestTools;
public class LiveQaIpcTests {
 [UnityTest]public IEnumerator QaNegotiatesAndRegeneratesBoundedAdmission(){
  var names=new[]{"GORILLA_QA_OVERLAY","GORILLA_MOBILE_LAB","GORILLA_MOBILE_OPERATOR"};var old=Array.ConvertAll(names,Environment.GetEnvironmentVariable);
  byte[] bytes=new byte[32];using(var random=System.Security.Cryptography.RandomNumberGenerator.Create())random.GetBytes(bytes);
  Environment.SetEnvironmentVariable(names[0],"1");Environment.SetEnvironmentVariable(names[1],"1");Environment.SetEnvironmentVariable(names[2],Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_'));
  using(var s=new JavaProbeSupervisor(new IpcLaunchBudget(),enableLobby:true)){
   var run=s.RunLifecycleAsync(Environment.GetEnvironmentVariable("GORILLA_TEST_JAVA"),Environment.GetEnvironmentVariable("GORILLA_TEST_JAR"),CancellationToken.None);
   try{
    var clock=System.Diagnostics.Stopwatch.StartNew();while(s.State!=IpcState.RUNNING&&!run.IsCompleted&&clock.Elapsed.TotalSeconds<20)yield return null;Assert.AreEqual(IpcState.RUNNING,s.State,run.Exception?.ToString());
    s.RegenerateQa();clock.Restart();while(string.IsNullOrEmpty(s.Qa?.modules)&&!run.IsCompleted&&clock.Elapsed.TotalSeconds<5)yield return null;
    Assert.AreEqual("AVAILABLE",s.Qa.state);Assert.That(s.Qa.ttlMs,Is.InRange(1,30000));Assert.That(s.Qa.size,Is.InRange(21,89));var first=s.Qa.modules;Assert.IsNotEmpty(first);
    s.RegenerateQa();clock.Restart();while(s.Qa.modules==first&&!run.IsCompleted&&clock.Elapsed.TotalSeconds<5)yield return null;Assert.AreNotEqual(first,s.Qa.modules);Assert.AreEqual(0,s.Lobby.Snapshot.players.Length);
    s.RequestStop();clock.Restart();while(!run.IsCompleted&&clock.Elapsed.TotalSeconds<15)yield return null;Assert.IsTrue(run.IsCompleted);Assert.IsFalse(run.IsFaulted,run.Exception?.ToString());Assert.IsTrue(s.CleanupComplete);Assert.IsNull(s.Qa,"A retired backend cannot retain a presentable admission");Assert.IsNull(s.QaPhones);Assert.IsFalse(Directory.Exists("/proc/"+s.LastSummary.pid));
   }finally{s.RequestStop();if(!run.IsCompleted)run.Wait(15000);for(int i=0;i<names.Length;i++)Environment.SetEnvironmentVariable(names[i],old[i]);}
  }
 }
}
