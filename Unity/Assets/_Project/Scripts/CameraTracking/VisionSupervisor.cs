using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using GorillaEscape.Contracts;
using UnityEngine;
namespace GorillaEscape.CameraTracking
{
    /// <summary>Local owned Linux vision adapter. Private child pipes, latest-only delivery, no image transport.</summary>
    public sealed class VisionSupervisor : IDisposable {
        public const int MaxLine=16384;
        readonly object gate=new object(); CameraInputWire pending;Process child;
        Task stdout,stderr; bool ready; long received;
        string controlSnapshot,controlPython,controlScript,controlDevice;bool physical;
        readonly bool telemetryEnabled=Environment.GetEnvironmentVariable("GORILLA_TELEMETRY_MODE")!="minimal";
        readonly Queue<long> transit=new Queue<long>();long transitCount,transitMax,invalidTransit;
        public bool ControlsRestored {get;private set;}=true;
        public string RestoreFailure {get;private set;}
        public string LatencySummary {get {lock(gate){var values=transit.ToArray();Array.Sort(values);return JsonUtility.ToJson(new Transit{N=transitCount,retained=values.Length,overwritten=Math.Max(0,transitCount-values.Length),enabled=telemetryEnabled,P50=values.Length==0?-1:values[(int)Math.Ceiling(values.Length*.5)-1],P95=values.Length==0?-1:values[(int)Math.Ceiling(values.Length*.95)-1],max=transitMax,invalid=invalidTransit,clock="same-PC UTC milliseconds: processTimestamp to pipe receive; no optical/gameplay claim"});}}}
        [Serializable]sealed class Transit {public long N,P50,P95,max,invalid,overwritten;public int retained;public bool enabled;public string clock;}
        public string State {get;private set;}="STOPPED";
        public string Error {get;private set;} public int Pid {get;private set;}
        public int? ExitCode {get;private set;} public bool CleanupComplete {get;private set;}=true;
        public bool Forced {get;private set;} public long Overwritten {get;private set;} public long StderrBytes {get;private set;}
        public int PreviewPort {get;private set;}
        public QaCameraMetrics QaMetrics {get;private set;}
        [Serializable] public sealed class QaCameraMetrics {public double captureFps=-1,inferenceFps=-1,previewFps=-1;public long overwritten,errors;}
        long previewRevision;
        public bool PreviewControlPending {get;private set;}
        public void SetQaPreview(bool active){
            if(Environment.GetEnvironmentVariable("GORILLA_QA_OVERLAY")!="1"||State!="RUNNING")return;
            PreviewPort=0;previewRevision++;PreviewControlPending=true;
            try{child.StandardInput.WriteLine("QA_PREVIEW 1 "+previewRevision+" "+(active?"1":"0"));child.StandardInput.Flush();}
            catch(Exception){Error="PREVIEW_CONTROL_FAILED";State="FAILED";PreviewControlPending=false;}
        }
        [Serializable] sealed class PreviewEndpoint {public int version,port;public long revision;}
        public string Summary {get;private set;}
        public bool Take(out CameraInputWire frame,out long timestamp){lock(gate){frame=pending;timestamp=received;pending=null;return frame!=null;}}
        static string Quote(string value){if(value==null||value.IndexOfAny(new[]{'"','\n','\r'})>=0)throw new ArgumentException("Invalid adapter path");return "\""+value+"\"";}
        public Task StartPackagedAsync(string productDirectory,CancellationToken token) {
            try {var paths=CameraRuntimePaths.Resolve(productDirectory);return StartAsync(paths.Python,paths.Worker,paths.Mode,paths.Fixture,paths.Model,paths.Device,token);}
            catch(FileNotFoundException error){Error=error.Message;State="FAILED";return Task.FromException(error);}
        }
        public async Task StartAsync(string python,string script,string mode,string fixture,string model,string device,CancellationToken token){
            if(child!=null)throw new InvalidOperationException("Stop prior own camera child first");
            CleanupComplete=false;State="STARTING";Error=null;ready=false;Forced=false;ExitCode=null;Summary=null;RestoreFailure=null;
            controlSnapshot=null;controlPython=python;controlScript=Path.Combine(Path.GetDirectoryName(script),"camera_controls.py");controlDevice=device??"/dev/video0";physical=mode=="physical";ControlsRestored=true;
            lock(gate){transit.Clear();transitCount=transitMax=invalidTransit=0;}
            try {
                child=new Process{StartInfo=new ProcessStartInfo{FileName=python,Arguments=Quote(script)+" --mode "+Quote(mode)+" --fixture "+Quote(fixture??"")+" --model "+Quote(model??"")+" --device "+Quote(device??"/dev/video0"),UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true}};
                child.Start();Pid=child.Id;stdout=ReadOutput(child.StandardOutput);stderr=Drain(child.StandardError);
                var clock=Stopwatch.StartNew();while(!ready){token.ThrowIfCancellationRequested();if(Error!=null||child.HasExited)throw new IOException(Error??"VISION_EXITED");if(clock.ElapsedMilliseconds>=30000)throw new TimeoutException("CAMERA_STARTUP_TIMEOUT");await Task.Delay(10,token);}
                State="RUNNING";
            }catch(Exception e){Error=e is OperationCanceledException?"CAMERA_CANCELLED":e is TimeoutException?"CAMERA_STARTUP_TIMEOUT":Error??"CAMERA_UNAVAILABLE";State="FAILED";await StopAsync();throw;}
        }
        async Task ReadOutput(StreamReader reader){
            try{var line=new StringBuilder();var buffer=new char[256];int n;
                while((n=await reader.ReadAsync(buffer,0,buffer.Length))>0)for(int i=0;i<n;i++){
                    if(buffer[i]=='\n'){Handle(line.ToString());line.Clear();}else{if(line.Length>=MaxLine)throw new IOException("CAMERA_FRAME_OVERSIZED");if(buffer[i]!='\r')line.Append(buffer[i]);}
                }
                if(line.Length!=0)throw new IOException("CAMERA_FRAME_TRUNCATED");
            }catch(Exception e){Error=e is IOException?e.Message:"CAMERA_PROTOCOL_INVALID";State="FAILED";try{child?.StandardInput.Close();}catch{}}
        }
        void Handle(string line){
            if(line.StartsWith("CAMERA_PREVIEW_PORT ")){if(Environment.GetEnvironmentVariable("GORILLA_QA_OVERLAY")!="1")throw new IOException("PREVIEW_UNEXPECTED");var endpoint=JsonUtility.FromJson<PreviewEndpoint>(line.Substring(20));if(endpoint.version!=1||endpoint.port<0||endpoint.port>65535)throw new IOException("PREVIEW_PORT");if(endpoint.revision==previewRevision){PreviewPort=endpoint.port;PreviewControlPending=false;}return;}
            if(line.StartsWith("CAMERA_QA_METRICS ")){if(Environment.GetEnvironmentVariable("GORILLA_QA_OVERLAY")!="1")throw new IOException("QA_UNEXPECTED");QaMetrics=JsonUtility.FromJson<QaCameraMetrics>(line.Substring(18));return;}
            if(line.StartsWith("CAMERA_CONTROL_SNAPSHOT ")){if(!physical||ready||controlSnapshot!=null||line.Length>4096)throw new IOException("CAMERA_CONTROL_SNAPSHOT_INVALID");controlSnapshot=line.Substring(24);ControlsRestored=false;return;}
            if(line=="CAMERA_READY"){if(ready)throw new IOException("CAMERA_READY_DUPLICATE");ready=true;return;}
            if(line.StartsWith("CAMERA_SUMMARY ")){Summary=line.Substring(15);return;}
            if(!ready||!line.StartsWith("CAMERA_FRAME "))throw new IOException("CAMERA_PROTOCOL_INVALID");
            CameraInputWire f;try{f=JsonUtility.FromJson<CameraInputWire>(line.Substring(13));}catch{throw new IOException("CAMERA_PROTOCOL_INVALID");}
            if(!CameraInputStore.Valid(f))throw new IOException("CAMERA_CONTRACT_INVALID");
            lock(gate){if(pending!=null)Overwritten++;pending=f;received=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();if(telemetryEnabled&&f.source=="physical-webcam"){long delta=received-f.processTimestamp;if(delta<0||delta>10000)invalidTransit++;else{transitCount++;transitMax=Math.Max(transitMax,delta);transit.Enqueue(delta);if(transit.Count>4096)transit.Dequeue();}}}
        }
        async Task Drain(StreamReader reader){try{var buffer=new char[1024];int n;while((n=await reader.ReadAsync(buffer,0,buffer.Length))>0)StderrBytes+=n;}catch(ObjectDisposedException){}catch(IOException){}}
        public void Poll(){if(State=="RUNNING"&&(Error!=null||child.HasExited)){Error=Error??"VISION_EXITED";State="FAILED";}}
        public async Task StopAsync(){
            if(child==null){if(controlSnapshot!=null)await RestoreControls();CleanupComplete=ControlsRestored;State=Error==null?"STOPPED":"FAILED";return;}
            State="STOPPING";try{child.StandardInput.Close();}catch{}
            var clock=Stopwatch.StartNew();while(!child.HasExited&&clock.ElapsedMilliseconds<3000)await Task.Delay(10);
            if(!child.HasExited){Forced=true;child.Kill();while(!child.HasExited&&clock.ElapsedMilliseconds<5000)await Task.Delay(10);}
            CleanupComplete=child.HasExited;
            if(CleanupComplete){ExitCode=child.ExitCode;await Task.WhenAll(stdout??Task.CompletedTask,stderr??Task.CompletedTask);child.Dispose();child=null;lock(gate)pending=null;await RestoreControls();}
            State=Error==null&&CleanupComplete?"STOPPED":"FAILED";
        }
        async Task RestoreControls(){
            if(controlSnapshot==null)return;
            try{using(var helper=new Process{StartInfo=new ProcessStartInfo{FileName=controlPython,Arguments=Quote(controlScript)+" --restore-device "+Quote(controlDevice),UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true}}){
                helper.Start();var result=helper.StandardOutput.ReadToEndAsync();var errors=helper.StandardError.ReadToEndAsync();await helper.StandardInput.WriteLineAsync(controlSnapshot);helper.StandardInput.Close();
                var clock=Stopwatch.StartNew();while(!helper.HasExited&&clock.ElapsedMilliseconds<10000)await Task.Delay(10);
                if(!helper.HasExited){helper.Kill();throw new IOException("CAMERA_RESTORE_TIMEOUT");}
                ControlsRestored=helper.ExitCode==0&&(await result).Trim()=="CAMERA_RESTORED";await errors;
                if(!ControlsRestored)throw new IOException("CAMERA_RESTORE_HELPER_EXIT_"+helper.ExitCode);controlSnapshot=null;RestoreFailure=null;
            }}catch(Exception e){ControlsRestored=false;RestoreFailure=e.Message;Error="CAMERA_RESTORE_FAILED";CleanupComplete=false;}
        }
        public void Dispose(){if(child!=null&&!CleanupComplete)throw new InvalidOperationException("Await StopAsync before disposal");child?.Dispose();}
    }
}
