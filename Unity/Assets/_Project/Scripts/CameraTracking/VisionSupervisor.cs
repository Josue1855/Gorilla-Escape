using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GorillaEscape.Contracts;
using UnityEngine;
namespace GorillaEscape.CameraTracking
{
    /// <summary>Local opt-in Linux lab adapter. Private child pipes, latest-only delivery, no image transport.</summary>
    public sealed class VisionSupervisor : IDisposable {
        public const int MaxLine=16384;
        readonly object gate=new object(); CameraInputWire pending;Process child;
        Task stdout,stderr; bool ready; long received;
        public string State {get;private set;}="STOPPED";
        public string Error {get;private set;} public int Pid {get;private set;}
        public int? ExitCode {get;private set;} public bool CleanupComplete {get;private set;}=true;
        public bool Forced {get;private set;} public long Overwritten {get;private set;} public long StderrBytes {get;private set;}
        public string Summary {get;private set;}
        public bool Take(out CameraInputWire frame,out long timestamp){lock(gate){frame=pending;timestamp=received;pending=null;return frame!=null;}}
        static string Quote(string value){if(value==null||value.IndexOfAny(new[]{'"','\n','\r'})>=0)throw new ArgumentException("Invalid adapter path");return "\""+value+"\"";}
        public async Task StartAsync(string python,string script,string mode,string fixture,string model,string device,CancellationToken token){
            if(child!=null)throw new InvalidOperationException("Stop prior own camera child first");
            CleanupComplete=false;State="STARTING";Error=null;ready=false;Forced=false;ExitCode=null;Summary=null;
            try {
                child=new Process{StartInfo=new ProcessStartInfo{FileName=python,Arguments=Quote(script)+" --mode "+Quote(mode)+" --fixture "+Quote(fixture??"")+" --model "+Quote(model??"")+" --device "+Quote(device??"/dev/video0"),UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true}};
                child.Start();Pid=child.Id;stdout=ReadOutput(child.StandardOutput);stderr=Drain(child.StandardError);
                var clock=Stopwatch.StartNew();while(!ready){token.ThrowIfCancellationRequested();if(Error!=null||child.HasExited)throw new IOException(Error??"VISION_EXITED");if(clock.ElapsedMilliseconds>=10000)throw new TimeoutException("CAMERA_STARTUP_TIMEOUT");await Task.Delay(10,token);}
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
            if(line=="CAMERA_READY"){if(ready)throw new IOException("CAMERA_READY_DUPLICATE");ready=true;return;}
            if(line.StartsWith("CAMERA_SUMMARY ")){Summary=line.Substring(15);return;}
            if(!ready||!line.StartsWith("CAMERA_FRAME "))throw new IOException("CAMERA_PROTOCOL_INVALID");
            CameraInputWire f;try{f=JsonUtility.FromJson<CameraInputWire>(line.Substring(13));}catch{throw new IOException("CAMERA_PROTOCOL_INVALID");}
            if(!CameraInputStore.Valid(f))throw new IOException("CAMERA_CONTRACT_INVALID");
            lock(gate){if(pending!=null)Overwritten++;pending=f;received=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();}
        }
        async Task Drain(StreamReader reader){try{var buffer=new char[1024];int n;while((n=await reader.ReadAsync(buffer,0,buffer.Length))>0)StderrBytes+=n;}catch(ObjectDisposedException){}catch(IOException){}}
        public void Poll(){if(State=="RUNNING"&&(Error!=null||child.HasExited)){Error=Error??"VISION_EXITED";State="FAILED";}}
        public async Task StopAsync(){
            if(child==null){CleanupComplete=true;State=Error==null?"STOPPED":"FAILED";return;}
            State="STOPPING";try{child.StandardInput.Close();}catch{}
            var clock=Stopwatch.StartNew();while(!child.HasExited&&clock.ElapsedMilliseconds<3000)await Task.Delay(10);
            if(!child.HasExited){Forced=true;child.Kill();while(!child.HasExited&&clock.ElapsedMilliseconds<5000)await Task.Delay(10);}
            CleanupComplete=child.HasExited;
            if(CleanupComplete){ExitCode=child.ExitCode;await Task.WhenAll(stdout??Task.CompletedTask,stderr??Task.CompletedTask);child.Dispose();child=null;lock(gate)pending=null;}
            State=Error==null&&CleanupComplete?"STOPPED":"FAILED";
        }
        public void Dispose(){if(child!=null&&!CleanupComplete)throw new InvalidOperationException("Await StopAsync before disposal");child?.Dispose();}
    }
}
