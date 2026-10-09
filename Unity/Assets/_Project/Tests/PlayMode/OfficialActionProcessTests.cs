using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GorillaEscape.Network;
using GorillaEscape.Input;
using NUnit.Framework;
using UnityEngine.TestTools;
public class OfficialActionProcessTests {
    private IEnumerator Until(Func<bool> predicate,string label,int seconds=30){var clock=Stopwatch.StartNew();while(!predicate()&&clock.Elapsed.TotalSeconds<seconds)yield return null;Assert.IsTrue(predicate(),label);}
    [UnityTest] public IEnumerator RealRtcActionCancellationRetentionAndReplay(){
        string java=Environment.GetEnvironmentVariable("GORILLA_TEST_JAVA"),jar=Environment.GetEnvironmentVariable("GORILLA_TEST_JAR");
        string node=Environment.GetEnvironmentVariable("GORILLA_TEST_NODE"),script=Environment.GetEnvironmentVariable("GORILLA_TEST_ACTION_CLIENTS"),output=Environment.GetEnvironmentVariable("GORILLA_TEST_ACTION_OUTPUT");
        Assert.IsTrue(File.Exists(java)&&File.Exists(jar)&&File.Exists(node)&&File.Exists(script),"Real Java, Node and lobby-client fixture paths required");
        string previousLab=Environment.GetEnvironmentVariable("GORILLA_MOBILE_LAB"),previousOperator=Environment.GetEnvironmentVariable("GORILLA_MOBILE_OPERATOR");
        byte[] bytes=new byte[32];using(var random=System.Security.Cryptography.RandomNumberGenerator.Create())random.GetBytes(bytes);
        string auth=Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');
        Environment.SetEnvironmentVariable("GORILLA_MOBILE_LAB","1");Environment.SetEnvironmentVariable("GORILLA_MOBILE_OPERATOR",auth);
        var messages=new ConcurrentQueue<string>();int port=0;Process browsers=null;Task<ProbeSummary> running=null;
        using(var s=new JavaProbeSupervisor(new IpcLaunchBudget(),enableLobby:true)) {
            try {
                s.HttpReady+=p=>port=p;running=s.RunLifecycleAsync(java,jar,CancellationToken.None);
                yield return Until(()=>s.State==IpcState.RUNNING||running.IsCompleted,"Unity IPC RUNNING");Assert.IsFalse(running.IsCompleted,running.Exception?.ToString());
                var start=new ProcessStartInfo{FileName=node,Arguments=(Environment.GetEnvironmentVariable("GORILLA_TEST_NODE_ARGUMENTS")??"")+" \""+script+"\" http://127.0.0.1:"+port+" \""+output+"\"",UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
                browsers=new Process{StartInfo=start,EnableRaisingEvents=true};browsers.OutputDataReceived+=(o,e)=>{if(e.Data!=null)messages.Enqueue(e.Data);};browsers.ErrorDataReceived+=(o,e)=>{};
                Assert.IsTrue(browsers.Start());browsers.StandardInput.WriteLine("AUTH "+auth);browsers.StandardInput.Flush();browsers.BeginOutputReadLine();browsers.BeginErrorReadLine();
                yield return Until(()=>messages.TryPeek(out _)||browsers.HasExited,"Browser NETWORK_ONLY",60);Assert.IsTrue(messages.TryDequeue(out var stage));Assert.AreEqual("NETWORK_ONLY",stage);
                yield return Until(()=>s.Lobby.Snapshot?.players.Length==4,"Four associated players");
                foreach(var p in s.Lobby.Snapshot.players){Assert.IsTrue(p.networkReady);Assert.IsFalse(p.inputReady);Assert.IsFalse(p.playerReady);}
                var premature=s.Lobby.SetPlayerReadyAsync(1,true);yield return Until(()=>premature.IsCompleted,"Premature READY receipt");Assert.IsFalse(premature.Result.accepted);Assert.AreEqual("LOBBY_PREREQUISITES",premature.Result.code);
                browsers.StandardInput.WriteLine("INPUT");browsers.StandardInput.Flush();
                yield return Until(()=>messages.TryPeek(out _)||browsers.HasExited,"Input streaming");Assert.IsTrue(messages.TryDequeue(out stage));Assert.AreEqual("INPUT_STREAMING",stage);
                yield return Until(()=>Array.TrueForAll(s.Lobby.Snapshot.players,p=>p.inputReady),"Fresh movement prerequisites");
                foreach(var p in s.Lobby.Snapshot.players){var ready=s.Lobby.SetPlayerReadyAsync(p.playerId,true);yield return Until(()=>ready.IsCompleted,"Unity READY receipt");Assert.IsTrue(ready.Result.accepted);}
                var starting=s.Lobby.SetPhaseAsync("STARTING");yield return Until(()=>starting.IsCompleted,"Unity STARTING receipt");Assert.IsTrue(starting.Result.accepted);
                var started=s.Lobby.SetPhaseAsync("STARTED");yield return Until(()=>started.IsCompleted,"Unity STARTED receipt");Assert.IsTrue(started.Result.accepted);Assert.AreEqual("STARTED",s.Lobby.Snapshot.phase);
                string device=s.Lobby.Snapshot.players[0].deviceSessionId;long epoch=s.Lobby.Snapshot.players[0].connectionEpoch;
                // Unity future gameplay boundary: explicit creation and confirmation from accepted input.
                ActionKey confirmed=default;
                yield return Until(()=>s.PhoneInputs.TryActionSource(1,"lab-result",out confirmed),"Unity source from real RTC input");
                Assert.AreEqual(ActionDecision.CREATED,s.PhoneInputs.Actions.TryBegin(confirmed,out _));
                Assert.AreEqual(ActionDecision.CONFIRMED,s.PhoneInputs.Actions.Confirm(confirmed,"lab-accepted",out var official));
                var retained=official.Result;int effects=1;
                Assert.AreEqual(ActionDecision.IDEMPOTENT,s.PhoneInputs.Actions.Confirm(confirmed,"lab-accepted",out _));
                ActionKey candidate=default;
                yield return Until(()=>s.PhoneInputs.TryActionSource(1,"lab-pending",out candidate)&&candidate.SourceSequence>confirmed.SourceSequence,"Independent pending source");
                Assert.AreEqual(ActionDecision.CREATED,s.PhoneInputs.Actions.TryBegin(candidate,out _));
                browsers.StandardInput.WriteLine("STARTED");browsers.StandardInput.Flush();
                yield return Until(()=>messages.TryPeek(out _)||browsers.HasExited,"Disconnect observed by browser",60);
                Assert.IsTrue(messages.TryDequeue(out stage));Assert.AreEqual("DISCONNECTED",stage);
                yield return Until(()=>s.PhoneInputs.Actions.TryGet(candidate,out var record)&&record.State==OfficialActionState.CANCELLED,"Unity cancelled unconfirmed action");
                Assert.AreEqual(ActionDecision.TERMINAL,s.PhoneInputs.Actions.Confirm(candidate,"late",out _));
                Assert.IsTrue(s.PhoneInputs.Actions.TryGet(confirmed,out var saved));Assert.AreSame(retained,saved.Result);
                browsers.StandardInput.WriteLine("RECONNECT");browsers.StandardInput.Flush();
                yield return Until(()=>messages.TryPeek(out _)||browsers.HasExited,"Post-start resume",60);Assert.IsTrue(messages.TryDequeue(out stage));Assert.AreEqual("RESUMED",stage);
                yield return Until(()=>s.Lobby.Snapshot.players[0].connectionEpoch==epoch+1,"Unity sees resumed epoch");
                Assert.AreEqual(1,s.Lobby.Snapshot.players[0].playerId);Assert.AreEqual(device,s.Lobby.Snapshot.players[0].deviceSessionId);Assert.IsFalse(s.Lobby.Snapshot.players[0].playerReady);Assert.AreEqual("STARTED",s.Lobby.Snapshot.phase);
                yield return Until(()=>s.PhoneInputs.TryLatest(1,out var sample)&&sample.connectionEpoch==epoch+1,"Resumed input reaches Unity");
                Assert.IsTrue(s.PhoneInputs.TryLatest(1,out var input));Assert.AreEqual(epoch+1,input.connectionEpoch);Assert.AreEqual(device,input.deviceSessionId);
                Assert.IsTrue(s.PhoneInputs.Actions.TryGet(confirmed,out saved));Assert.AreSame(retained,saved.Result);
                Assert.AreEqual(ActionDecision.DUPLICATE,s.PhoneInputs.Actions.TryBegin(confirmed,out _));
                Assert.AreEqual(ActionDecision.DUPLICATE,s.PhoneInputs.Actions.TryBegin(candidate,out _));
                var oldEpoch=new ActionKey(confirmed.SessionId,confirmed.DeviceSessionId,1,epoch,input.sequence,"old-unused");
                Assert.AreEqual(ActionDecision.STALE,s.PhoneInputs.Actions.TryBegin(oldEpoch,out _));
                Assert.IsTrue(s.PhoneInputs.TryActionSource(1,"new-epoch",out var independent));
                yield return Until(()=>s.Lobby.Snapshot.players[0].inputReady,"Resumed fresh input");
                Assert.IsTrue(s.PhoneInputs.TryActionSource(1,"new-epoch",out independent));
                Assert.AreEqual(ActionDecision.CREATED,s.PhoneInputs.Actions.TryBegin(independent,out _));
                Assert.AreEqual(ActionDecision.CONFIRMED,s.PhoneInputs.Actions.Confirm(independent,"lab-accepted",out _));effects++;
                Assert.AreEqual(ActionDecision.IDEMPOTENT,s.PhoneInputs.Actions.Confirm(independent,"lab-accepted",out _));Assert.AreEqual(2,effects);
                browsers.StandardInput.WriteLine("FINISH");browsers.StandardInput.Flush();yield return Until(()=>browsers.HasExited,"Browser cleanup");Assert.AreEqual(0,browsers.ExitCode);
                s.RequestStop();yield return Until(()=>running.IsCompleted,"Owned Java cleanup");Assert.IsFalse(running.IsFaulted,running.Exception?.ToString());Assert.IsTrue(s.CleanupComplete);Assert.AreEqual(0,s.LastSummary.exitCode);Assert.IsNull(s.Lobby.Snapshot);Assert.IsFalse(Directory.Exists("/proc/"+s.LastSummary.pid));
            } finally {
                if(browsers!=null){if(!browsers.HasExited){browsers.StandardInput.Close();if(!browsers.WaitForExit(5000)){browsers.Kill();browsers.WaitForExit(2000);}}browsers.Dispose();}
                s.RequestStop();if(running!=null&&!running.IsCompleted)running.Wait(12000);
                Environment.SetEnvironmentVariable("GORILLA_MOBILE_LAB",previousLab);Environment.SetEnvironmentVariable("GORILLA_MOBILE_OPERATOR",previousOperator);
            }
        }
    }
}
